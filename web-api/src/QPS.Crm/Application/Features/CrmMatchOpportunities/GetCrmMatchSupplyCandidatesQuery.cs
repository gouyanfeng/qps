using QPS.Shared.Common;
using QPS.Crm.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Crm.Contracts.DTOs.CrmMatchOpportunities;
using QPS.Shared.Extensions;
using QPS.Shared.Contracts;
using QPS.Crm.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Crm.Application.Features.CrmMatchOpportunities;

public class GetCrmMatchSupplyCandidatesQuery : PaginationRequest, IRequest<PaginationResponse<CrmMatchSupplyCandidateDto>>
{
    public Guid DemandItemId { get; set; }
    public Guid? ExcludeOpportunityId { get; set; }
    /// <summary>当前机会：被该机会占用的候选允许回显，其他机会占用的排除。</summary>
    public Guid? CurrentOpportunityId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public decimal? MinAvailableQuantity { get; set; }
    public string Specification { get; set; } = string.Empty;
    public string QualityRequirement { get; set; } = string.Empty;
    public string SupplyCycle { get; set; } = string.Empty;
    public decimal? MaxExpectedPrice { get; set; }
}

public class GetCrmMatchSupplyCandidatesHandler(ICrmDbContext dbContext) : IRequestHandler<GetCrmMatchSupplyCandidatesQuery, PaginationResponse<CrmMatchSupplyCandidateDto>>
{
    public async Task<PaginationResponse<CrmMatchSupplyCandidateDto>> Handle(GetCrmMatchSupplyCandidatesQuery request, CancellationToken cancellationToken)
    {
        await CrmMatchOpportunitySource.LoadDemandItemAsync(dbContext, request.DemandItemId, cancellationToken);
        var today = DateTime.Today;
        var supplies = dbContext.CrmHerbBaseSupplies
            .Where(item => item.Status == CrmHerbBaseSupply.Active)
            .Where(item => item.ValidUntil.HasValue && item.ValidUntil.Value.Date >= today)
            .Include(item => item.HerbBase)
            .AsQueryable();

        var occupiedSupplyIds = CrmMatchOpportunitySource.OccupiedEntityIds(dbContext, CrmMatchOpportunityItem.EntityTypeSupply);
        if (request.CurrentOpportunityId.HasValue)
        {
            var currentId = request.CurrentOpportunityId.Value;
            supplies = supplies.Where(item => !dbContext.CrmMatchOpportunityItems.Any(item2 =>
                item2.EntityType == CrmMatchOpportunityItem.EntityTypeSupply &&
                item2.EntityId == item.Id &&
                item2.OpportunityId != currentId &&
                item2.Status != CrmMatchOpportunityItem.Eliminated &&
                dbContext.CrmMatchOpportunities.Any(opportunity =>
                    opportunity.Id == item2.OpportunityId && opportunity.Status != CrmMatchOpportunity.Closed)));
        }
        else
        {
            supplies = supplies.Where(item => !occupiedSupplyIds.Contains(item.Id));
        }

        if (!string.IsNullOrWhiteSpace(request.ProductName))
        {
            var productName = request.ProductName.Trim();
            supplies = supplies.Where(item => item.ProductName == productName);
        }

        if (request.ExcludeOpportunityId.HasValue)
        {
            var opportunityId = request.ExcludeOpportunityId.Value;
            supplies = supplies.Where(supply => !dbContext.CrmMatchOpportunityItems
                .Any(item => item.OpportunityId == opportunityId &&
                    item.EntityType == CrmMatchOpportunityItem.EntityTypeSupply && item.EntityId == supply.Id));
        }

        if (!string.IsNullOrWhiteSpace(request.Region))
        {
            var region = request.Region.Trim();
            supplies = supplies.Where(item => item.HerbBase != null &&
                (item.HerbBase.Province.Contains(region) || item.HerbBase.City.Contains(region) || item.HerbBase.Area.Contains(region)));
        }

        if (request.MinAvailableQuantity.HasValue)
        {
            supplies = supplies.Where(item => item.AvailableQuantity >= request.MinAvailableQuantity);
        }

        if (!string.IsNullOrWhiteSpace(request.Specification))
        {
            var specification = request.Specification.Trim();
            supplies = supplies.Where(item => item.Specification.Contains(specification));
        }

        if (!string.IsNullOrWhiteSpace(request.QualityRequirement))
        {
            var qualityRequirement = request.QualityRequirement.Trim();
            supplies = supplies.Where(item => item.QualityRequirement.Contains(qualityRequirement));
        }

        if (!string.IsNullOrWhiteSpace(request.SupplyCycle))
        {
            var supplyCycle = request.SupplyCycle.Trim();
            supplies = supplies.Where(item => item.SupplyCycle.Contains(supplyCycle));
        }

        if (request.MaxExpectedPrice.HasValue)
        {
            supplies = supplies.Where(item => item.ExpectedPrice <= request.MaxExpectedPrice);
        }

        var dtoQuery = supplies.Select(item => new CrmMatchSupplyCandidateDto
        {
            HerbBaseSupplyId = item.Id,
            HerbBaseId = item.HerbBaseId,
            HerbBaseSubjectId = item.HerbBaseSubjectId,
            SubjectName = item.HerbBase != null ? item.HerbBase.SubjectName : string.Empty,
            HerbBaseName = item.HerbBase != null ? item.HerbBase.BaseName : string.Empty,
            Province = item.HerbBase != null ? item.HerbBase.Province : string.Empty,
            City = item.HerbBase != null ? item.HerbBase.City : string.Empty,
            Area = item.HerbBase != null ? item.HerbBase.Area : string.Empty,
            ProductName = item.ProductName,
            AvailableQuantity = item.AvailableQuantity,
            QuantityUnit = item.QuantityUnit,
            Specification = item.Specification,
            QualityRequirement = item.QualityRequirement,
            ExpectedPrice = item.ExpectedPrice,
            PriceUnit = item.PriceUnit,
            SupplyCycle = item.SupplyCycle,
            ValidUntil = item.ValidUntil,
            OccupancyStatus = occupiedSupplyIds.Contains(item.Id) ? "占用" : "空闲"
        });

        return await dtoQuery.ToPaginationResponseAsync(request);
    }
}

internal static class CrmMatchOpportunitySource
{
    public static async Task<CrmVendorDemandItem> LoadDemandItemAsync(ICrmDbContext dbContext, Guid demandItemId, CancellationToken cancellationToken)
    {
        var demandItem = await dbContext.CrmVendorDemandItems
            .Include(item => item.VendorDemand)
            .FirstOrDefaultAsync(item => item.Id == demandItemId, cancellationToken);
        if (demandItem?.VendorDemand == null)
        {
            throw new BusinessException(404, "采购列表明细不存在");
        }

        return demandItem;
    }

    public static async Task<List<CrmHerbBaseSupply>> LoadEligibleSuppliesAsync(
        ICrmDbContext dbContext,
        IReadOnlyCollection<Guid> supplyIds,
        CancellationToken cancellationToken)
    {
        var today = DateTime.Today;
        var supplies = await dbContext.CrmHerbBaseSupplies
            .Include(item => item.HerbBase)
            .Where(item => supplyIds.Contains(item.Id))
            .Where(item => item.Status == CrmHerbBaseSupply.Active)
            .Where(item => item.ValidUntil.HasValue && item.ValidUntil.Value.Date >= today)
            .ToListAsync(cancellationToken);

        if (supplies.Count != supplyIds.Count || supplies.Any(item => item.HerbBase == null))
        {
            throw new BusinessException(400, "供应信息不存在或不符合撮合候选条件");
        }

        return supplies;
    }

    /// <summary>未关闭撮合机会中仍在名单里的指定类型明细 ID；淘汰与已关闭均视为释放。</summary>
    public static IQueryable<Guid> OccupiedEntityIds(ICrmDbContext dbContext, string entityType) =>
        from item in dbContext.CrmMatchOpportunityItems
        join opportunity in dbContext.CrmMatchOpportunities on item.OpportunityId equals opportunity.Id
        where item.EntityType == entityType &&
              opportunity.Status != CrmMatchOpportunity.Closed &&
              item.Status != CrmMatchOpportunityItem.Eliminated
        select item.EntityId;

    /// <summary>未关闭撮合机会中仍在名单里的采购明细 ID（兼容旧调用）。</summary>
    public static IQueryable<Guid> OccupiedDemandItemIds(ICrmDbContext dbContext) =>
        OccupiedEntityIds(dbContext, CrmMatchOpportunityItem.EntityTypeDemand);

    /// <summary>已成交关闭机会中的供应 ID：中选候选，或供应侧发起机会的发起供应。</summary>
    public static IQueryable<Guid> MatchedSupplyIds(ICrmDbContext dbContext) =>
        from item in dbContext.CrmMatchOpportunityItems
        join opportunity in dbContext.CrmMatchOpportunities on item.OpportunityId equals opportunity.Id
        where item.EntityType == CrmMatchOpportunityItem.EntityTypeSupply &&
              opportunity.Status == CrmMatchOpportunity.Closed &&
              opportunity.CloseResult == CrmMatchOpportunity.Matched &&
              (item.Role == CrmMatchOpportunityItem.Initiator || item.Status == CrmMatchOpportunityItem.Selected)
        select item.EntityId;

    /// <summary>已成交关闭机会中的采购明细 ID：需求侧发起明细，或供应侧中选候选需求。</summary>
    public static IQueryable<Guid> MatchedDemandItemIds(ICrmDbContext dbContext) =>
        from item in dbContext.CrmMatchOpportunityItems
        join opportunity in dbContext.CrmMatchOpportunities on item.OpportunityId equals opportunity.Id
        where item.EntityType == CrmMatchOpportunityItem.EntityTypeDemand &&
              opportunity.Status == CrmMatchOpportunity.Closed &&
              opportunity.CloseResult == CrmMatchOpportunity.Matched &&
              (item.Role == CrmMatchOpportunityItem.Initiator || item.Status == CrmMatchOpportunityItem.Selected)
        select item.EntityId;

    /// <summary>采购明细分别被哪些未关闭机会占用，供候选列表判断是否为「当前机会自己」。</summary>
    public static async Task<Dictionary<Guid, List<Guid>>> LoadDemandOccupancyAsync(
        ICrmDbContext dbContext, IReadOnlyCollection<Guid> demandItemIds, CancellationToken cancellationToken)
    {
        var ids = demandItemIds.Distinct().ToList();
        if (ids.Count == 0) return new();
        var rows = await (
            from item in dbContext.CrmMatchOpportunityItems
            join opportunity in dbContext.CrmMatchOpportunities on item.OpportunityId equals opportunity.Id
            where item.EntityType == CrmMatchOpportunityItem.EntityTypeDemand &&
                  ids.Contains(item.EntityId) &&
                  item.Status != CrmMatchOpportunityItem.Eliminated &&
                  opportunity.Status != CrmMatchOpportunity.Closed
            select new { DemandItemId = item.EntityId, OpportunityId = opportunity.Id })
            .Distinct()
            .ToListAsync(cancellationToken);
        return rows.GroupBy(row => row.DemandItemId)
            .ToDictionary(group => group.Key, group => group.Select(row => row.OpportunityId).ToList());
    }

    /// <summary>写入前防止同一资源挂在多个未关闭机会上；allowedOpportunityId 用于重选时放行自己。</summary>
    public static async Task EnsureSuppliesNotOccupiedElsewhereAsync(
        ICrmDbContext dbContext, IReadOnlyCollection<Guid> supplyIds, Guid? allowedOpportunityId, CancellationToken cancellationToken)
    {
        var ids = supplyIds.Distinct().ToList();
        if (ids.Count == 0) return;
        var conflict = await dbContext.CrmMatchOpportunityItems.AnyAsync(item =>
            item.EntityType == CrmMatchOpportunityItem.EntityTypeSupply &&
            ids.Contains(item.EntityId) &&
            item.Status != CrmMatchOpportunityItem.Eliminated &&
            (!allowedOpportunityId.HasValue || item.OpportunityId != allowedOpportunityId.Value) &&
            dbContext.CrmMatchOpportunities.Any(opportunity =>
                opportunity.Id == item.OpportunityId && opportunity.Status != CrmMatchOpportunity.Closed),
            cancellationToken);
        if (conflict) throw new BusinessException(400, "所选供应信息已被其他撮合机会占用");
    }

    public static async Task EnsureDemandItemsNotOccupiedElsewhereAsync(
        ICrmDbContext dbContext, IReadOnlyCollection<Guid> demandItemIds, Guid? allowedOpportunityId, CancellationToken cancellationToken)
    {
        var ids = demandItemIds.Distinct().ToList();
        if (ids.Count == 0) return;
        var conflict = await dbContext.CrmMatchOpportunityItems.AnyAsync(item =>
            item.EntityType == CrmMatchOpportunityItem.EntityTypeDemand &&
            ids.Contains(item.EntityId) &&
            item.Status != CrmMatchOpportunityItem.Eliminated &&
            (!allowedOpportunityId.HasValue || item.OpportunityId != allowedOpportunityId.Value) &&
            dbContext.CrmMatchOpportunities.Any(opportunity =>
                opportunity.Id == item.OpportunityId && opportunity.Status != CrmMatchOpportunity.Closed),
            cancellationToken);
        if (conflict) throw new BusinessException(400, "所选采购需求已被其他撮合机会占用");
    }

    public static void ValidateSupplyIds(IReadOnlyCollection<Guid>? supplyIds)
    {
        if (supplyIds == null || supplyIds.Count == 0)
        {
            throw new BusinessException(400, "请至少选择一条候选供应信息");
        }

        if (supplyIds.Distinct().Count() != supplyIds.Count)
        {
            throw new BusinessException(400, "候选供应信息存在重复");
        }
    }

    public static void ValidateDemandItemIds(IReadOnlyCollection<Guid>? demandItemIds)
    {
        if (demandItemIds == null || demandItemIds.Count == 0)
        {
            throw new BusinessException(400, "请至少选择一条候选采购需求");
        }

        if (demandItemIds.Distinct().Count() != demandItemIds.Count)
        {
            throw new BusinessException(400, "候选采购需求存在重复");
        }
    }

    public static async Task<List<CrmVendorDemandItem>> LoadEligibleDemandItemsAsync(
        ICrmDbContext dbContext,
        IReadOnlyCollection<Guid> demandItemIds,
        CancellationToken cancellationToken)
    {
        var items = await dbContext.CrmVendorDemandItems
            .Include(item => item.VendorDemand)
            .Where(item => demandItemIds.Contains(item.Id))
            .ToListAsync(cancellationToken);
        if (items.Count != demandItemIds.Count || items.Any(item => item.VendorDemand == null))
        {
            throw new BusinessException(400, "采购需求不存在或不符合撮合候选条件");
        }

        var vendorIds = items.Select(item => item.VendorDemand!.VendorId).Distinct().ToList();
        var vendors = await dbContext.CrmVendors
            .Where(item => vendorIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);

        foreach (var item in items)
        {
            if (!vendors.ContainsKey(item.VendorDemand!.VendorId))
            {
                throw new BusinessException(400, "采购需求对应的需求厂商不存在");
            }
        }

        return items;
    }

    public static CrmMatchOpportunityItem CreateDemandItem(CrmMatchOpportunity opportunity, Guid demandItemId, string role, int sortOrder) =>
        CrmMatchOpportunityItem.Create(opportunity.Id, CrmMatchOpportunityItem.EntityTypeDemand, demandItemId, role, sortOrder);

    public static CrmMatchOpportunityItem CreateSupplyItem(CrmMatchOpportunity opportunity, Guid supplyId, string role, int sortOrder) =>
        CrmMatchOpportunityItem.Create(opportunity.Id, CrmMatchOpportunityItem.EntityTypeSupply, supplyId, role, sortOrder);

    public static void ValidateNextAction(string? nextAction, DateTime? nextActionAt)
    {
        if (string.IsNullOrWhiteSpace(nextAction) || !nextActionAt.HasValue)
        {
            throw new BusinessException(400, "下一步动作和计划时间必须同时填写");
        }
    }

    /// <summary>成交冻结（批量）：任一明细被成交机会引用即抛异常。</summary>
    public static async Task EnsureEntitiesNotLockedByDealAsync(
        ICrmDbContext dbContext,
        string entityType,
        IReadOnlyCollection<Guid> entityIds,
        CancellationToken cancellationToken)
    {
        if (entityIds.Count == 0) return;
        var ids = entityIds.Distinct().ToList();
        var locked = await dbContext.CrmMatchOpportunityItems.AnyAsync(item =>
            item.EntityType == entityType &&
            ids.Contains(item.EntityId) &&
            (item.Role == CrmMatchOpportunityItem.Initiator || item.Status == CrmMatchOpportunityItem.Selected) &&
            dbContext.CrmMatchOpportunities.Any(opportunity =>
                opportunity.Id == item.OpportunityId &&
                opportunity.Status == CrmMatchOpportunity.Closed &&
                opportunity.CloseResult == CrmMatchOpportunity.Matched),
            cancellationToken);
        if (locked) throw new BusinessException(400, "存在已成交锁定的明细，不可修改或删除");
    }

    /// <summary>成交冻结：明细一旦被「已关闭且撮合成功」机会的中选/发起记录引用，禁止修改或删除。</summary>
    public static async Task EnsureEntityNotLockedByDealAsync(
        ICrmDbContext dbContext,
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken)
    {
        var locked = await dbContext.CrmMatchOpportunityItems.AnyAsync(item =>
            item.EntityType == entityType &&
            item.EntityId == entityId &&
            (item.Role == CrmMatchOpportunityItem.Initiator || item.Status == CrmMatchOpportunityItem.Selected) &&
            dbContext.CrmMatchOpportunities.Any(opportunity =>
                opportunity.Id == item.OpportunityId &&
                opportunity.Status == CrmMatchOpportunity.Closed &&
                opportunity.CloseResult == CrmMatchOpportunity.Matched),
            cancellationToken);
        if (locked) throw new BusinessException(400, "该明细已成交锁定，不可修改或删除");
    }

    public static async Task EnsureSupplyNotSelectedElsewhereAsync(
        ICrmDbContext dbContext,
        Guid supplyId,
        Guid opportunityId,
        CancellationToken cancellationToken)
    {
        var isSelectedElsewhere = await dbContext.CrmMatchOpportunityItems.AnyAsync(item =>
            item.EntityType == CrmMatchOpportunityItem.EntityTypeSupply &&
            item.EntityId == supplyId &&
            item.OpportunityId != opportunityId &&
            item.Status == CrmMatchOpportunityItem.Selected &&
            dbContext.CrmMatchOpportunities.Any(opportunity =>
                opportunity.Id == item.OpportunityId &&
                opportunity.Status != CrmMatchOpportunity.Closed),
            cancellationToken);

        if (isSelectedElsewhere)
        {
            throw new BusinessException(400, "供应信息已在其他撮合机会中中选");
        }
    }

    public static string DescribeNextAction(string nextAction, string nextActionRemark) =>
        nextAction == CrmMatchOpportunityNextAction.Other
            ? $"{nextAction}（{nextActionRemark}）"
            : nextAction;

    public static CrmBusinessActivity CreateActivity(Guid opportunityId, string actionType, string content) =>
        CrmBusinessActivity.Create("MATCH_OPPORTUNITY", opportunityId, actionType, content);
}

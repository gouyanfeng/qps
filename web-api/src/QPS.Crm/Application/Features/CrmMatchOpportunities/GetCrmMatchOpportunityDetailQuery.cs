using QPS.Crm.Contracts.Permissions;
using QPS.Crm.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Shared.Contracts;
using QPS.Crm.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Crm.Application.Features.CrmMatchOpportunities;

[Permission(CrmPermissionCodes.CRM_MATCH_OPPORTUNITY_VIEW)]
public class GetCrmMatchOpportunityDetailQuery : IRequest<CrmMatchOpportunityDetailDto>
{
    public Guid Id { get; set; }
}

[Permission(CrmPermissionCodes.CRM_MATCH_OPPORTUNITY_VIEW)]
public class GetCrmMatchOpportunityActivitiesQuery : IRequest<List<CrmMatchOpportunityActivityDto>>
{
    public Guid OpportunityId { get; set; }
}

public class CrmMatchOpportunityDetailDto : CrmMatchOpportunityListDto
{
    public List<CrmMatchOpportunityItemDto> Items { get; set; } = [];
    public List<CrmMatchOpportunityActivityDto> Activities { get; set; } = [];
}

/// <summary>统一的机会明细 DTO：供应侧与需求侧字段合并，业务字段实时取原明细当前值。</summary>
public class CrmMatchOpportunityItemDto
{
    public Guid Id { get; set; }
    /// <summary>supply=供应明细，demand=采购明细。</summary>
    public string ItemType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string EliminationReason { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    // 采购明细字段（ItemType=demand 时有效）
    public Guid? DemandItemId { get; set; }
    public Guid VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal? Quantity { get; set; }
    public string QuantityUnit { get; set; } = string.Empty;
    public string Specification { get; set; } = string.Empty;
    public string QualityRequirement { get; set; } = string.Empty;
    public decimal? TargetPrice { get; set; }
    public string PriceUnit { get; set; } = string.Empty;
    public DateTime? ExpectedDeliveryAt { get; set; }
    public string DeliveryAddress { get; set; } = string.Empty;

    // 供应明细字段（ItemType=supply 时有效）
    public Guid HerbBaseSupplyId { get; set; }
    public Guid HerbBaseId { get; set; }
    public Guid? HerbBaseSubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string HerbBaseName { get; set; } = string.Empty;
    public decimal? AvailableQuantity { get; set; }
    public decimal? ExpectedPrice { get; set; }
    public string SupplyCycle { get; set; } = string.Empty;
    public DateTime? ValidUntil { get; set; }
}

public class CrmMatchOpportunityActivityDto
{
    public Guid Id { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class GetCrmMatchOpportunityDetailHandler(ICrmDbContext dbContext)
    : IRequestHandler<GetCrmMatchOpportunityDetailQuery, CrmMatchOpportunityDetailDto>
{
    public async Task<CrmMatchOpportunityDetailDto> Handle(
        GetCrmMatchOpportunityDetailQuery request,
        CancellationToken cancellationToken)
    {
        var detail = await GetDetailAsync(request.Id, cancellationToken);
        await FillOwnerNameAsync(detail, cancellationToken);
        detail.Items = await GetItemsAsync(request.Id, cancellationToken);
        detail.Activities = await CrmMatchOpportunityActivityQuery.GetAsync(dbContext, request.Id, cancellationToken);
        return detail;
    }

    private async Task FillOwnerNameAsync(CrmMatchOpportunityDetailDto detail, CancellationToken cancellationToken)
    {
        if (!detail.OwnerUserId.HasValue) return;
        var users = await CrmUserNames.GetAsync(dbContext, [detail.OwnerUserId.Value], cancellationToken);
        if (users.TryGetValue(detail.OwnerUserId.Value, out var owner))
        {
            detail.OwnerUserName = CrmUserNames.DisplayName(owner.Username, owner.RealName);
        }
    }

    private async Task<CrmMatchOpportunityDetailDto> GetDetailAsync(Guid opportunityId, CancellationToken cancellationToken)
    {
        var detail = await (
            from opportunity in dbContext.CrmMatchOpportunities.AsNoTracking()
            where opportunity.Id == opportunityId
            select new CrmMatchOpportunityDetailDto
            {
                Id = opportunity.Id,
                OwnerUserId = opportunity.OwnerUserId,
                Status = opportunity.Status,
                Stage = opportunity.Stage,
                NextAction = opportunity.NextAction,
                NextActionRemark = opportunity.NextActionRemark,
                NextActionAt = opportunity.NextActionAt,
                CloseResult = opportunity.CloseResult,
                ClosedReason = opportunity.ClosedReason,
                Source = opportunity.Source,
                CreatedAt = opportunity.CreatedAt,
                UpdatedAt = opportunity.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return detail ?? throw new BusinessException(404, "撮合机会不存在");
    }

    /// <summary>
    /// 明细实时 join 原明细：供应侧取 CrmHerbBaseSupplies + CrmHerbBases，
    /// 需求侧取 CrmVendorDemandItems + CrmVendorDemands + CrmVendors。
    /// </summary>
    private async Task<List<CrmMatchOpportunityItemDto>> GetItemsAsync(Guid opportunityId, CancellationToken cancellationToken)
    {
        var items = await dbContext.CrmMatchOpportunityItems
            .AsNoTracking()
            .Where(item => item.OpportunityId == opportunityId)
            .OrderBy(item => item.SortOrder)
            .ToListAsync(cancellationToken);

        var result = new List<CrmMatchOpportunityItemDto>();
        var supplyIds = items.Where(x => x.EntityType == CrmMatchOpportunityItem.EntityTypeSupply).Select(x => x.EntityId).Distinct().ToList();
        var demandIds = items.Where(x => x.EntityType == CrmMatchOpportunityItem.EntityTypeDemand).Select(x => x.EntityId).Distinct().ToList();

        var supplies = await (
                from supply in dbContext.CrmHerbBaseSupplies.AsNoTracking().IgnoreQueryFilters()
                join herbBase in dbContext.CrmHerbBases.AsNoTracking().IgnoreQueryFilters()
                    on supply.HerbBaseId equals herbBase.Id into herbBaseGroup
                from herbBase in herbBaseGroup.DefaultIfEmpty()
                where supplyIds.Contains(supply.Id)
                select new
                {
                    supply.Id,
                    supply.HerbBaseId,
                    supply.HerbBaseSubjectId,
                    supply.ProductName,
                    supply.AvailableQuantity,
                    supply.QuantityUnit,
                    supply.Specification,
                    supply.QualityRequirement,
                    supply.ExpectedPrice,
                    supply.PriceUnit,
                    supply.SupplyCycle,
                    supply.ValidUntil,
                    SubjectName = herbBase == null ? string.Empty : herbBase.SubjectName,
                    HerbBaseName = herbBase == null ? string.Empty : herbBase.BaseName
                })
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var demandRows = await (
                from item in dbContext.CrmVendorDemandItems.AsNoTracking()
                join demand in dbContext.CrmVendorDemands.AsNoTracking().IgnoreQueryFilters() on item.VendorDemandId equals demand.Id
                join vendor in dbContext.CrmVendors.AsNoTracking().IgnoreQueryFilters() on demand.VendorId equals vendor.Id into vendorGroup
                from vendor in vendorGroup.DefaultIfEmpty()
                where demandIds.Contains(item.Id)
                select new
                {
                    item.Id,
                    VendorId = demand.VendorId,
                    VendorName = vendor == null ? string.Empty : vendor.VendorName,
                    item.ProductName,
                    item.Quantity,
                    item.QuantityUnit,
                    item.Specification,
                    item.QualityRequirement,
                    item.TargetPrice,
                    item.PriceUnit,
                    item.ExpectedDeliveryAt,
                    DeliveryAddress = demand.ReceivingAddress
                })
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        foreach (var item in items)
        {
            var dto = new CrmMatchOpportunityItemDto
            {
                Id = item.Id,
                ItemType = item.EntityType,
                EntityId = item.EntityId,
                Role = item.Role,
                Status = item.Status,
                EliminationReason = item.EliminationReason,
                SortOrder = item.SortOrder
            };
            if (item.EntityType == CrmMatchOpportunityItem.EntityTypeSupply)
            {
                if (supplies.TryGetValue(item.EntityId, out var supply))
                {
                    dto.HerbBaseSupplyId = supply.Id;
                    dto.HerbBaseId = supply.HerbBaseId;
                    dto.HerbBaseSubjectId = supply.HerbBaseSubjectId;
                    dto.SubjectName = supply.SubjectName;
                    dto.HerbBaseName = supply.HerbBaseName;
                    dto.ProductName = supply.ProductName;
                    dto.AvailableQuantity = supply.AvailableQuantity;
                    dto.QuantityUnit = supply.QuantityUnit;
                    dto.Specification = supply.Specification;
                    dto.QualityRequirement = supply.QualityRequirement;
                    dto.ExpectedPrice = supply.ExpectedPrice;
                    dto.PriceUnit = supply.PriceUnit;
                    dto.SupplyCycle = supply.SupplyCycle;
                    dto.ValidUntil = supply.ValidUntil;
                }
            }
            else
            {
                dto.DemandItemId = item.EntityId;
                if (demandRows.TryGetValue(item.EntityId, out var demand))
                {
                    dto.VendorId = demand.VendorId;
                    dto.VendorName = demand.VendorName;
                    dto.ProductName = demand.ProductName;
                    dto.Quantity = demand.Quantity;
                    dto.QuantityUnit = demand.QuantityUnit;
                    dto.Specification = demand.Specification;
                    dto.QualityRequirement = demand.QualityRequirement;
                    dto.TargetPrice = demand.TargetPrice;
                    dto.PriceUnit = demand.PriceUnit;
                    dto.ExpectedDeliveryAt = demand.ExpectedDeliveryAt;
                    dto.DeliveryAddress = demand.DeliveryAddress;
                }
            }
            result.Add(dto);
        }

        return result;
    }
}

public class GetCrmMatchOpportunityActivitiesHandler(ICrmDbContext dbContext)
    : IRequestHandler<GetCrmMatchOpportunityActivitiesQuery, List<CrmMatchOpportunityActivityDto>>
{
    public async Task<List<CrmMatchOpportunityActivityDto>> Handle(
        GetCrmMatchOpportunityActivitiesQuery request,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.CrmMatchOpportunities.AnyAsync(item => item.Id == request.OpportunityId, cancellationToken))
        {
            throw new BusinessException(404, "撮合机会不存在");
        }
        return await CrmMatchOpportunityActivityQuery.GetAsync(dbContext, request.OpportunityId, cancellationToken);
    }
}

internal static class CrmMatchOpportunityActivityQuery
{
    public static async Task<List<CrmMatchOpportunityActivityDto>> GetAsync(
        ICrmDbContext dbContext,
        Guid opportunityId,
        CancellationToken cancellationToken)
    {
        var activities = await dbContext.CrmBusinessActivities
            .AsNoTracking()
            .Where(activity => activity.EntityType == "MATCH_OPPORTUNITY" && activity.EntityId == opportunityId)
            .OrderBy(activity => activity.CreatedAt)
            .Select(activity => new CrmMatchOpportunityActivityDto
            {
                Id = activity.Id,
                ActionType = activity.ActionType,
                Content = activity.Content,
                CreatedBy = activity.CreatedBy,
                CreatedAt = activity.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var userIds = activities
            .Select(activity => Guid.TryParse(activity.CreatedBy, out var userId) ? (Guid?)userId : null)
            .Where(userId => userId.HasValue)
            .Select(userId => userId!.Value)
            .Distinct()
            .ToList();
        if (userIds.Count == 0)
        {
            return activities;
        }

        var users = await CrmUserNames.GetAsync(dbContext, userIds, cancellationToken);
        foreach (var activity in activities)
        {
            if (Guid.TryParse(activity.CreatedBy, out var userId) &&
                users.TryGetValue(userId, out var user))
            {
                activity.CreatedBy = CrmUserNames.DisplayName(user.Username, user.RealName);
            }
        }
        return activities;
    }
}

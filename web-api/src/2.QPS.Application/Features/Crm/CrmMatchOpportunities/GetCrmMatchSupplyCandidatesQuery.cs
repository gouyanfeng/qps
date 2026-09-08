using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Contracts.Crm.CrmMatchOpportunities;
using QPS.Application.Extensions;
using QPS.Application.Interfaces;
using QPS.Domain.Entities.Crm;
using QPS.Domain.Exceptions;

namespace QPS.Application.Features.Crm.CrmMatchOpportunities;

public class GetCrmMatchSupplyCandidatesQuery : PaginationRequest, IRequest<PaginationResponse<CrmMatchSupplyCandidateDto>>
{
    public Guid DemandItemId { get; set; }
    public Guid? ExcludeOpportunityId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public decimal? MinAvailableQuantity { get; set; }
    public string Specification { get; set; } = string.Empty;
    public string QualityRequirement { get; set; } = string.Empty;
    public string SupplyCycle { get; set; } = string.Empty;
    public decimal? MaxExpectedPrice { get; set; }
}

public class GetCrmMatchSupplyCandidatesHandler(IDbContext dbContext) : IRequestHandler<GetCrmMatchSupplyCandidatesQuery, PaginationResponse<CrmMatchSupplyCandidateDto>>
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

        if (!string.IsNullOrWhiteSpace(request.ProductName))
        {
            var productName = request.ProductName.Trim();
            supplies = supplies.Where(item => item.ProductName == productName);
        }

        if (request.ExcludeOpportunityId.HasValue)
        {
            var opportunityId = request.ExcludeOpportunityId.Value;
            supplies = supplies.Where(supply => !dbContext.CrmMatchCandidates
                .Any(candidate => candidate.MatchOpportunityId == opportunityId && candidate.HerbBaseSupplyId == supply.Id));
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
            ValidUntil = item.ValidUntil
        });

        return await dtoQuery.ToPaginationResponseAsync(request);
    }
}

internal static class CrmMatchOpportunitySource
{
    public static async Task<CrmVendorDemandItem> LoadDemandItemAsync(IDbContext dbContext, Guid demandItemId, CancellationToken cancellationToken)
    {
        var demandItem = await dbContext.CrmVendorDemandItems
            .Include(item => item.VendorDemand)
            .FirstOrDefaultAsync(item => item.Id == demandItemId && !item.IsDeleted, cancellationToken);
        if (demandItem?.VendorDemand == null || demandItem.VendorDemand.IsDeleted)
        {
            throw new BusinessException(404, "采购需求明细不存在");
        }

        if (demandItem.VendorDemand.Status != CrmVendorDemand.Active)
        {
            throw new BusinessException(400, "采购需求状态必须为有效");
        }

        return demandItem;
    }

    public static async Task<List<CrmHerbBaseSupply>> LoadEligibleSuppliesAsync(
        IDbContext dbContext,
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

    public static void ValidateNextAction(string? nextAction, DateTime? nextActionAt)
    {
        if (string.IsNullOrWhiteSpace(nextAction) || !nextActionAt.HasValue)
        {
            throw new BusinessException(400, "下一步动作和计划时间必须同时填写");
        }
    }

    public static string DescribeNextAction(string nextAction, string nextActionRemark) =>
        nextAction == CrmMatchOpportunityNextAction.Other
            ? $"{nextAction}（{nextActionRemark}）"
            : nextAction;

    public static CrmMatchCandidate CreateCandidate(CrmMatchOpportunity opportunity, CrmHerbBaseSupply supply, int sortOrder)
    {
        var herbBase = supply.HerbBase!;
        return CrmMatchCandidate.Create(
            opportunity.Id,
            supply.Id,
            supply.HerbBaseId,
            supply.HerbBaseSubjectId,
            herbBase.SubjectName,
            herbBase.BaseName,
            supply.ProductName,
            supply.AvailableQuantity,
            supply.QuantityUnit,
            supply.Specification,
            supply.QualityRequirement,
            supply.ExpectedPrice,
            supply.PriceUnit,
            supply.SupplyCycle,
            supply.ValidUntil,
            sortOrder);
    }

    public static CrmBusinessActivity CreateActivity(Guid opportunityId, string actionType, string content) =>
        CrmBusinessActivity.Create("MATCH_OPPORTUNITY", opportunityId, actionType, content);
}

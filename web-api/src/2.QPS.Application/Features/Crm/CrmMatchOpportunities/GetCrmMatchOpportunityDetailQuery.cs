using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Interfaces;
using QPS.Domain.Exceptions;

namespace QPS.Application.Features.Crm.CrmMatchOpportunities;

public class GetCrmMatchOpportunityDetailQuery : IRequest<CrmMatchOpportunityDetailDto>
{
    public Guid Id { get; set; }
}

public class GetCrmMatchOpportunityActivitiesQuery : IRequest<List<CrmMatchOpportunityActivityDto>>
{
    public Guid OpportunityId { get; set; }
}

public class CrmMatchOpportunityDetailDto : CrmMatchOpportunityListDto
{
    public List<CrmMatchOpportunityCandidateDto> Candidates { get; set; } = [];
    public List<CrmMatchOpportunityActivityDto> Activities { get; set; } = [];
}

public class CrmMatchOpportunityCandidateDto
{
    public Guid Id { get; set; }
    public Guid HerbBaseSupplyId { get; set; }
    public Guid HerbBaseId { get; set; }
    public Guid? HerbBaseSubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string HerbBaseName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal? AvailableQuantity { get; set; }
    public string QuantityUnit { get; set; } = string.Empty;
    public string Specification { get; set; } = string.Empty;
    public string QualityRequirement { get; set; } = string.Empty;
    public decimal? ExpectedPrice { get; set; }
    public string PriceUnit { get; set; } = string.Empty;
    public string SupplyCycle { get; set; } = string.Empty;
    public DateTime? ValidUntil { get; set; }
    public string Status { get; set; } = string.Empty;
    public string EliminationReason { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class CrmMatchOpportunityActivityDto
{
    public Guid Id { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class GetCrmMatchOpportunityDetailHandler(IDbContext dbContext, ICurrentUserService currentUserService)
    : IRequestHandler<GetCrmMatchOpportunityDetailQuery, CrmMatchOpportunityDetailDto>
{
    /// <summary>
    /// 编排撮合机会详情及其候选、活动查询。
    /// </summary>
    public async Task<CrmMatchOpportunityDetailDto> Handle(
        GetCrmMatchOpportunityDetailQuery request,
        CancellationToken cancellationToken)
    {
        await CrmMatchOpportunityAuthorization.EnsureViewAsync(dbContext, currentUserService, cancellationToken);
        var detail = await GetDetailAsync(request.Id, cancellationToken);
        detail.Candidates = await GetCandidatesAsync(request.Id, cancellationToken);
        detail.Activities = await CrmMatchOpportunityActivityQuery.GetAsync(dbContext, request.Id, cancellationToken);
        return detail;
    }

    private async Task<CrmMatchOpportunityDetailDto> GetDetailAsync(Guid opportunityId, CancellationToken cancellationToken)
    {
        var detail = await (
            from opportunity in dbContext.CrmMatchOpportunities.AsNoTracking()
            join vendor in dbContext.CrmVendors on opportunity.VendorId equals vendor.Id into vendorGroup
            from vendor in vendorGroup.DefaultIfEmpty()
            join owner in dbContext.SystemUsers on opportunity.OwnerUserId equals owner.Id into ownerGroup
            from owner in ownerGroup.DefaultIfEmpty()
            where opportunity.Id == opportunityId
            select new CrmMatchOpportunityDetailDto
            {
                Id = opportunity.Id,
                DemandItemId = opportunity.DemandItemId,
                DemandId = opportunity.DemandId,
                VendorId = opportunity.VendorId,
                VendorName = vendor == null ? string.Empty : vendor.VendorName,
                DemandProductName = opportunity.DemandProductName,
                DemandQuantity = opportunity.DemandQuantity,
                QuantityUnit = opportunity.QuantityUnit,
                Specification = opportunity.Specification,
                QualityRequirement = opportunity.QualityRequirement,
                ExpectedDeliveryAt = opportunity.ExpectedDeliveryAt,
                DeliveryAddress = opportunity.DeliveryAddress,
                OwnerUserId = opportunity.OwnerUserId,
                OwnerUserName = owner == null
                    ? string.Empty
                    : !string.IsNullOrWhiteSpace(owner.RealName) ? owner.RealName : owner.Username,
                Status = opportunity.Status,
                NextAction = opportunity.NextAction,
                NextActionAt = opportunity.NextActionAt,
                ClosedReason = opportunity.ClosedReason,
                CreatedAt = opportunity.CreatedAt,
                UpdatedAt = opportunity.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return detail ?? throw new BusinessException(404, "撮合机会不存在");
    }

    private async Task<List<CrmMatchOpportunityCandidateDto>> GetCandidatesAsync(Guid opportunityId, CancellationToken cancellationToken)
    {
        return await dbContext.CrmMatchCandidates
            .AsNoTracking()
            .Where(item => item.MatchOpportunityId == opportunityId)
            .OrderBy(item => item.SortOrder)
            .Select(item => new CrmMatchOpportunityCandidateDto
            {
                Id = item.Id,
                HerbBaseSupplyId = item.HerbBaseSupplyId,
                HerbBaseId = item.HerbBaseId,
                HerbBaseSubjectId = item.HerbBaseSubjectId,
                SubjectName = item.SubjectName,
                HerbBaseName = item.HerbBaseName,
                ProductName = item.ProductName,
                AvailableQuantity = item.AvailableQuantity,
                QuantityUnit = item.QuantityUnit,
                Specification = item.Specification,
                QualityRequirement = item.QualityRequirement,
                ExpectedPrice = item.ExpectedPrice,
                PriceUnit = item.PriceUnit,
                SupplyCycle = item.SupplyCycle,
                ValidUntil = item.ValidUntil,
                Status = item.Status,
                EliminationReason = item.EliminationReason,
                SortOrder = item.SortOrder
            })
            .ToListAsync(cancellationToken);
    }
}

public class GetCrmMatchOpportunityActivitiesHandler(IDbContext dbContext, ICurrentUserService currentUserService)
    : IRequestHandler<GetCrmMatchOpportunityActivitiesQuery, List<CrmMatchOpportunityActivityDto>>
{
    /// <summary>
    /// 查询一个撮合机会的活动时间线。
    /// </summary>
    public async Task<List<CrmMatchOpportunityActivityDto>> Handle(
        GetCrmMatchOpportunityActivitiesQuery request,
        CancellationToken cancellationToken)
    {
        await CrmMatchOpportunityAuthorization.EnsureViewAsync(dbContext, currentUserService, cancellationToken);
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
        IDbContext dbContext,
        Guid opportunityId,
        CancellationToken cancellationToken)
    {
        return await (
            from activity in dbContext.CrmBusinessActivities.AsNoTracking()
            join user in dbContext.SystemUsers on activity.CreatedBy equals user.Id.ToString() into userGroup
            from user in userGroup.DefaultIfEmpty()
            where activity.EntityType == "MATCH_OPPORTUNITY" && activity.EntityId == opportunityId
            orderby activity.CreatedAt
            select new CrmMatchOpportunityActivityDto
            {
                Id = activity.Id,
                ActionType = activity.ActionType,
                Content = activity.Content,
                CreatedBy = user == null
                    ? activity.CreatedBy
                    : !string.IsNullOrWhiteSpace(user.RealName) ? user.RealName : user.Username,
                CreatedAt = activity.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }
}

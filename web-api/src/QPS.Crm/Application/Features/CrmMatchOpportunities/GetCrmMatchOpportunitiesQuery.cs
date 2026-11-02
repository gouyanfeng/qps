using QPS.Crm.Contracts.Permissions;
using QPS.Shared.Common;
using QPS.Crm.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Shared.Contracts;
using QPS.Shared.Extensions;
using QPS.Crm.Domain.Entities;

namespace QPS.Crm.Application.Features.CrmMatchOpportunities;

[Permission(CrmPermissionCodes.CRM_MATCH_OPPORTUNITY_VIEW)]
public class GetCrmMatchOpportunitiesQuery : PaginationRequest, IRequest<PaginationResponse<CrmMatchOpportunityListDto>>
{
    public Guid? DemandItemId { get; set; }
    public string? Status { get; set; }
    public string? ProductName { get; set; }
    public Guid? VendorId { get; set; }
    public Guid? HerbBaseSubjectId { get; set; }
    public Guid? OwnerUserId { get; set; }
    public DateTime? NextActionAtFrom { get; set; }
    public DateTime? NextActionAtTo { get; set; }
    public string? Keyword { get; set; }
}

public class CrmMatchOpportunityListDto
{
    public Guid Id { get; set; }
    public Guid DemandItemId { get; set; }
    public Guid DemandId { get; set; }
    public Guid VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string DemandProductName { get; set; } = string.Empty;
    public decimal? DemandQuantity { get; set; }
    public string QuantityUnit { get; set; } = string.Empty;
    public string Specification { get; set; } = string.Empty;
    public string QualityRequirement { get; set; } = string.Empty;
    public DateTime? ExpectedDeliveryAt { get; set; }
    public string DeliveryAddress { get; set; } = string.Empty;
    public Guid? OwnerUserId { get; set; }
    public string OwnerUserName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Stage { get; set; } = string.Empty;
    public string NextAction { get; set; } = string.Empty;
    public string NextActionRemark { get; set; } = string.Empty;
    public DateTime? NextActionAt { get; set; }
    public string CloseResult { get; set; } = string.Empty;
    public string ClosedReason { get; set; } = string.Empty;
    public int CandidateCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class GetCrmMatchOpportunitiesHandler(ICrmDbContext dbContext)
    : IRequestHandler<GetCrmMatchOpportunitiesQuery, PaginationResponse<CrmMatchOpportunityListDto>>
{
    /// <summary>
    /// 编排撮合机会工作台列表查询。
    /// </summary>
    public async Task<PaginationResponse<CrmMatchOpportunityListDto>> Handle(
        GetCrmMatchOpportunitiesQuery request,
        CancellationToken cancellationToken)
    {
        var opportunities = BuildFilteredQuery(request);
        var totalCount = await opportunities.CountAsync(cancellationToken);
        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var list = await (
            from opportunity in opportunities
            join vendor in dbContext.CrmVendors on opportunity.VendorId equals vendor.Id into vendorGroup
            from vendor in vendorGroup.DefaultIfEmpty()
            orderby opportunity.Status == CrmMatchOpportunity.InProgress descending,
                opportunity.NextActionAt == null,
                opportunity.NextActionAt,
                opportunity.UpdatedAt descending
            select new CrmMatchOpportunityListDto
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
                Status = opportunity.Status,
                Stage = opportunity.Stage,
                NextAction = opportunity.NextAction,
                NextActionRemark = opportunity.NextActionRemark,
                NextActionAt = opportunity.NextActionAt,
                CloseResult = opportunity.CloseResult,
                ClosedReason = opportunity.ClosedReason,
                CandidateCount = dbContext.CrmMatchCandidates.Count(candidate => candidate.MatchOpportunityId == opportunity.Id),
                CreatedAt = opportunity.CreatedAt,
                UpdatedAt = opportunity.UpdatedAt
            })
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        await FillOwnerNamesAsync(list, cancellationToken);
        return new PaginationResponse<CrmMatchOpportunityListDto>(list, totalCount, page, pageSize);
    }

    private async Task FillOwnerNamesAsync(List<CrmMatchOpportunityListDto> items, CancellationToken cancellationToken)
    {
        var ownerIds = items
            .Where(item => item.OwnerUserId.HasValue)
            .Select(item => item.OwnerUserId!.Value)
            .Distinct()
            .ToList();
        if (ownerIds.Count == 0)
            return;

        var users = await CrmUserNames.GetAsync(dbContext, ownerIds, cancellationToken);

        foreach (var item in items)
        {
            if (item.OwnerUserId.HasValue &&
                users.TryGetValue(item.OwnerUserId.Value, out var owner))
            {
                item.OwnerUserName = CrmUserNames.DisplayName(owner.Username, owner.RealName);
            }
        }
    }

    private IQueryable<CrmMatchOpportunity> BuildFilteredQuery(GetCrmMatchOpportunitiesQuery request)
    {
        var opportunities = dbContext.CrmMatchOpportunities.AsNoTracking().AsQueryable();

        if (request.DemandItemId.HasValue)
        {
            opportunities = opportunities.Where(item => item.DemandItemId == request.DemandItemId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = request.Status.Trim();
            opportunities = opportunities.Where(item => item.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(request.ProductName))
        {
            var productName = request.ProductName.Trim();
            opportunities = opportunities.Where(item => item.DemandProductName.Contains(productName));
        }

        if (request.VendorId.HasValue)
        {
            opportunities = opportunities.Where(item => item.VendorId == request.VendorId.Value);
        }

        if (request.HerbBaseSubjectId.HasValue)
        {
            var subjectId = request.HerbBaseSubjectId.Value;
            opportunities = opportunities.Where(opportunity => dbContext.CrmMatchCandidates
                .Any(candidate => candidate.MatchOpportunityId == opportunity.Id && candidate.HerbBaseSubjectId == subjectId));
        }

        if (request.OwnerUserId.HasValue)
        {
            opportunities = opportunities.Where(item => item.OwnerUserId == request.OwnerUserId.Value);
        }

        if (request.NextActionAtFrom.HasValue)
        {
            opportunities = opportunities.Where(item => item.NextActionAt >= request.NextActionAtFrom.Value);
        }

        if (request.NextActionAtTo.HasValue)
        {
            opportunities = opportunities.Where(item => item.NextActionAt <= request.NextActionAtTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            opportunities = opportunities.Where(opportunity =>
                opportunity.DemandProductName.Contains(keyword) ||
                opportunity.DeliveryAddress.Contains(keyword) ||
                opportunity.NextAction.Contains(keyword) ||
                dbContext.CrmMatchCandidates.Any(candidate =>
                    candidate.MatchOpportunityId == opportunity.Id &&
                    (candidate.SubjectName.Contains(keyword) || candidate.HerbBaseName.Contains(keyword))));
        }

        return opportunities;
    }
}

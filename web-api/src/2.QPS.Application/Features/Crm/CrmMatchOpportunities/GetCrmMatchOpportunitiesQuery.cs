using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Attributes;
using QPS.Application.Extensions;
using QPS.Application.Features.System;
using QPS.Application.Interfaces;
using QPS.Domain.Entities.Crm;

namespace QPS.Application.Features.Crm.CrmMatchOpportunities;

[Permission(SystemPermissionCodes.CRM_MATCH_OPPORTUNITY_VIEW)]
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
    public string NextAction { get; set; } = string.Empty;
    public DateTime? NextActionAt { get; set; }
    public string CloseResult { get; set; } = string.Empty;
    public string ClosedReason { get; set; } = string.Empty;
    public int CandidateCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class GetCrmMatchOpportunitiesHandler(IDbContext dbContext)
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
            join owner in dbContext.SystemUsers on opportunity.OwnerUserId equals owner.Id into ownerGroup
            from owner in ownerGroup.DefaultIfEmpty()
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
                OwnerUserName = owner == null
                    ? string.Empty
                    : !string.IsNullOrWhiteSpace(owner.RealName) ? owner.RealName : owner.Username,
                Status = opportunity.Status,
                NextAction = opportunity.NextAction,
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

        return new PaginationResponse<CrmMatchOpportunityListDto>(list, totalCount, page, pageSize);
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

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
    public string? Source { get; set; }
}

public class CrmMatchOpportunityListDto
{
    public Guid Id { get; set; }
    public Guid? DemandItemId { get; set; }
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
    /// <summary>发起方向：demand=需求 → 供应，supply=供应 → 需求。</summary>
    public string Source { get; set; } = "demand";
    /// <summary>发起方名称：供应侧为基地主体，需求侧为药厂。</summary>
    public string InitiatorName { get; set; } = string.Empty;
    /// <summary>候选名单：供应侧为候选药厂，需求侧为候选基地，多个用「、」拼接。</summary>
    public string CandidateNames { get; set; } = string.Empty;
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
                Source = opportunity.Source,
                InitiatorName = opportunity.Source == "supply"
                    ? dbContext.CrmMatchCandidates
                        .Where(candidate => candidate.MatchOpportunityId == opportunity.Id && candidate.Role == "initiator")
                        .Select(candidate => candidate.SubjectName).FirstOrDefault() ?? string.Empty
                    : dbContext.CrmMatchOpportunityDemands
                        .Where(demand => demand.OpportunityId == opportunity.Id && demand.Role == "initiator")
                        .Select(demand => demand.VendorName).FirstOrDefault() ?? string.Empty,
                CandidateCount = 0,
                CreatedAt = opportunity.CreatedAt,
                UpdatedAt = opportunity.UpdatedAt
            })
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        await FillOwnerNamesAsync(list, cancellationToken);
        await FillCandidateNamesAsync(list, cancellationToken);
        return new PaginationResponse<CrmMatchOpportunityListDto>(list, totalCount, page, pageSize);
    }

    /// <summary>
    /// 填充候选名单：供应侧机会展示候选药厂，需求侧机会展示候选基地，多个用「、」拼接。
    /// </summary>
    private async Task FillCandidateNamesAsync(List<CrmMatchOpportunityListDto> items, CancellationToken cancellationToken)
    {
        if (items.Count == 0)
            return;

        var ids = items.Select(item => item.Id).ToList();
        var candidateVendors = await dbContext.CrmMatchOpportunityDemands
            .Where(demand => ids.Contains(demand.OpportunityId) && demand.Role == "candidate")
            .GroupBy(demand => demand.OpportunityId)
            .ToDictionaryAsync(
                group => group.Key,
                group => group.Select(demand => demand.VendorName).ToList(),
                cancellationToken);
        var candidateBases = await dbContext.CrmMatchCandidates
            .Where(candidate => ids.Contains(candidate.MatchOpportunityId) && candidate.Role == "candidate")
            .GroupBy(candidate => candidate.MatchOpportunityId)
            .ToDictionaryAsync(
                group => group.Key,
                group => group.Select(candidate => candidate.HerbBaseName).ToList(),
                cancellationToken);

        foreach (var item in items)
        {
            var names = (item.Source == "supply"
                ? candidateVendors.GetValueOrDefault(item.Id)
                : candidateBases.GetValueOrDefault(item.Id))
                ?? [];
            var distinctNames = names.Where(name => !string.IsNullOrWhiteSpace(name)).Distinct().ToList();
            item.CandidateNames = string.Join("、", distinctNames);
            item.CandidateCount = distinctNames.Count;
        }
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

        if (!string.IsNullOrWhiteSpace(request.Source))
        {
            var source = request.Source.Trim();
            opportunities = opportunities.Where(item => item.Source == source);
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

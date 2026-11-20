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
    /// <summary>发起方品类（实时取原明细）。</summary>
    public string ProductName { get; set; } = string.Empty;
    /// <summary>发起方数量（实时取原明细）。</summary>
    public decimal? Quantity { get; set; }
    public string QuantityUnit { get; set; } = string.Empty;
    /// <summary>候选名单，多个用「、」拼接。</summary>
    public string CandidateNames { get; set; } = string.Empty;
    public int CandidateCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class GetCrmMatchOpportunitiesHandler(ICrmDbContext dbContext)
    : IRequestHandler<GetCrmMatchOpportunitiesQuery, PaginationResponse<CrmMatchOpportunityListDto>>
{
    public async Task<PaginationResponse<CrmMatchOpportunityListDto>> Handle(
        GetCrmMatchOpportunitiesQuery request,
        CancellationToken cancellationToken)
    {
        var opportunities = BuildFilteredQuery(request);
        var totalCount = await opportunities.CountAsync(cancellationToken);
        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        // 一次性取本页机会的发起方名称与品类，避免 N+1
        var pageIds = await opportunities
            .OrderBy(opportunity => opportunity.Status == CrmMatchOpportunity.InProgress ? 0 : 1)
            .ThenBy(opportunity => opportunity.NextActionAt == null ? 1 : 0)
            .ThenBy(opportunity => opportunity.NextActionAt)
            .ThenByDescending(opportunity => opportunity.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(opportunity => opportunity.Id)
            .ToListAsync(cancellationToken);

        var initiatorInfo = await LoadInitiatorInfoAsync(pageIds, cancellationToken);
        var candidateNames = await LoadCandidateNamesAsync(pageIds, cancellationToken);

        var opportunityRows = await dbContext.CrmMatchOpportunities
            .AsNoTracking()
            .Where(opportunity => pageIds.Contains(opportunity.Id))
            .ToListAsync(cancellationToken);
        var orderMap = pageIds.Select((id, index) => (id, index)).ToDictionary(x => x.id, x => x.index);

        var list = opportunityRows
            .OrderBy(opportunity => orderMap[opportunity.Id])
            .Select(opportunity =>
            {
                initiatorInfo.TryGetValue(opportunity.Id, out var info);
                candidateNames.TryGetValue(opportunity.Id, out var names);
                var distinctNames = (names ?? []).Where(name => !string.IsNullOrWhiteSpace(name)).Distinct().ToList();
                return new CrmMatchOpportunityListDto
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
                    InitiatorName = info?.Name ?? string.Empty,
                    ProductName = info?.ProductName ?? string.Empty,
                    Quantity = info?.Quantity,
                    QuantityUnit = info?.QuantityUnit ?? string.Empty,
                    CandidateNames = string.Join("、", distinctNames),
                    CandidateCount = distinctNames.Count,
                    CreatedAt = opportunity.CreatedAt,
                    UpdatedAt = opportunity.UpdatedAt
                };
            })
            .ToList();

        await FillOwnerNamesAsync(list, cancellationToken);
        return new PaginationResponse<CrmMatchOpportunityListDto>(list, totalCount, page, pageSize);
    }

    private sealed record InitiatorInfo(string Name, string ProductName, decimal? Quantity, string QuantityUnit);

    /// <summary>发起方名称与品类：supply 机会取发起基地供应，demand 机会取发起采购明细+药厂。</summary>
    private async Task<Dictionary<Guid, InitiatorInfo>> LoadInitiatorInfoAsync(
        List<Guid> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0) return new();
        var result = new Dictionary<Guid, InitiatorInfo>();

        var supplyInitiators = await (
                from item in dbContext.CrmMatchOpportunityItems.AsNoTracking()
                join supply in dbContext.CrmHerbBaseSupplies.AsNoTracking().IgnoreQueryFilters() on item.EntityId equals supply.Id
                join herbBase in dbContext.CrmHerbBases.AsNoTracking().IgnoreQueryFilters() on supply.HerbBaseId equals herbBase.Id into hbGroup
                from herbBase in hbGroup.DefaultIfEmpty()
                where ids.Contains(item.OpportunityId) &&
                    item.EntityType == CrmMatchOpportunityItem.EntityTypeSupply &&
                    item.Role == CrmMatchOpportunityItem.Initiator
                select new
                {
                    item.OpportunityId,
                    Name = herbBase == null ? string.Empty : herbBase.SubjectName,
                    supply.ProductName,
                    supply.AvailableQuantity,
                    supply.QuantityUnit
                })
            .ToListAsync(cancellationToken);
        foreach (var row in supplyInitiators)
        {
            result[row.OpportunityId] = new InitiatorInfo(row.Name, row.ProductName, row.AvailableQuantity, row.QuantityUnit);
        }

        var demandInitiators = await (
                from item in dbContext.CrmMatchOpportunityItems.AsNoTracking()
                join demandItem in dbContext.CrmVendorDemandItems.AsNoTracking() on item.EntityId equals demandItem.Id
                join demand in dbContext.CrmVendorDemands.AsNoTracking().IgnoreQueryFilters() on demandItem.VendorDemandId equals demand.Id
                join vendor in dbContext.CrmVendors.AsNoTracking().IgnoreQueryFilters() on demand.VendorId equals vendor.Id into vGroup
                from vendor in vGroup.DefaultIfEmpty()
                where ids.Contains(item.OpportunityId) &&
                    item.EntityType == CrmMatchOpportunityItem.EntityTypeDemand &&
                    item.Role == CrmMatchOpportunityItem.Initiator
                select new
                {
                    item.OpportunityId,
                    Name = vendor == null ? string.Empty : vendor.VendorName,
                    demandItem.ProductName,
                    demandItem.Quantity,
                    demandItem.QuantityUnit
                })
            .ToListAsync(cancellationToken);
        foreach (var row in demandInitiators)
        {
            result[row.OpportunityId] = new InitiatorInfo(row.Name, row.ProductName, row.Quantity, row.QuantityUnit);
        }

        return result;
    }

    private async Task<Dictionary<Guid, List<string>>> LoadCandidateNamesAsync(List<Guid> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0) return new();
        var result = new Dictionary<Guid, List<string>>();

        var demandCandidates = await (
                from item in dbContext.CrmMatchOpportunityItems.AsNoTracking()
                join demandItem in dbContext.CrmVendorDemandItems.AsNoTracking() on item.EntityId equals demandItem.Id
                join demand in dbContext.CrmVendorDemands.AsNoTracking().IgnoreQueryFilters() on demandItem.VendorDemandId equals demand.Id
                join vendor in dbContext.CrmVendors.AsNoTracking().IgnoreQueryFilters() on demand.VendorId equals vendor.Id into vGroup
                from vendor in vGroup.DefaultIfEmpty()
                where ids.Contains(item.OpportunityId) &&
                    item.EntityType == CrmMatchOpportunityItem.EntityTypeDemand &&
                    item.Role == CrmMatchOpportunityItem.Candidate
                select new { item.OpportunityId, Name = vendor == null ? string.Empty : vendor.VendorName })
            .ToListAsync(cancellationToken);
        foreach (var row in demandCandidates)
        {
            if (!result.TryGetValue(row.OpportunityId, out var list)) result[row.OpportunityId] = list = new();
            list.Add(row.Name);
        }

        var supplyCandidates = await (
                from item in dbContext.CrmMatchOpportunityItems.AsNoTracking()
                join supply in dbContext.CrmHerbBaseSupplies.AsNoTracking().IgnoreQueryFilters() on item.EntityId equals supply.Id
                join herbBase in dbContext.CrmHerbBases.AsNoTracking().IgnoreQueryFilters() on supply.HerbBaseId equals herbBase.Id into hbGroup
                from herbBase in hbGroup.DefaultIfEmpty()
                where ids.Contains(item.OpportunityId) &&
                    item.EntityType == CrmMatchOpportunityItem.EntityTypeSupply &&
                    item.Role == CrmMatchOpportunityItem.Candidate
                select new { item.OpportunityId, Name = herbBase == null ? string.Empty : herbBase.SubjectName })
            .ToListAsync(cancellationToken);
        foreach (var row in supplyCandidates)
        {
            if (!result.TryGetValue(row.OpportunityId, out var list)) result[row.OpportunityId] = list = new();
            list.Add(row.Name);
        }

        return result;
    }

    private async Task FillOwnerNamesAsync(List<CrmMatchOpportunityListDto> items, CancellationToken cancellationToken)
    {
        var ownerIds = items.Where(item => item.OwnerUserId.HasValue)
            .Select(item => item.OwnerUserId!.Value).Distinct().ToList();
        if (ownerIds.Count == 0) return;
        var users = await CrmUserNames.GetAsync(dbContext, ownerIds, cancellationToken);
        foreach (var item in items)
        {
            if (item.OwnerUserId.HasValue && users.TryGetValue(item.OwnerUserId.Value, out var owner))
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
            var demandItemId = request.DemandItemId.Value;
            opportunities = opportunities.Where(opportunity => dbContext.CrmMatchOpportunityItems.Any(item =>
                item.OpportunityId == opportunity.Id &&
                item.EntityType == CrmMatchOpportunityItem.EntityTypeDemand &&
                item.EntityId == demandItemId));
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
            opportunities = opportunities.Where(opportunity => dbContext.CrmMatchOpportunityItems.Any(item =>
                item.OpportunityId == opportunity.Id && item.Role == CrmMatchOpportunityItem.Initiator &&
                ((item.EntityType == CrmMatchOpportunityItem.EntityTypeDemand &&
                    dbContext.CrmVendorDemandItems.Any(d => d.Id == item.EntityId && d.ProductName.Contains(productName))) ||
                 (item.EntityType == CrmMatchOpportunityItem.EntityTypeSupply &&
                    dbContext.CrmHerbBaseSupplies.Any(s => s.Id == item.EntityId && s.ProductName.Contains(productName))))));
        }

        if (request.VendorId.HasValue)
        {
            var vendorId = request.VendorId.Value;
            opportunities = opportunities.Where(opportunity => dbContext.CrmMatchOpportunityItems.Any(item =>
                item.OpportunityId == opportunity.Id &&
                item.EntityType == CrmMatchOpportunityItem.EntityTypeDemand &&
                dbContext.CrmVendorDemandItems.Any(d =>
                    d.Id == item.EntityId &&
                    dbContext.CrmVendorDemands.Any(dm => dm.Id == d.VendorDemandId && dm.VendorId == vendorId))));
        }

        if (request.HerbBaseSubjectId.HasValue)
        {
            var subjectId = request.HerbBaseSubjectId.Value;
            opportunities = opportunities.Where(opportunity => dbContext.CrmMatchOpportunityItems.Any(item =>
                item.OpportunityId == opportunity.Id &&
                item.EntityType == CrmMatchOpportunityItem.EntityTypeSupply &&
                dbContext.CrmHerbBaseSupplies.Any(s => s.Id == item.EntityId && s.HerbBaseSubjectId == subjectId)));
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
                opportunity.NextAction.Contains(keyword) ||
                opportunity.ClosedReason.Contains(keyword) ||
                dbContext.CrmMatchOpportunityItems.Any(item =>
                    item.OpportunityId == opportunity.Id &&
                    ((item.EntityType == CrmMatchOpportunityItem.EntityTypeDemand &&
                        dbContext.CrmVendorDemandItems.Any(d => d.Id == item.EntityId && d.ProductName.Contains(keyword))) ||
                     (item.EntityType == CrmMatchOpportunityItem.EntityTypeSupply &&
                        (dbContext.CrmHerbBaseSupplies.Any(s => s.Id == item.EntityId && s.ProductName.Contains(keyword)) ||
                         dbContext.CrmHerbBases.Any(h => dbContext.CrmHerbBaseSupplies.Any(s => s.Id == item.EntityId && s.HerbBaseId == h.Id) &&
                            (h.SubjectName.Contains(keyword) || h.BaseName.Contains(keyword))))))));
        }

        return opportunities;
    }
}

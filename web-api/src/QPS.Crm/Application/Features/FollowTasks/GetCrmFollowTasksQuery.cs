using QPS.Crm.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Crm.Contracts.DTOs;
using QPS.Crm.Contracts.DTOs.FollowTasks;
using QPS.Shared.Contracts;
using QPS.Crm.Domain.Entities;

namespace QPS.Crm.Application.Features.FollowTasks;

public class GetCrmFollowTasksQuery : IRequest<CrmFollowTaskResponse>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Category { get; set; }
    public string? EntityType { get; set; }
    public string? Keyword { get; set; }
}

public class GetCrmFollowTasksHandler : IRequestHandler<GetCrmFollowTasksQuery, CrmFollowTaskResponse>
{
    private const string SubjectType = CrmCodes.HerbBaseSubjectEntityType;
    private const string VendorType = CrmCodes.VendorEntityType;
    private const string MatchOpportunityType = CrmCodes.MatchOpportunityEntityType;
    private readonly ICrmDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public GetCrmFollowTasksHandler(ICrmDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<CrmFollowTaskResponse> Handle(GetCrmFollowTasksQuery request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var ownerId))
            return new CrmFollowTaskResponse { CurrentPage = request.Page, PageSize = request.PageSize };

        var now = DateTime.Now;
        var today = now.Date;
        var tomorrow = today.AddDays(1);
        var subjects = await _dbContext.CrmHerbBaseSubjects.Where(x => x.OwnerUserId == ownerId)
            .Select(x => new CrmFollowTaskDto { EntityId = x.Id, EntityType = SubjectType, EntityName = x.SubjectName ?? string.Empty, ContactName = x.PrimaryContactName ?? string.Empty, ContactPhone = x.PrimaryContactPhone ?? string.Empty, LastFollowAt = x.LastFollowAt, LastFollowResult = x.LastFollowResult ?? string.Empty, NextFollowAt = x.NextFollowAt }).ToListAsync(cancellationToken);
        var vendors = await _dbContext.CrmVendors.Where(x => x.OwnerUserId == ownerId)
            .Select(x => new CrmFollowTaskDto { EntityId = x.Id, EntityType = VendorType, EntityName = x.VendorName, ContactName = _dbContext.CrmContacts.Where(c => c.EntityType == VendorType && c.EntityId == x.Id && c.Status != "无效").OrderByDescending(c => c.IsPrimary).ThenBy(c => c.CreatedAt).Select(c => c.ContactName).FirstOrDefault() ?? string.Empty, ContactPhone = _dbContext.CrmContacts.Where(c => c.EntityType == VendorType && c.EntityId == x.Id && c.Status != "无效").OrderByDescending(c => c.IsPrimary).ThenBy(c => c.CreatedAt).Select(c => c.Phone).FirstOrDefault() ?? string.Empty, LastFollowAt = x.LastFollowAt, LastFollowResult = x.LastFollowResult, NextFollowAt = x.NextFollowAt }).ToListAsync(cancellationToken);
        var opportunityRows = await _dbContext.CrmMatchOpportunities
            .Where(x => x.OwnerUserId == ownerId && x.Status == CrmMatchOpportunity.InProgress)
            .Select(x => new { x.Id, x.Source, x.Stage, x.NextAction, x.NextActionRemark, x.NextActionAt })
            .ToListAsync(cancellationToken);

        var opportunityIds = opportunityRows.Select(x => x.Id).ToList();

        // 发起方名称与品类实时取子表关联的原明细：按实体类型分开取，避免条件分支无法翻译成 SQL
        var supplyInitiators = new Dictionary<Guid, (string SubjectName, string ProductName)>();
        var demandInitiators = new Dictionary<Guid, (string VendorName, string ProductName)>();
        if (opportunityIds.Count > 0)
        {
            var supplyRows = await _dbContext.CrmMatchOpportunityItems
                .Where(i => i.Role == CrmMatchOpportunityItem.Initiator && i.EntityType == CrmMatchOpportunityItem.EntityTypeSupply)
                .Where(i => opportunityIds.Contains(i.OpportunityId))
                .Join(_dbContext.CrmHerbBaseSupplies, i => i.EntityId, s => s.Id, (i, s) => new { i.OpportunityId, s.ProductName, s.HerbBaseId })
                .Join(_dbContext.CrmHerbBases, x => x.HerbBaseId, h => h.Id, (x, h) => new { x.OpportunityId, x.ProductName, h.SubjectName })
                .ToListAsync(cancellationToken);
            supplyInitiators = supplyRows
                .GroupBy(x => x.OpportunityId)
                .ToDictionary(g => g.Key, g => (g.First().SubjectName, g.First().ProductName));

            var demandRows = await _dbContext.CrmMatchOpportunityItems
                .Where(i => i.Role == CrmMatchOpportunityItem.Initiator && i.EntityType == CrmMatchOpportunityItem.EntityTypeDemand)
                .Where(i => opportunityIds.Contains(i.OpportunityId))
                .Join(_dbContext.CrmVendorDemandItems, i => i.EntityId, d => d.Id, (i, d) => new { i.OpportunityId, d.ProductName, d.VendorDemandId })
                .Join(_dbContext.CrmVendorDemands, x => x.VendorDemandId, dm => dm.Id, (x, dm) => new { x.OpportunityId, x.ProductName, dm.VendorId })
                .Join(_dbContext.CrmVendors, x => x.VendorId, v => v.Id, (x, v) => new { x.OpportunityId, x.ProductName, v.VendorName })
                .ToListAsync(cancellationToken);
            demandInitiators = demandRows
                .GroupBy(x => x.OpportunityId)
                .ToDictionary(g => g.Key, g => (g.First().VendorName, g.First().ProductName));
        }

        var opportunities = opportunityRows
            .Select(x =>
            {
                supplyInitiators.TryGetValue(x.Id, out var supplyInitiator);
                demandInitiators.TryGetValue(x.Id, out var demandInitiator);
                return new CrmFollowTaskDto
                {
                    EntityId = x.Id,
                    EntityType = MatchOpportunityType,
                    EntityName = (x.Source == "supply" ? supplyInitiator.SubjectName : demandInitiator.VendorName) ?? string.Empty,
                    EntitySubtitle = (supplyInitiator.ProductName ?? demandInitiator.ProductName) ?? string.Empty,
                    LastFollowResult = x.Stage,
                    Stage = x.Stage,
                    TaskContent = string.IsNullOrWhiteSpace(x.NextActionRemark) ? x.NextAction : $"{x.NextAction}（{x.NextActionRemark}）",
                    NextFollowAt = x.NextActionAt
                };
            })
            .ToList();
        var tasks = subjects.Concat(vendors).Concat(opportunities).Select(x => { x.Category = GetCategory(x.NextFollowAt, now, today, tomorrow); return x; }).ToList();
        IEnumerable<CrmFollowTaskDto> query = tasks;
        if (!string.IsNullOrWhiteSpace(request.Category)) query = query.Where(x => x.Category == request.Category);
        if (!string.IsNullOrWhiteSpace(request.EntityType)) query = query.Where(x => x.EntityType == request.EntityType);
        if (!string.IsNullOrWhiteSpace(request.Keyword)) { var key = request.Keyword.Trim(); query = query.Where(x => x.EntityName.Contains(key, StringComparison.OrdinalIgnoreCase) || x.ContactName.Contains(key, StringComparison.OrdinalIgnoreCase) || x.ContactPhone.Contains(key, StringComparison.OrdinalIgnoreCase)); }
        var ordered = query.OrderBy(x => x.Category switch { "OVERDUE" => 0, "TODAY" => 1, "NO_PLAN" => 2, _ => 3 }).ThenBy(x => x.NextFollowAt ?? x.LastFollowAt).ThenBy(x => x.EntityName).ToList();
        return new CrmFollowTaskResponse { TotalCount = ordered.Count, CurrentPage = request.Page, PageSize = request.PageSize, List = ordered.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToList() };
    }

    private static string GetCategory(DateTime? next, DateTime now, DateTime today, DateTime tomorrow) => !next.HasValue ? "NO_PLAN" : next < now ? "OVERDUE" : next < tomorrow ? "TODAY" : "FUTURE";
}
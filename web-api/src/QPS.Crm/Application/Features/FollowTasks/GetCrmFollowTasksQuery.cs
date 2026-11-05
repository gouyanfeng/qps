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
        var opportunities = await _dbContext.CrmMatchOpportunities
            .Where(x => x.OwnerUserId == ownerId && x.Status == CrmMatchOpportunity.InProgress)
            .Select(x => new CrmFollowTaskDto
            {
                EntityId = x.Id,
                EntityType = MatchOpportunityType,
                EntityName = _dbContext.CrmVendors.Where(v => v.Id == x.VendorId).Select(v => v.VendorName).FirstOrDefault() ?? string.Empty,
                EntitySubtitle = x.DemandProductName,
                LastFollowResult = x.Stage,
                Stage = x.Stage,
                TaskContent = string.IsNullOrWhiteSpace(x.NextActionRemark) ? x.NextAction : $"{x.NextAction}（{x.NextActionRemark}）",
                NextFollowAt = x.NextActionAt
            })
            .ToListAsync(cancellationToken);
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

using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Contracts.Crm;
using QPS.Application.Interfaces;

namespace QPS.Application.Features.Crm.FollowTasks;

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
    private readonly IDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public GetCrmFollowTasksHandler(IDbContext dbContext, ICurrentUserService currentUserService)
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
        var subjects = await _dbContext.CrmHerbBaseSubjects.Where(x => !x.IsDeleted && x.OwnerUserId == ownerId)
            .Select(x => new CrmFollowTaskDto { EntityId = x.Id, EntityType = SubjectType, EntityName = x.SubjectName ?? string.Empty, ContactName = x.PrimaryContactName ?? string.Empty, ContactPhone = x.PrimaryContactPhone ?? string.Empty, LastFollowAt = x.LastFollowAt, LastFollowResult = x.LastFollowResult ?? string.Empty, NextFollowAt = x.NextFollowAt }).ToListAsync(cancellationToken);
        var vendors = await _dbContext.CrmVendors.Where(x => !x.IsDeleted && x.OwnerUserId == ownerId)
            .Select(x => new CrmFollowTaskDto { EntityId = x.Id, EntityType = VendorType, EntityName = x.VendorName, ContactName = _dbContext.CrmContacts.Where(c => !c.IsDeleted && c.EntityType == VendorType && c.EntityId == x.Id && c.Status != "无效").OrderByDescending(c => c.IsPrimary).ThenBy(c => c.CreatedAt).Select(c => c.ContactName).FirstOrDefault() ?? string.Empty, ContactPhone = _dbContext.CrmContacts.Where(c => !c.IsDeleted && c.EntityType == VendorType && c.EntityId == x.Id && c.Status != "无效").OrderByDescending(c => c.IsPrimary).ThenBy(c => c.CreatedAt).Select(c => c.Phone).FirstOrDefault() ?? string.Empty, LastFollowAt = x.LastFollowAt, LastFollowResult = x.LastFollowResult, NextFollowAt = x.NextFollowAt }).ToListAsync(cancellationToken);
        var tasks = subjects.Concat(vendors).Select(x => { x.Category = GetCategory(x.NextFollowAt, now, today, tomorrow); return x; }).ToList();
        var overview = new CrmFollowTaskOverviewDto { OverdueCount = tasks.Count(x => x.Category == "OVERDUE"), TodayCount = tasks.Count(x => x.Category == "TODAY"), NoPlanCount = tasks.Count(x => x.Category == "NO_PLAN"), CompletedLast7DaysCount = await _dbContext.CrmFollowRecords.CountAsync(x => !x.IsDeleted && x.OperatorUserId == ownerId && x.CreatedAt >= today.AddDays(-6) && x.CreatedAt < tomorrow, cancellationToken) };
        IEnumerable<CrmFollowTaskDto> query = tasks;
        if (!string.IsNullOrWhiteSpace(request.Category)) query = query.Where(x => x.Category == request.Category);
        if (!string.IsNullOrWhiteSpace(request.EntityType)) query = query.Where(x => x.EntityType == request.EntityType);
        if (!string.IsNullOrWhiteSpace(request.Keyword)) { var key = request.Keyword.Trim(); query = query.Where(x => x.EntityName.Contains(key, StringComparison.OrdinalIgnoreCase) || x.ContactName.Contains(key, StringComparison.OrdinalIgnoreCase) || x.ContactPhone.Contains(key, StringComparison.OrdinalIgnoreCase)); }
        var ordered = query.OrderBy(x => x.Category switch { "OVERDUE" => 0, "TODAY" => 1, "NO_PLAN" => 2, _ => 3 }).ThenBy(x => x.NextFollowAt ?? x.LastFollowAt).ThenBy(x => x.EntityName).ToList();
        return new CrmFollowTaskResponse { Overview = overview, TotalCount = ordered.Count, CurrentPage = request.Page, PageSize = request.PageSize, List = ordered.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToList() };
    }

    private static string GetCategory(DateTime? next, DateTime now, DateTime today, DateTime tomorrow) => !next.HasValue ? "NO_PLAN" : next < now ? "OVERDUE" : next < tomorrow ? "TODAY" : "FUTURE";
}

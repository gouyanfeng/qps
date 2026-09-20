using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Contracts.Crm;
using QPS.Application.Contracts.Crm.FollowTasks;
using QPS.Application.Interfaces;
using QPS.Domain.Entities.Crm;

namespace QPS.Application.Features.Crm.FollowTasks;

public class GetCrmFollowTaskOverviewQuery : IRequest<CrmFollowTaskOverviewDto>;

public class GetCrmFollowTaskOverviewHandler : IRequestHandler<GetCrmFollowTaskOverviewQuery, CrmFollowTaskOverviewDto>
{
    private const string SubjectType = CrmCodes.HerbBaseSubjectEntityType;
    private const string VendorType = CrmCodes.VendorEntityType;
    private const string MatchOpportunityType = CrmCodes.MatchOpportunityEntityType;
    private readonly IDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public GetCrmFollowTaskOverviewHandler(IDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<CrmFollowTaskOverviewDto> Handle(GetCrmFollowTaskOverviewQuery request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var ownerId))
            return new CrmFollowTaskOverviewDto();

        var now = DateTime.Now;
        var today = now.Date;
        var tomorrow = today.AddDays(1);

        var subjects = await _dbContext.CrmHerbBaseSubjects
            .Where(x => !x.IsDeleted && x.OwnerUserId == ownerId)
            .Select(x => (DateTime?)x.NextFollowAt)
            .ToListAsync(cancellationToken);

        var vendors = await _dbContext.CrmVendors
            .Where(x => !x.IsDeleted && x.OwnerUserId == ownerId)
            .Select(x => (DateTime?)x.NextFollowAt)
            .ToListAsync(cancellationToken);

        var opportunities = await _dbContext.CrmMatchOpportunities
            .Where(x => !x.IsDeleted && x.OwnerUserId == ownerId && x.Status == CrmMatchOpportunity.InProgress)
            .Select(x => x.NextActionAt)
            .ToListAsync(cancellationToken);

        var nextFollowAts = subjects.Concat(vendors).Concat(opportunities).ToList();
        var overdueCount = nextFollowAts.Count(x => x.HasValue && x < now);
        var todayCount = nextFollowAts.Count(x => x.HasValue && x >= now && x < tomorrow);
        var noPlanCount = nextFollowAts.Count(x => !x.HasValue);

        var completedLast7DaysCount = await _dbContext.CrmFollowRecords.CountAsync(
            x => !x.IsDeleted && x.OperatorUserId == ownerId && x.CreatedAt >= today.AddDays(-6) && x.CreatedAt < tomorrow,
            cancellationToken);

        return new CrmFollowTaskOverviewDto
        {
            OverdueCount = overdueCount,
            TodayCount = todayCount,
            NoPlanCount = noPlanCount,
            CompletedLast7DaysCount = completedLast7DaysCount,
        };
    }
}

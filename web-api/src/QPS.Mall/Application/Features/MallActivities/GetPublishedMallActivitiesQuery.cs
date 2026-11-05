using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallActivities;

public class GetPublishedMallActivitiesQuery : IRequest<List<MallActivityListItemDto>>
{
}

public class GetPublishedMallActivitiesHandler
    : IRequestHandler<GetPublishedMallActivitiesQuery, List<MallActivityListItemDto>>
{
    private readonly IMallDbContext _dbContext;

    public GetPublishedMallActivitiesHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<MallActivityListItemDto>> Handle(
        GetPublishedMallActivitiesQuery query,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var activities = await _dbContext.MallActivities
            .AsNoTracking()
            .Where(activity =>
                activity.IsActive &&
                (activity.StartDate == null || activity.StartDate <= now) &&
                (activity.EndDate == null || activity.EndDate >= now))
            .OrderBy(activity => activity.SortOrder)
            .ThenByDescending(activity => activity.CreatedAt)
            .ToListAsync(cancellationToken);

        return activities
            .Select(MallActivityMapper.ToListItem)
            .ToList();
    }
}

public class GetPublishedMallActivityDetailQuery : IRequest<MallActivityDto>
{
    public Guid Id { get; set; }
}

public class GetPublishedMallActivityDetailHandler
    : IRequestHandler<GetPublishedMallActivityDetailQuery, MallActivityDto>
{
    private readonly IMallDbContext _dbContext;

    public GetPublishedMallActivityDetailHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MallActivityDto> Handle(
        GetPublishedMallActivityDetailQuery query,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var activity = await _dbContext.MallActivities
            .AsNoTracking()
            .FirstOrDefaultAsync(item =>
                item.Id == query.Id &&
                item.IsActive &&
                (item.StartDate == null || item.StartDate <= now) &&
                (item.EndDate == null || item.EndDate >= now), cancellationToken);

        if (activity is null)
        {
            throw new BusinessException(404, "活动不存在或已结束");
        }

        return MallActivityMapper.ToDetail(activity);
    }
}
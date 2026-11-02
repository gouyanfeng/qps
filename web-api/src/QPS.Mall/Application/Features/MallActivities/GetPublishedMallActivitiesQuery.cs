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

        var activityIds = activities.Select(a => a.Id).ToList();
        var linksByActivity = activityIds.Count == 0
            ? new Dictionary<Guid, List<MallActivityPromotion>>()
            : (await _dbContext.MallActivityPromotions
                .AsNoTracking()
                .Where(link => activityIds.Contains(link.ActivityId) && !link.IsDeleted)
                .ToListAsync(cancellationToken))
            .GroupBy(link => link.ActivityId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var promotions = await LoadActivePromotionsAsync(linksByActivity, now, cancellationToken);

        return activities
            .Select(activity => MallActivityMapper.ToListItem(
                activity,
                linksByActivity.TryGetValue(activity.Id, out var links) ? links : new List<MallActivityPromotion>(),
                promotions,
                includeInactivePromotions: false))
            .ToList();
    }

    private async Task<Dictionary<Guid, MallPromotion>> LoadActivePromotionsAsync(
        Dictionary<Guid, List<MallActivityPromotion>> linksByActivity,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var promotionIds = linksByActivity.Values
            .SelectMany(links => links)
            .Select(link => link.PromotionId)
            .Distinct()
            .ToList();

        if (promotionIds.Count == 0)
        {
            return new Dictionary<Guid, MallPromotion>();
        }

        return await _dbContext.MallPromotions
            .AsNoTracking()
            .Where(promotion =>
                promotionIds.Contains(promotion.Id) &&
                promotion.IsActive &&
                (promotion.StartDate == null || promotion.StartDate <= now) &&
                (promotion.EndDate == null || promotion.EndDate >= now))
            .ToDictionaryAsync(promotion => promotion.Id, cancellationToken);
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

        var links = await _dbContext.MallActivityPromotions
            .AsNoTracking()
            .Where(link => link.ActivityId == activity.Id && !link.IsDeleted)
            .ToListAsync(cancellationToken);

        var promotionIds = links.Select(link => link.PromotionId).Distinct().ToList();
        var promotions = promotionIds.Count == 0
            ? new Dictionary<Guid, MallPromotion>()
            : await _dbContext.MallPromotions
                .AsNoTracking()
                .Where(promotion =>
                    promotionIds.Contains(promotion.Id) &&
                    promotion.IsActive &&
                    (promotion.StartDate == null || promotion.StartDate <= now) &&
                    (promotion.EndDate == null || promotion.EndDate >= now))
                .ToDictionaryAsync(promotion => promotion.Id, cancellationToken);

        return MallActivityMapper.ToDetail(activity, links, promotions, includeInactivePromotions: false);
    }
}
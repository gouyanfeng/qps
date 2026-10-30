using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallActivities;

/// <summary>
/// 移动端：查询进行中/未开始的活动列表（仅启用且在时间窗口内）。
/// </summary>
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
            .Include(activity => activity.Promotions)
            .Where(activity =>
                activity.IsActive &&
                (activity.StartDate == null || activity.StartDate <= now) &&
                (activity.EndDate == null || activity.EndDate >= now))
            .OrderBy(activity => activity.SortOrder)
            .ThenByDescending(activity => activity.CreatedAt)
            .ToListAsync(cancellationToken);

        var promotions = await LoadActivePromotionsAsync(activities, now, cancellationToken);

        return activities
            .Select(activity => MallActivityMapper.ToListItem(activity, promotions, includeInactivePromotions: false))
            .ToList();
    }

    private async Task<Dictionary<Guid, MallPromotion>> LoadActivePromotionsAsync(
        IReadOnlyCollection<MallActivity> activities,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var promotionIds = activities
            .SelectMany(activity => activity.Promotions)
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

/// <summary>
/// 移动端：查询单个活动详情。
/// </summary>
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
            .Include(item => item.Promotions)
            .FirstOrDefaultAsync(item =>
                item.Id == query.Id &&
                item.IsActive &&
                (item.StartDate == null || item.StartDate <= now) &&
                (item.EndDate == null || item.EndDate >= now), cancellationToken);

        if (activity is null)
        {
            throw new BusinessException(404, "活动不存在或已结束");
        }

        var promotionIds = activity.Promotions.Select(link => link.PromotionId).Distinct().ToList();
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

        return MallActivityMapper.ToDetail(activity, promotions, includeInactivePromotions: false);
    }
}

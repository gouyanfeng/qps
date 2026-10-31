using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;

namespace QPS.Mall.Application.Features.MallActivities;

public class GetMallActivitiesQuery : IRequest<List<MallActivityListItemDto>>
{
    public string? Keyword { get; set; }

    public bool? IsActive { get; set; }
}

public class GetMallActivitiesHandler : IRequestHandler<GetMallActivitiesQuery, List<MallActivityListItemDto>>
{
    private readonly IMallDbContext _dbContext;

    public GetMallActivitiesHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<MallActivityListItemDto>> Handle(
        GetMallActivitiesQuery query,
        CancellationToken cancellationToken)
    {
        var keyword = query.Keyword?.Trim();

        var activitiesQuery = _dbContext.MallActivities
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            activitiesQuery = activitiesQuery.Where(activity =>
                activity.Name.Contains(keyword) || activity.Subtitle.Contains(keyword));
        }

        if (query.IsActive.HasValue)
        {
            activitiesQuery = activitiesQuery.Where(activity => activity.IsActive == query.IsActive.Value);
        }

        var activities = await activitiesQuery
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

        var promotions = await LoadPromotionsAsync(linksByActivity, cancellationToken);

        return activities
            .Select(activity => MallActivityMapper.ToListItem(
                activity,
                linksByActivity.TryGetValue(activity.Id, out var links) ? links : new List<MallActivityPromotion>(),
                promotions))
            .ToList();
    }

    private async Task<Dictionary<Guid, MallPromotion>> LoadPromotionsAsync(
        Dictionary<Guid, List<MallActivityPromotion>> linksByActivity,
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
            .Where(promotion => promotionIds.Contains(promotion.Id))
            .ToDictionaryAsync(promotion => promotion.Id, cancellationToken);
    }
}
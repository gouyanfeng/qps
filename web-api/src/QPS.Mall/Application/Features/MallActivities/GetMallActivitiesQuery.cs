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
            .Include(activity => activity.Promotions)
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

        var promotions = await LoadPromotionsAsync(activities, cancellationToken);

        return activities
            .Select(activity => MallActivityMapper.ToListItem(activity, promotions))
            .ToList();
    }

    private async Task<Dictionary<Guid, MallPromotion>> LoadPromotionsAsync(
        IReadOnlyCollection<MallActivity> activities,
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
            .Where(promotion => promotionIds.Contains(promotion.Id))
            .ToDictionaryAsync(promotion => promotion.Id, cancellationToken);
    }
}

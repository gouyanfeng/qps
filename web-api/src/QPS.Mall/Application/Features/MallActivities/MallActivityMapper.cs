using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;

namespace QPS.Mall.Application.Features.MallActivities;

internal static class MallActivityMapper
{
    public static MallActivityListItemDto ToListItem(
        MallActivity activity,
        IReadOnlyList<MallActivityPromotion> links,
        IReadOnlyDictionary<Guid, MallPromotion> promotions,
        bool includeInactivePromotions = true)
    {
        return new MallActivityListItemDto
        {
            Id = activity.Id,
            Name = activity.Name,
            Subtitle = activity.Subtitle,
            CoverImage = activity.CoverImage,
            StartDate = activity.StartDate,
            EndDate = activity.EndDate,
            SortOrder = activity.SortOrder,
            IsActive = activity.IsActive,
            CreatedAt = activity.CreatedAt,
            Promotions = BuildPromotions(links, promotions, includeInactivePromotions)
        };
    }

    public static MallActivityDto ToDetail(
        MallActivity activity,
        IReadOnlyList<MallActivityPromotion> links,
        IReadOnlyDictionary<Guid, MallPromotion> promotions,
        bool includeInactivePromotions = true)
    {
        return new MallActivityDto
        {
            Id = activity.Id,
            Name = activity.Name,
            Subtitle = activity.Subtitle,
            CoverImage = activity.CoverImage,
            Description = activity.Description,
            DetailHtml = activity.DetailHtml,
            StartDate = activity.StartDate,
            EndDate = activity.EndDate,
            SortOrder = activity.SortOrder,
            IsActive = activity.IsActive,
            CreatedAt = activity.CreatedAt,
            UpdatedAt = activity.UpdatedAt,
            Promotions = BuildPromotions(links, promotions, includeInactivePromotions)
        };
    }

    private static List<MallActivityPromotionDto> BuildPromotions(
        IReadOnlyList<MallActivityPromotion> links,
        IReadOnlyDictionary<Guid, MallPromotion> promotions,
        bool includeInactivePromotions)
    {
        var result = new List<MallActivityPromotionDto>();

        foreach (var link in links)
        {
            if (!promotions.TryGetValue(link.PromotionId, out var promotion))
            {
                continue;
            }

            if (!includeInactivePromotions && !promotion.IsActive)
            {
                continue;
            }

            result.Add(new MallActivityPromotionDto
            {
                Id = link.Id,
                PromotionId = promotion.Id,
                Name = promotion.Name,
                Description = promotion.Description,
                TriggerType = promotion.TriggerType,
                ActionType = promotion.ActionType,
                StartDate = promotion.StartDate,
                EndDate = promotion.EndDate,
                IsActive = promotion.IsActive
            });
        }

        return result;
    }
}
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;

namespace QPS.Mall.Application.Features.MallActivities;

internal static class MallActivityMapper
{
    public static MallActivityListItemDto ToListItem(MallActivity activity)
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
            CreatedAt = activity.CreatedAt
        };
    }

    public static MallActivityDto ToDetail(MallActivity activity)
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
            UpdatedAt = activity.UpdatedAt
        };
    }
}
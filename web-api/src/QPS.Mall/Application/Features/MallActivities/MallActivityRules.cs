using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallActivities;

/// <summary>
/// 活动字段规范化与校验。
/// </summary>
internal static class MallActivityRules
{
    public const int NameMaxLength = 100;
    public const int SubtitleMaxLength = 100;
    public const int CoverImageMaxLength = 500;
    public const int DescriptionMaxLength = 500;
    public const int DetailHtmlMaxLength = 50000;
    public const int MaxSortOrder = 9999;

    public static string NormalizeName(string? name)
    {
        var normalized = name?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new BusinessException(400, "请输入活动名称");
        }

        if (normalized.Length > NameMaxLength)
        {
            throw new BusinessException(400, $"活动名称不能超过 {NameMaxLength} 个字符");
        }

        return normalized;
    }

    public static string NormalizeSubtitle(string? subtitle)
    {
        var normalized = subtitle?.Trim() ?? string.Empty;

        if (normalized.Length > SubtitleMaxLength)
        {
            throw new BusinessException(400, $"活动副标题不能超过 {SubtitleMaxLength} 个字符");
        }

        return normalized;
    }

    public static string NormalizeCoverImage(string? coverImage)
    {
        var normalized = coverImage?.Trim() ?? string.Empty;

        if (normalized.Length > CoverImageMaxLength)
        {
            throw new BusinessException(400, $"封面图地址不能超过 {CoverImageMaxLength} 个字符");
        }

        return normalized;
    }

    public static string NormalizeDescription(string? description)
    {
        var normalized = description?.Trim() ?? string.Empty;

        if (normalized.Length > DescriptionMaxLength)
        {
            throw new BusinessException(400, $"活动说明不能超过 {DescriptionMaxLength} 个字符");
        }

        return normalized;
    }

    public static string NormalizeDetailHtml(string? detailHtml)
    {
        var normalized = detailHtml ?? string.Empty;

        if (normalized.Length > DetailHtmlMaxLength)
        {
            throw new BusinessException(400, $"活动详情不能超过 {DetailHtmlMaxLength} 个字符");
        }

        return normalized;
    }

    public static int NormalizeSortOrder(int sortOrder)
    {
        if (sortOrder < 0 || sortOrder > MaxSortOrder)
        {
            throw new BusinessException(400, $"排序值必须在 0 - {MaxSortOrder} 之间");
        }

        return sortOrder;
    }

    public static void ValidateTimeWindow(DateTime? startDate, DateTime? endDate)
    {
        if (startDate.HasValue && endDate.HasValue && endDate.Value < startDate.Value)
        {
            throw new BusinessException(400, "活动结束时间不能早于开始时间");
        }
    }
}
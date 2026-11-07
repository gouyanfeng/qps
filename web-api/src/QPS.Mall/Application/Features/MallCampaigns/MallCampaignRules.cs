using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallCampaigns;

/// <summary>
/// 活动专题字段规范化与校验。
/// </summary>
internal static class MallCampaignRules
{
    public const int NameMaxLength = 100;
    public const int CoverImageUrlMaxLength = 500;
    public const int ContentHtmlMaxLength = 200000;
    public const int GroupNameMaxLength = 50;
    public const int MaxSortOrder = 9999;

    /// <summary>
    /// 校验活动类型：本期支持商品推荐型与领券型，报名型仅保留枚举位。
    /// </summary>
    public static MallCampaignType NormalizeType(int type)
    {
        if (type == (int)MallCampaignType.ProductShowcase)
        {
            return MallCampaignType.ProductShowcase;
        }

        if (type == (int)MallCampaignType.Coupon)
        {
            return MallCampaignType.Coupon;
        }

        throw new BusinessException(400, "本期仅支持商品推荐型或领券型活动");
    }

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

    public static string NormalizeCoverImageUrl(string? coverImageUrl)
    {
        var normalized = coverImageUrl?.Trim() ?? string.Empty;

        if (normalized.Length > CoverImageUrlMaxLength)
        {
            throw new BusinessException(400, $"头图地址不能超过 {CoverImageUrlMaxLength} 个字符");
        }

        return normalized;
    }

    public static string NormalizeContentHtml(string? contentHtml)
    {
        var normalized = contentHtml ?? string.Empty;

        if (normalized.Length > ContentHtmlMaxLength)
        {
            throw new BusinessException(400, $"图文介绍不能超过 {ContentHtmlMaxLength} 个字符");
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

    /// <summary>
    /// 校验活动时间：开始/结束时间必填，且结束时间必须晚于开始时间（左闭右开）。
    /// </summary>
    public static void ValidateTimeWindow(DateTime startAt, DateTime endAt)
    {
        if (startAt == default)
        {
            throw new BusinessException(400, "请选择活动开始时间");
        }

        if (endAt == default)
        {
            throw new BusinessException(400, "请选择活动结束时间");
        }

        if (endAt <= startAt)
        {
            throw new BusinessException(400, "活动结束时间必须晚于开始时间");
        }
    }

    /// <summary>
    /// 校验选品：至少一个商品、商品不可重复、分组名长度与排序值合法。
    /// </summary>
    public static List<MallCampaignProductRequest> NormalizeItems(IEnumerable<MallCampaignProductRequest>? items)
    {
        var source = items?.ToList() ?? new List<MallCampaignProductRequest>();

        if (source.Count == 0)
        {
            throw new BusinessException(400, "请至少选择一个商品");
        }

        return NormalizeItemsCore(source, requireAny: true);
    }

    /// <summary>
    /// 领券型活动不使用商品选品，允许为空；若传入则按同一规则校验。
    /// </summary>
    public static List<MallCampaignProductRequest> NormalizeOptionalItems(
        IEnumerable<MallCampaignProductRequest>? items)
    {
        var source = items?.ToList() ?? new List<MallCampaignProductRequest>();

        return source.Count == 0
            ? new List<MallCampaignProductRequest>()
            : NormalizeItemsCore(source, requireAny: false);
    }

    /// <summary>
    /// 校验领券型活动绑定的券：至少一张、不可重复、排序值合法。
    /// </summary>
    public static List<Guid> NormalizeCouponIds(IEnumerable<Guid>? couponIds, bool required)
    {
        var source = couponIds?.ToList() ?? new List<Guid>();

        if (required && source.Count == 0)
        {
            throw new BusinessException(400, "请至少选择一张优惠券");
        }

        var normalized = new List<Guid>();
        var seen = new HashSet<Guid>();

        foreach (var couponId in source)
        {
            if (couponId == Guid.Empty)
            {
                throw new BusinessException(400, "选择的优惠券中存在无效的券");
            }

            if (!seen.Add(couponId))
            {
                throw new BusinessException(400, "同一张优惠券不能重复绑定");
            }

            normalized.Add(couponId);
        }

        return normalized;
    }

    /// <summary>
    /// 校验券均存在且处于启用状态。
    /// </summary>
    public static async Task ValidateCouponsExistAsync(
        IMallDbContext dbContext,
        IEnumerable<Guid> couponIds,
        CancellationToken cancellationToken)
    {
        var ids = couponIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return;
        }

        var validCount = await dbContext.MallCoupons
            .AsNoTracking()
            .CountAsync(coupon => ids.Contains(coupon.Id) && coupon.IsActive, cancellationToken);

        if (validCount != ids.Count)
        {
            throw new BusinessException(400, "选择的优惠券中存在不存在或已停用的券");
        }
    }

    private static List<MallCampaignProductRequest> NormalizeItemsCore(
        List<MallCampaignProductRequest> source,
        bool requireAny)
    {
        if (requireAny && source.Count == 0)
        {
            throw new BusinessException(400, "请至少选择一个商品");
        }

        var normalized = new List<MallCampaignProductRequest>(source.Count);
        var productIds = new HashSet<Guid>();

        foreach (var item in source)
        {
            if (item.ProductId == Guid.Empty)
            {
                throw new BusinessException(400, "选品中存在无效的商品");
            }

            if (!productIds.Add(item.ProductId))
            {
                throw new BusinessException(400, "同一个商品不能重复加入活动");
            }

            var groupName = item.GroupName?.Trim() ?? string.Empty;
            if (groupName.Length > GroupNameMaxLength)
            {
                throw new BusinessException(400, $"分组名不能超过 {GroupNameMaxLength} 个字符");
            }

            normalized.Add(new MallCampaignProductRequest
            {
                ProductId = item.ProductId,
                GroupName = groupName,
                SortOrder = NormalizeSortOrder(item.SortOrder)
            });
        }

        return normalized;
    }

    /// <summary>
    /// 校验选品中的商品均存在（未被删除）；已下架商品允许保留在专题中。
    /// </summary>
    public static async Task ValidateProductsExistAsync(
        IMallDbContext dbContext,
        IEnumerable<MallCampaignProductRequest> items,
        CancellationToken cancellationToken)
    {
        var productIds = items.Select(item => item.ProductId).Distinct().ToList();

        var existingCount = await dbContext.MallProducts
            .AsNoTracking()
            .CountAsync(product => productIds.Contains(product.Id), cancellationToken);

        if (existingCount != productIds.Count)
        {
            throw new BusinessException(400, "选品中存在不存在或已删除的商品");
        }
    }
}

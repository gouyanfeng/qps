using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;

namespace QPS.Mall.Application.Features.MallCampaigns;

/// <summary>
/// 活动专题 DTO 映射。
/// </summary>
internal static class MallCampaignMapper
{
    public static MallCampaignListItemDto ToListItem(MallCampaign campaign, int productCount, DateTime now)
    {
        return new MallCampaignListItemDto
        {
            Id = campaign.Id,
            Type = campaign.Type,
            Name = campaign.Name,
            CoverImageUrl = campaign.CoverImageUrl,
            StartAt = campaign.StartAt,
            EndAt = campaign.EndAt,
            Status = MallCampaignStatus.Resolve(campaign.StartAt, campaign.EndAt, now),
            IsActive = campaign.IsActive,
            SortOrder = campaign.SortOrder,
            ProductCount = productCount,
            CreatedAt = campaign.CreatedAt,
            UpdatedAt = campaign.UpdatedAt
        };
    }

    public static MallCampaignDto ToDetail(
        MallCampaign campaign,
        IReadOnlyCollection<MallCampaignProductItemDto> items,
        IReadOnlyCollection<MallCampaignCouponDto> coupons,
        DateTime now)
    {
        return new MallCampaignDto
        {
            Id = campaign.Id,
            Type = campaign.Type,
            Name = campaign.Name,
            CoverImageUrl = campaign.CoverImageUrl,
            ContentHtml = campaign.ContentHtml,
            StartAt = campaign.StartAt,
            EndAt = campaign.EndAt,
            Status = MallCampaignStatus.Resolve(campaign.StartAt, campaign.EndAt, now),
            IsActive = campaign.IsActive,
            SortOrder = campaign.SortOrder,
            CreatedAt = campaign.CreatedAt,
            UpdatedAt = campaign.UpdatedAt,
            Items = items.ToList(),
            Coupons = coupons.ToList()
        };
    }

    public static MallCampaignCouponDto ToCouponDto(MallCampaignCoupon link, MallCoupon coupon, int sortOrder)
    {
        return new MallCampaignCouponDto
        {
            Id = link.Id,
            CouponId = coupon.Id,
            Name = coupon.Name,
            Code = coupon.Code,
            CouponType = coupon.CouponType,
            ThresholdAmount = coupon.ThresholdAmount,
            DiscountAmount = coupon.DiscountAmount,
            MaxDiscount = coupon.MaxDiscount,
            TotalCount = coupon.TotalCount,
            RemainCount = coupon.RemainCount,
            PerLimit = coupon.PerLimit,
            ValidDays = coupon.ValidDays,
            StartAt = coupon.StartAt,
            EndAt = coupon.EndAt,
            ScopeType = coupon.ScopeType,
            IsActive = coupon.IsActive,
            IsClaimable = coupon.CanIssue(),
            SortOrder = sortOrder
        };
    }

    public static MallCampaignPublicListItemDto ToPublicListItem(MallCampaign campaign, DateTime now)
    {
        return new MallCampaignPublicListItemDto
        {
            Id = campaign.Id,
            Type = campaign.Type,
            Name = campaign.Name,
            CoverImageUrl = campaign.CoverImageUrl,
            StartAt = campaign.StartAt,
            EndAt = campaign.EndAt,
            Status = MallCampaignStatus.Resolve(campaign.StartAt, campaign.EndAt, now),
            SortOrder = campaign.SortOrder
        };
    }

    public static MallCampaignPublicDetailDto ToPublicDetail(
        MallCampaign campaign,
        IReadOnlyCollection<MallCampaignCouponDto> coupons,
        DateTime now)
    {
        return new MallCampaignPublicDetailDto
        {
            Id = campaign.Id,
            Type = campaign.Type,
            Name = campaign.Name,
            CoverImageUrl = campaign.CoverImageUrl,
            ContentHtml = campaign.ContentHtml,
            StartAt = campaign.StartAt,
            EndAt = campaign.EndAt,
            Status = MallCampaignStatus.Resolve(campaign.StartAt, campaign.EndAt, now),
            Coupons = coupons.ToList()
        };
    }
}

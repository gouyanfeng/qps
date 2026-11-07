using QPS.Shared.Common;

namespace QPS.Mall.Domain.Entities;

/// <summary>
/// 活动专题绑定的优惠券（领券型活动使用），按 SortOrder 决定展示顺序。
/// </summary>
public class MallCampaignCoupon : BaseEntity
{
    public Guid CampaignId { get; private set; }

    public Guid CouponId { get; private set; }

    public int SortOrder { get; private set; }

    private MallCampaignCoupon()
    {
    }

    private MallCampaignCoupon(Guid campaignId, Guid couponId, int sortOrder)
    {
        CampaignId = campaignId;
        CouponId = couponId;
        SortOrder = sortOrder;
    }

    public static MallCampaignCoupon Create(Guid campaignId, Guid couponId, int sortOrder)
    {
        return new MallCampaignCoupon(campaignId, couponId, sortOrder);
    }
}

using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;

namespace QPS.Mall.Application.Features.MallCampaigns;

/// <summary>
/// 活动专题绑定券的装载。
/// </summary>
internal static class MallCampaignCouponLoader
{
    public static async Task<List<MallCampaignCouponDto>> LoadAsync(
        IMallDbContext dbContext,
        Guid campaignId,
        CancellationToken cancellationToken)
    {
        var links = await dbContext.MallCampaignCoupons
            .AsNoTracking()
            .Where(item => item.CampaignId == campaignId)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.CreatedAt)
            .ToListAsync(cancellationToken);

        if (links.Count == 0)
        {
            return new List<MallCampaignCouponDto>();
        }

        var couponIds = links.Select(link => link.CouponId).Distinct().ToList();

        var coupons = await dbContext.MallCoupons
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(coupon => couponIds.Contains(coupon.Id))
            .ToListAsync(cancellationToken);

        var couponMap = coupons.Where(coupon => !coupon.IsDeleted).ToDictionary(coupon => coupon.Id);

        var result = new List<MallCampaignCouponDto>(links.Count);
        for (var index = 0; index < links.Count; index++)
        {
            var coupon = couponMap.GetValueOrDefault(links[index].CouponId);
            if (coupon is null)
            {
                continue;
            }

            result.Add(MallCampaignMapper.ToCouponDto(links[index], coupon, index));
        }

        return result;
    }
}

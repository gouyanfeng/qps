using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Common;

namespace QPS.Mall.Application.Features.MallCampaigns;

/// <summary>
/// 管理端选择券时的候选券分页查询（仅启用中的券）。
/// </summary>
public class GetMallCampaignCouponOptionsQuery : PaginationRequest,
    IRequest<PaginationResponse<MallCampaignCouponOptionDto>>
{
    public string? Keyword { get; set; }
}

public class GetMallCampaignCouponOptionsHandler
    : IRequestHandler<GetMallCampaignCouponOptionsQuery, PaginationResponse<MallCampaignCouponOptionDto>>
{
    private readonly IMallDbContext _dbContext;

    public GetMallCampaignCouponOptionsHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginationResponse<MallCampaignCouponOptionDto>> Handle(
        GetMallCampaignCouponOptionsQuery request,
        CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 10 : request.PageSize;

        var query = _dbContext.MallCoupons
            .AsNoTracking()
            .Where(coupon => coupon.IsActive);

        var keyword = request.Keyword?.Trim();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(coupon =>
                coupon.Name.Contains(keyword) || coupon.Code.Contains(keyword));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var coupons = await query
            .OrderByDescending(coupon => coupon.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(coupon => new MallCampaignCouponOptionDto
            {
                Id = coupon.Id,
                Name = coupon.Name,
                Code = coupon.Code,
                CouponType = coupon.CouponType,
                ThresholdAmount = coupon.ThresholdAmount,
                DiscountAmount = coupon.DiscountAmount,
                MaxDiscount = coupon.MaxDiscount,
                TotalCount = coupon.TotalCount,
                RemainCount = coupon.RemainCount,
                PerLimit = coupon.PerLimit,
                ScopeType = coupon.ScopeType,
                IsActive = coupon.IsActive
            })
            .ToListAsync(cancellationToken);

        return new PaginationResponse<MallCampaignCouponOptionDto>(coupons, totalCount, page, pageSize);
    }
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs.MallCoupons;

namespace QPS.Mall.Application.Features.MallCoupons;

public class GetAvailableMallCouponsQuery : IRequest<List<MallCouponDto>>
{
    public Guid? MemberId { get; set; }
}

public class GetAvailableMallCouponsHandler : IRequestHandler<GetAvailableMallCouponsQuery, List<MallCouponDto>>
{
    private readonly IMallDbContext _db;
    public GetAvailableMallCouponsHandler(IMallDbContext db) => _db = db;

    public async Task<List<MallCouponDto>> Handle(GetAvailableMallCouponsQuery req, CancellationToken ct)
    {
        var now = DateTime.Now;

        var query = _db.MallCoupons
            .AsNoTracking()
            .Where(c => c.IsActive
                && (c.StartAt == null || c.StartAt <= now)
                && (c.EndAt == null || c.EndAt >= now)
                && c.RemainCount > 0);

        if (req.MemberId.HasValue)
        {
            var claimedIds = await _db.MallMemberCoupons
                .AsNoTracking()
                .Where(mc => mc.MemberId == req.MemberId.Value)
                .Select(mc => mc.CouponId)
                .Distinct()
                .ToListAsync(ct);

            query = query.Where(c => !claimedIds.Contains(c.Id));
        }

        return await query
            .OrderByDescending(c => c.DiscountAmount)
            .Select(c => new MallCouponDto
            {
                Id = c.Id,
                Code = c.Code,
                Name = c.Name,
                CouponType = c.CouponType,
                ThresholdAmount = c.ThresholdAmount,
                DiscountAmount = c.DiscountAmount,
                TotalCount = c.TotalCount,
                RemainCount = c.RemainCount,
                PerLimit = c.PerLimit,
                ValidDays = c.ValidDays,
                StartAt = c.StartAt,
                EndAt = c.EndAt,
                ScopeType = c.ScopeType,
                ScopeValue = c.ScopeValue,
                IsActive = c.IsActive
            })
            .ToListAsync(ct);
    }
}
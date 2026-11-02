using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs.MallCoupons;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallMemberCoupons;

public class GetMallMemberCouponsQuery : IRequest<List<MallMemberCouponDto>>
{
    public MallMemberCouponQueryRequest Request { get; set; } = null!;
}

public class GetMallMemberCouponCountQuery : IRequest<int>
{
    public Guid MemberId { get; set; }
}

public class UseMallMemberCouponCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
}

public class MallMemberCouponHandlers :
    IRequestHandler<GetMallMemberCouponsQuery, List<MallMemberCouponDto>>,
    IRequestHandler<GetMallMemberCouponCountQuery, int>,
    IRequestHandler<UseMallMemberCouponCommand, bool>
{
    private readonly IMallDbContext _db;

    public MallMemberCouponHandlers(IMallDbContext db) => _db = db;

    public async Task<List<MallMemberCouponDto>> Handle(GetMallMemberCouponsQuery req, CancellationToken ct)
    {
        var r = req.Request;
        var q = from mc in _db.MallMemberCoupons.AsNoTracking()
                join c in _db.MallCoupons.AsNoTracking() on mc.CouponId equals c.Id
                where mc.MemberId == r.MemberId
                select new MallMemberCouponDto
                {
                    Id = mc.Id, CouponId = mc.CouponId, MemberId = mc.MemberId,
                    CouponCode = mc.CouponCode, CouponName = c.Name,
                    ThresholdAmount = c.ThresholdAmount, DiscountAmount = c.DiscountAmount,
                    Status = mc.Status, ExpireAt = mc.ExpireAt,
                    ClaimedAt = mc.ClaimedAt, UsedAt = mc.UsedAt, UsedOrderNo = mc.UsedOrderNo
                };

        if (!string.IsNullOrWhiteSpace(r.Status)) q = q.Where(x => x.Status == r.Status);
        return await q.OrderByDescending(x => x.ClaimedAt).ToListAsync(ct);
    }

    public Task<int> Handle(GetMallMemberCouponCountQuery req, CancellationToken ct)
    {
        return _db.MallMemberCoupons
            .CountAsync(coupon => coupon.MemberId == req.MemberId && coupon.Status == "claimed", ct);
    }

    public async Task<bool> Handle(UseMallMemberCouponCommand req, CancellationToken ct)
    {
        var mc = await _db.MallMemberCoupons.FirstOrDefaultAsync(x => x.Id == req.Id, ct);
        if (mc == null) throw new BusinessException(404, "券不存在");
        mc.Use(req.OrderNo);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
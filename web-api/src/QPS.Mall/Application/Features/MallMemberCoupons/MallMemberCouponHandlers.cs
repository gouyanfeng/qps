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

public class GetAvailableMallCouponsQuery : IRequest<List<MallCouponDto>>
{
    public Guid MemberId { get; set; }
    public Guid StoreId { get; set; }
}

public class UseMallMemberCouponCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
}

public class MallMemberCouponHandlers :
    IRequestHandler<GetMallMemberCouponsQuery, List<MallMemberCouponDto>>,
    IRequestHandler<GetAvailableMallCouponsQuery, List<MallCouponDto>>,
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

    public async Task<List<MallCouponDto>> Handle(GetAvailableMallCouponsQuery req, CancellationToken ct)
    {
        return await _db.MallCoupons.AsNoTracking()
            .Where(c => c.StoreId == req.StoreId && c.IsActive)
            .Where(c => c.StartAt == null || DateTime.Now >= c.StartAt)
            .Where(c => c.EndAt == null || DateTime.Now <= c.EndAt)
            .Select(c => new MallCouponDto
            {
                Id = c.Id, StoreId = c.StoreId, Code = c.Code, Name = c.Name,
                CouponType = c.CouponType, ThresholdAmount = c.ThresholdAmount,
                DiscountAmount = c.DiscountAmount, MaxDiscount = c.MaxDiscount,
                TotalCount = c.TotalCount, RemainCount = c.RemainCount,
                PerLimit = c.PerLimit, ValidDays = c.ValidDays,
                StartAt = c.StartAt, EndAt = c.EndAt,
                ScopeType = c.ScopeType, ScopeValue = c.ScopeValue, IsActive = c.IsActive
            })
            .ToListAsync(ct);
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
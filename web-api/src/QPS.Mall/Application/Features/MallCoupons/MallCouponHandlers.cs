using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs.MallCoupons;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallCoupons;

public class QueryMallCouponsCommand : IRequest<(List<MallCouponDto> Items, int Total)>
{
    public MallCouponQueryRequest Request { get; set; } = null!;
}

public class GetMallCouponQuery : IRequest<MallCouponDto>
{
    public Guid Id { get; set; }
}

public class CreateMallCouponCommand : IRequest<MallCouponDto>
{
    public MallCouponCreateRequest Request { get; set; } = null!;
}

public class UpdateMallCouponCommand : IRequest<MallCouponDto>
{
    public Guid Id { get; set; }
    public MallCouponUpdateRequest Request { get; set; } = null!;
}

public class DeleteMallCouponCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}

public class ChangeMallCouponStatusCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public bool IsActive { get; set; }
}

public class ReplenishMallCouponCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public int Count { get; set; }
}

public class ClaimMallCouponCommand : IRequest<MallMemberCouponDto>
{
    public Guid CouponId { get; set; }
    public Guid MemberId { get; set; }
}

public class MallCouponHandlers :
    IRequestHandler<QueryMallCouponsCommand, (List<MallCouponDto> Items, int Total)>,
    IRequestHandler<GetMallCouponQuery, MallCouponDto>,
    IRequestHandler<CreateMallCouponCommand, MallCouponDto>,
    IRequestHandler<UpdateMallCouponCommand, MallCouponDto>,
    IRequestHandler<DeleteMallCouponCommand, bool>,
    IRequestHandler<ChangeMallCouponStatusCommand, bool>,
    IRequestHandler<ReplenishMallCouponCommand, bool>,
    IRequestHandler<ClaimMallCouponCommand, MallMemberCouponDto>
{
    private readonly IMallDbContext _db;

    public MallCouponHandlers(IMallDbContext db) => _db = db;

    public async Task<(List<MallCouponDto> Items, int Total)> Handle(QueryMallCouponsCommand req, CancellationToken ct)
    {
        var r = req.Request;
        var q = _db.MallCoupons.AsNoTracking();
        if (r.StoreId.HasValue) q = q.Where(x => x.StoreId == r.StoreId);
        if (!string.IsNullOrWhiteSpace(r.CouponType)) q = q.Where(x => x.CouponType == r.CouponType);
        if (r.IsActive.HasValue) q = q.Where(x => x.IsActive == r.IsActive);
        if (!string.IsNullOrWhiteSpace(r.Keyword))
        {
            var kw = r.Keyword;
            q = q.Where(x => x.Code.Contains(kw) || x.Name.Contains(kw));
        }
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(x => x.CreatedAt)
            .Skip((r.PageIndex - 1) * r.PageSize).Take(r.PageSize)
            .Select(x => ToDto(x)).ToListAsync(ct);
        return (items, total);
    }

    public async Task<MallCouponDto> Handle(GetMallCouponQuery req, CancellationToken ct)
    {
        var c = await _db.MallCoupons.AsNoTracking().FirstOrDefaultAsync(x => x.Id == req.Id, ct);
        if (c == null) throw new BusinessException(404, "券不存在");
        return ToDto(c);
    }

    public async Task<MallCouponDto> Handle(CreateMallCouponCommand req, CancellationToken ct)
    {
        var r = req.Request;
        var exists = await _db.MallCoupons
            .AnyAsync(x => x.StoreId == r.StoreId && x.Code == r.Code, ct);
        if (exists) throw new BusinessException(400, "该门店已存在同编码的券");

        var coupon = MallCoupon.Create(r.StoreId, r.Code, r.Name, r.CouponType,
            r.ThresholdAmount, r.DiscountAmount, r.MaxDiscount,
            r.TotalCount, r.PerLimit, r.ValidDays,
            r.StartAt, r.EndAt, r.ScopeType, r.ScopeValue);
        _db.MallCoupons.Add(coupon);
        await _db.SaveChangesAsync(ct);
        return ToDto(coupon);
    }

    public async Task<MallCouponDto> Handle(UpdateMallCouponCommand req, CancellationToken ct)
    {
        var c = await _db.MallCoupons.FirstOrDefaultAsync(x => x.Id == req.Id, ct);
        if (c == null) throw new BusinessException(404, "券不存在");
        var r = req.Request;
        c.Update(r.Name, r.ThresholdAmount, r.DiscountAmount, r.MaxDiscount,
            r.PerLimit, r.ValidDays, r.StartAt, r.EndAt, r.ScopeType, r.ScopeValue);
        await _db.SaveChangesAsync(ct);
        return ToDto(c);
    }

    public async Task<bool> Handle(DeleteMallCouponCommand req, CancellationToken ct)
    {
        var c = await _db.MallCoupons.FirstOrDefaultAsync(x => x.Id == req.Id, ct);
        if (c == null) throw new BusinessException(404, "券不存在");
        _db.MallCoupons.Remove(c);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> Handle(ChangeMallCouponStatusCommand req, CancellationToken ct)
    {
        var c = await _db.MallCoupons.FirstOrDefaultAsync(x => x.Id == req.Id, ct);
        if (c == null) throw new BusinessException(404, "券不存在");
        if (req.IsActive) c.Activate(); else c.Deactivate();
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> Handle(ReplenishMallCouponCommand req, CancellationToken ct)
    {
        var c = await _db.MallCoupons.FirstOrDefaultAsync(x => x.Id == req.Id, ct);
        if (c == null) throw new BusinessException(404, "券不存在");
        c.Replenish(req.Count);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<MallMemberCouponDto> Handle(ClaimMallCouponCommand req, CancellationToken ct)
    {
        var coupon = await _db.MallCoupons.FirstOrDefaultAsync(x => x.Id == req.CouponId, ct);
        if (coupon == null) throw new BusinessException(404, "券不存在");
        if (!coupon.CanIssue()) throw new BusinessException(400, "券不可领取（已售罄或未开始/已结束）");

        var member = await _db.MallMembers.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == req.MemberId, ct);
        if (member == null) throw new BusinessException(404, "会员不存在");

        var claimCount = await _db.MallMemberCoupons
            .CountAsync(x => x.CouponId == req.CouponId && x.MemberId == req.MemberId, ct);
        if (claimCount >= coupon.PerLimit)
            throw new BusinessException(400, $"每人限领{coupon.PerLimit}张");

        coupon.IssueOne();

        var code = $"CP{DateTime.Now:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";
        var expireAt = DateTime.Now.AddDays(coupon.ValidDays);
        var mc = MallMemberCoupon.Issue(coupon.Id, req.MemberId, code, expireAt);
        _db.MallMemberCoupons.Add(mc);
        await _db.SaveChangesAsync(ct);

        return new MallMemberCouponDto
        {
            Id = mc.Id, CouponId = mc.CouponId, MemberId = mc.MemberId,
            CouponCode = mc.CouponCode, CouponName = coupon.Name,
            ThresholdAmount = coupon.ThresholdAmount, DiscountAmount = coupon.DiscountAmount,
            Status = mc.Status, ExpireAt = mc.ExpireAt,
            ClaimedAt = mc.ClaimedAt, UsedAt = mc.UsedAt, UsedOrderNo = mc.UsedOrderNo
        };
    }

    private static MallCouponDto ToDto(MallCoupon c) => new()
    {
        Id = c.Id, StoreId = c.StoreId, Code = c.Code, Name = c.Name,
        CouponType = c.CouponType, ThresholdAmount = c.ThresholdAmount,
        DiscountAmount = c.DiscountAmount, MaxDiscount = c.MaxDiscount,
        TotalCount = c.TotalCount, RemainCount = c.RemainCount,
        PerLimit = c.PerLimit, ValidDays = c.ValidDays,
        StartAt = c.StartAt, EndAt = c.EndAt,
        ScopeType = c.ScopeType, ScopeValue = c.ScopeValue, IsActive = c.IsActive
    };
}
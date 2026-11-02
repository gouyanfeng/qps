using QPS.Shared.Common;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Domain.Entities;

public class MallMemberCoupon : BaseEntity
{
    public Guid CouponId { get; private set; }
    public Guid MemberId { get; private set; }
    public string CouponCode { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public DateTime ExpireAt { get; private set; }
    public DateTime? ClaimedAt { get; private set; }
    public DateTime? UsedAt { get; private set; }
    public string? UsedOrderNo { get; private set; }

    private MallMemberCoupon()
    {
    }

    private MallMemberCoupon(Guid couponId, Guid memberId, string couponCode, DateTime expireAt)
    {
        CouponId = couponId;
        MemberId = memberId;
        CouponCode = couponCode;
        ExpireAt = expireAt;
        Status = "claimed";
        ClaimedAt = DateTime.Now;
    }

    public static MallMemberCoupon Issue(Guid couponId, Guid memberId, string couponCode, DateTime expireAt)
    {
        return new MallMemberCoupon(couponId, memberId, couponCode, expireAt);
    }

    public bool IsAvailable()
    {
        if (Status != "claimed") return false;
        if (ExpireAt < DateTime.Now) return false;
        return true;
    }

    public void Use(string orderNo)
    {
        if (!IsAvailable()) throw new BusinessException(400, "券不可用");
        Status = "used";
        UsedAt = DateTime.Now;
        UsedOrderNo = orderNo;
    }

    public void Expire()
    {
        if (Status == "claimed") Status = "expired";
    }
}
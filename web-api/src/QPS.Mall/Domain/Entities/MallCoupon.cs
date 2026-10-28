using QPS.Shared.Common;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Domain.Entities;

public class MallCoupon : BaseEntity
{
    public Guid StoreId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string CouponType { get; private set; } = string.Empty;
    public decimal ThresholdAmount { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal? MaxDiscount { get; private set; }
    public int TotalCount { get; private set; }
    public int RemainCount { get; private set; }
    public int PerLimit { get; private set; }
    public int ValidDays { get; private set; }
    public DateTime? StartAt { get; private set; }
    public DateTime? EndAt { get; private set; }
    public string ScopeType { get; private set; } = "all";
    public string? ScopeValue { get; private set; }
    public bool IsActive { get; private set; }

    private MallCoupon()
    {
    }

    private MallCoupon(
        Guid storeId, string code, string name, string couponType,
        decimal thresholdAmount, decimal discountAmount, decimal? maxDiscount,
        int totalCount, int perLimit, int validDays,
        DateTime? startAt, DateTime? endAt,
        string scopeType, string? scopeValue)
    {
        StoreId = storeId;
        Code = code;
        Name = name;
        CouponType = couponType;
        ThresholdAmount = thresholdAmount;
        DiscountAmount = discountAmount;
        MaxDiscount = maxDiscount;
        TotalCount = totalCount;
        RemainCount = totalCount;
        PerLimit = perLimit;
        ValidDays = validDays;
        StartAt = startAt;
        EndAt = endAt;
        ScopeType = scopeType;
        ScopeValue = scopeValue;
        IsActive = true;
    }

    public static MallCoupon Create(
        Guid storeId, string code, string name, string couponType,
        decimal thresholdAmount, decimal discountAmount, decimal? maxDiscount,
        int totalCount, int perLimit, int validDays,
        DateTime? startAt, DateTime? endAt,
        string scopeType, string? scopeValue)
    {
        return new MallCoupon(storeId, code, name, couponType,
            thresholdAmount, discountAmount, maxDiscount,
            totalCount, perLimit, validDays,
            startAt, endAt, scopeType, scopeValue);
    }

    public void Update(
        string name, decimal thresholdAmount, decimal discountAmount,
        decimal? maxDiscount, int perLimit, int validDays,
        DateTime? startAt, DateTime? endAt,
        string scopeType, string? scopeValue)
    {
        Name = name;
        ThresholdAmount = thresholdAmount;
        DiscountAmount = discountAmount;
        MaxDiscount = maxDiscount;
        PerLimit = perLimit;
        ValidDays = validDays;
        StartAt = startAt;
        EndAt = endAt;
        ScopeType = scopeType;
        ScopeValue = scopeValue;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    public bool CanIssue()
    {
        return IsActive && RemainCount > 0
            && (StartAt == null || DateTime.Now >= StartAt)
            && (EndAt == null || DateTime.Now <= EndAt);
    }

    public void IssueOne()
    {
        if (!CanIssue()) throw new BusinessException(400, "券已售罄或未开始/已结束");
        RemainCount -= 1;
    }

    public void Replenish(int count)
    {
        if (count <= 0) throw new BusinessException(400, "补货数量必须大于0");
        RemainCount += count;
        TotalCount += count;
    }

    public decimal CalculateDiscount(decimal orderAmount)
    {
        if (orderAmount < ThresholdAmount) return 0;

        decimal discount = CouponType switch
        {
            "fixed" => DiscountAmount,
            "percent" => Math.Round(orderAmount * DiscountAmount, 2),
            _ => 0
        };

        if (MaxDiscount.HasValue && discount > MaxDiscount.Value)
            discount = MaxDiscount.Value;

        return discount;
    }
}
using QPS.Shared.Exceptions;

namespace QPS.Mall.Domain.Services;

/// <summary>
/// 订单价格拆分：应付 = max(0, 商品额 − 促销优惠 − 券抵扣) + 运费。
/// 应付金额的唯一口径，购物车预览、结算预览、下单落库与订单金额校验均从此出，
/// 避免同一公式散落在多个 handler 中出现口径漂移。
/// 购物车阶段无券、无运费，对应字段传 0 即可。
/// </summary>
public sealed class PriceBreakdown
{
    /// <summary>商品原价合计。</summary>
    public decimal GoodsAmount { get; }

    /// <summary>促销优惠金额。</summary>
    public decimal PromotionDiscountAmount { get; }

    /// <summary>优惠券抵扣金额；购物车预览无券时传 0。</summary>
    public decimal CouponDiscountAmount { get; }

    /// <summary>运费金额；购物车预览未选地址时传 0。</summary>
    public decimal FreightAmount { get; }

    /// <summary>促销与券抵扣后的商品金额，封底为 0。</summary>
    public decimal DiscountedGoodsAmount { get; }

    /// <summary>应付金额 = 封底后商品金额 + 运费。</summary>
    public decimal PayableAmount { get; }

    public PriceBreakdown(
        decimal goodsAmount,
        decimal promotionDiscountAmount,
        decimal couponDiscountAmount = 0,
        decimal freightAmount = 0)
    {
        if (goodsAmount < 0 || promotionDiscountAmount < 0 || couponDiscountAmount < 0 || freightAmount < 0)
        {
            throw new BusinessException(400, "订单金额不能为负数");
        }

        GoodsAmount = goodsAmount;
        PromotionDiscountAmount = promotionDiscountAmount;
        CouponDiscountAmount = couponDiscountAmount;
        FreightAmount = freightAmount;
        DiscountedGoodsAmount = Math.Max(0, goodsAmount - promotionDiscountAmount - couponDiscountAmount);
        PayableAmount = DiscountedGoodsAmount + freightAmount;
    }

    /// <summary>
    /// 金额恒等式校验：外部传入的应付金额必须与本拆分算出的结果一致。
    /// 下单时与订单查询时共用，口径不一致直接暴露错误，不静默展示。
    /// </summary>
    public void EnsurePayableMatches(decimal payableAmount, string orderNo)
    {
        if (Math.Abs(PayableAmount - payableAmount) <= 0.01m) return;

        throw new BusinessException(
            500,
            $"订单 {orderNo} 金额不自洽：商品 {GoodsAmount:0.00} - 促销 {PromotionDiscountAmount:0.00} - 券 {CouponDiscountAmount:0.00} + 运费 {FreightAmount:0.00} = {PayableAmount:0.00}，实际应付 {payableAmount:0.00}");
    }
}

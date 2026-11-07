/**
 * 优惠券类型统一映射
 * 后端 MallCoupon.CouponType 仅有两个取值：fixed（固定金额/满减）、percent（折扣）
 */
export function isPercentCoupon(type?: string | null): boolean {
  return type === 'percent'
}

export function couponTypeLabel(type?: string | null): string {
  if (type === 'percent') return '折扣券'
  if (type === 'fixed') return '满减券'
  return '优惠券'
}

/** 折扣率文案：后端 percent 存的是减免比例（0.1 表示立减 10%），换算为折扣：0.1 -> 9折，0.15 -> 8.5折 */
export function couponDiscountRateText(amount: number | string): string {
  const rate = (1 - Number(amount)) * 10
  const text = Number.isFinite(rate) ? rate.toFixed(1).replace(/\.0$/, '') : '0'
  return `${text}折`
}

/** 券面额文案：满减券 ¥10，折扣券 9折 */
export function couponFaceValueText(type: string | null | undefined, amount: number | string): string {
  return isPercentCoupon(type) ? couponDiscountRateText(amount) : `¥${Number(amount).toFixed(2)}`
}

/** 适用范围文案 */
export function couponScopeText(scopeType?: string | null): string {
  if (scopeType === 'product') return '指定商品'
  if (scopeType === 'category') return '指定分类'
  return '全场'
}

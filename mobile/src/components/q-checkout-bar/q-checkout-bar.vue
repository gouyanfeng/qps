<template>
  <view class="q-checkout-bar-wrap">
    <!-- 常驻结算栏：应付在上、合计在下 + 明细入口 + 页面自定义操作按钮 -->
    <view class="checkout-bar" :style="{ bottom: bottomOffset }">
      <view class="bar-left">
        <view class="bar-pay-line">
          <text class="bar-pay-label">应付</text>
          <view class="bar-amount">
            <text class="bar-yen">¥</text>
            <text class="bar-amount-int">{{ payableInteger }}</text>
            <text class="bar-amount-dec">.{{ payableDecimal }}</text>
          </view>
          <view class="bar-detail-btn" @tap="drawerVisible = true">
            <text class="bar-detail-text">明细</text>
            <q-icon name="chevron-down" size="22" color="#71746E" class="bar-detail-icon" />
          </view>
        </view>
        <view class="bar-total-line">
          <text class="bar-total-label">合计</text>
          <text class="bar-total-origin">¥{{ goodsAmount.toFixed(2) }}</text>
          <text v-if="savedAmount > 0" class="bar-saved">(已省¥{{ savedAmount.toFixed(2) }})</text>
        </view>
      </view>
      <slot name="action" />
    </view>

    <!-- 价格明细抽屉 -->
    <view v-if="drawerVisible" class="detail-mask" @tap="drawerVisible = false">
      <view class="detail-sheet" @tap.stop>
        <view class="detail-head">
          <text class="detail-title">价格明细</text>
          <view class="detail-close" @tap="drawerVisible = false">
            <q-icon name="close" size="32" color="#71746E" />
          </view>
        </view>
        <view class="detail-body">
          <view class="detail-row">
            <text class="detail-label">商品金额</text>
            <text class="detail-value">¥{{ goodsAmount.toFixed(2) }}</text>
          </view>
          <!-- freightAmount 为 null 时不展示运费行（购物车接口不计算运费） -->
          <view v-if="freightAmount !== null" class="detail-row">
            <view class="detail-label-group">
              <text class="detail-label">运费</text>
              <text v-if="freightAmount === 0" class="detail-note">（免运费）</text>
            </view>
            <text class="detail-value" :class="{ 'detail-free': freightAmount === 0 }">
              ¥{{ freightAmount.toFixed(2) }}
            </text>
          </view>
          <view v-if="promotionDiscount > 0" class="detail-row">
            <text class="detail-label">促销优惠</text>
            <text class="detail-value detail-neg">-¥{{ promotionDiscount.toFixed(2) }}</text>
          </view>
          <view v-if="couponDiscount > 0" class="detail-row">
            <text class="detail-label">优惠券{{ couponName ? `（${couponName}）` : '' }}</text>
            <text class="detail-value detail-neg">-¥{{ couponDiscount.toFixed(2) }}</text>
          </view>
          <view class="detail-divider"></view>
          <view class="detail-total-row">
            <text class="detail-total-label">合计应付</text>
            <view class="detail-total-right">
              <view class="detail-amount">
                <text class="bar-yen">¥</text>
                <text class="bar-amount-int">{{ payableInteger }}</text>
                <text class="bar-amount-dec">.{{ payableDecimal }}</text>
              </view>
              <text v-if="savedAmount > 0" class="detail-saved">(已省¥{{ savedAmount.toFixed(2) }})</text>
            </view>
          </view>
        </view>
      </view>
    </view>
  </view>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'

const props = withDefaults(defineProps<{
  /** 后端返回的应付金额 */
  payableAmount: number
  /** 后端返回的选中商品原价合计 */
  goodsAmount: number
  /** 后端返回的促销优惠金额 */
  promotionDiscount?: number
  /** 后端返回的优惠券抵扣金额 */
  couponDiscount?: number
  /** 已选优惠券名称 */
  couponName?: string
  /** 运费；传 null 表示当前页面不计算运费，不展示运费行 */
  freightAmount?: number | null
  /** 底部偏移，tabBar 页需让出 tabBar 高度 */
  bottomOffset?: string
}>(), {
  promotionDiscount: 0,
  couponDiscount: 0,
  couponName: '',
  freightAmount: null,
  bottomOffset: '0'
})

const drawerVisible = ref(false)

const savedAmount = computed(() => props.promotionDiscount + props.couponDiscount)
const payableInteger = computed(() => props.payableAmount.toFixed(2).split('.')[0])
const payableDecimal = computed(() => props.payableAmount.toFixed(2).split('.')[1])
</script>

<style lang="scss" scoped>
$theme-primary: #2E7D32;

/* ===== 底部结算栏（常驻收起态） ===== */
.checkout-bar {
  position: fixed;
  left: 0;
  right: 0;
  bottom: 0;
  padding: 20rpx 24rpx calc(20rpx + env(safe-area-inset-bottom));
  background: #FCFAF7;
  border-top: 1rpx solid #E8E4DC;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 20rpx;
  box-shadow: 0 -6rpx 28rpx rgba(46, 125, 50, 0.08);
  z-index: 99;
}
.bar-left {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 8rpx;
}
.bar-total-line {
  display: flex;
  align-items: center;
  gap: 8rpx;
  font-size: 21rpx;
  color: #71746E;
  line-height: 1;
}
.bar-total-label { font-size: 21rpx; }
.bar-total-origin {
  font-size: 21rpx;
  text-decoration: line-through;
}
.bar-saved {
  font-size: 21rpx;
  color: #8B1E1E;
  font-weight: 500;
}
.bar-pay-line {
  display: flex;
  align-items: baseline;
  gap: 8rpx;
  min-width: 0;
}
.bar-pay-label {
  font-size: 24rpx;
  font-weight: 500;
  color: #1C1D1B;
}
.bar-amount {
  display: flex;
  align-items: baseline;
  color: #8B1E1E;
  font-weight: 700;
  line-height: 1;
}
.bar-yen {
  font-size: 22rpx;
  margin-right: 2rpx;
}
.bar-amount-int {
  font-size: 48rpx;
  line-height: 1;
}
.bar-amount-dec {
  font-size: 24rpx;
  font-weight: 700;
}
.bar-detail-btn {
  align-self: center;
  display: inline-flex;
  align-items: center;
  gap: 2rpx;
  height: 40rpx;
  padding: 0 14rpx;
  margin-left: 4rpx;
  border-radius: 100rpx;
  background: #F7F5F0;
  border: 1rpx solid rgba(232, 228, 220, 0.6);
}
.bar-detail-text {
  font-size: 21rpx;
  color: #71746E;
}
.bar-detail-icon {
  transform: rotate(180deg);
}

/* ===== 价格明细抽屉（展开态） ===== */
.detail-mask {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.45);
  /* 高于 H5 内置 tabBar 的 998，保证抽屉盖住底部导航 */
  z-index: 1000;
  display: flex;
  align-items: flex-end;
}
.detail-sheet {
  width: 100%;
  background: #FCFAF7;
  border-radius: 32rpx 32rpx 0 0;
  border: 1rpx solid #E8E4DC;
  border-bottom: none;
  overflow: hidden;
  padding-bottom: env(safe-area-inset-bottom);
  box-shadow: 0 -12rpx 36rpx -4rpx rgba(30, 41, 28, 0.12);
  animation: sheet-up 0.22s ease-out;
}
@keyframes sheet-up {
  from { transform: translateY(24%); opacity: 0.6; }
  to { transform: translateY(0); opacity: 1; }
}
.detail-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 24rpx 40rpx;
  border-bottom: 1rpx solid rgba(232, 228, 220, 0.6);
}
.detail-title {
  font-size: 28rpx;
  font-weight: 700;
  color: #1C1D1B;
}
.detail-close {
  width: 48rpx;
  height: 48rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
}
.detail-body {
  padding: 32rpx 40rpx 40rpx;
  display: flex;
  flex-direction: column;
  gap: 24rpx;
}
.detail-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16rpx;
}
.detail-label-group {
  display: flex;
  align-items: center;
  gap: 6rpx;
}
.detail-label {
  font-size: 25rpx;
  color: #55584F;
}
.detail-note {
  font-size: 20rpx;
  color: $theme-primary;
}
.detail-value {
  font-size: 25rpx;
  font-weight: 500;
  color: #1C1D1B;
}
.detail-free { color: $theme-primary; }
.detail-neg { color: #8B1E1E; }
.detail-divider {
  height: 1rpx;
  background: rgba(232, 228, 220, 0.7);
  margin: 4rpx 0;
}
.detail-total-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding-top: 4rpx;
}
.detail-total-label {
  font-size: 24rpx;
  color: #71746E;
}
.detail-total-right {
  display: flex;
  align-items: baseline;
  gap: 12rpx;
}
.detail-amount {
  display: flex;
  align-items: baseline;
  color: #8B1E1E;
  font-weight: 700;
  line-height: 1;
}
.detail-amount .bar-amount-int {
  font-size: 40rpx;
}
.detail-amount .bar-amount-dec {
  font-size: 22rpx;
}
.detail-saved {
  font-size: 21rpx;
  color: #71746E;
}
</style>

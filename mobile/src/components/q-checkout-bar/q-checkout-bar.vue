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
@import "./q-checkout-bar.scss";
</style>
<template>
  <view class="page">
    <!-- ============ 顶栏 ============ -->
    <q-page-header-back title="确认订单" subtitle="CHECKOUT ORDER · 道地本草直发" />

    <scroll-view scroll-y class="page-body">
      <!-- 冷链配送地址卡 -->
      <section class="card address-card">
        <view class="address-head">
          <view class="addr-pill">
            <q-icon name="snowflake" size="24" color="#2E7D32" />
            <text>顺丰冷链 · 避光恒温保效直发</text>
          </view>
          <view class="address-head-right" @tap="editAddress">
            <text class="link">更换地址</text>
            <q-icon name="chevron-right" size="24" color="#9A9A96" />
          </view>
        </view>
        <view class="address-user">
          <text class="name">{{ address?.receiverName || '请选择收货地址' }}</text>
          <text class="phone">{{ address?.receiverPhone || '' }}</text>
          <view v-if="address?.isDefault" class="tag-default tag-sm">默认</view>
        </view>
        <view class="address-detail">
          <q-icon name="location" size="28" color="#2E7D32" />
          <text class="address-text">{{ address?.fullAddress || '点击更换地址并新增收货地址' }}</text>
        </view>
        <view class="time-pill">
          <q-icon name="time" size="24" color="#E2601F" />
          <text class="time-text">预计<text class="time-strong">明天 14:00</text>前送达（中药密封温控箱配送）</text>
        </view>
        <!-- 卡片底部的信封封边 -->
        <view class="rx-divider"></view>
      </section>

      <!-- 配送方式 -->
      <section class="card delivery-card">
        <view class="delivery-icon">
          <q-icon name="vehicle" size="30" color="#2E7D32" />
        </view>
        <view class="delivery-text">
          <text class="delivery-name">顺丰特快</text>
          <text class="delivery-desc">已免运费（已满 ¥99 免邮，为您减免 ¥15.00）</text>
        </view>
        <view class="delivery-tag">包邮专享</view>
      </section>

      <!-- 商品清单店铺分组 -->
      <section class="card shop-card">
        <view class="shop-head">
          <view class="shop-head-left">
            <q-icon name="shop" size="32" color="#2E7D32" />
            <text class="shop-name">本草国医馆·道地药房</text>
            <view class="tag tag-green tag-sm">
              <q-icon name="check-circle-filled" size="20" color="#FFFFFF" />
              <text>道地认证</text>
            </view>
          </view>
        </view>
        <view class="goods-list">
          <view class="goods-item" v-for="item in items" :key="item.productId">
            <image class="goods-img" :src="item.productImage" mode="aspectFill" />
            <view class="goods-info">
              <view class="goods-name">{{ item.productName }}</view>
              <view class="goods-spec">{{ item.specName }}</view>
              <view class="goods-bottom">
                <text class="goods-price">¥{{ item.price.toFixed(2) }}</text>
                <text class="goods-qty">×{{ item.quantity }}</text>
              </view>
            </view>
          </view>
        </view>
        <view class="dashed-divider"></view>
        <view class="shop-subtotal">
          <text>商品小计</text>
          <text class="subtotal-price">¥{{ subtotal.toFixed(2) }}</text>
        </view>
      </section>

      <!-- 优惠明细 -->
      <section class="card discount-card">
        <view class="discount-row" @tap="selectCoupon">
          <view class="discount-row-left">
            <q-icon name="coupon" size="30" color="#FF9800" />
            <text>优惠券</text>
          </view>
          <view class="discount-row-right">
            <text class="discount-hint">暂无可用优惠券</text>
            <q-icon name="chevron-right" size="28" color="#999999" />
          </view>
        </view>
        <view class="discount-row promotion-row">
          <view class="discount-row-left">
            <q-icon name="coupon" size="30" color="#E2601F" />
            <text>促销活动</text>
          </view>
          <view class="discount-row-right">
            <text class="discount-hint">下单时自动匹配，优惠金额以结算结果为准</text>
          </view>
        </view>
        <view class="discount-row" @tap="addRemark">
          <view class="discount-row-left">
            <q-icon name="edit" size="30" color="#2E7D32" />
            <text>订单备注</text>
          </view>
          <view class="discount-row-right">
            <text class="discount-hint" :class="{ placeholder: !remark }">
              {{ remark || '选填：如忌口、优先发顺丰等' }}
            </text>
            <q-icon name="chevron-right" size="28" color="#999999" />
          </view>
        </view>

        <view class="fee-detail">
          <view class="fee-line">
            <text class="fee-label">商品金额</text>
            <text class="fee-value">¥{{ subtotal.toFixed(2) }}</text>
          </view>
          <view class="fee-line">
            <text class="fee-label">运费</text>
            <text class="fee-value fee-free">已免</text>
          </view>
        </view>
      </section>

      <!-- 选择支付方式 -->
      <view class="pay-section">
        <view class="pay-section-title">选择支付方式</view>
        <view :class="['pay-option', { 'pay-option-on': payMethod === 'wechat' }]" @tap="payMethod = 'wechat'">
          <view class="pay-logo wechat">微</view>
          <view class="pay-main">
            <view class="pay-title-row">
              <text class="pay-name">微信支付</text>
              <view class="pay-tag">推荐</view>
            </view>
            <text class="pay-desc">微信立减金可用，支持微信分期</text>
          </view>
          <view :class="['pay-radio', { 'pay-radio-on': payMethod === 'wechat' }]">
            <q-icon v-if="payMethod === 'wechat'" name="check" size="18" color="#ffffff" />
          </view>
        </view>
        <view :class="['pay-option', { 'pay-option-on': payMethod === 'alipay' }]" @tap="payMethod = 'alipay'">
          <view class="pay-logo alipay">支</view>
          <view class="pay-main">
            <view class="pay-title-row">
              <text class="pay-name">支付宝支付</text>
            </view>
            <text class="pay-desc">支持花呗分期还款</text>
          </view>
          <view :class="['pay-radio', { 'pay-radio-on': payMethod === 'alipay' }]">
            <q-icon v-if="payMethod === 'alipay'" name="check" size="18" color="#ffffff" />
          </view>
        </view>
      </view>

      <!-- 药房品质保证 -->
      <section class="card guar-card">
        <view class="guar-item">
          <q-icon name="check-circle" size="28" color="#2E7D32" />
          <text>道地保真</text>
        </view>
        <view class="guar-item">
          <q-icon name="check-circle" size="28" color="#2E7D32" />
          <text>老药师鉴选</text>
        </view>
        <view class="guar-item">
          <q-icon name="check-circle" size="28" color="#2E7D32" />
          <text>溯源可查</text>
        </view>
      </section>

      <view class="bottom-placeholder"></view>
    </scroll-view>

    <!-- 底部固定结算栏 -->
    <view class="page-footer">
      <view class="bottom-left">
        <text class="pay-label">实付：</text>
        <text class="pay-amount">¥{{ finalPrice.toFixed(2) }}</text>
      </view>
      <view class="submit-btn" @tap="submitOrder">
        提交订单
      </view>
    </view>
  </view>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { onShow } from '@dcloudio/uni-app'
import { getCartItems, clearCart, getAddresses, getStoreId, createOrder, isLoggedIn } from '@/api'
import type { MallAddress } from '@/api'
import type { CartItem } from '@/api/types'

const items = ref<CartItem[]>([])
const address = ref<MallAddress | null>(null)
const payMethod = ref('wechat')
const remark = ref('')

const subtotal = computed(() =>
  items.value.reduce((s, i) => s + i.price * i.quantity, 0)
)

const finalPrice = computed(() => {
  return subtotal.value
})

onShow(async () => {
  try {
    items.value = (await getCartItems()).filter(item => item.selected)
    if (!isLoggedIn()) {
      address.value = null
      return
    }
    const addresses = await getAddresses()
    const selectedAddressId = uni.getStorageSync('qps_checkout_address_id')
    address.value = addresses.find(item => item.id === selectedAddressId)
      ?? addresses.find(item => item.isDefault)
      ?? addresses[0]
      ?? null
  } catch (error) {
    console.error('加载结算数据失败', error)
    uni.showToast({ title: '结算数据加载失败', icon: 'none' })
  }
})

function editAddress() { uni.navigateTo({ url: '/pages/addresses/index' }) }
function selectCoupon() { uni.showToast({ title: '暂无可用优惠券', icon: 'none' }) }
function addRemark() {
  uni.showModal({
    title: '订单备注', editable: true, placeholderText: '选填',
    success: r => { if (r.confirm && r.content) remark.value = r.content }
  })
}
function submitOrder() {
  if (!isLoggedIn()) {
    uni.showToast({ title: '请先登录后再提交订单', icon: 'none' })
    uni.navigateTo({ url: '/pages/account/index' })
    return
  }
  if (!address.value) {
    uni.showToast({ title: '请先添加收货地址', icon: 'none' })
    editAddress()
    return
  }
  if (items.value.length === 0) {
    uni.showToast({ title: '购物车没有已选商品', icon: 'none' })
    return
  }

  uni.showModal({
    title: '确认提交',
    content: `商品金额（优惠前）¥${finalPrice.value.toFixed(2)}，促销优惠将在下单时自动核算。确认创建订单？`,
    success: async r => {
      if (!r.confirm || !address.value) return
      try {
        const order = await createOrder({
          storeId: await getStoreId(),
          receiverName: address.value.receiverName,
          receiverPhone: address.value.receiverPhone,
          provinceCode: address.value.provinceCode,
          provinceName: address.value.provinceName,
          cityCode: address.value.cityCode,
          cityName: address.value.cityName,
          districtCode: address.value.districtCode,
          districtName: address.value.districtName,
          receiverAddress: address.value.detailAddress,
          remark: remark.value,
          items: items.value.map(item => ({ skuId: item.skuId, quantity: item.quantity }))
        })
        try {
          await clearCart()
        } catch (error) {
          console.error('订单已创建但清空购物车失败', error)
          uni.showToast({ title: '订单已创建，购物车同步失败', icon: 'none' })
        }
        uni.showModal({
          title: '订单已创建',
          content: `订单号：${order.orderNo}\n促销优惠：¥${Number(order.promotionDiscountAmount || 0).toFixed(2)}\n实付金额：¥${Number(order.payableAmount).toFixed(2)}`,
          showCancel: false,
          success: () => uni.switchTab({ url: '/pages/profile/index' })
        })
      } catch (error) {
        console.error('创建订单失败', error)
        uni.showToast({ title: '下单失败，请检查库存和收货信息', icon: 'none' })
      }
    }
  })
}
</script>

<style lang="scss" scoped>
$theme-primary: #2E7D32;
$theme-primary-dark: #1B5E20;
$theme-accent: #FF9800;
$theme-bg: #FCF9F8;
$card-radius: 24rpx;
$page-padding: 24rpx;

.page {
    min-height: 100vh;
  background: var(--page-bg);
  display: flex;
  flex-direction: column;
}

/* ===== 滚动容器 ===== */
.page-body {
  flex: 1;
  padding: 0 $page-padding;
  box-sizing: border-box;
}

/* ===== 卡片通用 ===== */
.card {
  background: #FFFFFF;
  border-radius: $card-radius;
  padding: 28rpx;
  margin-top: 24rpx;
  box-shadow: 0 4rpx 16rpx rgba(46, 125, 50, 0.04);
}
.card-title {
  font-size: 30rpx;
  font-weight: 700;
  color: #1B5E20;
  margin-bottom: 20rpx;
}

/* ===== Tag ===== */
.tag {
  display: inline-flex;
  align-items: center;
  gap: 6rpx;
  border-radius: 40rpx;
  font-size: 22rpx;
  padding: 6rpx 16rpx;
  line-height: 1;
}
.tag-sm { font-size: 20rpx; padding: 4rpx 12rpx; }
.tag-green { background: $theme-primary; color: #FFFFFF; }
.tag-orange { background: $theme-accent; color: #FFFFFF; }
.tag-default { background: #F0F0F0; color: #666; }

/* ===== 地址卡 ===== */
.address-card {
  position: relative;
  overflow: hidden;
  padding: 26rpx 28rpx 34rpx;
  border: 1rpx solid #F0EBE1;
}
.address-head {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
.addr-pill {
  display: inline-flex;
  align-items: center;
  gap: 6rpx;
  height: 48rpx;
  padding: 0 18rpx;
  background: #EDF4E4;
  border-radius: 100rpx;
  font-size: 22rpx;
  font-weight: 600;
  color: $theme-primary;
}
.address-head-right {
  display: flex;
  align-items: center;
  gap: 2rpx;
  color: #9A9A96;
  font-size: 24rpx;
}
.link { color: #9A9A96; font-size: 24rpx; }
.address-user {
  display: flex;
  align-items: center;
  gap: 14rpx;
  margin-top: 20rpx;
}
.address-user .name {
  font-size: 34rpx;
  font-weight: 700;
  color: #1B1C1C;
  letter-spacing: 1rpx;
}
.address-user .phone {
  font-size: 26rpx;
  color: #555;
}
.address-user .tag-default {
  background: $theme-primary;
  color: #FFFFFF;
  font-weight: 600;
}
.address-detail {
  display: flex;
  align-items: flex-start;
  gap: 8rpx;
  margin-top: 14rpx;
}
.address-detail .q-icon { flex-shrink: 0; margin-top: 4rpx; }
.address-text {
  flex: 1;
  font-size: 27rpx;
  color: #333;
  line-height: 1.55;
}
.time-pill {
  margin-top: 18rpx;
  display: flex;
  align-items: center;
  gap: 8rpx;
  background: #FDF1E7;
  padding: 14rpx 18rpx;
  border-radius: 12rpx;
}
.time-text {
  flex: 1;
  font-size: 24rpx;
  color: #C0622F;
  line-height: 1.4;
}
.time-strong { font-weight: 700; color: #C24A18; }

/* ===== 红绿相间药方虚线（信封封边，贴在地址卡内侧底部） ===== */
.rx-divider {
  position: absolute;
  left: -24rpx;
  right: -24rpx;
  bottom: 0;
  height: 10rpx;
  transform: skewX(-20deg);
  background: repeating-linear-gradient(
    to right,
    $theme-primary 0 20rpx,
    transparent 20rpx 26rpx,
    #C62828 26rpx 46rpx,
    transparent 46rpx 52rpx
  );
}

/* ===== 配送方式 ===== */
.delivery-card {
  display: flex;
  align-items: center;
  gap: 16rpx;
  padding: 20rpx 24rpx;
  border: 1rpx solid #F0EBE1;
}
.delivery-icon {
  width: 76rpx;
  height: 76rpx;
  flex-shrink: 0;
  border-radius: 50%;
  background: #EFF5E8;
  display: flex;
  align-items: center;
  justify-content: center;
}
.delivery-text {
  flex: 1;
  min-width: 0;
  font-size: 23rpx;
  color: #6B6B6B;
  line-height: 1.5;
}
.delivery-name {
  font-weight: 700;
  color: #1B1C1C;
}
.delivery-tag {
  flex-shrink: 0;
  height: 44rpx;
  padding: 0 18rpx;
  display: flex;
  align-items: center;
  border-radius: 100rpx;
  background: $theme-primary;
  color: #FFFFFF;
  font-size: 22rpx;
  font-weight: 600;
}

/* ===== 商品清单 ===== */
.shop-card { padding: 24rpx 28rpx; }
.shop-head {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
.shop-head-left {
  display: flex;
  align-items: center;
  gap: 12rpx;
}
.shop-name {
  font-size: 28rpx;
  font-weight: 700;
  color: #1B5E20;
}
.goods-list { margin-top: 16rpx; }
.goods-item {
  display: flex;
  gap: 20rpx;
  padding: 16rpx 0;
}
.goods-img {
  width: 160rpx;
  height: 160rpx;
  border-radius: 16rpx;
  background: #F3F3F3;
  flex-shrink: 0;
}
.goods-info { flex: 1; display: flex; flex-direction: column; justify-content: space-between; }
.goods-name {
  font-size: 28rpx;
  color: #222;
  font-weight: 600;
  line-height: 1.4;
}
.goods-spec {
  font-size: 22rpx;
  color: #999;
  margin-top: 4rpx;
}
.goods-bottom {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
.goods-price {
  color: $theme-accent;
  font-size: 30rpx;
  font-weight: 700;
}
.goods-qty {
  color: #888;
  font-size: 26rpx;
}
.gift-row {
  display: flex;
  align-items: center;
  gap: 12rpx;
  padding: 12rpx 0 12rpx 180rpx;
}
.gift-name { font-size: 24rpx; color: #666; }

.dashed-divider {
  height: 2rpx;
  margin: 16rpx 0;
  background: repeating-linear-gradient(
    to right,
    #CCCCCC 0,
    #CCCCCC 10rpx,
    transparent 10rpx,
    transparent 20rpx
  );
}
.shop-subtotal {
  display: flex;
  justify-content: flex-end;
  align-items: center;
  gap: 16rpx;
  font-size: 26rpx;
  color: #555;
}
.subtotal-price {
  font-size: 28rpx;
  font-weight: 700;
  color: #222;
}

/* ===== 优惠明细 ===== */
.discount-card { padding: 8rpx 28rpx; }
.discount-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 28rpx 0;
  border-bottom: 1rpx solid #F3F3F3;
}
.discount-row:last-of-type { border-bottom: none; }
.discount-row-left {
  display: flex;
  align-items: center;
  gap: 12rpx;
  font-size: 28rpx;
  color: #333;
}
.discount-row-right {
  display: flex;
  align-items: center;
  gap: 10rpx;
}
.discount-neg {
  color: $theme-accent;
  font-size: 28rpx;
  font-weight: 700;
}
.discount-hint {
  font-size: 24rpx;
  color: #999;
}
.discount-hint.placeholder { color: #BBB; }
.points-switch {
  transform: scale(0.75);
  transform-origin: right center;
  margin-left: 4rpx;
}

.fee-detail {
  margin-top: 16rpx;
  padding-top: 20rpx;
  border-top: 1rpx solid #F3F3F3;
}
.fee-line {
  display: flex;
  justify-content: space-between;
  padding: 10rpx 0;
  font-size: 26rpx;
}
.fee-label { color: #666; }
.fee-value { color: #333; }
.fee-free { color: $theme-primary; }
.fee-neg { color: $theme-accent; }

/* ===== 选择支付方式 ===== */
.pay-section { margin-top: 24rpx; }
.pay-section-title {
  font-size: 32rpx;
  font-weight: 700;
  color: #1B1C1C;
  margin: 4rpx 0 18rpx;
}
.pay-option {
  display: flex;
  align-items: center;
  gap: 20rpx;
  padding: 22rpx 24rpx;
  margin-bottom: 16rpx;
  background: #FFFFFF;
  border: 2rpx solid #E8E3D8;
  border-radius: 16rpx;
}
.pay-option-on {
  background: #F4F9EF;
  border-color: $theme-primary;
}
.pay-logo {
  width: 76rpx;
  height: 76rpx;
  flex-shrink: 0;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 32rpx;
  font-weight: 700;
}
.pay-logo.wechat { background: #E4F3E5; color: $theme-primary; }
.pay-logo.alipay { background: #E8F0FE; color: #1677FF; }
.pay-main {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 8rpx;
}
.pay-title-row {
  display: flex;
  align-items: center;
  gap: 10rpx;
}
.pay-name {
  font-size: 30rpx;
  font-weight: 700;
  color: #1B1C1C;
}
.pay-desc {
  font-size: 22rpx;
  color: #8A8578;
}
.pay-tag {
  height: 34rpx;
  padding: 0 10rpx;
  display: flex;
  align-items: center;
  border-radius: 6rpx;
  background: $theme-primary;
  color: #FFFFFF;
  font-size: 20rpx;
  font-weight: 600;
}
.pay-radio {
  width: 44rpx;
  height: 44rpx;
  flex-shrink: 0;
  border-radius: 50%;
  border: 2rpx solid #C9C5BA;
  background: #FFFFFF;
  display: flex;
  align-items: center;
  justify-content: center;
  box-sizing: border-box;
}
.pay-radio-on {
  background: $theme-primary;
  border-color: $theme-primary;
}

/* ===== 品质保证 ===== */
.guar-card {
  display: flex;
  justify-content: space-around;
  padding: 28rpx 16rpx;
}
.guar-item {
  display: flex;
  align-items: center;
  gap: 8rpx;
  font-size: 24rpx;
  color: $theme-primary;
}

/* ===== 底部占位 ===== */
.bottom-placeholder { height: 180rpx; }

/* ===== 底部结算栏 ===== */
.page-footer {
  position: fixed;
  left: 0;
  right: 0;
  bottom: 0;
  padding: 20rpx 24rpx calc(20rpx + env(safe-area-inset-bottom));
  background: #FFFFFF;
  display: flex;
  align-items: center;
  justify-content: space-between;
  box-shadow: 0 -4rpx 20rpx rgba(0, 0, 0, 0.05);
  z-index: 99;
}
.bottom-left { display: flex; align-items: baseline; gap: 4rpx; }
.pay-label {
  font-size: 28rpx;
  color: #333;
}
.pay-amount {
  font-size: 40rpx;
  font-weight: 700;
  color: $theme-accent;
}
.submit-btn {
  background: linear-gradient(135deg, $theme-primary, $theme-primary-dark);
  color: #FFFFFF;
  font-size: 32rpx;
  font-weight: 700;
  padding: 24rpx 64rpx;
  border-radius: 48rpx;
  letter-spacing: 4rpx;
  box-shadow: 0 8rpx 24rpx rgba(46, 125, 50, 0.35);
}
</style>

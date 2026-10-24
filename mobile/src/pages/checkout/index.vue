<template>
  <view class="page">
    <!-- ============ 顶栏 ============ -->
    <q-page-header-back title="确认订单" subtitle="CHECKOUT ORDER · 道地本草直发" />

    <scroll-view scroll-y class="page-body">
      <!-- 冷链配送地址卡 -->
      <section class="card address-card">
        <view class="address-head">
          <view class="tag tag-green tag-lg">
            <q-icon name="vehicle" size="26" color="#FFFFFF" />
            <text>顺丰冷链</text>
          </view>
          <view class="address-head-right" @tap="editAddress">
            <text class="link">更换地址</text>
            <q-icon name="chevron-right" size="28" color="#999999" />
          </view>
        </view>
        <view class="address-user">
          <text class="name">李时珍</text>
          <text class="phone">138****9527</text>
          <view class="tag tag-default tag-sm">默认</view>
        </view>
        <view class="address-detail">
          湖北省黄冈市蕲春县李时珍大道 88 号·本草国医馆 1 楼
        </view>
        <view class="time-pill">
          <q-icon name="time" size="24" color="#FFFFFF" />
          <text>预计明天 14:00 前送达·中药密封温控箱配送</text>
        </view>
      </section>

      <!-- 包邮专享横幅 -->
      <section class="banner-free">
        <q-icon name="check-circle-filled" size="36" color="#FF9800" />
        <text>已享全场满 ¥99 免运费</text>
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
          <view class="gift-row" v-for="(g, idx) in gifts" :key="idx">
            <view class="tag tag-orange tag-sm">赠品</view>
            <text class="gift-name">{{ g }}</text>
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
            <text class="discount-neg">-¥40.00</text>
            <text class="discount-hint">满300减40</text>
            <q-icon name="chevron-right" size="28" color="#999999" />
          </view>
        </view>
        <view class="discount-row">
          <view class="discount-row-left">
            <q-icon name="star" size="30" color="#2E7D32" />
            <text>积分抵扣</text>
          </view>
          <view class="discount-row-right">
            <text class="discount-hint">500 积分抵 ¥5.00</text>
            <t-switch v-model="usePoints" theme="#2E7D32" size="small" />
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
          <view class="fee-line">
            <text class="fee-label">优惠券</text>
            <text class="fee-value fee-neg">-¥40.00</text>
          </view>
          <view class="fee-line" v-if="usePoints">
            <text class="fee-label">积分抵扣</text>
            <text class="fee-value fee-neg">-¥5.00</text>
          </view>
        </view>
      </section>

      <!-- 支付方式 -->
      <section class="card pay-card">
        <view class="card-title">支付方式</view>
        <view class="pay-row" @tap="payMethod = 'wechat'">
          <view class="pay-row-left">
            <view class="pay-icon wechat">微</view>
            <view class="pay-text">
              <text class="pay-name">微信支付</text>
              <text class="pay-tag">推荐</text>
            </view>
          </view>
          <t-radio :checked="payMethod === 'wechat'" theme="#2E7D32" />
        </view>
        <view class="pay-row" @tap="payMethod = 'alipay'">
          <view class="pay-row-left">
            <view class="pay-icon alipay">支</view>
            <view class="pay-text">
              <text class="pay-name">支付宝</text>
            </view>
          </view>
          <t-radio :checked="payMethod === 'alipay'" theme="#2E7D32" />
        </view>
      </section>

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
import { getCartItems, clearCart } from '@/api'
import type { CartItem } from '@/api/types'

const items = ref<CartItem[]>([])
const gifts = ref<string[]>(['正宗宁夏枸杞 10g', '道地黄芪片 试用品'])
const usePoints = ref(false)
const payMethod = ref('wechat')
const remark = ref('')

const subtotal = computed(() =>
  items.value.reduce((s, i) => s + i.price * i.quantity, 0)
)

const finalPrice = computed(() => {
  let p = subtotal.value - 40
  if (usePoints.value) p -= 5
  return Math.max(p, 0)
})

items.value = getCartItems().length > 0 ? getCartItems() : [
  { productId: '1', productName: '长白山十五年野生生晒参', productImage: '/static/images/photo-1549737557-0e4e05d31f9c.jpg', skuId: 's1', specName: '600g礼盒装', price: 298, quantity: 1, weightGram: 600, selected: true },
  { productId: '2', productName: '宁夏中宁特级枸杞', productImage: '/static/images/photo-1599909533730-3c5c6589a060.jpg', skuId: 's2', specName: '250g精品装', price: 72, quantity: 1, weightGram: 250, selected: true }
]

function editAddress() { uni.showToast({ title: '地址管理开发中', icon: 'none' }) }
function selectCoupon() { uni.showToast({ title: '优惠券选择开发中', icon: 'none' }) }
function addRemark() {
  uni.showModal({
    title: '订单备注', editable: true, placeholderText: '选填',
    success: r => { if (r.confirm && r.content) remark.value = r.content }
  })
}
function submitOrder() {
  uni.showModal({
    title: '确认提交',
    content: `实付 ¥${finalPrice.value.toFixed(2)}，使用${payMethod.value === 'wechat' ? '微信' : '支付宝'}支付？`,
    success: r => {
      if (r.confirm) {
        clearCart()
        uni.showToast({ title: '下单成功！', icon: 'success' })
        setTimeout(() => uni.switchTab({ url: '/pages/index/index' }), 1200)
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
.tag-lg { font-size: 24rpx; padding: 10rpx 22rpx; }
.tag-sm { font-size: 20rpx; padding: 4rpx 12rpx; }
.tag-green { background: $theme-primary; color: #FFFFFF; }
.tag-orange { background: $theme-accent; color: #FFFFFF; }
.tag-default { background: #F0F0F0; color: #666; }

/* ===== 地址卡 ===== */
.address-card { padding: 28rpx 28rpx 24rpx; }
.address-head {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
.address-head-right {
  display: flex;
  align-items: center;
  gap: 4rpx;
  color: #999;
  font-size: 26rpx;
}
.link { color: #999; font-size: 26rpx; }
.address-user {
  display: flex;
  align-items: center;
  gap: 16rpx;
  margin-top: 20rpx;
}
.address-user .name {
  font-size: 32rpx;
  font-weight: 700;
  color: #1B5E20;
}
.address-user .phone {
  font-size: 26rpx;
  color: #555;
}
.address-detail {
  font-size: 26rpx;
  color: #444;
  margin-top: 10rpx;
  line-height: 1.5;
}
.time-pill {
  margin-top: 20rpx;
  display: flex;
  align-items: center;
  gap: 8rpx;
  background: $theme-primary;
  color: #FFFFFF;
  padding: 16rpx 22rpx;
  border-radius: 16rpx;
  font-size: 24rpx;
  line-height: 1.4;
}

/* ===== 包邮横幅 ===== */
.banner-free {
  margin-top: 24rpx;
  background: #FFF8E1;
  border-radius: 16rpx;
  padding: 20rpx 24rpx;
  display: flex;
  align-items: center;
  gap: 12rpx;
  font-size: 26rpx;
  color: #E65100;
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

/* ===== 支付方式 ===== */
.pay-card { padding: 28rpx; }
.pay-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 20rpx 0;
}
.pay-row-left {
  display: flex;
  align-items: center;
  gap: 16rpx;
}
.pay-icon {
  width: 64rpx;
  height: 64rpx;
  border-radius: 16rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #FFFFFF;
  font-size: 30rpx;
  font-weight: 700;
}
.pay-icon.wechat { background: #07C160; }
.pay-icon.alipay { background: #1677FF; }
.pay-text {
  display: flex;
  align-items: center;
  gap: 10rpx;
}
.pay-name {
  font-size: 28rpx;
  color: #222;
}
.pay-tag {
  font-size: 20rpx;
  background: #FFF0E0;
  color: $theme-accent;
  padding: 2rpx 12rpx;
  border-radius: 8rpx;
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

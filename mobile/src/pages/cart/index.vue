<template>
  <view class="page">
    <!-- ============ 顶栏：标题 + 管理按钮 ============ -->
    <q-page-header-back :show-back="false" title="购物车">
      <template #right>
        <text :class="['manage-btn', { 'manage-btn-on': editMode }]" @click="toggleEditMode">
          {{ editMode ? '完成' : '管理' }}
        </text>
      </template>
    </q-page-header-back>

    <!-- ============ 内容区 ============ -->
    <scroll-view scroll-y class="page-body" enhanced>
      <view class="page-body-inner">
      <!-- 空车态 -->
      <view v-if="cartItems.length === 0" class="empty-wrap">
        <q-icon name="cart" size="72" color="#D8D3C8" />
        <text class="empty-desc">购物车还是空的，快去挑选心仪的中药材吧</text>
        <view class="empty-btn" @click="goToIndex">去逛逛</view>
      </view>

      <block v-else>
        <!-- section 满额免邮横幅 -->
        <section class="promo-section">
          <view class="promo-icon">
            <q-icon name="vehicle" size="18" color="#2E7D32" />
          </view>
          <text class="promo-text">
            已享全场满¥99免运费，再凑<text class="promo-gap-hl">¥32.00</text>享全单9折
          </text>
          <view class="promo-action" @click="goToIndex">
            <text class="promo-action-text">去凑单</text>
            <q-icon name="chevron-right" size="14" color="#2E7D32" />
          </view>
        </section>

        <!-- section 店铺分组卡片 -->
        <section class="shop-section">
          <view class="shop-header">
            <view class="shop-select" @click="toggleSelectAll">
              <view :class="['cb', { 'cb-on': isAllSelected }]">
                <q-icon v-if="isAllSelected" name="check" size="20" color="#ffffff" />
              </view>
            </view>
            <view class="shop-name-wrap">
              <q-icon name="shop" size="19" color="#2E7D32" />
              <text class="shop-name">本草国医馆·道地药房</text>
            </view>
            <view class="shop-badge">
              <q-icon name="check-circle-filled" size="13" color="#2E7D32" />
              <text class="shop-badge-text">GAP道地直采</text>
            </view>
          </view>

          <!-- 商品卡列表 -->
          <view class="goods-list">
            <view
              v-for="item in cartItems"
              :key="item.skuId"
              class="goods-card"
            >
              <!-- 1. 选择框 -->
              <view class="goods-check" @click="toggleSelection(item.skuId)">
                <view :class="['cb', { 'cb-on': item.selected }]">
                  <q-icon v-if="item.selected" name="check" size="20" color="#ffffff" />
                </view>
              </view>

              <!-- 2. 商品图 -->
              <view class="goods-image-wrap">
                <q-cached-image :src="item.productImage" mode="aspectFill" class="goods-image" />
                <view v-if="item.promotionBadgeText" class="goods-badge" :style="{ backgroundColor: item.promotionBadgeColor || '#C2410C' }">
                  <text class="goods-badge-text">{{ item.promotionBadgeText }}</text>
                </view>
              </view>

              <!-- 3-7. 商品信息 -->
              <view class="goods-info">
                <!-- 商品名 -->
                <text class="goods-name">{{ item.productName }}</text>

                <!-- 规格 chip + chevron-down -->
                <view class="spec-chip">
                  <text class="spec-text">{{ item.specName }}</text>
                  <q-icon name="chevron-down" size="12" color="#707A6C" />
                </view>

                <!-- 道地认证 chip -->
                <view v-if="item.certText" class="cert-chip">
                  <q-icon name="check-circle" size="12" color="#2E7D32" />
                  <text class="cert-text">{{ item.certText }}</text>
                </view>

                <!-- 库存紧张红色提示 -->
                <view v-if="item.stockTip || item.isValid === false" class="stock-tip">
                  <q-icon name="error-circle" size="12" color="#D32F2F" />
                  <text class="stock-tip-text">{{ item.stockTip || item.invalidReason || '该商品暂不可购买' }}</text>
                </view>

                <!-- 底部价格+小计 / stepper -->
                <view class="goods-bottom">
                  <view class="price-wrap">
                    <view class="price-row">
                      <text class="price-symbol">¥</text>
                      <text class="price-value">{{ itemPrice(item).toFixed(2) }}</text>
                      <text v-if="itemHasDiscount(item)" class="price-origin">¥{{ item.price.toFixed(2) }}</text>
                    </view>
                    <text class="goods-subtotal">小计 ¥{{ (itemPrice(item) * item.quantity).toFixed(2) }}</text>
                  </view>

                  <!-- 管理态：单行删除 -->
                  <view v-if="editMode" class="row-actions">
                    <view class="row-action-btn" @click="deleteItem(item.skuId)">
                      <q-icon name="delete" size="16" color="#D32F2F" />
                      <text class="row-action-text">删除</text>
                    </view>
                  </view>

                  <!-- stepper: 自绘 -/数量/+ -->
                  <view v-else class="stepper">
                    <view
                      :class="['step-btn', { disabled: item.quantity <= 1 }]"
                      @click="item.quantity > 1 && decreaseQuantity(item.skuId)"
                    >
                      <q-icon name="minus" size="14" color="#40493D" />
                    </view>
                    <text class="step-val">{{ item.quantity }}</text>
                    <view class="step-btn" @click="increaseQuantity(item.skuId)">
                      <q-icon name="add" size="14" color="#1B1C1C" />
                    </view>
                  </view>
                </view>
              </view>
            </view>
          </view>

          <!-- 赠品区：0 元计价，不参与金额计算，仅按件数展示 -->
          <view v-if="gifts.length" class="gift-block">
            <view class="gift-block-title">
              <q-icon name="gift" size="16" color="#E2601F" />
              <text class="gift-block-text">赠品 · 已随选中商品赠送</text>
            </view>
            <view v-for="gift in gifts" :key="gift.skuId" class="gift-row">
              <q-cached-image :src="gift.productImage" mode="aspectFill" class="gift-image" />
              <view class="gift-info">
                <view class="gift-name-line">
                  <text class="gift-name">{{ gift.productName }}</text>
                  <view class="gift-tag"><text>赠品</text></view>
                </view>
                <text class="gift-spec">{{ gift.specName }}</text>
                <text class="gift-promo">满赠活动：{{ gift.promotionName }}</text>
              </view>
              <view class="gift-right">
                <text class="gift-price">¥0</text>
                <text class="gift-qty">×{{ gift.quantity }}</text>
              </view>
            </view>
          </view>
        </section>

        <!-- section 道地承诺 三栏 -->
        <section class="promise-section">
          <view class="promise-item">
            <view class="promise-icon">
              <q-icon name="shield-error" size="20" color="#2E7D32" />
            </view>
            <text class="promise-title">源头保真</text>
            <text class="promise-sub">一物一码全程溯源</text>
          </view>
          <view class="promise-item">
            <view class="promise-icon">
              <q-icon name="user-search" size="20" color="#2E7D32" />
            </view>
            <text class="promise-title">老药师鉴选</text>
            <text class="promise-sub">经验炮制守药典籍</text>
          </view>
          <view class="promise-item">
            <view class="promise-icon">
              <q-icon name="snowflake" size="20" color="#2E7D32" />
            </view>
            <text class="promise-title">冷链恒温</text>
            <text class="promise-sub">避光锁鲜极速直发</text>
          </view>
        </section>
      </block>
      </view>
    </scroll-view>

    <!-- ============ 底部固定结算栏：应付/合计 + 明细抽屉 + 去结算 ============ -->
    <q-checkout-bar
      v-if="cartItems.length > 0"
      :payable-amount="payableAmount"
      :goods-amount="selectedAmount"
      :promotion-discount="promotionDiscount"
      bottom-offset="var(--window-bottom, 0)"
    >
      <template #action>
        <!-- 管理态：批量删除选中行（复用后端批量移除接口） -->
        <view
          v-if="editMode"
          :class="['checkout-btn', { 'checkout-btn-disabled': selectedLineCount === 0 }]"
          @click="deleteSelected"
        >
          <q-icon name="delete" size="18" color="#ffffff" />
          <text class="checkout-text">删除选中</text>
          <text v-if="selectedLineCount > 0" class="checkout-text">({{ selectedLineCount }})</text>
        </view>
        <view v-else :class="['checkout-btn', { 'checkout-btn-disabled': selectedLineCount === 0 }]" @click="checkout">
          <text class="checkout-text">去结算</text>
          <text v-if="checkoutCount > 0" class="checkout-text">({{ checkoutCount }})</text>
          <q-icon name="lock-on" size="18" color="#ffffff" />
        </view>
      </template>
    </q-checkout-bar>
  </view>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { onShow } from '@dcloudio/uni-app'
import {
  getCartOverview,
  updateCartItemQuantity,
  removeCartItem,
  removeCartItems,
  toggleCartItemSelection,
  setCartItemsSelection
} from '@/api'
import type { CartItem } from '@/api/types'
import type { CartGiftItem, CartSummary } from '@/api'
import { UnauthorizedError } from '@/utils/http'

const cartItems = ref<CartItem[]>([])
const gifts = ref<CartGiftItem[]>([])
const selectedAmount = ref(0)
const promotionDiscount = ref(0)
const payableAmount = ref(0)
const editMode = ref(false)

const selectedItems = computed(() => cartItems.value.filter(i => i.selected))
const selectedLineCount = computed(() => selectedItems.value.length)
// 件数口径：按商品数量累加；赠品 0 元但计入件数
const selectedCount = computed(() => selectedItems.value.reduce((sum, item) => sum + item.quantity, 0))
const giftCount = computed(() => gifts.value.reduce((sum, gift) => sum + gift.quantity, 0))
const checkoutCount = computed(() => selectedCount.value + giftCount.value)
const isAllSelected = computed(() => cartItems.value.length > 0 && selectedLineCount.value === cartItems.value.length)

let refreshSeq = 0

function applySummary(summary: CartSummary) {
  cartItems.value = summary.items
  gifts.value = summary.gifts || []
  selectedAmount.value = summary.selectedAmount
  promotionDiscount.value = summary.promotionDiscountAmount
  payableAmount.value = summary.payableAmount
}

function itemPrice(item: CartItem): number {
  return item.discountedPrice ?? item.price
}

function itemHasDiscount(item: CartItem): boolean {
  return itemPrice(item) < item.price
}

async function refreshCart() {
  const seq = ++refreshSeq
  // 首次加载本地还没有商品时按全部选中处理
  const selectedSkuIds = cartItems.value.length
    ? cartItems.value.filter(item => item.selected).map(item => item.skuId)
    : undefined
  const summary = await getCartOverview(selectedSkuIds)
  if (seq !== refreshSeq) return
  applySummary(summary)
}

onShow(async () => {
  try {
    await refreshCart()
  } catch (error) {
    console.error('加载购物车失败', error)
    if (error instanceof UnauthorizedError) return
    uni.showToast({ title: '购物车加载失败', icon: 'none' })
  }
})

function toggleEditMode() { editMode.value = !editMode.value }
function toggleSelection(skuId: string) {
  cartItems.value = toggleCartItemSelection(cartItems.value, skuId)
  syncPricePreview()
}
function toggleSelectAll() {
  cartItems.value = setCartItemsSelection(cartItems.value, !isAllSelected.value)
  syncPricePreview()
}
function syncPricePreview() {
  refreshCart().catch(error => {
    console.error('计算优惠失败', error)
    if (error instanceof UnauthorizedError) return
    uni.showToast({ title: '优惠计算失败', icon: 'none' })
  })
}
async function increaseQuantity(skuId: string) {
  const it = cartItems.value.find(i => i.skuId === skuId)
  if (it) await updateQuantity(skuId, it.quantity + 1)
}
async function decreaseQuantity(skuId: string) {
  const it = cartItems.value.find(i => i.skuId === skuId)
  if (it && it.quantity > 1) await updateQuantity(skuId, it.quantity - 1)
}
async function updateQuantity(skuId: string, quantity: number) {
  try {
    // 写接口已返回最新购物车与金额，无需再次拉取整份购物车
    applySummary(await updateCartItemQuantity(skuId, quantity))
  } catch (error) {
    console.error('更新购物车数量失败', error)
    if (error instanceof UnauthorizedError) return
    uni.showToast({ title: '更新数量失败', icon: 'none' })
  }
}
function deleteItem(skuId: string) {
  uni.showModal({
    title: '提示',
    content: '确定删除？',
    success: async r => {
      if (!r.confirm) return
      try {
        applySummary(await removeCartItem(skuId))
      } catch (error) {
        console.error('删除购物车商品失败', error)
        if (error instanceof UnauthorizedError) return
        uni.showToast({ title: '删除失败', icon: 'none' })
      }
    }
  })
}
function checkout() {
  if (selectedLineCount.value === 0) { uni.showToast({ title: '请选择商品', icon: 'none' }); return }
  if (selectedItems.value.some(item => item.isValid === false)) {
    uni.showToast({ title: '请先处理不可购买的商品', icon: 'none' })
    return
  }
  uni.navigateTo({ url: '/pages/checkout/index' })
}
/** 管理态批量删除选中的行：复用后端按 SKU 批量移除接口 */
function deleteSelected() {
  const skuIds = selectedItems.value.map(item => item.skuId)
  if (skuIds.length === 0) { uni.showToast({ title: '请选择商品', icon: 'none' }); return }
  uni.showModal({
    title: '提示',
    content: `确定删除选中的 ${skuIds.length} 件商品？`,
    success: async r => {
      if (!r.confirm) return
      try {
        applySummary(await removeCartItems(skuIds))
      } catch (error) {
        console.error('删除购物车商品失败', error)
        if (error instanceof UnauthorizedError) return
        uni.showToast({ title: '删除失败', icon: 'none' })
      }
    }
  })
}
function goToIndex() { uni.switchTab({ url: '/pages/index/index' }) }
</script>

<style lang="scss" scoped>
// ===== 主题令牌 =====
$theme-primary: #2E7D32;
$theme-accent: #FF9800;
$theme-bg: #FCF9F8;

$on-surface: #1b1c1c;
$on-surface-variant: #40493d;
$outline-variant: #bfcaba;
$surface-lowest: #ffffff;
$surface-low: #f6f3f2;
$surface-highest: #e5e2e1;
$herbal-shadow: 0 4rpx 20rpx -4rpx rgba(46, 125, 50, 0.06);
$float-shadow: 0 -8rpx 48rpx -4rpx rgba(33, 33, 33, 0.07);

.page {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
  background: var(--page-bg);
}

.title {
  flex: 1;
  text-align: center;
  font-size: 34rpx;
  font-weight: 700;
  color: $on-surface;
}

// ============ 顶栏 ============
.manage-btn {
  flex-shrink: 0;
  font-size: 26rpx;
  color: $on-surface-variant;

  &.manage-btn-on {
    color: $theme-primary;
  }
}

// ============ main 内容 ============
.page-body {
  flex: 1;
}
.page-body-inner {
  padding: 0 30rpx 240rpx;
}

.empty-wrap {
  padding: 120rpx 60rpx;
  display: flex; flex-direction: column; align-items: center; gap: 32rpx;
}
.empty-desc {
  font-size: 24rpx;
  color: #A9A498;
  text-align: center;
  line-height: 1.6;
}
.empty-btn {
  width: 360rpx;
  height: 80rpx;
  margin-top: 20rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 100rpx;
  background: $theme-primary;
  color: #FFFFFF;
  font-size: 28rpx;
  font-weight: 600;
}

// ============ section 满额免邮横幅 ============
.promo-section {
  display: flex; align-items: center; gap: 12rpx;
  padding: 18rpx 20rpx;
  margin: 20rpx 0;
  border-radius: 18rpx;
  background: #EDF7E7;
  border: 1rpx solid #D9EBCE;
}
.promo-icon {
  width: 46rpx; height: 46rpx; border-radius: 50%;
  background: #DCEFCF;
  display: flex; align-items: center; justify-content: center; flex-shrink: 0;
}
.promo-text {
  flex: 1; min-width: 0;
  font-size: 21rpx; font-weight: 500; color: #40493D;
  white-space: nowrap; overflow: hidden; text-overflow: ellipsis;
}
.promo-gap-hl { font-weight: 700; color: #A85A00; }
.promo-action { display: flex; align-items: center; gap: 2rpx; flex-shrink: 0; }
.promo-action-text { font-size: 23rpx; font-weight: 700; color: $theme-primary; }

// ============ section 店铺分组 ============
.shop-section {
  background: $surface-lowest;
  border-radius: 24rpx; overflow: hidden;
  border: 1rpx solid rgba(229, 226, 225, 0.6);
  box-shadow: $herbal-shadow;
  margin-bottom: 24rpx;
}
.shop-header {
  display: flex; align-items: center; gap: 12rpx;
  padding: 22rpx 28rpx;
  border-bottom: 1rpx solid rgba(229, 226, 225, 0.4);
  background: rgba(246, 243, 242, 0.4);
}
.shop-select { flex-shrink: 0; }
.shop-name-wrap { display: flex; align-items: center; gap: 10rpx; flex: 1; min-width: 0; }
.shop-name { font-size: 28rpx; font-weight: 600; color: $on-surface; }
.shop-badge {
  display: flex; align-items: center; gap: 4rpx;
  background: rgba(46, 125, 50, 0.1); padding: 6rpx 14rpx; border-radius: 100rpx;
  flex-shrink: 0;
}
.shop-badge-text { font-size: 20rpx; color: $theme-primary; font-weight: 500; }

// ============ 商品卡列表 ============
.goods-list { padding: 0 22rpx; }
.goods-card {
  display: flex; gap: 16rpx;
  padding: 22rpx 0;
  border-bottom: 1rpx solid $surface-highest;
  &:last-child { border-bottom: none; }
}
.goods-check {
  // 扩大整行左侧的点击热区，避免只点到 20px 圆点
  width: 64rpx;
  flex-shrink: 0;
  margin-left: -16rpx;
  padding-left: 16rpx;
  padding-top: 4rpx;
  box-sizing: border-box;
  display: flex;
  align-items: flex-start;
}

// 自绘选择框（与设计稿一致的绿色圆形勾选）
.cb {
  width: 40rpx; height: 40rpx; border-radius: 50%;
  border: 2rpx solid #BFCABA;
  background: #ffffff;
  display: flex; align-items: center; justify-content: center;
  box-sizing: border-box;
}
.cb.cb-on {
  background: $theme-primary;
  border-color: $theme-primary;
}

.goods-image-wrap {
  width: 152rpx; height: 152rpx;
  border-radius: 14rpx; overflow: hidden; flex-shrink: 0;
  background: $surface-low;
  position: relative;
}
.goods-image { width: 100%; height: 100%; }
.goods-badge {
  position: absolute; top: 0; left: 0;
  background: #2D5A27;
  padding: 4rpx 12rpx 4rpx 10rpx;
  border-radius: 16rpx 0 12rpx 0;
}
.goods-badge-text { font-size: 18rpx; font-weight: 600; color: #F5F0E8; letter-spacing: 0.5rpx; }

.goods-info { flex: 1; min-width: 0; display: flex; flex-direction: column; gap: 10rpx; }
.goods-name {
  font-size: 26rpx; font-weight: 700; color: $on-surface;
  overflow: hidden; text-overflow: ellipsis; white-space: nowrap;
}

.spec-chip {
  display: inline-flex; align-items: center; gap: 4rpx;
  background: $surface-highest; padding: 6rpx 14rpx; border-radius: 6rpx;
  align-self: flex-start; max-width: 360rpx;
}
.spec-text { font-size: 22rpx; color: $on-surface-variant; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }

.cert-chip {
  display: inline-flex; align-items: center; gap: 4rpx;
  background: rgba(46, 125, 50, 0.06); padding: 4rpx 12rpx; border-radius: 6rpx;
  border: 1rpx solid rgba(46, 125, 50, 0.35);
  align-self: flex-start;
}
.cert-text { font-size: 20rpx; color: $theme-primary; font-weight: 500; }

.stock-tip { display: flex; align-items: center; gap: 4rpx; }
.stock-tip-text { font-size: 20rpx; color: #D32F2F; font-weight: 500; }

.goods-bottom { display: flex; justify-content: space-between; align-items: flex-end; margin-top: 8rpx; }
.price-wrap { display: flex; flex-direction: column; gap: 2rpx; }
.price-row { display: flex; align-items: baseline; gap: 2rpx; }
.price-symbol { font-size: 22rpx; font-weight: 700; color: #8B5000; }
.price-value { font-size: 36rpx; font-weight: 700; color: #8B5000; line-height: 1; }
.price-origin { margin-left: 6rpx; font-size: 20rpx; color: #B0ACA2; text-decoration: line-through; }
.goods-subtotal { font-size: 20rpx; color: #8A8F86; font-weight: 500; }

// stepper
.stepper {
  display: flex; align-items: center;
  border: 1rpx solid #E6E2DA; border-radius: 10rpx;
  background: #FBFAF7; overflow: hidden;
}
.step-btn {
  width: 46rpx; height: 50rpx; display: flex; align-items: center; justify-content: center;
  &.disabled { opacity: 0.3; }
}
.step-val { width: 54rpx; text-align: center; font-size: 25rpx; font-weight: 600; color: $on-surface; }

// 管理态单行操作
.row-actions { display: flex; align-items: center; }
.row-action-btn {
  display: flex; align-items: center; gap: 6rpx;
  padding: 12rpx 20rpx;
  border: 1rpx solid rgba(211, 47, 47, 0.35);
  border-radius: 100rpx;
  background: rgba(211, 47, 47, 0.06);
}
.row-action-text { font-size: 24rpx; font-weight: 600; color: #D32F2F; }

// ============ 赠品区 ============
.gift-block {
  margin: 0 22rpx 22rpx;
  padding: 18rpx 20rpx;
  border-radius: 16rpx;
  background: #FDF6EC;
  border: 1rpx dashed #E9C9A6;
}
.gift-block-title { display: flex; align-items: center; gap: 8rpx; margin-bottom: 12rpx; }
.gift-block-text { font-size: 22rpx; font-weight: 600; color: #A85A00; }
.gift-row { display: flex; align-items: center; gap: 14rpx; padding: 10rpx 0; }
.gift-image {
  width: 88rpx; height: 88rpx; border-radius: 12rpx; flex-shrink: 0;
  background: #ffffff;
}
.gift-info { flex: 1; min-width: 0; display: flex; flex-direction: column; gap: 6rpx; }
.gift-name-line { display: flex; align-items: center; gap: 10rpx; }
.gift-name {
  font-size: 24rpx; font-weight: 600; color: $on-surface;
  overflow: hidden; text-overflow: ellipsis; white-space: nowrap;
}
.gift-tag {
  flex-shrink: 0; padding: 2rpx 10rpx; border-radius: 6rpx;
  background: #E2601F;
}
.gift-tag text { font-size: 18rpx; font-weight: 600; color: #FFFFFF; }
.gift-spec { font-size: 21rpx; color: #8A8F86; }
.gift-promo {
  font-size: 20rpx; color: #A85A00;
  overflow: hidden; text-overflow: ellipsis; white-space: nowrap;
}
.gift-right { display: flex; flex-direction: column; align-items: flex-end; gap: 4rpx; flex-shrink: 0; }
.gift-price { font-size: 28rpx; font-weight: 700; color: #8B5000; }
.gift-qty { font-size: 21rpx; color: #8A8F86; }

// ============ section 道地承诺 三栏 ============
.promise-section {
  display: flex; align-items: center; gap: 8rpx;
  padding: 24rpx 20rpx;
  background: #ffffff;
  border-radius: 24rpx;
  border: 1rpx solid rgba(229, 226, 225, 0.6);
  box-shadow: $herbal-shadow;
  justify-content: space-around;
}
.promise-item {
  display: flex; flex-direction: column; align-items: center; gap: 8rpx;
  flex: 1; min-width: 0;
}
.promise-icon {
  width: 76rpx; height: 76rpx; border-radius: 50%;
  background: rgba(46, 125, 50, 0.08);
  display: flex; align-items: center; justify-content: center;
}
.promise-title { font-size: 23rpx; font-weight: 700; color: $on-surface; }
.promise-sub {
  font-size: 19rpx; color: #8A8F86; text-align: center;
  line-height: 1.3;
}

// ============ 底部结算栏操作按钮（q-checkout-bar 插槽） ============
.checkout-btn {
  flex-shrink: 0;
  height: 76rpx;
  padding: 0 28rpx;
  display: flex; align-items: center; justify-content: center; gap: 6rpx;
  background: $theme-primary;
  border-radius: 100rpx;
  box-shadow: 0 6rpx 18rpx rgba(46, 125, 50, 0.28);
  &.checkout-btn-disabled { opacity: 0.45; box-shadow: none; }
}
.checkout-text {
  font-size: 27rpx; font-weight: 700; color: #ffffff;
}
</style>
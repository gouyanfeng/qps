<template>
  <view class="page">
    <!-- ============ 顶栏：标题 + 管理按钮 ============ -->
    <q-page-header background="#ffffff">
      <text class="title">购物车</text>
      <text :class="['manage-btn', { 'manage-btn-on': editMode }]" @click="toggleEditMode">
        {{ editMode ? '完成' : '管理' }}
      </text>
    </q-page-header>

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
                <image :src="item.productImage" mode="aspectFill" class="goods-image" />
                <view v-if="item.badge" class="goods-badge">
                  <text class="goods-badge-text">{{ item.badge }}</text>
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
                      <text class="price-value">{{ item.price.toFixed(2) }}</text>
                    </view>
                    <text class="goods-subtotal">小计 ¥{{ (item.price * item.quantity).toFixed(2) }}</text>
                  </view>

                  <!-- stepper: 自绘 -/数量/+ -->
                  <view class="stepper">
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
        </section>

        <!-- section 药食同源·超值换购 -->
        <section class="bonus-section">
          <view class="bonus-header">
            <view class="bonus-title-wrap">
              <q-icon name="gift" size="15" color="#B26A00" />
              <text class="bonus-title-text">药食同源 · 超值换购</text>
            </view>
            <text class="bonus-cond">满¥199可用｜已达标（已选 0/1 件）</text>
          </view>

          <view v-for="b in bonusItems" :key="b.id" class="bonus-card">
            <image :src="b.image" mode="aspectFill" class="bonus-image" />
            <view class="bonus-info">
              <text class="bonus-name">{{ b.name }}</text>
              <text class="bonus-desc">{{ b.desc }}</text>
              <view class="bonus-price-row">
                <text class="bonus-price-label">加¥</text>
                <text class="bonus-price">{{ b.price.toFixed(1) }}</text>
                <text class="bonus-origin">¥{{ b.originPrice.toFixed(2) }}</text>
              </view>
            </view>
            <view class="bonus-add-btn" @click="addBonus(b)">
              <text class="bonus-add-text">+ 换购</text>
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

    <!-- ============ 底部固定结算栏 ============ -->
    <view v-if="cartItems.length > 0" class="page-footer">
      <view class="bb-total">
        <view class="bb-total-row">
          <text class="bb-total-label">合计：</text>
          <text class="bb-total-symbol">¥</text>
          <text class="bb-total-price">{{ totalPrice.toFixed(2) }}</text>
        </view>
        <text class="bb-fee">已免运费 ¥12 · 优惠已抵扣</text>
      </view>

      <view :class="['checkout-btn', { 'checkout-btn-disabled': selectedCount === 0 }]" @click="checkout">
        <text class="checkout-text">去结算</text>
        <text v-if="selectedCount > 0" class="checkout-text">({{ selectedCount }})</text>
        <q-icon name="lock-on" size="18" color="#ffffff" />
      </view>
    </view>
  </view>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { onShow } from '@dcloudio/uni-app'
import {
  getCartItems,
  getProducts,
  addToCart,
  updateCartItemQuantity,
  removeCartItem,
  toggleCartItemSelection,
  setCartItemsSelection
} from '@/api'
import type { CartItem, Product } from '@/api/types'
import { UnauthorizedError } from '@/utils/http'

const cartItems = ref<CartItem[]>([])
const editMode = ref(false)

// 换购商品（静态示例，与设计稿一致）
const bonusItems = ref<Array<{
  id: string
  skuId: string
  name: string
  desc: string
  price: number
  originPrice: number
  image: string
  weightGram: number
  specName: string
}>>([])

const selectedItems = computed(() => cartItems.value.filter(i => i.selected))
const selectedCount = computed(() => selectedItems.value.length)
const totalPrice = computed(() => selectedItems.value.reduce((s, i) => s + i.price * i.quantity, 0))
const isAllSelected = computed(() => cartItems.value.length > 0 && selectedCount.value === cartItems.value.length)

onShow(async () => {
  try {
    const [items, pRes] = await Promise.all([getCartItems(), getProducts()])
    cartItems.value = items
    bonusItems.value = pRes.items
      .filter((product: Product) => product.skus.length > 0 && !items.some(item => item.productId === product.id))
      .slice(0, 1)
      .map(mapBonusProduct)
  } catch (error) {
    console.error('加载购物车失败', error)
    if (error instanceof UnauthorizedError) return
    uni.showToast({ title: '购物车加载失败', icon: 'none' })
  }
})

function toggleEditMode() { editMode.value = !editMode.value }
function toggleSelection(skuId: string) {
  cartItems.value = toggleCartItemSelection(cartItems.value, skuId)
}
function toggleSelectAll() {
  cartItems.value = setCartItemsSelection(cartItems.value, !isAllSelected.value)
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
    cartItems.value = await updateCartItemQuantity(skuId, quantity)
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
        cartItems.value = await removeCartItem(skuId)
      } catch (error) {
        console.error('删除购物车商品失败', error)
        if (error instanceof UnauthorizedError) return
        uni.showToast({ title: '删除失败', icon: 'none' })
      }
    }
  })
}
function checkout() {
  if (selectedCount.value === 0) { uni.showToast({ title: '请选择商品', icon: 'none' }); return }
  if (selectedItems.value.some(item => item.isValid === false)) {
    uni.showToast({ title: '请先处理不可购买的商品', icon: 'none' })
    return
  }
  uni.navigateTo({ url: '/pages/checkout/index' })
}
function goToIndex() { uni.switchTab({ url: '/pages/index/index' }) }
async function addBonus(b: (typeof bonusItems.value)[number]) {
  try {
    cartItems.value = await addToCart({
    productId: b.id,
    productName: b.name,
    productImage: b.image,
    skuId: b.skuId,
    specName: b.specName,
    price: b.price,
    quantity: 1,
    weightGram: b.weightGram,
    selected: true
  })
    uni.showToast({ title: '已加入购物车', icon: 'none' })
  } catch (error) {
    console.error('加入购物车失败', error)
    if (error instanceof UnauthorizedError) return
    uni.showToast({ title: '加入购物车失败', icon: 'none' })
  }
}
function mapBonusProduct(product: Product) {
  const sku = product.skus[0]
  return {
    id: product.id,
    skuId: sku.id,
    name: product.name,
    desc: product.description,
    price: sku.price,
    originPrice: sku.price * 1.15,
    image: product.images[0] || '',
    weightGram: sku.weightGram,
    specName: sku.specName
  }
}
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
.goods-check { padding-top: 4rpx; flex-shrink: 0; }

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

// ============ section 药食同源·超值换购 ============
.bonus-section {
  margin-bottom: 24rpx;
  padding: 22rpx 24rpx;
  background: #FFFDF6;
  border: 1rpx solid #F0DCB0;
  border-radius: 24rpx;
  box-shadow: $herbal-shadow;
}
.bonus-header {
  display: flex; align-items: center; justify-content: space-between;
  gap: 12rpx; margin-bottom: 16rpx;
}
.bonus-title-wrap { display: flex; align-items: center; gap: 8rpx; flex-shrink: 0; }
.bonus-title-text { font-size: 27rpx; font-weight: 700; color: #8B5000; }
.bonus-cond { font-size: 19rpx; color: #A98B5E; text-align: right; }

.bonus-card {
  display: flex; align-items: center; gap: 18rpx;
  padding: 16rpx;
  background: #ffffff;
  border: 1rpx solid rgba(240, 220, 176, 0.7);
  border-radius: 18rpx;
}
.bonus-image {
  width: 132rpx; height: 132rpx; border-radius: 14rpx;
  background: $surface-low; flex-shrink: 0;
}
.bonus-info { flex: 1; min-width: 0; display: flex; flex-direction: column; gap: 6rpx; }
.bonus-name {
  font-size: 25rpx; font-weight: 600; color: $on-surface;
  overflow: hidden; text-overflow: ellipsis; white-space: nowrap;
}
.bonus-desc { font-size: 20rpx; color: #8A8F86; }
.bonus-price-row { display: flex; align-items: baseline; gap: 4rpx; }
.bonus-price-label { font-size: 20rpx; font-weight: 700; color: $theme-accent; }
.bonus-price { font-size: 34rpx; font-weight: 700; color: $theme-accent; line-height: 1; }
.bonus-origin {
  font-size: 19rpx; color: #B0B5AC;
  text-decoration: line-through; margin-left: 8rpx;
}
.bonus-add-btn {
  flex-shrink: 0;
  height: 56rpx; padding: 0 26rpx;
  display: flex; align-items: center; justify-content: center;
  background: $theme-accent;
  border-radius: 100rpx;
  box-shadow: 0 4rpx 12rpx rgba(255, 152, 0, 0.3);
}
.bonus-add-text { font-size: 23rpx; font-weight: 700; color: #ffffff; }

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

// ============ 底部固定结算栏 ============
.page-footer {
  position: fixed;
  bottom: var(--window-bottom, 0);
  left: 0; right: 0; z-index: 50;
  display: flex; align-items: center; gap: 14rpx;
  background: #ffffff;
  border-top: 1rpx solid rgba(232, 227, 214, 0.9);
  padding: 14rpx 24rpx;
  padding-bottom: calc(14rpx + env(safe-area-inset-bottom));
}

.bb-total { flex: 1; min-width: 0; display: flex; flex-direction: column; align-items: flex-start; gap: 4rpx; }
.bb-total-row { display: flex; align-items: baseline; gap: 4rpx; }
.bb-total-label { font-size: 23rpx; color: $on-surface-variant; font-weight: 500; }
.bb-total-symbol { font-size: 24rpx; font-weight: 700; color: $theme-primary; }
.bb-total-price { font-size: 44rpx; font-weight: 800; color: $theme-primary; line-height: 1; letter-spacing: -0.5rpx; }
.bb-fee { font-size: 19rpx; color: #8A8F86; }

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
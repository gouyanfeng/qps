<template>
  <view v-if="product" class="page">
    <!-- ============ 顶栏：与其他页面统一 ============ -->
    <q-page-header-back :title="product.name" />

    <scroll-view scroll-y class="page-body" enhanced>
      <!-- ============ section 大图轮播 ============ -->
      <view class="swiper-section">
        <swiper
          class="product-swiper"
          circular
          autoplay
          interval="4000"
          :current="swiperIdx"
          @change="onSwiperChange"
        >
          <swiper-item v-for="(img, idx) in product.images" :key="idx">
            <image :src="img" mode="aspectFill" class="swiper-img" />
          </swiper-item>
        </swiper>

        <view v-if="product.tags.length" class="gap-badge">
          <text>{{ product.tags[0].name }}</text>
        </view>

        <!-- 右下角分式指示器 + 缩放放大镜 -->
        <view class="swiper-indicator">
          <view class="frac-indicator">
            <text>{{ swiperIdx + 1 }}/{{ product.images.length }}</text>
          </view>
          <view class="zoom-btn">
            <q-icon name="search" size="14" color="#ffffff" />
          </view>
        </view>
      </view>

      <!-- ============ section 价格信息卡 ============ -->
      <view class="price-card">
        <view class="price-row">
          <view class="price-main">
            <text class="price-symbol">¥</text>
            <text class="price-value">{{ currentSku.price }}</text>
            <text v-if="currentSku.marketPrice > 0" class="price-original">¥{{ currentSku.marketPrice }}</text>
          </view>
          <view v-if="product.tags.length" class="cert-pill">
            <text>{{ product.tags[0].name }}</text>
          </view>
        </view>
        <text class="product-name">{{ product.name }}</text>
        <view v-if="product.tags.length" class="product-tags-row">
          <text v-for="tag in product.tags" :key="tag.id" class="product-tag">{{ tag.name }}</text>
        </view>
      </view>

      <!-- ============ section 规格选择卡 #spec-group ============ -->
      <view id="spec-group" class="spec-card">
        <view class="spec-head">
          <view class="spec-head-left">
            <text class="spec-title">选择规格</text>
            <view v-if="product.tags.length" class="spec-tag"><text>{{ product.tags[0].name }}</text></view>
          </view>
          <text class="spec-stock">库存 {{ currentSku.stock }} 件</text>
        </view>

        <view class="spec-grid">
          <view
            v-for="sku in product.skus"
            :key="sku.id"
            :class="['spec-option', { active: isSelected('规格', sku.specName) }]"
            @click="selectSpec('规格', sku.specName)"
          >
            <view class="spec-option-top">
              <text class="spec-option-name">{{ sku.specName }}</text>
              <view v-if="isSelected('规格', sku.specName)" class="spec-check">
                <q-icon name="check" size="10" color="#ffffff" />
              </view>
            </view>
            <view class="spec-option-price">
              <text class="spec-price">¥{{ sku.price }}</text>
              <text class="spec-unit">{{ unitPriceText(sku) }}</text>
            </view>
            <text :class="['spec-option-stock', { tight: sku.stock > 0 && sku.stock <= 3 }]">
              {{ stockText(sku) }}
            </text>
          </view>
        </view>

        <view class="qty-row">
          <text class="qty-label">购买数量</text>
          <view class="qty-stepper">
            <view id="btn-minus" class="qty-btn" @click="quantity > 1 && quantity--">
              <q-icon name="minus" size="16" color="#40493D" />
            </view>
            <text id="quantity-val" class="qty-val">{{ quantity }}</text>
            <view id="btn-plus" class="qty-btn" @click="quantity++">
              <q-icon name="add" size="16" color="#1B1C1C" />
            </view>
          </view>
        </view>

        <view class="promise-row">
          <view class="promise-item">
            <q-icon name="snowflake" size="26" color="#2E7D32" />
            <text class="promise-title">冷链直发</text>
            <text class="promise-sub">恒温锁鲜直达</text>
          </view>
          <view class="promise-divider"></view>
          <view class="promise-item">
            <q-icon name="qrcode" size="26" color="#2E7D32" />
            <text class="promise-title">一物一码</text>
            <text class="promise-sub">药典全程溯源</text>
          </view>
          <view class="promise-divider"></view>
          <view class="promise-item">
            <q-icon name="shield-error" size="26" color="#2E7D32" />
            <text class="promise-title">破损包赔</text>
            <text class="promise-sub">开箱验货无忧</text>
          </view>
        </view>
      </view>

      <!-- ============ section 商品介绍区长图流 ============ -->
      <view class="intro-section">
        <text class="intro-title">商品介绍</text>

        <view class="intro-img-wrap h-800">
          <image :src="'/static/images/photo-1501004318641-b39e6451bec6.jpg'" mode="widthFix" class="intro-img" />
          <view class="intro-caption"><text>道地产区 · 吉林长白山核心溯源基地</text></view>
        </view>

        <view class="intro-img-wrap h-600">
          <image :src="'/static/images/photo-1549737557-0e4e05d31f9c.jpg'" mode="widthFix" class="intro-img" />
          <view class="intro-caption"><text>十五年足龄 · 芦头清晰珍珠疙瘩密布</text></view>
        </view>

        <view class="intro-img-wrap h-500">
          <image :src="'/static/images/photo-1556228578-8c89e6adf883.jpg'" mode="widthFix" class="intro-img" />
          <view class="intro-caption"><text>古法生晒 · 低温慢烘保留皂苷活性</text></view>
        </view>

        <view class="intro-img-wrap h-700">
          <image :src="'/static/images/photo-1599909533730-3c5c6589a060.jpg'" mode="widthFix" class="intro-img" />
          <view class="intro-caption"><text>国风礼盒 · 烫金logo · 送礼有面</text></view>
        </view>
      </view>

      <view class="safe-space"></view>
    </scroll-view>

    <!-- ============ 底部固定栏 ============ -->
    <view class="page-footer">
      <view class="bb-icon" @click="consult">
        <q-icon name="service" size="22" color="#40493D" />
        <text>药师咨询</text>
      </view>
      <view class="bb-icon" @click="goCart">
        <q-icon name="cart" size="22" color="#40493D" />
        <text>购物车</text>
      </view>
      <view class="bb-btn ghost" @click="addToCart">加入购物车</view>
      <view class="bb-btn solid" @click="buyNow">立即购买</view>
    </view>
  </view>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { onLoad } from '@dcloudio/uni-app'
import { getProductById, addToCart as addCart } from '@/api'
import type { Product, Sku } from '@/api/types'
import { UnauthorizedError } from '@/utils/http'

const product = ref<Product | null>(null)
const quantity = ref(1)
const selectedSpecs = ref<Record<string, string>>({})

const swiperIdx = ref(0)

onLoad(async (opts) => {
  if (!opts?.id) return
  try {
    product.value = (await getProductById(opts.id)) ?? null
    const firstSku = product.value?.skus[0]
    if (firstSku) selectedSpecs.value = { 规格: firstSku.specName }
  } catch (error) {
    console.error('加载商品详情失败', error)
    if (error instanceof UnauthorizedError) return
    uni.showToast({ title: '商品详情加载失败', icon: 'none' })
  }
})

const currentSku = computed(() => {
  const skus = product.value?.skus ?? []
  return skus.find(s => s.specName === selectedSpecs.value['规格']) ?? skus[0] ?? { id: '', specName: '', price: 0, marketPrice: 0, stock: 0, weightGram: 0 }
})

function unitPriceText(sku: Sku) {
  return sku.weightGram > 0 ? `单克 ¥${(sku.price / sku.weightGram).toFixed(2)}` : '单规格'
}

function stockText(sku: Sku) {
  if (sku.stock <= 0) return '暂时缺货'
  return sku.stock <= 3 ? `仅剩 ${sku.stock} 件` : '库存充足'
}

function isSelected(name: string, opt: string) { return selectedSpecs.value[name] === opt }
function selectSpec(name: string, opt: string) { selectedSpecs.value[name] = opt }

function onSwiperChange(e: { detail: { current: number } }) { swiperIdx.value = e.detail.current }
function consult() { uni.showToast({ title: '正在连接药师…', icon: 'none' }) }

async function addToCart(): Promise<boolean> {
  if (!product.value) return false
  try {
    await addCart({
      skuId: currentSku.value.id,
      productId: product.value.id,
      productName: product.value.name,
      productImage: product.value.images[0] || '',
      specName: currentSku.value.specName || '默认规格',
      price: currentSku.value.price,
      quantity: quantity.value,
      weightGram: currentSku.value.weightGram,
      selected: true,
      badge: product.value.tags[0]?.name,
      certText: product.value.tags[1]?.name || product.value.tags[0]?.name
    })
    uni.showToast({ title: '已加入购物车', icon: 'success' })
    return true
  } catch (error) {
    console.error('加入购物车失败', error)
    if (error instanceof UnauthorizedError) return false
    uni.showToast({ title: '加入购物车失败', icon: 'none' })
    return false
  }
}
async function buyNow() {
  if (!await addToCart()) return
  setTimeout(() => uni.navigateTo({ url: '/pages/checkout/index' }), 500)
}
function goCart() { uni.switchTab({ url: '/pages/cart/index' }) }
</script>

<style lang="scss" scoped>
$primary: #2E7D32;
$primary-dark: #1B5E20;
$price: #8B5000;
$surface: #ffffff;
$on-surface: #1B1C1C;
$on-surface-var: #40493D;

$shadow-card: 0 4rpx 20rpx -4rpx rgba(46, 125, 50, 0.08);

.page {
  display: flex; flex-direction: column; min-height: 100vh; background: var(--page-bg); }

.page-body {
  flex: 1;
  padding-bottom: 200rpx;
}

/* ===== section 大图轮播 ===== */
.swiper-section {
  position: relative;
  margin: 20rpx 24rpx 0;
  border-radius: 24rpx;
  overflow: hidden;
}
.product-swiper { width: 702rpx; height: 526rpx; }
.swiper-img { width: 100%; height: 100%; }

.gap-badge {
  position: absolute; top: 24rpx; left: 24rpx; z-index: 5;
  display: flex; align-items: center; gap: 6rpx;
  font-size: 22rpx; font-weight: 600; color: $primary;
  background: rgba(255, 255, 255, 0.96);
  padding: 10rpx 18rpx; border-radius: 10rpx;
  box-shadow: 0 4rpx 16rpx rgba(0,0,0,0.12);
}

.swiper-indicator {
  position: absolute; bottom: 24rpx; right: 24rpx; z-index: 5;
  display: flex; align-items: center; gap: 10rpx;
}
.frac-indicator {
  background: rgba(0,0,0,0.55);
  padding: 8rpx 20rpx; border-radius: 24rpx;
}
.frac-indicator text { font-size: 22rpx; color: #ffffff; font-weight: 600; letter-spacing: 1rpx; }
.zoom-btn {
  width: 52rpx; height: 52rpx; border-radius: 50%;
  background: rgba(0,0,0,0.55);
  display: flex; align-items: center; justify-content: center;
}

/* ===== section 价格信息卡 ===== */
.price-card {
  margin: 24rpx 24rpx 0; padding: 30rpx 28rpx;
  background: $surface; border-radius: 24rpx;
  box-shadow: $shadow-card;
}
.price-row {
  display: flex; align-items: flex-end; justify-content: space-between;
  gap: 16rpx;
}
.price-main { display: flex; align-items: baseline; gap: 4rpx; min-width: 0; }
.price-symbol { font-size: 28rpx; font-weight: 700; color: $price; }
.price-value { font-size: 60rpx; font-weight: 800; color: $price; line-height: 1; letter-spacing: -2rpx; }
.price-original { font-size: 24rpx; color: #B0ACA9; text-decoration: line-through; margin-left: 12rpx; }
.cert-pill {
  flex-shrink: 0;
  display: flex; align-items: center; gap: 6rpx;
  padding: 8rpx 16rpx;
  background: #F3F8F1; border: 1rpx solid rgba(46, 125, 50, 0.24);
  border-radius: 100rpx;
}
.cert-pill text { font-size: 19rpx; font-weight: 600; color: $primary; }

.product-name {
  display: block;
  font-family: 'Noto Serif SC', 'Songti SC', 'STSong', serif;
  font-size: 32rpx; font-weight: 700;
  color: $on-surface; line-height: 1.375;
  letter-spacing: -0.5rpx;
  margin: 20rpx 0;
}
.meta-row {
  display: flex; flex-wrap: wrap; align-items: center;
  gap: 8rpx 0;
  padding-top: 16rpx;
  border-top: 1rpx solid #EFEEEB;
}
.meta-item { font-size: 19rpx; color: #8A8578; }
.meta-divider { width: 1rpx; height: 20rpx; background: #E5E2E1; margin: 0 16rpx; }

/* ===== section 规格选择卡 #spec-group ===== */
#spec-group {
  margin: 20rpx 24rpx 0; padding: 28rpx;
  background: $surface; border-radius: 24rpx;
  box-shadow: $shadow-card;
}
.spec-head {
  display: flex; align-items: center; justify-content: space-between;
  margin-bottom: 20rpx;
}
.spec-head-left { display: flex; align-items: center; gap: 12rpx; }
.spec-title { font-size: 30rpx; font-weight: 700; color: $on-surface; }
.spec-tag { padding: 4rpx 12rpx; background: #F3F8F1; border-radius: 6rpx; }
.spec-tag text { font-size: 18rpx; color: $primary; font-weight: 500; }
.spec-stock { font-size: 20rpx; color: #9E9E9E; }

.spec-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 16rpx;
}
.spec-option {
  position: relative;
  box-sizing: border-box;
  min-height: 150rpx;
  padding: 18rpx 18rpx 16rpx;
  background: #ffffff;
  border: 2rpx solid #EDEAE7;
  border-radius: 16rpx;
  transition: background 0.15s ease, border-color 0.15s ease;

  &.active {
    background: #F3F8F1;
    border-color: $primary;
  }
}
.spec-option-top { display: flex; align-items: flex-start; padding-right: 28rpx; }
.spec-option-name {
  font-size: 27rpx; font-weight: 700; color: $on-surface;
  line-height: 1.35;
}
.spec-check {
  position: absolute; top: 0; right: 0;
  width: 36rpx; height: 36rpx;
  background: $primary;
  border-radius: 0 14rpx 0 14rpx;
  display: flex; align-items: center; justify-content: center;
}
.spec-option-price {
  display: flex; align-items: baseline; gap: 8rpx;
  margin-top: 10rpx;
}
.spec-price { font-size: 26rpx; font-weight: 700; color: $price; }
.spec-unit { font-size: 18rpx; color: #B0ACA9; }
.spec-option-stock {
  display: block; margin-top: 8rpx;
  font-size: 18rpx; color: #8A8578;
  &.tight { color: #B26A00; }
}

.qty-row {
  display: flex; justify-content: space-between; align-items: center;
  margin-top: 20rpx; padding-top: 20rpx;
  border-top: 1rpx solid #EFEEEB;
}
.qty-label { font-size: 24rpx; font-weight: 600; color: $on-surface; }
.qty-stepper {
  display: flex; align-items: center;
  border: 1rpx solid #E5E2E1;
  border-radius: 10rpx; overflow: hidden; background: #ffffff;
}
.qty-btn {
  width: 64rpx; height: 64rpx;
  display: flex; align-items: center; justify-content: center;
}
.qty-val {
  width: 66rpx; text-align: center;
  font-size: 26rpx; font-weight: 700; color: $on-surface;
}

.promise-row {
  display: flex; align-items: center;
  margin-top: 20rpx; padding-top: 20rpx;
  border-top: 1rpx solid #EFEEEB;
}
.promise-item {
  flex: 1;
  display: flex; flex-direction: column; align-items: center; gap: 4rpx;
}
.promise-title { font-size: 20rpx; font-weight: 600; color: $on-surface-var; }
.promise-sub { font-size: 16rpx; color: #A8A29A; }
.promise-divider { width: 1rpx; height: 44rpx; background: #EFEEEB; }

/* ===== section 商品介绍区长图流 ===== */
.intro-section {
  margin-top: 24rpx;
  background: transparent;
}
.intro-title {
  display: block;
  padding: 20rpx 24rpx 20rpx;
  font-size: 30rpx; font-weight: 700; color: $on-surface;
}
.intro-img-wrap {
  position: relative;
  margin: 0 24rpx 20rpx;
  border-radius: 20rpx;
  overflow: hidden;
  line-height: 0;      /* 消除 image 底部缝隙 */
}
.intro-img {
  width: 100%;
  display: block;        /* 消除 inline 缝隙, 100%屏宽 */
}
.intro-caption {
  position: absolute; left: 0; right: 0; bottom: 0;
  padding: 40rpx 32rpx 28rpx;
  background: linear-gradient(to top, rgba(0,0,0,0.6) 0%, transparent 100%);
}
.intro-caption text {
  font-size: 26rpx; color: #ffffff; font-weight: 500; letter-spacing: 1rpx;
}

/* 固定高度占位(配合 widthFix 保持稳定比例) */
.h-800 { height: 800rpx; }
.h-600 { height: 600rpx; }
.h-500 { height: 500rpx; }
.h-700 { height: 700rpx; }

.safe-space { height: 200rpx; }

/* ===== 底部固定栏 ===== */
.page-footer {
  position: fixed; bottom: 0; left: 0; right: 0; z-index: 50;
  background: #ffffff;
  padding: 14rpx 20rpx;
  padding-bottom: calc(14rpx + env(safe-area-inset-bottom));
  display: flex; align-items: center; gap: 12rpx;
}
.bb-icon {
  display: flex; flex-direction: column; align-items: center;
  padding: 4rpx 12rpx; gap: 2rpx;
  font-size: 20rpx; color: $on-surface-var;
}
.bb-btn {
  flex: 1; height: 88rpx; line-height: 88rpx; text-align: center;
  border-radius: 44rpx; font-size: 28rpx; font-weight: 600;

  &.ghost {
    background: #ffffff;
    border: 2rpx solid $primary;
    color: $primary;
  }
  &.solid {
    background: linear-gradient(135deg, $primary 0%, $primary-dark 100%);
    color: #ffffff;
  }
}
</style>
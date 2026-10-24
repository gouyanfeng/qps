<template>
  <view class="page">
    <!-- ============ 顶栏：定位 + 馆名 + 消息 ============ -->
    <q-page-header background="#ffffff">
      <view class="location-chip">
        <q-icon name="location" size="26" color="#2E7D32" />
        <text class="location-text">杭州·国医直发仓</text>
        <q-icon name="chevron-down" size="20" color="#707A6C" />
      </view>
      <text class="brand-name">本草国医馆</text>
      <view class="action-btn">
        <q-icon name="notification" size="30" color="#1B1C1C" />
        <view class="badge-dot"></view>
      </view>
    </q-page-header>
    <view class="page-subbar">
      <view class="search-bar" @click="goToSearch">
        <q-icon name="search" size="24" color="#707A6C" />
        <input
          class="search-input"
          type="text"
          placeholder="搜索道地人参、正品黄芪、新会陈皮"
          placeholder-class="search-placeholder"
          disabled
        />
        <q-icon name="microphone" size="24" color="#9E9E9E" />
      </view>
      <view class="verify-btn" @click="handleVerify">
        <q-icon name="qrcode" size="22" color="#ffffff" />
        <text class="verify-text">查真伪</text>
      </view>
    </view>

    <!-- ============ main 主内容区 ============ -->
    <scroll-view scroll-y class="page-body" enhanced>
      <!-- ===== Hero Banner ===== -->
      <view class="hero-banner" @click="goToCategory">
        <view class="hero-info">
          <view class="hero-tag">
            <q-icon name="check-circle-filled" size="17" color="#ffffff" />
            <text class="hero-tag-text">原产地溯源保真 · 药典级正品</text>
          </view>
          <text class="hero-title">道地药材 · 健康之选</text>
          <text class="hero-desc">古法炮制 · 一物一码溯源 · 核心产区直供</text>
          <view class="hero-cta">
            <text class="hero-cta-text">抢金秋养生券</text>
            <q-icon name="chevron-right" size="18" color="#ffffff" />
          </view>
        </view>
        <image class="hero-image" :src="'/static/images/hero-herbs.jpg'" mode="aspectFill" />
      </view>

      <!-- ===== 品质背书横条 ===== -->
      <view class="trust-bar">
        <view class="trust-item">
          <q-icon name="tree-round-dot" size="24" color="#2E7D32" />
          <text class="trust-text">GAP道地直采</text>
        </view>
        <view class="trust-divider"></view>
        <view class="trust-item">
          <q-icon name="certificate" size="24" color="#2E7D32" />
          <text class="trust-text">中检院质检</text>
        </view>
        <view class="trust-divider"></view>
        <view class="trust-item">
          <q-icon name="shield-error" size="24" color="#2E7D32" />
          <text class="trust-text">破损无忧赔</text>
        </view>
        <view class="trust-divider"></view>
        <view class="trust-item">
          <q-icon name="snowflake" size="24" color="#2E7D32" />
          <text class="trust-text">冷链恒温达</text>
        </view>
      </view>

      <!-- ===== 6 分类入口 ===== -->
      <view class="category-grid">
        <view
          v-for="(item, idx) in categoryItems"
          :key="idx"
          class="category-item"
          @click="goToCategory"
        >
          <image class="category-dot" :src="item.icon" mode="aspectFill" />
          <text class="category-name">{{ item.name }}</text>
          <text class="category-origin">{{ item.origin }}</text>
        </view>
      </view>

      <!-- ===== 节气养生卡 ===== -->
      <view class="solar-card" @click="goToCategory">
        <view class="solar-icon-wrap">
          <q-icon name="sunny" size="40" color="#F57C00" />
        </view>
        <view class="solar-text">
          <view class="solar-title-row">
            <view class="solar-tag">当季精选</view>
            <text class="solar-title">节气养生 · 润秋燥</text>
          </view>
          <text class="solar-desc">百合、麦冬、新会陈皮搭配调理，益肺润津</text>
        </view>
        <q-icon name="chevron-right" size="30" color="#8B5E1F" />
      </view>

      <!-- ===== 道地精选商品网格 ===== -->
      <view class="product-section">
        <view class="section-header">
          <text class="section-title">道地精选</text>
          <view class="shield-chip">
            <text class="shield-text">药典标准 · 假一赔十</text>
          </view>
          <view class="section-more" @click="goToCategory">
            <text>查看全部</text>
            <q-icon name="chevron-right" size="18" color="#2E7D32" />
          </view>
        </view>

        <view class="product-grid">
          <view
            v-for="p in featured"
            :key="p.id"
            class="product-card"
            @click="goToProductDetail(p.id)"
          >
            <image :src="p.image" mode="aspectFill" class="product-image" />
            <view class="product-info">
              <view class="product-tags">
                <view class="mini-tag" :style="{ borderColor: p.tone, color: p.tone }">
                  <text>{{ p.cert }}</text>
                </view>
                <text class="mini-tag-sub">{{ p.certSub }}</text>
              </view>
              <text class="product-name">{{ p.name }}</text>
              <text class="product-spec">规格：{{ p.spec }}</text>
              <view class="product-bottom">
                <view class="price-col">
                  <view class="price-row">
                    <text class="price-symbol">¥</text>
                    <text class="price-value">{{ p.price }}</text>
                  </view>
                  <text class="sales-text">已售 {{ p.sales }}+</text>
                </view>
                <view class="add-cart-btn" @click.stop="quickAdd(p)">
                  <q-icon name="add" size="22" color="#ffffff" />
                </view>
              </view>
            </view>
          </view>
        </view>
      </view>

      <!-- ===== footer ===== -->
      <view class="footer">
        <text class="footer-text">— 大医精诚 · 悬壶济世 · 道地保真 —</text>
      </view>
    </scroll-view>
  </view>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { getProductById, addToCart } from '@/api'

interface FeaturedItem {
  id: string
  name: string
  image: string
  price: string
  spec: string
  cert: string
  certSub: string
  tone: string
  sales: string
}

const categoryItems = [
  { name: '人参', origin: '吉林抚松', icon: '/static/images/cat-icon-1.jpg' },
  { name: '饮片', origin: '甘肃岷县', icon: '/static/images/cat-icon-2.jpg' },
  { name: '滋补', origin: '养生膏方', icon: '/static/images/cat-icon-3.jpg' },
  { name: '花茶', origin: '平阴玫瑰', icon: '/static/images/cat-icon-4.jpg' },
  { name: '膏方', origin: '古法熬制', icon: '/static/images/cat-icon-5.jpg' },
  { name: '颗粒', origin: '国医对症', icon: '/static/images/cat-icon-6.jpg' }
]

/** 与设计稿一致的商品顺序与卖点文案 */
const featuredMeta = [
  { id: 'prod-101', cert: 'GAP道地认证', certSub: '支支防伪', tone: '#2E7D32', sales: '2400', spec: '50g（8支精装）' },
  { id: 'prod-102', cert: '干仓储存', certSub: '茶香浓郁', tone: '#D97700', sales: '5100', spec: '250g密封金罐' },
  { id: 'prod-103', cert: '头茬免洗', certSub: '泡水上浮', tone: '#2E7D32', sales: '1.2万', spec: '500g充氮双罐' },
  { id: 'prod-104', cert: '气血双补', certSub: '油润微辛', tone: '#2E7D32', sales: '8700', spec: '300g锁鲜装' },
  { id: 'prod-105', cert: '天然花青素', certSub: '舒肝解郁', tone: '#C2185B', sales: '6400', spec: '120g精制茶罐' },
  { id: 'prod-106', cert: '真驴皮熬制', certSub: '浓厚滋补', tone: '#D97700', sales: '3200', spec: '400g独立片装礼盒' }
]

const featured = ref<FeaturedItem[]>([])

onMounted(() => {
  featured.value = featuredMeta
    .map((meta) => {
      const product = getProductById(meta.id)
      if (!product) return null
      const sku = product.skus[0]
      return {
        ...meta,
        name: product.name,
        image: product.images[0],
        price: sku.price % 1 === 0 ? String(sku.price) : sku.price.toFixed(1)
      } as FeaturedItem
    })
    .filter((item): item is FeaturedItem => item !== null)
})

function quickAdd(item: FeaturedItem) {
  const product = getProductById(item.id)
  if (!product) return
  const sku = product.skus[0]
  addToCart({
    productId: product.id,
    productName: product.name,
    productImage: product.images[0],
    skuId: sku.id,
    specName: sku.specName,
    price: sku.price,
    quantity: 1,
    weightGram: sku.weightGram,
    selected: true,
    certText: item.cert
  })
  uni.showToast({ title: '已加入购物车', icon: 'none' })
}

function goToSearch() { uni.switchTab({ url: '/pages/search/index' }) }
function goToCategory() { uni.switchTab({ url: '/pages/category/index' }) }
function goToProductDetail(id: string) { uni.navigateTo({ url: `/pages/product-detail/index?id=${id}` }) }
function handleVerify() { uni.showToast({ title: '扫码验真功能演示', icon: 'none' }) }
</script>

<style lang="scss" scoped>
// ===== 设计 token =====
$primary: #2E7D32;
$primary-dark: #1B5E20;
$primary-light: #E8F5E9;
$accent: #FF9800;
$bg: #FCF9F8;
$card: #FFFFFF;
$text-primary: #1B1C1C;
$text-secondary: #6B6B6B;
$text-hint: #9E9E9E;
$line: #E8E5E3;
$shadow-soft: 0 4rpx 20rpx -6rpx rgba(46, 125, 50, 0.1);
$shadow-card: 0 8rpx 28rpx -8rpx rgba(46, 125, 50, 0.18);

.page {
    display: flex;
  flex-direction: column;
  min-height: 100vh;
  background: var(--page-bg);
}

// ============ 顶栏内容 ============
.location-chip {
  display: flex;
  align-items: center;
  gap: 4rpx;
}

.location-text {
  font-size: 26rpx;
  color: $text-primary;
  font-weight: 500;
}

.brand-name {
  font-size: 38rpx;
  font-weight: 800;
  color: $primary-dark;
  letter-spacing: 3rpx;
}

.action-btn {
  position: relative;
  width: 52rpx;
  height: 52rpx;
  display: flex;
  align-items: center;
  justify-content: center;
}

.badge-dot {
  position: absolute;
  top: 4rpx;
  right: 4rpx;
  width: 14rpx;
  height: 14rpx;
  border-radius: 50%;
  background: #F57C00;
}

// ============ 搜索副栏 ============
.page-subbar {
  display: flex;
  align-items: center;
  gap: 16rpx;
  padding: 16rpx 24rpx 0;
}

.search-bar {
  flex: 1;
  display: flex;
  align-items: center;
  gap: 12rpx;
  height: 76rpx;
  padding: 0 22rpx;
  background: #ffffff;
  border: 2rpx solid $line;
  border-radius: 38rpx;
}

.search-input {
  flex: 1;
  font-size: 24rpx;
  color: $text-primary;
}

.search-placeholder {
  color: #8D8D8D;
  font-size: 24rpx;
}

.verify-btn {
  display: flex;
  align-items: center;
  gap: 6rpx;
  height: 76rpx;
  padding: 0 22rpx;
  background: $primary-dark;
  border-radius: 38rpx;
}

.verify-text {
  font-size: 24rpx;
  font-weight: 700;
  color: #ffffff;
}

.page-body {
  flex: 1;
  padding-bottom: 120rpx;
}

// ============ Hero Banner ============
.hero-banner {
  position: relative;
  margin: 20rpx 24rpx 0;
  padding: 26rpx 26rpx 28rpx;
  border-radius: 22rpx;
  overflow: hidden;
  background:
    radial-gradient(circle at 12rpx 12rpx, rgba(255, 255, 255, 0.10) 2rpx, transparent 3rpx) 0 0 / 44rpx 44rpx,
    linear-gradient(135deg, #23611F 0%, #2E7D32 60%, #2F8036 100%);
  box-shadow: $shadow-card;
}

.hero-info {
  position: relative;
  z-index: 2;
  padding-right: 200rpx;
  display: flex;
  flex-direction: column;
}

.hero-tag {
  display: inline-flex;
  align-items: center;
  gap: 6rpx;
  align-self: flex-start;
  padding: 6rpx 16rpx;
  border: 1rpx solid rgba(255, 255, 255, 0.35);
  background: rgba(255, 255, 255, 0.12);
  border-radius: 100rpx;
}

.hero-tag-text {
  font-size: 19rpx;
  color: #ffffff;
}

.hero-title {
  margin-top: 22rpx;
  font-size: 42rpx;
  font-weight: 800;
  color: #ffffff;
  letter-spacing: 2rpx;
}

.hero-desc {
  margin-top: 10rpx;
  font-size: 23rpx;
  color: rgba(255, 255, 255, 0.88);
}

.hero-cta {
  display: inline-flex;
  align-items: center;
  gap: 4rpx;
  align-self: flex-start;
  margin-top: 24rpx;
  padding: 14rpx 26rpx;
  background: linear-gradient(135deg, #F57C00 0%, #FB8C00 100%);
  border-radius: 100rpx;
  box-shadow: 0 8rpx 20rpx -6rpx rgba(245, 124, 0, 0.7);
}

.hero-cta-text {
  font-size: 24rpx;
  font-weight: 700;
  color: #ffffff;
}

.hero-image {
  position: absolute;
  top: 26rpx;
  right: 24rpx;
  width: 178rpx;
  height: 178rpx;
  border-radius: 16rpx;
  z-index: 1;
}

// ============ 品质背书 ============
.trust-bar {
  display: flex;
  align-items: center;
  margin: 20rpx 24rpx 0;
  padding: 24rpx 6rpx;
  background: $card;
  border-radius: 20rpx;
  box-shadow: $shadow-soft;
}

.trust-item {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6rpx;
}

.trust-text {
  font-size: 19rpx;
  color: $text-primary;
  font-weight: 500;
  white-space: nowrap;
}

.trust-divider {
  width: 1rpx;
  height: 40rpx;
  background: $line;
}

// ============ 分类入口 ============
.category-grid {
  display: flex;
  margin: 20rpx 24rpx 0;
  padding: 24rpx 8rpx 22rpx;
  background: $card;
  border-radius: 20rpx;
  box-shadow: $shadow-soft;
}

.category-item {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8rpx;
}

.category-dot {
  width: 84rpx;
  height: 84rpx;
  border-radius: 50%;
  display: block;
}

.category-name {
  font-size: 24rpx;
  font-weight: 700;
  color: $text-primary;
}

.category-origin {
  font-size: 17rpx;
  color: $text-secondary;
  white-space: nowrap;
}

// ============ 节气养生 ============
.solar-card {
  display: flex;
  align-items: center;
  gap: 18rpx;
  margin: 22rpx 24rpx 0;
  padding: 22rpx 20rpx;
  background: #FCF8E8;
  border: 2rpx solid #F1E4C3;
  border-radius: 20rpx;
}

.solar-icon-wrap {
  width: 88rpx;
  height: 88rpx;
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #FFE8C6;
  border-radius: 18rpx;
}

.solar-text {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 8rpx;
}

.solar-title-row {
  display: flex;
  align-items: center;
  gap: 12rpx;
}

.solar-tag {
  padding: 4rpx 12rpx;
  background: #F59E0B;
  border-radius: 6rpx;
  font-size: 17rpx;
  font-weight: 700;
  color: #ffffff;
}

.solar-title {
  font-size: 27rpx;
  font-weight: 700;
  color: $text-primary;
}

.solar-desc {
  font-size: 21rpx;
  color: $text-secondary;
}

// ============ 商品网格 ============
.product-section {
  padding: 30rpx 24rpx 0;
}

.section-header {
  display: flex;
  align-items: center;
  gap: 14rpx;
  margin-bottom: 20rpx;
}

.section-title {
  font-size: 36rpx;
  font-weight: 800;
  color: $text-primary;
  letter-spacing: 1rpx;
}

.shield-chip {
  padding: 6rpx 14rpx;
  background: $primary-light;
  border-radius: 8rpx;
}

.shield-text {
  font-size: 18rpx;
  color: $primary;
  font-weight: 500;
}

.section-more {
  display: inline-flex;
  align-items: center;
  gap: 2rpx;
  margin-left: auto;
  font-size: 21rpx;
  color: $text-secondary;
}

.product-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 20rpx;
}

.product-card {
  background: $card;
  border-radius: 18rpx;
  overflow: hidden;
  box-shadow: $shadow-soft;
}

.product-image {
  width: calc(100% - 40rpx);
  height: 375rpx;
  display: block;
  margin: 20rpx 20rpx 0;
  border-radius: 12rpx;
}

.product-info {
  padding: 16rpx 20rpx 20rpx;
  display: flex;
  flex-direction: column;
}

.product-tags {
  display: flex;
  align-items: center;
  gap: 8rpx;
  margin-bottom: 10rpx;
}

.mini-tag {
  padding: 2rpx 10rpx;
  border: 1rpx solid $primary;
  border-radius: 6rpx;
  font-size: 17rpx;
  font-weight: 500;
  line-height: 1.6;
}

.mini-tag-sub {
  font-size: 17rpx;
  color: $text-secondary;
}

.product-name {
  font-size: 27rpx;
  font-weight: 700;
  color: $text-primary;
  line-height: 1.4;
}

.product-spec {
  margin-top: 6rpx;
  font-size: 20rpx;
  color: $text-hint;
}

.product-bottom {
  display: flex;
  justify-content: space-between;
  align-items: flex-end;
  margin-top: 14rpx;
  padding-top: 14rpx;
  border-top: 1rpx solid #EFEEEB;
}

.price-col {
  display: flex;
  flex-direction: column;
  gap: 4rpx;
}

.price-row {
  display: flex;
  align-items: baseline;
}

.price-symbol {
  font-size: 22rpx;
  font-weight: 700;
  color: $primary-dark;
}

.price-value {
  font-size: 34rpx;
  font-weight: 800;
  color: $primary-dark;
  line-height: 1;
}

.sales-text {
  font-size: 18rpx;
  color: #B0ACA9;
}

.add-cart-btn {
  width: 56rpx;
  height: 56rpx;
  border-radius: 50%;
  background: $primary;
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 4rpx 12rpx rgba(46, 125, 50, 0.35);
}

// ============ footer ============
.footer {
  padding: 54rpx 0 40rpx;
  text-align: center;
}

.footer-text {
  font-size: 21rpx;
  color: #B0ACA9;
  letter-spacing: 3rpx;
}
</style>

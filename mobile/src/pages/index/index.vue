<template>
  <view class="page">
    <!-- ============ 顶栏：统一标题 ============ -->
    <q-page-header-back :show-back="false" title="本草国医馆" />
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
      <swiper v-if="banners.length" class="catalog-banner" autoplay circular indicator-dots>
        <swiper-item v-for="banner in banners" :key="banner.id" @click="handleBannerClick(banner)">
          <q-cached-image :src="banner.image" mode="aspectFill" class="catalog-banner-image" />
          <view class="catalog-banner-title">{{ banner.title }}</view>
        </swiper-item>
      </swiper>

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

      <!-- ===== 分类入口 ===== -->
      <view class="category-grid">
        <view
          v-for="item in categoryItems"
          :key="item.id"
          class="category-item"
          @click="goToCategory(item.id)"
        >
          <q-cached-image v-if="item.icon" class="category-dot" :src="item.icon" mode="aspectFill" />
          <view v-else class="category-dot category-dot-empty">
            <q-icon name="tea" size="36" color="#2E7D32" />
          </view>
          <text class="category-name">{{ item.name }}</text>
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
            <q-cached-image :src="p.image" mode="aspectFill" class="product-image" />
            <view v-if="p.promoBadge" class="promo-corner" :style="{ backgroundColor: p.promoBadge.color }">
              <text class="promo-corner-text">{{ p.promoBadge.name }}</text>
            </view>
            <view class="product-info">
              <view class="product-tags">
                <view class="mini-tag" :style="{ borderColor: p.tone, color: p.tone }">
                  <text>{{ p.cert }}</text>
                </view>
                <view v-if="p.certSub" class="mini-tag" :style="{ borderColor: p.toneSub, color: p.toneSub }">
                  <text>{{ p.certSub }}</text>
                </view>
              </view>
              <text class="product-name">{{ p.name }}</text>
              <text class="product-spec">规格：{{ p.spec }}</text>
              <view class="product-bottom">
                <view class="price-col">
                  <view class="price-row">
                    <text class="price-symbol">¥</text>
                    <text class="price-value">{{ p.price }}</text>
                    <text v-if="p.marketPrice > 0" class="price-original">¥{{ p.marketPrice }}</text>
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
import { getCategories, getHotProducts, getBanners, addToCart } from '@/api'
import { BannerLinkType } from '@/api/types'
import type { Category, Banner, Product } from '@/api/types'
import { UnauthorizedError } from '@/utils/http'
import { resolveTagColor } from '@/utils/tagColor'

interface FeaturedItem {
  id: string
  skuId: string
  name: string
  image: string
  price: string
  marketPrice: number
  spec: string
  skuSpecName: string
  weightGram: number
  cert: string
  certSub: string
  tone: string
  toneSub: string
  sales: string
  promoBadge?: { name: string; color: string }
}

const categoryItems = ref<Array<{ id: string; name: string; icon: string }>>([])
const banners = ref<Banner[]>([])

const featured = ref<FeaturedItem[]>([])

onMounted(async () => {
  try {
    const [categories, products, loadedBanners] = await Promise.all([
      getCategories(),
      getHotProducts(),
      getBanners()
    ])
    const childCategories = categories.flatMap((category: Category) => category.children)
    categoryItems.value = childCategories.slice(0, 6).map((category) => ({
      id: category.id,
      name: category.name,
      icon: category.icon
    }))
    banners.value = loadedBanners
    featured.value = products.filter(product => product.skus.length > 0).slice(0, 6).map((product: Product) => {
      const sku = product.skus[0]
      const displayTags = product.tags.filter((tag) => tag.type !== 'Promotion')
      const promoTag = product.tags.find((tag) => tag.type === 'Promotion')
      const firstTag = displayTags[0]
      const secondTag = displayTags[1]
      return {
        id: product.id,
        skuId: sku.id,
        name: product.name,
        image: product.images[0] || '',
        price: sku.price % 1 === 0 ? String(sku.price) : sku.price.toFixed(1),
        marketPrice: sku.marketPrice || 0,
        spec: sku.specName,
        skuSpecName: sku.specName,
        weightGram: sku.weightGram,
        sales: String(product.sales),
        cert: firstTag?.name || product.categoryName,
        certSub: secondTag?.name || '',
        tone: resolveTagColor(firstTag?.color),
        toneSub: resolveTagColor(secondTag?.color),
        promoBadge: promoTag
          ? { name: promoTag.name, color: resolveTagColor(promoTag.color) || '#C2410C' }
          : undefined
      }
    })
  } catch (error) {
    console.error('加载商城首页数据失败', error)
    if (error instanceof UnauthorizedError) return
    uni.showToast({ title: '首页数据加载失败', icon: 'none' })
  }
})

async function quickAdd(item: FeaturedItem) {
  try {
    await addToCart({
      productId: item.id,
      productName: item.name,
      productImage: item.image,
      skuId: item.skuId,
      specName: item.skuSpecName,
      price: Number(item.price),
      quantity: 1,
      weightGram: item.weightGram,
      selected: true,
      badge: item.cert,
      certText: item.cert
    })
    uni.showToast({ title: '已加入购物车', icon: 'none' })
  } catch (error) {
    console.error('加入购物车失败', error)
    if (error instanceof UnauthorizedError) return
    uni.showToast({ title: '加入购物车失败', icon: 'none' })
  }
}

function goToSearch() { uni.navigateTo({ url: '/pages/search/index' }) }
function goToCategory(catId?: string | MouseEvent) {
  const id = typeof catId === 'string' ? catId : undefined
  if (id) uni.setStorageSync('pendingCategoryId', id)
  else uni.removeStorageSync('pendingCategoryId')
  uni.switchTab({ url: '/pages/category/index' })
}
function goToProductDetail(id: string) { uni.navigateTo({ url: `/pages/product-detail/index?id=${id}` }) }
function handleBannerClick(banner: Banner) {
  if (!banner.link) return

  if (banner.linkType === BannerLinkType.InternalProduct) {
    goToProductDetail(banner.link)
  } else if (banner.linkType === BannerLinkType.InternalCategory) {
    goToCategory(banner.link)
  } else if (banner.linkType === BannerLinkType.InternalActivity || banner.linkType === BannerLinkType.InternalRegistrationActivity) {
    uni.navigateTo({ url: `/pages/activity-detail/index?id=${encodeURIComponent(banner.link)}` })
  }
}
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
.catalog-banner {
  position: relative;
  height: 300rpx;
  margin: 20rpx 24rpx 0;
  overflow: hidden;
  border-radius: 22rpx;
}

.catalog-banner-image {
  width: 100%;
  height: 100%;
}

.catalog-banner-title {
  position: absolute;
  left: 24rpx;
  bottom: 34rpx;
  color: #ffffff;
  font-size: 30rpx;
  font-weight: 700;
  text-shadow: 0 2rpx 8rpx rgba(0, 0, 0, 0.6);
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

.category-dot-empty {
  display: flex;
  align-items: center;
  justify-content: center;
  background: $primary-light;
}

.category-name {
  font-size: 24rpx;
  font-weight: 700;
  color: $text-primary;
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
  position: relative;
  background: $card;
  border-radius: 18rpx;
  overflow: hidden;
  box-shadow: $shadow-soft;
}

.promo-corner {
  position: absolute;
  top: 20rpx;
  left: 20rpx;
  z-index: 2;
  padding: 4rpx 14rpx;
  border-radius: 12rpx 0 12rpx 12rpx;
}

.promo-corner-text {
  color: #ffffff;
  font-size: 20rpx;
  line-height: 30rpx;
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

.price-original {
  margin-left: 8rpx;
  font-size: 20rpx;
  color: #B0ACA9;
  text-decoration: line-through;
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
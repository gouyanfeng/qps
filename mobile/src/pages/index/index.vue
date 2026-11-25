<template>
  <view class="page">
    <!-- ============ 顶栏：统一标题 ============ -->
    <q-page-header-back :show-back="false" title="本草国医馆" />
    <view class="page-subbar">
      <view class="search-bar" @click="goToSearch">
        <q-icon name="search" size="24" color="#707A6C" />
        <text class="search-placeholder">搜索道地人参、正品黄芪、滋补阿胶</text>
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
          <q-image :src="banner.image" thumb :width="750" mode="aspectFill" class="catalog-banner-image" />
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
          <q-image v-if="item.icon" class="category-dot" :src="item.icon" thumb :width="120" mode="aspectFill" />
          <view v-else class="category-dot category-dot-empty">
            <q-icon name="tea" size="36" color="#2E7D32" />
          </view>
          <text class="category-name">{{ item.name }}</text>
        </view>
      </view>

      <!-- ===== 节气养生卡 ===== -->
      <view v-if="solarBanner" class="solar-card" @click="handleSolarClick">
        <q-image :src="solarBanner.image" thumb :width="750" mode="widthFix" class="solar-banner-image" />
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
            <q-image :src="p.image" thumb :width="375" mode="aspectFill" class="product-image" />
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
                    <text v-if="p.originalPrice > 0" class="price-original">¥{{ p.originalPrice }}</text>
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
  originalPrice: number
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
const solarBanner = ref<Banner | null>(null)

const featured = ref<FeaturedItem[]>([])

onMounted(async () => {
  try {
    const [categories, products, loadedBanners, solarList] = await Promise.all([
      getCategories(),
      getHotProducts(),
      getBanners(),
      getBanners('home-solar')
    ])
    const childCategories = categories.flatMap((category: Category) => category.children)
    categoryItems.value = childCategories.slice(0, 6).map((category) => ({
      id: category.id,
      name: category.name,
      icon: category.icon
    }))
    banners.value = loadedBanners
    solarBanner.value = solarList[0] ?? null
    featured.value = products.filter(product => product.skus.length > 0).slice(0, 6).map((product: Product) => {
      // 列表价取所有规格中折后价最低的一条，折后价由后端计算
      const sku = product.skus.reduce((min, item) => {
        const current = item.discountedPrice ?? item.price
        const minValue = min.discountedPrice ?? min.price
        return current < minValue ? item : min
      })
      const displayPrice = sku.discountedPrice ?? sku.price
      const hasDiscount = sku.discountedPrice !== null && sku.discountedPrice < sku.price
      const displayTags = product.tags.filter((tag) => tag.type !== 'Promotion')
      const firstTag = displayTags[0]
      const secondTag = displayTags[1]
      return {
        id: product.id,
        skuId: sku.id,
        name: product.name,
        image: product.images[0] || '',
        price: displayPrice % 1 === 0 ? String(displayPrice) : displayPrice.toFixed(2),
        originalPrice: hasDiscount ? sku.price : 0,
        spec: sku.specName,
        skuSpecName: sku.specName,
        weightGram: sku.weightGram,
        sales: String(product.sales),
        cert: firstTag?.name || product.categoryName,
        certSub: secondTag?.name || '',
        tone: resolveTagColor(firstTag?.color),
        toneSub: resolveTagColor(secondTag?.color),
        // 角标只看后端满减标识，不在前端重复判断促销类型
        promoBadge: product.promotionBadgeText
          ? { name: product.promotionBadgeText, color: product.promotionBadgeColor || '#C2410C' }
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
  } else if (banner.linkType === BannerLinkType.InternalCampaign) {
    uni.navigateTo({ url: `/pages/campaign/index?id=${encodeURIComponent(banner.link)}` })
  }
}
function handleSolarClick() {
  if (solarBanner.value) handleBannerClick(solarBanner.value)
}
function handleVerify() { uni.showToast({ title: '扫码验真功能演示', icon: 'none' }) }
</script>

<style lang="scss" scoped>
@import "./index.scss";
</style>
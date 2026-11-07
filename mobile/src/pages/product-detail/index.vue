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
            <q-cached-image :src="img" mode="aspectFill" class="swiper-img" />
          </swiper-item>
        </swiper>

        <view
          v-if="product.promotionBadgeText"
          class="promo-corner"
          :style="{ backgroundColor: product.promotionBadgeColor || '#C2410C' }"
        >
          <text class="promo-corner-text">{{ product.promotionBadgeText }}</text>
        </view>

        <view class="swiper-indicator">
          <view class="frac-indicator">
            <text>{{ swiperIdx + 1 }}/{{ product.images.length }} 页</text>
          </view>
        </view>
      </view>

      <!-- ============ section 价格信息卡 ============ -->
      <view class="price-card">
        <view class="price-row">
          <view class="price-main">
            <text class="price-symbol">¥</text>
            <text class="price-value">{{ displayPrice }}</text>
            <text v-if="crossedPrice > 0" class="price-original">¥{{ crossedPrice }}</text>
            <text v-if="currentSku.discountLabel" class="price-discount">{{ currentSku.discountLabel }}</text>
          </view>
        </view>

        <view class="name-row">
          <text class="product-name">{{ product.name }}</text>
          <view v-if="productTags.length" class="pill-row">
            <view v-for="tag in productTags" :key="tag.id" class="pill" :style="tagChipStyle(tag.color)">
              <text class="pill-text" :style="{ color: resolveTagColor(tag.color) }">{{ tag.name }}</text>
            </view>
          </view>
        </view>

        <view v-if="product.storeName" class="store-line">
          <q-icon name="location" size="22" color="#6B6B6B" />
          <text class="store-line-text">门店：{{ product.storeName }}</text>
        </view>

        <view v-if="product.description" class="sell-point">
          <text class="sell-point-text">{{ product.description }}</text>
        </view>
      </view>

      <!-- ============ section 规格选择卡 #spec-group ============ -->
      <view id="spec-group" class="spec-card">
        <view class="spec-head">
          <view class="spec-head-left">
            <q-icon name="task" size="28" color="#2E7D32" />
            <text class="spec-title">规格选择</text>
          </view>
          <view class="spec-head-right">
            <view class="spec-layout-switch">
              <view
                :class="['spec-layout-option', { active: specLayout === 'grid' }]"
                @click="specLayout = 'grid'"
              >
                <view class="layout-icon layout-icon-grid">
                  <view class="grid-cell"></view>
                  <view class="grid-cell"></view>
                  <view class="grid-cell"></view>
                  <view class="grid-cell"></view>
                </view>
              </view>
              <view
                :class="['spec-layout-option', { active: specLayout === 'flat' }]"
                @click="specLayout = 'flat'"
              >
                <view class="layout-icon layout-icon-list">
                  <view class="list-line"></view>
                  <view class="list-line"></view>
                  <view class="list-line"></view>
                </view>
              </view>
            </view>
          </view>
        </view>

        <view :class="['spec-list', specLayout]">
          <view
            v-for="sku in product.skus"
            :key="sku.id"
            :class="['spec-item', { active: isSelected('规格', sku.specName), soldout: sku.stock <= 0 }]"
            @click="selectSpec('规格', sku.specName)"
          >
            <view class="spec-info">
              <text class="spec-name">{{ sku.specName }}</text>
              <text class="spec-stock">{{ sku.stock > 0 ? `库存 ${sku.stock}` : '售罄' }}</text>
            </view>
            <q-icon
              v-if="isSelected('规格', sku.specName) && sku.stock > 0"
              name="check-circle-filled"
              size="22"
              color="#2E7D32"
            />
          </view>
        </view>

        <view class="qty-row">
          <view class="qty-left">
            <text class="qty-label">购买数量</text>
            <text class="qty-note">{{ stockText(currentSku) }}</text>
          </view>
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
      </view>

      <!-- ============ section 用户口碑 · 药师鉴评 ============ -->
      <view v-if="reviewVisible" class="review-card">
        <view class="review-head">
          <view class="review-head-left">
            <q-icon name="chat" size="28" color="#2E7D32" />
            <text class="review-title">用户口碑 · 药师鉴评</text>
            <text class="review-count">({{ reviews.allTotalCount }}条真实评价)</text>
          </view>
          <text class="review-rate">好评率 {{ positiveRateText }}</text>
        </view>

        <view class="review-tags">
          <view class="review-tag" @click="goReviews()">
            <text>全部 ({{ reviews.allTotalCount }})</text>
          </view>
          <view
            v-for="stat in reviews.tagStats"
            :key="stat.tag"
            class="review-tag"
            @click="goReviews(stat.tag)"
          >
            <text>{{ stat.tag }} ({{ stat.count }})</text>
          </view>
        </view>

        <view v-if="reviews.items.length" class="review-list">
          <view
            v-for="(review, idx) in reviews.items"
            :key="review.id"
            :class="['review-item', { last: idx === reviews.items.length - 1 }]"
          >
            <view class="review-item-head">
              <view class="review-user">
                <view :class="['review-avatar', { alt: idx % 2 === 1 }]">
                  <text>{{ avatarChar(review) }}</text>
                </view>
                <view class="review-user-meta">
                  <view class="review-user-name-row">
                    <text class="review-nickname">{{ review.memberNickname }}</text>
                    <text v-if="review.memberLevelName" class="review-level">{{ review.memberLevelName }}</text>
                  </view>
                </view>
              </view>
              <view class="review-stars">
                <q-icon
                  v-for="n in 5"
                  :key="n"
                  :name="n <= review.rating ? 'star-filled' : 'star'"
                  size="24"
                  :color="n <= review.rating ? '#FF9800' : '#D8D3CB'"
                />
              </view>
            </view>
            <text class="review-content">{{ review.content }}</text>
            <view v-if="review.images.length" class="review-images">
              <q-cached-image
                v-for="(img, imgIdx) in review.images"
                :key="imgIdx"
                class="review-image"
                :src="img"
                mode="aspectFill"
                @click="previewReviewImages(review.images, imgIdx)"
              />
            </view>
            <view v-if="review.replyContent" class="review-reply">
              <text class="review-reply-label">商家回复</text>
              <text class="review-reply-text">{{ review.replyContent }}</text>
            </view>
            <view class="review-foot">
              <text class="review-spec">规格：{{ review.specText }}</text>
              <text class="review-date">{{ formatReviewDate(review.reviewTime) }}</text>
            </view>
          </view>
        </view>
        <view v-else class="review-empty">
          <text>暂无精选评价</text>
        </view>

        <view class="review-more" @click="goReviews()">
          <text>查看全部 {{ reviews.allTotalCount }} 条评价</text>
          <q-icon name="chevron-right" size="20" color="#2E7D32" />
        </view>
      </view>

      <!-- ============ section 商品介绍（图文溯源） ============ -->
      <view class="intro-section">
        <view class="intro-head-card">
          <view class="intro-head-line"></view>
          <text class="intro-title">本草详案 · 图文溯源</text>
          <view class="intro-head-line"></view>
        </view>

        <view v-if="detailHtml" class="intro-html">
          <rich-text :nodes="detailHtml" />
        </view>

        <view v-else class="intro-fallback">
          <text class="intro-fallback-text">{{ product.description || '该商品暂无详细介绍' }}</text>
        </view>
      </view>

      <!-- ============ 服务保障（随内容滚动，不固定在底部） ============ -->
      <view class="trust-card">
        <view class="trust-grid">
          <view class="trust-item">
            <q-icon name="certificate" size="22" color="#2E7D32" />
            <text class="trust-label">道地保真</text>
            <text class="trust-sub">假一赔十</text>
          </view>
          <view class="trust-item">
            <q-icon name="snowflake" size="22" color="#2E7D32" />
            <text class="trust-label">顺丰冷运</text>
            <text class="trust-sub">专线恒温</text>
          </view>
          <view class="trust-item">
            <q-icon name="qrcode" size="22" color="#2E7D32" />
            <text class="trust-label">一物一码</text>
            <text class="trust-sub">国家追溯</text>
          </view>
          <view class="trust-item">
            <q-icon name="refresh" size="22" color="#2E7D32" />
            <text class="trust-label">无忧退换</text>
            <text class="trust-sub">7天包退</text>
          </view>
        </view>
      </view>
    </scroll-view>

    <!-- ============ 底部固定操作栏 ============ -->
    <view class="page-footer">
      <view class="footer-bar">
        <view class="bb-icon" @click="consult">
          <q-icon name="service" size="22" color="#40493D" />
          <text>药师咨询</text>
        </view>
        <view class="bb-icon" @click="goCart">
          <q-icon name="cart" size="22" color="#40493D" />
          <text>购物车</text>
        </view>
        <view class="bb-btn ghost" @click="addToCart">加入购物车</view>
        <view class="bb-btn solid" @click="buyNow">立即购买 ¥{{ displayPrice }}</view>
      </view>
    </view>
  </view>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { onLoad } from '@dcloudio/uni-app'
import { getProductById, getProductReviews, addToCart as addCart } from '@/api'
import type { Product, Sku, ProductReview, ProductReviewPage } from '@/api/types'
import { resolveFileUrl, UnauthorizedError } from '@/utils/http'
import { resolveTagColor, tagChipStyle } from '@/utils/tagColor'

const product = ref<Product | null>(null)
const productId = ref('')
const quantity = ref(1)
const selectedSpecs = ref<Record<string, string>>({})
// 规格展示形式：grid 网格（默认）/ flat 平铺；可在此切换默认值
const specLayout = ref<'grid' | 'flat'>('grid')

const swiperIdx = ref(0)

const reviews = ref<ProductReviewPage>({
  items: [],
  totalCount: 0,
  positiveRate: 0,
  allTotalCount: 0,
  allPositiveRate: 0,
  tagStats: []
})

onLoad(async (opts) => {
  if (!opts?.id) return
  productId.value = opts.id
  void loadFeaturedReviews(opts.id)
  try {
    product.value = (await getProductById(opts.id)) ?? null
    // 默认选中第一个有库存的规格，全部售罄时退回第一条供页面展示
    const skus = product.value?.skus ?? []
    const initialSku = skus.find(sku => sku.stock > 0) ?? skus[0]
    if (initialSku) selectedSpecs.value = { 规格: initialSku.specName }
  } catch (error) {
    console.error('加载商品详情失败', error)
    if (error instanceof UnauthorizedError) return
    uni.showToast({ title: '商品详情加载失败', icon: 'none' })
  }
})

// 详情页只展示后台勾选的精选评价，按页取满直到取完
async function loadFeaturedReviews(productId: string) {
  try {
    const pageSize = 50
    let page = 1
    const items: ProductReview[] = []
    let summary: ProductReviewPage | null = null

    while (true) {
      const result = await getProductReviews(productId, page, pageSize, undefined, true)
      summary = result
      items.push(...result.items)
      if (result.items.length < pageSize || items.length >= result.totalCount) break
      page++
    }

    if (summary) reviews.value = { ...summary, items }
  } catch (error) {
    console.error('加载商品评价失败', error)
  }
}

// 名称行只展示商品标签（type = Product），促销标签不在名称后出现
const productTags = computed(() =>
  (product.value?.tags ?? []).filter(tag => tag.type === 'Product')
)

const currentSku = computed(() => {
  const skus = product.value?.skus ?? []
  return skus.find(s => s.specName === selectedSpecs.value['规格']) ?? skus[0] ?? { id: '', specName: '', price: 0, marketPrice: 0, stock: 0, weightGram: 0, discountedPrice: null, discountLabel: '' }
})

// 主价：有折扣促销时显示后端折后价，否则显示原价
const displayPrice = computed(() => {
  const sku = currentSku.value
  return sku.discountedPrice ?? sku.price
})

// 划线价：有折扣时显示 SKU 原价，无折扣不展示；marketPrice 不再参与展示
const crossedPrice = computed(() => {
  const sku = currentSku.value
  return sku.discountedPrice !== null && sku.discountedPrice < sku.price ? sku.price : 0
})

// 详情富文本里的相对图片地址补全为可访问的完整地址
const detailHtml = computed(() => {
  const html = product.value?.detailHtml ?? ''
  if (!html) return ''
  return html.replace(/src="(\/uploads\/[^"]*)"/g, (_match, path: string) => `src="${resolveFileUrl(path)}"`)
})

// 无评价时整张口碑卡不渲染
const reviewVisible = computed(() => reviews.value.allTotalCount > 0)

const positiveRateText = computed(() => {
  const rate = Number(reviews.value.allPositiveRate) || 0
  return `${rate % 1 === 0 ? rate : rate.toFixed(1)}%`
})

function goReviews(tag?: string) {
  if (!productId.value) return
  const query = tag ? `&tag=${encodeURIComponent(tag)}` : ''
  uni.navigateTo({
    url: `/pages/product-reviews/index?productId=${encodeURIComponent(productId.value)}${query}`
  })
}

function previewReviewImages(images: string[], index: number) {
  if (!images.length) return
  uni.previewImage({
    urls: images.map(resolveFileUrl),
    current: resolveFileUrl(images[index])
  })
}

function avatarChar(review: ProductReview) {
  const nickname = (review.memberNickname || '').trim()
  return nickname ? nickname.charAt(0) : '匿'
}

function formatReviewDate(value: string) {
  if (!value) return ''
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value.slice(0, 10).replace(/-/g, '.')
  const pad = (num: number) => String(num).padStart(2, '0')
  return `${date.getFullYear()}.${pad(date.getMonth() + 1)}.${pad(date.getDate())}`
}

function stockText(sku: Sku) {
  if (sku.stock <= 0) return '暂时缺货'
  return sku.stock <= 3 ? `仅剩 ${sku.stock} 件` : '库存充足'
}

function isSelected(name: string, opt: string) { return selectedSpecs.value[name] === opt }
function selectSpec(name: string, opt: string) {
  const sku = product.value?.skus.find(item => item.specName === opt)
  if (!sku || sku.stock <= 0) {
    uni.showToast({ title: '该规格已售罄', icon: 'none' })
    return
  }
  selectedSpecs.value[name] = opt
}

// 库存刷新后若当前选中已售罄，自动切到有货规格；全部售罄则清空选中并提示
watch(
  () => (product.value?.skus ?? []).map(sku => `${sku.specName}:${sku.stock}`).join('|'),
  () => {
    const skus = product.value?.skus ?? []
    if (!skus.length) return
    const selected = skus.find(sku => sku.specName === selectedSpecs.value['规格'])
    if (selected && selected.stock > 0) return

    const fallback = skus.find(sku => sku.stock > 0)
    if (fallback) {
      selectedSpecs.value = { 规格: fallback.specName }
      quantity.value = 1
      uni.showToast({ title: `已切换为有货规格：${fallback.specName}`, icon: 'none' })
    } else {
      selectedSpecs.value = {}
      uni.showToast({ title: '该商品所有规格已售罄', icon: 'none' })
    }
  }
)

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
function buyNow() {
  if (!product.value) return
  const sku = currentSku.value
  if (!sku.id) {
    uni.showToast({ title: '请选择商品规格', icon: 'none' })
    return
  }
  // 立即购买不走购物车：写入直购缓存，由确认订单页读取
  uni.setStorageSync('qps_buy_now_item', {
    skuId: sku.id,
    productId: product.value.id,
    productName: product.value.name,
    productImage: product.value.images[0] || '',
    specName: sku.specName || '默认规格',
    price: sku.price,
    quantity: quantity.value,
    weightGram: sku.weightGram
  })
  uni.navigateTo({ url: '/pages/checkout/index' })
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
$muted: #8A8578;
$line: #EFEEEB;

$shadow-card: 0 4rpx 20rpx -4rpx rgba(46, 125, 50, 0.08);

.page {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
  background: var(--page-bg);
}

.page-body {
  flex: 1;
  padding-bottom: calc(140rpx + env(safe-area-inset-bottom));
}

/* ===== section 大图轮播（卡片式，保留边距） ===== */
.swiper-section {
  position: relative;
  margin: 20rpx 24rpx 0;
  border-radius: 24rpx;
  overflow: hidden;
}
.product-swiper { width: 702rpx; height: 526rpx; }
.swiper-img { width: 100%; height: 100%; }
.promo-corner {
  position: absolute;
  top: 0;
  left: 0;
  z-index: 6;
  padding: 6rpx 18rpx;
  border-radius: 24rpx 0 24rpx 24rpx;
}
.promo-corner-text {
  color: #ffffff;
  font-size: 22rpx;
  line-height: 32rpx;
}

.swiper-indicator {
  position: absolute; bottom: 24rpx; right: 24rpx; z-index: 5;
}
.frac-indicator {
  padding: 8rpx 20rpx;
  border-radius: 24rpx;
  background: rgba(0, 0, 0, 0.5);
}
.frac-indicator text {
  font-size: 20rpx; color: #ffffff; letter-spacing: 1rpx;
}

/* ===== section 价格信息卡 ===== */
.price-card {
  margin: 24rpx 24rpx 0; padding: 28rpx;
  background: $surface; border-radius: 24rpx;
  box-shadow: $shadow-card;
}
.price-row {
  display: flex; align-items: center; justify-content: space-between;
  gap: 16rpx;
}
.price-main { display: flex; align-items: flex-end; gap: 6rpx; min-width: 0; }
.price-symbol { font-size: 30rpx; font-weight: 700; color: $price; }
.price-value { font-size: 68rpx; font-weight: 800; color: $price; line-height: 1; letter-spacing: -2rpx; }
.price-original { font-size: 24rpx; color: #B0ACA9; text-decoration: line-through; margin-left: 10rpx; }
.price-discount {
  margin-left: 10rpx;
  padding: 4rpx 12rpx;
  border-radius: 8rpx;
  font-size: 22rpx;
  color: #ffffff;
  background: #C2410C;
  line-height: 1.4;
}
.price-off {
  flex-shrink: 0;
  padding: 8rpx 16rpx;
  background: #FDF3E3;
  border-radius: 8rpx;
}
.price-off text { font-size: 20rpx; font-weight: 600; color: #C2410C; }

.name-row {
  display: flex; flex-wrap: wrap; align-items: center; gap: 12rpx;
  margin: 18rpx 0 0;
}
.product-name {
  font-family: 'Noto Serif SC', 'Songti SC', 'STSong', serif;
  font-size: 36rpx; font-weight: 700;
  color: $on-surface; line-height: 1.45;
  letter-spacing: 1rpx;
}

.store-line {
  display: flex; align-items: center; gap: 8rpx;
  margin-top: 12rpx;
}
.store-line-text {
  font-size: 22rpx; color: $muted;
}

.sell-point {
  display: flex; align-items: center; gap: 8rpx;
  margin-top: 14rpx;
}
.sell-point-text {
  flex: 1; min-width: 0;
  font-size: 22rpx; color: $muted;
  overflow: hidden; text-overflow: ellipsis; white-space: nowrap;
}

.pill-row {
  display: flex; flex-wrap: wrap; gap: 10rpx;
}
.pill {
  display: flex; align-items: center; gap: 6rpx;
  padding: 6rpx 14rpx;
  background: #F1F7EF;
  border: 1rpx solid rgba(46, 125, 50, 0.18);
  border-radius: 999rpx;
}
.pill-text { font-size: 21rpx; font-weight: 600; color: $primary; }

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
.spec-head-left { display: flex; align-items: center; gap: 10rpx; min-width: 0; }
.spec-title { font-size: 30rpx; font-weight: 700; color: $on-surface; }

.spec-head-right { display: flex; align-items: center; gap: 16rpx; flex-shrink: 0; }
.spec-layout-switch {
  display: flex;
  align-items: center;
  gap: 4rpx;
  padding: 3rpx;
  background: #F1F5F0;
  border-radius: 10rpx;
}
.spec-layout-option {
  width: 46rpx;
  height: 38rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 8rpx;
  color: $muted;
}
.spec-layout-option.active {
  background: #ffffff;
  color: $primary;
  box-shadow: 0 1rpx 4rpx rgba(31, 45, 31, 0.1);
}
.layout-icon { display: flex; }
.layout-icon-grid { width: 20rpx; flex-wrap: wrap; gap: 3rpx; }
.grid-cell { width: 8rpx; height: 8rpx; border-radius: 2rpx; background: currentColor; }
.layout-icon-list { flex-direction: column; gap: 4rpx; }
.list-line { width: 20rpx; height: 4rpx; border-radius: 2rpx; background: currentColor; }

.spec-list { gap: 14rpx; }
.spec-list.grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
}
/* 平铺：一个规格占满一行，整行可点 */
.spec-list.flat {
  display: flex;
  flex-direction: column;
}
.spec-item {
  display: flex; align-items: center; justify-content: center; gap: 8rpx;
  min-height: 76rpx;
  padding: 14rpx 18rpx;
  background: var(--page-bg);
  border: 2rpx solid #EDEAE7;
  border-radius: 14rpx;
  transition: background 0.15s ease, border-color 0.15s ease;

  &.active {
    background: #F5FAF3;
    border-color: $primary;
  }

  /* 售罄：置灰、整项不可点，样式覆盖选中态 */
  &.soldout {
    background: #F4F2EF;
    border-color: #E8E5E1;

    .spec-name,
    .spec-stock { color: #A9A49C; }
  }
}
.spec-info {
  display: flex; flex-direction: column; align-items: center; gap: 4rpx;
  min-width: 0;
}
.spec-list.flat .spec-item {
  width: 100%;
  box-sizing: border-box;
  justify-content: space-between;
  padding: 20rpx 24rpx;
}
.spec-list.flat .spec-info {
  flex-direction: row;
  align-items: center;
  gap: 16rpx;
}
.spec-list.flat .spec-name { text-align: left; }
.spec-name {
  font-size: 26rpx; font-weight: 700; color: $on-surface;
  text-align: center; word-break: break-all;
}
.spec-stock { font-size: 19rpx; color: $muted; }
.spec-item.active .spec-name { color: $primary; }
.spec-item.active .spec-stock { color: $primary; }

.qty-row {
  display: flex; justify-content: space-between; align-items: center;
  margin-top: 24rpx; padding-top: 24rpx;
  border-top: 1rpx solid $line;
}
.qty-left { display: flex; flex-direction: column; gap: 4rpx; }
.qty-label { font-size: 24rpx; font-weight: 600; color: $on-surface; }
.qty-note { font-size: 19rpx; color: $muted; }
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

/* ===== section 用户口碑 · 药师鉴评 ===== */
.review-card {
  margin: 20rpx 24rpx 0; padding: 28rpx;
  background: $surface; border-radius: 20rpx;
  box-shadow: $shadow-card;
}
.review-head {
  display: flex; align-items: center; justify-content: space-between;
  gap: 16rpx;
  padding-bottom: 16rpx;
  border-bottom: 1rpx solid $line;
}
.review-head-left { display: flex; align-items: center; gap: 8rpx; flex: 1; min-width: 0; }
.review-title {
  flex-shrink: 0;
  font-family: 'Noto Serif SC', 'Songti SC', 'STSong', serif;
  font-size: 26rpx; font-weight: 700; color: $on-surface;
  letter-spacing: 1rpx;
}
.review-count { flex-shrink: 0; font-size: 19rpx; color: $muted; }
.review-rate { flex-shrink: 0; font-size: 20rpx; font-weight: 600; color: $price; }

.review-tags {
  display: flex; flex-wrap: wrap; gap: 12rpx;
  margin-top: 18rpx;
}
.review-tag {
  padding: 8rpx 20rpx;
  background: #F3F0ED;
  border-radius: 999rpx;
}
.review-tag text { font-size: 19rpx; color: #6F6B62; line-height: 1.4; }

.review-list { margin-top: 4rpx; }
.review-item {
  padding: 22rpx 0;
  border-bottom: 1rpx solid $line;
}
.review-item.last { padding-bottom: 6rpx; border-bottom: 0; }

.review-item-head {
  display: flex; align-items: center; justify-content: space-between;
  gap: 16rpx;
}
.review-user { display: flex; align-items: center; gap: 14rpx; flex: 1; min-width: 0; }
.review-avatar {
  width: 52rpx; height: 52rpx; flex-shrink: 0;
  display: flex; align-items: center; justify-content: center;
  border-radius: 50%;
  background: rgba(46, 125, 50, 0.1);
}
.review-avatar text { font-size: 22rpx; font-weight: 700; color: $primary; }
.review-avatar.alt { background: #FDECD8; }
.review-avatar.alt text { color: $price; }

.review-user-meta { flex: 1; min-width: 0; }
.review-user-name-row { display: flex; align-items: center; gap: 10rpx; min-width: 0; }
.review-nickname {
  min-width: 0;
  font-size: 23rpx; font-weight: 700; color: $on-surface;
  overflow: hidden; text-overflow: ellipsis; white-space: nowrap;
}
.review-level {
  flex-shrink: 0;
  padding: 2rpx 8rpx;
  background: rgba(46, 125, 50, 0.1);
  border-radius: 6rpx;
  font-size: 17rpx; font-weight: 600; color: $primary;
}
.review-stars { display: flex; align-items: center; gap: 2rpx; flex-shrink: 0; }

.review-content {
  display: block;
  margin-top: 12rpx;
  font-size: 23rpx; line-height: 1.65; color: #30342E;
  word-break: break-all;
}
.review-images {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 10rpx;
  margin-top: 14rpx;
}
.review-image {
  width: 100%;
  height: 200rpx;
  border-radius: 10rpx;
  background: #F3F3F0;
}
.review-reply {
  display: flex;
  flex-direction: column;
  gap: 6rpx;
  margin-top: 14rpx;
  padding: 14rpx 18rpx;
  background: #F7F5F1;
  border-radius: 10rpx;
}
.review-reply-label { font-size: 19rpx; font-weight: 700; color: $primary; }
.review-reply-text {
  font-size: 22rpx; line-height: 1.6; color: #5F5B53;
  word-break: break-all;
}
.review-empty {
  display: flex; align-items: center; justify-content: center;
  padding: 48rpx 0 24rpx;
}
.review-empty text { font-size: 22rpx; color: $muted; letter-spacing: 1rpx; }
.review-foot {
  display: flex; align-items: center; justify-content: space-between;
  gap: 16rpx;
  margin-top: 10rpx;
}
.review-spec {
  flex: 1; min-width: 0;
  font-size: 19rpx; color: #A09A90;
  overflow: hidden; text-overflow: ellipsis; white-space: nowrap;
}
.review-date { flex-shrink: 0; font-size: 19rpx; color: #A09A90; }

.review-more {
  display: flex; align-items: center; justify-content: center;
  gap: 4rpx;
  padding-top: 20rpx;
}
.review-more text { font-size: 22rpx; font-weight: 600; color: $primary; }

/* ===== section 图文溯源（标题条 + 内容卡片） ===== */
.intro-section {
  margin-top: 20rpx;
  padding-bottom: 20rpx;
}
.intro-head-card {
  display: flex; align-items: center; gap: 16rpx;
  margin: 0 24rpx;
  padding: 24rpx 28rpx;
  background: #F3F1EC;
  border-radius: 20rpx;
}
.intro-head-line { flex: 1; height: 1rpx; background: #DDD8D0; }
.intro-title {
  flex-shrink: 0;
  font-family: 'Noto Serif SC', 'Songti SC', 'STSong', serif;
  font-size: 27rpx; font-weight: 600; color: $on-surface;
  letter-spacing: 2rpx;
}

.intro-html {
  margin: 16rpx 24rpx 0;
  padding: 24rpx;
  background: $surface;
  border-radius: 20rpx;
  font-size: 26rpx;
  color: $on-surface-var;
  line-height: 1.8;
  word-break: break-all;
}
.intro-html :deep(h2) {
  font-size: 32rpx; font-weight: 700; color: $on-surface;
  margin: 12rpx 0 12rpx;
}
.intro-html :deep(p) { margin: 12rpx 0; }
.intro-html :deep(image),
.intro-html :deep(img) {
  display: block;
  width: 100%;
  height: auto;
  margin: 0 0 16rpx;
  border-radius: 12rpx;
}
.intro-fallback {
  margin: 0 24rpx;
  padding: 28rpx 24rpx;
  background: $surface;
  border-radius: 16rpx;
}
.intro-fallback-text {
  font-size: 26rpx;
  color: $on-surface-var;
  line-height: 1.8;
}

/* ===== 服务保障（按设计稿四列竖排） ===== */
.trust-card {
  margin: 0 24rpx 20rpx;
  padding: 40rpx 24rpx;
  background: $surface;
  border-radius: 20rpx;
  border-top: 1rpx solid #F0EEEB;
}
.trust-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 16rpx;
}
.trust-item {
  display: flex; flex-direction: column; align-items: center;
}
.trust-label { margin-top: 6rpx; font-size: 22rpx; font-weight: 700; color: $on-surface; }
.trust-sub { margin-top: 2rpx; font-size: 18rpx; color: #9E9E9E; }

/* ===== 底部固定操作栏 ===== */
.page-footer {
  position: fixed; bottom: 0; left: 0; right: 0; z-index: 50;
  background: #ffffff;
  border-top: 1rpx solid $line;
  padding-bottom: env(safe-area-inset-bottom);
}
.footer-bar {
  display: flex; align-items: center; gap: 12rpx;
  padding: 14rpx 20rpx;
}
.bb-icon {
  display: flex; flex-direction: column; align-items: center;
  padding: 4rpx 12rpx; gap: 2rpx;
  font-size: 19rpx; color: $on-surface-var;
}
.bb-btn {
  flex: 1; height: 84rpx; line-height: 84rpx; text-align: center;
  border-radius: 42rpx; font-size: 27rpx; font-weight: 600;

  &.ghost {
    background: #F1F7EF;
    border: 2rpx solid $primary;
    color: $primary;
  }
  &.solid {
    background: linear-gradient(135deg, $primary 0%, $primary-dark 100%);
    color: #ffffff;
  }
}
</style>
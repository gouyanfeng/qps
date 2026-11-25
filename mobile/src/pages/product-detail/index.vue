<template>
  <q-page v-if="product" refresh-enabled :refresher-triggered="refreshing" @refresh="onRefresh">
    <!-- ============ 顶栏：与其他页面统一 ============ -->
    <q-page-header-back :title="product.name" />

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
            <q-image :src="img" mode="aspectFill" class="swiper-img" />
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

        <!-- 促销标签：文案与类型均取后端促销字段，按作用域从小到大排列 -->
        <view v-if="promotionTags.length" class="promo-tags">
          <view v-for="tag in promotionTags" :key="tag.id" class="promo-tag">
            <text v-if="tag.kind" class="promo-tag-kind">{{ tag.kind }}</text>
            <text class="promo-tag-text">{{ tag.text }}</text>
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
              <q-image
                v-for="(img, imgIdx) in review.images"
                :key="imgIdx"
                class="review-image"
                :src="img"
                thumb
                :width="120"
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

      <!-- ============ 底部固定操作栏 ============ -->
      <template #footer>
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
      </template>
  </q-page>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { onLoad } from '@dcloudio/uni-app'
import { getProductById, getProductReviews, addToCart as addCart } from '@/api'
import type { Product, Sku, ProductPromotion, ProductReview, ProductReviewPage } from '@/api/types'
import { resolveFileUrl, UnauthorizedError } from '@/utils/http'
import { resolveTagColor, tagChipStyle } from '@/utils/tagColor'

const product = ref<Product | null>(null)
const productId = ref('')
const refreshing = ref(false)
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
  await loadProduct()
})

async function loadProduct() {
  if (!productId.value) return
  void loadFeaturedReviews(productId.value)
  try {
    product.value = (await getProductById(productId.value)) ?? null
    // 默认选中第一个有库存的规格，全部售罄时退回第一条供页面展示
    const skus = product.value?.skus ?? []
    const initialSku = skus.find(sku => sku.stock > 0) ?? skus[0]
    if (initialSku) selectedSpecs.value = { 规格: initialSku.specName }
  } catch (error) {
    console.error('加载商品详情失败', error)
    if (error instanceof UnauthorizedError) return
    uni.showToast({ title: '商品详情加载失败', icon: 'none' })
  }
}

async function onRefresh() {
  refreshing.value = true
  try {
    await loadProduct()
  } finally {
    refreshing.value = false
  }
}

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

// 促销标签：只展示后端返回的促销（ruleText 为后端规则文案，缺失时回退促销名）
const PROMOTION_KIND_LABEL: Record<number, string> = { 0: '满减', 1: '折扣', 2: '买赠' }
// 作用域从小到大：SKU → 商品 → 分类 → 全场
const PROMOTION_SCOPE_ORDER: Record<string, number> = { Sku: 0, Product: 1, Category: 2, Global: 3 }

// 优惠力度：满减取减免金额，折扣取折扣力度，买赠不参与比价
function promotionStrength(promotion: ProductPromotion): number {
  try {
    const config = JSON.parse(promotion.actionConfigJson || '{}') as { amount?: number; rate?: number }
    if (typeof config.amount === 'number') return config.amount
    if (typeof config.rate === 'number') return 1 - config.rate
  } catch {
    // 配置缺失或格式异常时按无力度处理，不影响标签展示
  }
  return 0
}

const promotionTags = computed(() => {
  const source = product.value?.promotions ?? []
  return [...source]
    .filter(promotion => Boolean(promotion.ruleText || promotion.name))
    .sort((a, b) =>
      (PROMOTION_SCOPE_ORDER[a.scopeType] ?? 4) - (PROMOTION_SCOPE_ORDER[b.scopeType] ?? 4)
      || a.promotionType - b.promotionType
      || promotionStrength(b) - promotionStrength(a)
      || a.thresholdAmount - b.thresholdAmount
      || a.id.localeCompare(b.id))
    .map(promotion => ({
      id: promotion.id,
      kind: PROMOTION_KIND_LABEL[promotion.promotionType] ?? '',
      text: promotion.ruleText || promotion.name
    }))
})

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

// 详情富文本里的文件名补全为可访问的完整地址（绝对路径由 resolveFileUrl 原样返回）
const detailHtml = computed(() => {
  const html = product.value?.detailHtml ?? ''
  if (!html) return ''
  return html.replace(/src="([^"]+)"/g, (_match, url: string) => `src="${resolveFileUrl(url)}"`)
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
@import "./index.scss";
</style>
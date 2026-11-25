<template>
  <view class="page">
    <q-page-header-back :title="detail?.name || '活动专题'" />

    <scroll-view scroll-y class="page-body">
      <view v-if="loading" class="loading-state">
        <text class="loading-text">正在加载活动…</text>
      </view>

      <view v-else-if="!detail" class="loading-state">
        <text class="loading-text">{{ errorText || '活动暂不可用' }}</text>
      </view>

      <view v-else class="campaign">
        <!-- 头图与主题 -->
        <view class="hero">
          <q-image v-if="detail.coverImage" :src="detail.coverImage" thumb :width="750" mode="aspectFill" class="hero-image" />
          <view v-else class="hero-image hero-image-empty"></view>
          <view class="hero-shade"></view>
          <view class="hero-content">
            <text class="hero-overline">草本手记 · 东方生活志</text>
            <text class="hero-title">{{ detail.name }}</text>
            <view class="hero-meta">
              <text class="status-pill" :class="`status-${detail.status.toLowerCase()}`">{{ statusText }}</text>
              <text class="hero-period">{{ periodText }}</text>
            </view>
          </view>
        </view>

        <!-- 状态提示 -->
        <view v-if="detail.status !== 'Ongoing'" class="state-notice">
          <text class="state-notice-title">{{ detail.status === 'Upcoming' ? '活动尚未开始' : '活动已结束' }}</text>
          <text class="state-notice-text">{{ stateNoticeText }}</text>
        </view>

        <!-- 图文介绍 -->
        <view v-if="detail.contentHtml" class="article">
          <rich-text class="article-rich" :nodes="richNodes" />
        </view>

        <!-- 商品推荐型：搭配加购 -->
        <block v-if="detail.type === 0">
          <view class="section-head">
            <text class="section-overline">本期搭配推荐</text>
            <text class="section-title">草本同源 · 组合加购</text>
            <text class="section-desc">由驻店药师挑选的时节搭配，可单品加购，也可整组带走。</text>
          </view>

          <view v-for="group in productGroups" :key="group.key" class="product-group">
            <view v-if="group.name" class="group-head">
              <text class="group-name">{{ group.name }}</text>
              <text class="group-count">{{ group.items.length }} 件</text>
            </view>

            <view v-for="item in group.items" :key="item.campaignProductId" class="product-row">
              <view class="product-main" @tap="goProduct(item)">
                <view class="product-image-wrap">
                  <q-image :src="item.image" thumb :width="375" mode="aspectFill" class="product-image" />
                  <view
                    v-if="item.badgeText"
                    class="product-badge"
                    :style="{ backgroundColor: item.badgeColor }"
                  >
                    <text class="product-badge-text">{{ item.badgeText }}</text>
                  </view>
                </view>
                <view class="product-info">
                  <text class="product-name">{{ item.name }}</text>
                  <text v-if="item.specText" class="product-spec">{{ item.specText }}</text>
                  <view v-if="item.tags.length" class="product-tags">
                    <text v-for="tag in item.tags" :key="tag" class="product-tag">{{ tag }}</text>
                  </view>
                  <view class="product-price-row">
                    <text class="price-symbol">¥</text>
                    <text class="price-value">{{ item.priceText }}</text>
                    <text v-if="item.originalPriceText" class="price-original">¥{{ item.originalPriceText }}</text>
                  </view>
                </view>
              </view>
              <view
                class="product-add"
                :class="{ disabled: !canTrade || !item.skuId }"
                @tap.stop="addSingle(item)"
              >
                <text class="product-add-text">{{ item.skuId ? '加入购物车' : '暂时缺货' }}</text>
              </view>
            </view>
          </view>

          <view v-if="!loadingProducts && !campaignProducts.length" class="empty-block">
            <text class="empty-title">本期搭配正在准备中</text>
            <text class="empty-text">活动商品暂未上架，稍后再来看看。</text>
          </view>

          <view v-if="loadingProducts" class="loading-more">
            <text class="loading-more-text">加载中…</text>
          </view>
          <view v-else-if="hasMore && campaignProducts.length" class="loading-more" @tap="loadMoreProducts">
            <text class="loading-more-text">加载更多商品</text>
          </view>
        </block>

        <!-- 领券型：优惠券 -->
        <block v-else-if="detail.type === 2">
          <view class="section-head">
            <text class="section-overline">本草手记券 · 限时礼券包</text>
            <text class="section-title">一券一诺 · 从产地到案头</text>
            <text class="section-desc">按活动规则领取，券会存入「我的 - 优惠券」，结算时自动可用。</text>
          </view>

          <view v-for="coupon in detail.coupons" :key="coupon.couponId" class="coupon-card">
            <view class="coupon-left">
              <view class="coupon-value">
                <text v-if="isPercentCoupon(coupon.couponType)" class="coupon-value-text">{{ couponDiscountRateText(coupon.discountAmount) }}</text>
                <block v-else>
                  <text class="coupon-value-symbol">¥</text>
                  <text class="coupon-value-text">{{ coupon.discountAmount }}</text>
                </block>
              </view>
              <text class="coupon-threshold">
                {{ coupon.thresholdAmount > 0 ? `满 ${coupon.thresholdAmount} 可用` : '无门槛' }}
              </text>
            </view>
            <view class="coupon-dash"></view>
            <view class="coupon-right">
              <text class="coupon-name">{{ coupon.name }}</text>
              <text class="coupon-scope">{{ scopeText(coupon.scopeType) }} · 每人限领 {{ coupon.perLimit }} 张</text>
              <text class="coupon-remain">{{ coupon.remainCount <= 0 ? '已领完' : `剩余 ${coupon.remainCount} 张` }}</text>
            </view>
            <view
              class="coupon-action"
              :class="{ disabled: !canClaim(coupon) }"
              @tap.stop="claimOne(coupon)"
            >
              <text class="coupon-action-text">{{ claimButtonText(coupon) }}</text>
            </view>
          </view>

          <view v-if="!detail.coupons.length" class="empty-block">
            <text class="empty-title">券包正在准备中</text>
            <text class="empty-text">活动优惠券暂未配置，稍后再来看看。</text>
          </view>
        </block>

        <view class="assurance">
          <text class="assurance-item">· 核心产地直采</text>
          <text class="assurance-item">· 中检院全项质检</text>
          <text class="assurance-item">· 顺丰冷链直发</text>
        </view>

        <view class="footer-space"></view>
      </view>
    </scroll-view>

    <!-- 底部操作栏 -->
    <view v-if="detail" class="action-bar">
      <block v-if="detail.type === 0">
        <view class="action-summary">
          <text class="summary-label">搭配合计</text>
          <view class="summary-price-row">
            <text class="summary-symbol">¥</text>
            <text class="summary-price">{{ groupTotalText }}</text>
            <text class="summary-count">共 {{ campaignProducts.length }} 件</text>
          </view>
        </view>
        <view class="action-button" :class="{ disabled: !canTrade || !campaignProducts.length }" @tap="addAll">
          <text class="action-button-text">{{ detail.status === 'Upcoming' ? '未开始' : detail.status === 'Ended' ? '已结束' : '整组加入购物车' }}</text>
        </view>
      </block>
      <block v-else>
        <view class="action-summary">
          <text class="summary-label">券包共 {{ detail.coupons.length }} 张</text>
          <text class="summary-count">已领取 {{ claimedIds.length }} 张</text>
        </view>
        <view
          class="action-button"
          :class="{ disabled: !canTrade || !claimableCoupons.length }"
          @tap="claimAll"
        >
          <text class="action-button-text">{{ claimAllButtonText }}</text>
        </view>
      </block>
    </view>
  </view>
</template>

<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { onLoad } from '@dcloudio/uni-app'
import {
  addToCart,
  claimCoupon,
  getCampaignDetail,
  getCampaignProducts,
  isLoggedIn
} from '@/api'
import type { CampaignCoupon, CampaignDetail, CampaignProductItem } from '@/api'
import type { Product, Sku } from '@/api/types'
import { resolveFileUrl, UnauthorizedError } from '@/utils/http'
import { couponDiscountRateText, isPercentCoupon } from '@/utils/coupon'

interface DisplayProduct {
  campaignProductId: string
  productId: string
  campaignProductName: string
  name: string
  image: string
  skuId: string
  skuSpecName: string
  weightGram: number
  price: number
  priceText: string
  originalPriceText: string
  specText: string
  tags: string[]
  badgeText: string
  badgeColor: string
}

interface ProductGroup {
  key: string
  name: string
  items: DisplayProduct[]
}

const detail = ref<CampaignDetail | null>(null)
const loading = ref(true)
const errorText = ref('')
const campaignProducts = ref<DisplayProduct[]>([])
const loadingProducts = ref(false)
const hasMore = ref(false)
const totalProducts = ref(0)
const page = ref(1)
const pageSize = 10
const claimedIds = reactive<string[]>([])

const canTrade = computed(() => !!detail.value && detail.value.status === 'Ongoing')
const statusText = computed(() => {
  if (!detail.value) return ''
  if (detail.value.status === 'Upcoming') return '未开始'
  if (detail.value.status === 'Ended') return '已结束'
  return '进行中'
})

const periodText = computed(() => {
  if (!detail.value) return ''
  return `${formatDateTime(detail.value.startAt)} - ${formatDateTime(detail.value.endAt)}`
})

const stateNoticeText = computed(() => {
  if (!detail.value) return ''
  return detail.value.status === 'Upcoming'
    ? `活动将于 ${formatDateTime(detail.value.startAt)} 开始，可先浏览图文与商品。`
    : `活动已于 ${formatDateTime(detail.value.endAt)} 结束，商品与领券暂不可用。`
})

const richNodes = computed(() => {
  const html = detail.value?.contentHtml || ''
  if (!html) return ''
  return html
    .replace(/src="([^"]+)"/g, (_match, url: string) => `src="${resolveFileUrl(url)}"`)
    .replace(/<img/g, '<img style="max-width:100%;height:auto;display:block;"')
})

const productGroups = computed<ProductGroup[]>(() => {
  const groups: ProductGroup[] = []
  const map = new Map<string, ProductGroup>()

  for (const item of campaignProducts.value) {
    const name = (item.campaignProductName || '').trim()
    const key = name || '__default__'
    let group = map.get(key)
    if (!group) {
      group = { key, name: name || '本期搭配推荐', items: [] }
      map.set(key, group)
      groups.push(group)
    }
    group.items.push(item)
  }

  return groups
})

const groupTotal = computed(() =>
  campaignProducts.value.reduce((total, item) => total + (item.price > 0 ? item.price : 0), 0)
)

const groupTotalText = computed(() => formatMoney(groupTotal.value))

const claimableCoupons = computed(() =>
  (detail.value?.coupons || []).filter(coupon => canClaim(coupon))
)

const claimAllButtonText = computed(() => {
  if (!detail.value) return '一键领券'
  if (detail.value.status === 'Upcoming') return '活动未开始'
  if (detail.value.status === 'Ended') return '活动已结束'
  if (!claimableCoupons.value.length) return '已全部领取'
  return `一键领券（${claimableCoupons.value.length}）`
})

onLoad(async (options) => {
  const id = options?.id
  if (!id) {
    loading.value = false
    errorText.value = '活动链接无效'
    return
  }

  try {
    detail.value = await getCampaignDetail(id)
    if (detail.value.type === 0) {
      await loadProducts(id, 1)
    }
  } catch (error) {
    if (error instanceof UnauthorizedError) return
    errorText.value = (error as Error)?.message || '活动暂不可用'
  } finally {
    loading.value = false
  }
})

async function loadProducts(campaignId: string, targetPage: number) {
  if (loadingProducts.value) return
  loadingProducts.value = true
  try {
    const data = await getCampaignProducts(campaignId, targetPage, pageSize)
    const items = data.items.map(mapCampaignProduct)
    campaignProducts.value = targetPage === 1 ? items : [...campaignProducts.value, ...items]
    totalProducts.value = data.total
    hasMore.value = data.hasMore
    page.value = targetPage
  } catch (error) {
    if (error instanceof UnauthorizedError) return
    if (targetPage === 1) {
      campaignProducts.value = []
      errorText.value = (error as Error)?.message || ''
    }
  } finally {
    loadingProducts.value = false
  }
}

function loadMoreProducts() {
  if (!detail.value || !hasMore.value || loadingProducts.value) return
  loadProducts(detail.value.id, page.value + 1)
}

interface CampaignProductWithGroup extends CampaignProductItem {
  campaignProductName?: string
}

function mapCampaignProduct(row: CampaignProductItem): DisplayProduct {
  const product = row.product
  const sku = pickSku(product)
  const price = effectivePrice(sku)
  const productTags = (product.tags || [])
    .filter(tag => tag.type !== 'Promotion')
    .map(tag => tag.name)
    .filter(Boolean)
    .slice(0, 2)

  return {
    campaignProductId: row.campaignProductId,
    productId: product.id,
    campaignProductName: row.groupName,
    name: product.name,
    image: product.images[0] || '',
    skuId: sku?.id || '',
    skuSpecName: sku?.specName || '',
    weightGram: sku?.weightGram || 0,
    price,
    priceText: formatMoney(price),
    originalPriceText: sku && sku.discountedPrice !== null && sku.discountedPrice !== undefined && sku.discountedPrice < sku.price
      ? formatMoney(sku.price)
      : '',
    specText: sku?.specName || '',
    tags: productTags,
    badgeText: product.promotionBadgeText || '',
    badgeColor: product.promotionBadgeColor || '#0d631b'
  }
}

function pickSku(product: Product): Sku | null {
  const skus = (product.skus || []).filter(sku => sku.stock > 0)
  const pool = skus.length ? skus : product.skus || []
  return pool.reduce<Sku | null>((best, sku) => {
    if (!best) return sku
    return effectivePrice(sku) < effectivePrice(best) ? sku : best
  }, null)
}

function effectivePrice(sku: { price: number; discountedPrice?: number | null } | null) {
  if (!sku) return 0
  return sku.discountedPrice !== null && sku.discountedPrice !== undefined ? sku.discountedPrice : sku.price
}

function formatMoney(value: number) {
  return Number(value).toFixed(2)
}

function formatDateTime(value: string) {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return ''
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${date.getFullYear()}.${pad(date.getMonth() + 1)}.${pad(date.getDate())} ${pad(date.getHours())}:${pad(date.getMinutes())}`
}

function scopeText(scopeType: string) {
  if (scopeType === 'all') return '全场通用'
  if (scopeType === 'category') return '指定品类'
  if (scopeType === 'product') return '指定商品'
  return '指定范围'
}

function goProduct(item: DisplayProduct) {
  uni.navigateTo({ url: `/pages/product-detail/index?id=${item.productId}` })
}

async function addSingle(item: DisplayProduct) {
  if (!canTrade.value) {
    uni.showToast({ title: detail.value?.status === 'Upcoming' ? '活动尚未开始' : '活动已结束', icon: 'none' })
    return
  }
  if (!item.skuId) {
    uni.showToast({ title: '该商品暂时缺货', icon: 'none' })
    return
  }
  await addOneToCart(item)
}

async function addAll() {
  if (!canTrade.value) {
    uni.showToast({ title: detail.value?.status === 'Upcoming' ? '活动尚未开始' : '活动已结束', icon: 'none' })
    return
  }
  const items = campaignProducts.value.filter(item => item.skuId)
  if (!items.length) {
    uni.showToast({ title: '暂无可加购的商品', icon: 'none' })
    return
  }

  let success = 0
  for (const item of items) {
    try {
      await addOneToCart(item, true)
      success++
    } catch (error) {
      if (error instanceof UnauthorizedError) return
    }
  }

  if (success) {
    uni.showToast({ title: `已将 ${success} 件商品加入购物车`, icon: 'none' })
  } else {
    uni.showToast({ title: '加入购物车失败', icon: 'none' })
  }
}

async function addOneToCart(item: DisplayProduct, silent = false) {
  try {
    await addToCart({
      productId: item.productId,
      productName: item.name,
      productImage: item.image,
      skuId: item.skuId,
      specName: item.skuSpecName,
      price: item.price,
      quantity: 1,
      weightGram: item.weightGram,
      selected: true
    })
    if (!silent) {
      uni.showToast({ title: '已加入购物车', icon: 'none' })
    }
  } catch (error) {
    if (error instanceof UnauthorizedError) throw error
    if (!silent) {
      uni.showToast({ title: '加入购物车失败', icon: 'none' })
    }
    throw error
  }
}

function canClaim(coupon: CampaignCoupon) {
  return canTrade.value && coupon.isClaimable && coupon.remainCount > 0 && !claimedIds.includes(coupon.couponId)
}

function claimButtonText(coupon: CampaignCoupon) {
  if (claimedIds.includes(coupon.couponId)) return '已领取'
  if (detail.value?.status === 'Upcoming') return '未开始'
  if (detail.value?.status === 'Ended') return '已结束'
  if (!coupon.isActive) return '不可领取'
  if (coupon.remainCount <= 0) return '已领完'
  if (!coupon.isClaimable) return '不可领取'
  return '立即领取'
}

async function claimOne(coupon: CampaignCoupon) {
  if (!canClaim(coupon)) {
    if (!isLoggedIn()) {
      uni.showToast({ title: '请先登录后领券', icon: 'none' })
    }
    return
  }

  try {
    await claimCoupon(coupon.couponId)
    claimedIds.push(coupon.couponId)
    coupon.remainCount = Math.max(0, coupon.remainCount - 1)
    uni.showToast({ title: '领取成功', icon: 'none' })
  } catch (error) {
    if (error instanceof UnauthorizedError) return
    uni.showToast({ title: (error as Error)?.message || '领取失败', icon: 'none' })
  }
}

async function claimAll() {
  if (!canTrade.value) {
    uni.showToast({ title: detail.value?.status === 'Upcoming' ? '活动尚未开始' : '活动已结束', icon: 'none' })
    return
  }

  const targets = claimableCoupons.value
  if (!targets.length) {
    uni.showToast({ title: '没有可领取的券', icon: 'none' })
    return
  }

  let success = 0
  for (const coupon of targets) {
    try {
      await claimCoupon(coupon.couponId)
      claimedIds.push(coupon.couponId)
      coupon.remainCount = Math.max(0, coupon.remainCount - 1)
      success++
    } catch (error) {
      if (error instanceof UnauthorizedError) return
      uni.showToast({ title: (error as Error)?.message || '领取失败', icon: 'none' })
    }
  }

  if (success) {
    uni.showToast({ title: `成功领取 ${success} 张券`, icon: 'none' })
  }
}
</script>

<style scoped lang="scss">
@import "./index.scss";
</style>

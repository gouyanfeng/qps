<template>
  <q-page refresh-enabled show-loadmore :refresher-triggered="refreshing" :loading="loading" :has-more="hasMore" @refresh="onRefresh" @loadmore="loadMore">
    <q-page-header-back title="我的订单" />

      <!-- Search Bar -->
      <view class="search-bar">
        <q-search-bar
          v-model="keyword"
          placeholder="搜索订单号 / 收货人 / 手机号"
          confirm-type="search"
          :maxlength="50"
          @confirm="onSearch"
          @clear="onSearch"
        />
      </view>

      <!-- Status Tabs -->
      <scroll-view scroll-x class="tabs-scroll" :show-scrollbar="false">
        <view class="tabs">
          <view
            v-for="tab in tabs"
            :key="tab.value"
            class="tab-item"
            :class="{ active: activeTab === tab.value }"
            @tap="switchTab(tab.value)"
          >
            <text class="tab-text">{{ tab.label }}</text>
            <view v-if="tab.badge" class="tab-badge">
              <text class="tab-badge-text">{{ tab.badge }}</text>
            </view>
            <view v-if="activeTab === tab.value" class="tab-indicator"></view>
          </view>
        </view>
      </scroll-view>

      <!-- Order Cards -->
      <view class="orders-list">
        <view
          v-for="order in orders"
          :key="order.orderNo"
          class="order-card"
        >
          <!-- Store Header & Status -->
          <view class="card-header">
            <view class="store-info">
              <view class="store-icon">
                <q-icon name="shop" size="14" color="#2E7D32" />
              </view>
              <text class="store-name">{{ order.items[0]?.storeName || '本草国医馆' }}</text>
              <q-icon name="chevron-right" size="14" color="#707A6C" />
            </view>
            <view class="status-wrap">
              <view v-if="order.status === 'Shipped'" class="status-dot"></view>
              <text class="status-text" :class="'status-' + order.status">{{ orderStatusText(order.status) }}</text>
            </view>
          </view>

          <!-- Order No -->
          <view class="order-no-row">
            <text class="order-no-text">订单号: {{ order.orderNo }}</text>
            <view class="order-no-copy" @tap.stop="copyOrderNo(order.orderNo)">
              <view class="copy-icon"></view>
              <text class="copy-text">复制</text>
            </view>
          </view>

          <!-- 运单摘要（已发货且有真实运单时展示） -->
          <view v-if="order.status === 'Shipped' && order.shipment" class="cold-tag">
            <q-icon name="snowflake" size="13" color="#2E7D32" />
            <text class="cold-tag-text">{{ order.shipment.expressCompany }} {{ order.shipment.trackingNo }}</text>
          </view>

          <!-- Order Items -->
          <view
            v-for="item in order.items"
            :key="item.id"
            class="item-row"
            @tap="goDetail(order.orderNo)"
          >
            <view class="item-image-wrap">
              <q-image
                v-if="item.productImage"
                class="item-image"
                :src="item.productImage"
                thumb
                :width="120"
                mode="aspectFill"
              />
              <view v-else class="item-image-placeholder">
                <q-icon name="tea" size="28" color="#BDBDBD" />
              </view>
              <view v-if="item.badge" class="item-badge">
                <text class="item-badge-text">{{ item.badge }}</text>
              </view>
            </view>
            <view class="item-info">
              <text class="item-name">{{ item.productName }}</text>
              <text class="item-spec">{{ item.specName }}</text>
              <view class="item-price-row">
                <view class="item-price-left">
                  <text class="item-price">¥{{ item.unitPrice.toFixed(2) }}</text>
                  <text
                    v-if="item.originalUnitPrice > item.unitPrice"
                    class="item-price-origin"
                  >¥{{ item.originalUnitPrice.toFixed(2) }}</text>
                </view>
                <text class="item-qty">x{{ item.quantity }}</text>
              </view>
              <text v-if="promotionText(item)" class="item-promo">{{ promotionText(item) }}</text>
            </view>
          </view>

          <!-- Order Summary -->
          <view class="card-summary">
            <text class="summary-count">共{{ totalQuantity(order) }}件商品</text>
            <text class="summary-label">{{ order.status === 'PendingPayment' ? '应付' : '实付' }}</text>
            <text class="summary-amount" :class="{ 'amount-accent': order.status === 'PendingPayment' }">
              ¥{{ (order.paidAmount > 0 ? order.paidAmount : order.payableAmount).toFixed(2) }}
            </text>
          </view>

          <!-- Actions -->
          <view class="card-actions">
            <view
              v-for="action in orderActions(order)"
              :key="action.label"
              class="action-btn"
              :class="action.type"
              @tap="action.handler"
            >
              <q-icon v-if="action.icon" :name="action.icon" size="14" :color="action.iconColor || '#1B1C1C'" />
              <text class="action-text">{{ action.label }}</text>
            </view>
          </view>
        </view>

        <view v-if="!loading && !orders.length" class="empty-state">
          <q-icon name="order" size="48" color="#BDBDBD" />
          <text class="empty-text">暂无相关订单</text>
        </view>
      </view>

      <!-- Reassurance Banner -->
      <view class="reassurance">
        <q-icon name="certificate" size="14" color="#2E7D32" />
        <text class="reassurance-text">国家中医药管理局规范标准 · 一物一码全程溯源 · 顺丰冷链恒温直达</text>
      </view>
  </q-page>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { onLoad, onShow } from '@dcloudio/uni-app'
import { getMyOrders, cancelOrder as apiCancelOrder, createPayment, simulatePayment, confirmReceipt as apiConfirmReceipt } from '@/api'
import type { MallOrder, MallOrderItem } from '@/api'
import { UnauthorizedError } from '@/utils/http'

const PAGE_SIZE = 10

const orders = ref<MallOrder[]>([])
const loading = ref(false)
const activeTab = ref('')
const keyword = ref('')
const currentPage = ref(1)
const hasMore = ref(false)
const refreshing = ref(false)

interface TabItem {
  label: string
  value: string
  badge?: number
}

const tabs = ref<TabItem[]>([
  { label: '全部', value: '' },
  { label: '待付款', value: 'PendingPayment' },
  { label: '待发货', value: 'Paid' },
  { label: '待收货', value: 'Shipped' },
  { label: '已完成', value: 'Completed' },
  { label: '退款/售后', value: 'Refunding,Refunded,Returning,Returned' }
])

onLoad(options => {
  const filter = options?.status || ''
  activeTab.value = ({
    unpaid: 'PendingPayment',
    ship: 'Paid',
    receive: 'Shipped',
    done: 'Completed',
    refund: 'Refunding,Refunded,Returning,Returned'
  } as Record<string, string>)[filter] || ''
})

onShow(() => {
  loadOrders(true)
})

async function loadOrders(reset: boolean = false) {
  if (reset) currentPage.value = 1
  loading.value = true
  try {
    const response = await getMyOrders(
      activeTab.value || undefined,
      currentPage.value,
      PAGE_SIZE,
      keyword.value.trim() || undefined
    )
    const list = response.list ?? response.items ?? []
    orders.value = reset ? list : [...orders.value, ...list]
    const total = response.totalCount ?? response.total ?? 0
    hasMore.value = currentPage.value * PAGE_SIZE < total
  } catch (error) {
    console.error('加载订单失败', error)
    if (error instanceof UnauthorizedError) return
    uni.showToast({ title: '订单加载失败', icon: 'none' })
  } finally {
    loading.value = false
  }
}

async function onRefresh() {
  refreshing.value = true
  try {
    await loadOrders(true)
  } finally {
    refreshing.value = false
  }
}

function loadMore() {
  if (!loading.value && hasMore.value) {
    currentPage.value++
    loadOrders()
  }
}

function switchTab(value: string) {
  activeTab.value = value
  loadOrders(true)
}

function onSearch() {
  loadOrders(true)
}

function orderStatusText(value: string) {
  const labels: Record<string, string> = {
    PendingPayment: '待付款',
    Paid: '待发货',
    Shipped: '待收货',
    Completed: '交易已完成',
    Cancelled: '已取消',
    Refunding: '退款中',
    Refunded: '已退款',
    Returning: '售后处理中',
    Returned: '售后完成'
  }
  return labels[value] || value
}

function totalQuantity(order: MallOrder) {
  return order.items.reduce((sum, item) => sum + item.quantity, 0)
}

interface PromotionSnapshotEntry {
  id: string
  name: string
  actionType: string
  amount: number
}

/** 促销文案由前端按结构化快照拼装，数据侧只存促销名称与金额 */
function promotionText(item: MallOrderItem) {
  let entries: PromotionSnapshotEntry[] = []
  try {
    entries = JSON.parse(item.promotionSnapshot || '[]') as PromotionSnapshotEntry[]
  } catch {
    entries = []
  }
  if (!Array.isArray(entries) || entries.length === 0) {
    return item.promotionName || ''
  }
  return entries
    .map(entry => (entry.amount > 0 ? `${entry.name} -¥${entry.amount.toFixed(2)}` : entry.name))
    .join('；')
}

function copyOrderNo(value: string) {
  uni.setClipboardData({
    data: value,
    success: () => uni.showToast({ title: '订单号已复制', icon: 'none' })
  })
}

interface OrderAction {
  label: string
  type: 'default' | 'primary' | 'accent' | 'outline'
  icon?: string
  iconColor?: string
  handler: () => void
}

function orderActions(order: MallOrder): OrderAction[] {
  const go = () => goDetail(order.orderNo)
  const actions: OrderAction[] = []

  switch (order.status) {
    case 'PendingPayment':
      actions.push(
        { label: '取消订单', type: 'outline', handler: () => cancelOrder(order.orderNo) },
        { label: '立即付款', type: 'accent', handler: () => payOrder(order.orderNo) }
      )
      break
    case 'Paid':
      actions.push(
        { label: '查看详情', type: 'default', handler: go }
      )
      break
    case 'Shipped':
      actions.push(
        { label: '道地溯源', type: 'default', icon: 'qrcode', iconColor: '#2E7D32', handler: go },
        { label: '查看物流', type: 'default', handler: go },
        { label: '确认收货', type: 'primary', handler: () => confirmReceive(order.orderNo) }
      )
      break
    case 'Completed':
      actions.push(
        { label: '再次购买', type: 'default', handler: () => rebuy(order) },
        { label: '评价有礼', type: 'primary', handler: () => goReview(order) }
      )
      break
    default:
      actions.push({ label: '查看详情', type: 'default', handler: go })
  }
  return actions
}

function goDetail(orderNo: string) {
  uni.navigateTo({ url: `/pages/order-detail/index?orderNo=${orderNo}` })
}

function rebuy(order: MallOrder) {
  const productId = order.items?.[0]?.productId
  if (!productId) {
    uni.showToast({ title: '商品信息缺失', icon: 'none' })
    return
  }
  uni.navigateTo({ url: `/pages/product-detail/index?id=${productId}` })
}

function goReview(order: MallOrder) {
  const target = order.items?.find(item => !item.reviewed)
  if (!target) {
    uni.showToast({ title: '该订单商品已全部评价', icon: 'none' })
    return
  }
  uni.navigateTo({
    url: `/pages/review-create/index?orderNo=${encodeURIComponent(order.orderNo)}&itemId=${encodeURIComponent(target.id)}`
  })
}

function cancelOrder(orderNo: string) {
  uni.showModal({
    title: '提示',
    content: '确定取消该订单吗？',
    success: async (res) => {
      if (!res.confirm) return
      try {
        uni.showLoading({ title: '取消中...' })
        await apiCancelOrder(orderNo)
        uni.hideLoading()
        uni.showToast({ title: '订单已取消', icon: 'success' })
        loadOrders(true)
      } catch (e: any) {
        uni.hideLoading()
        uni.showToast({ title: e?.message || '取消失败', icon: 'none' })
      }
    }
  })
}

async function payOrder(orderNo: string) {
  try {
    uni.showLoading({ title: '创建支付单...' })
    const payment = await createPayment(orderNo)
    uni.hideLoading()

    uni.showLoading({ title: '支付处理中...' })
    await simulatePayment(payment.paymentNo, true)
    uni.hideLoading()

    uni.showToast({ title: '支付成功', icon: 'success' })
    loadOrders(true)
  } catch (e: any) {
    uni.hideLoading()
    uni.showToast({ title: e?.message || '支付失败', icon: 'none' })
  }
}

function confirmReceive(orderNo: string) {
  uni.showModal({
    title: '确认收货',
    content: '请确认已收到商品，确认后款项将支付给商家',
    success: async (res) => {
      if (!res.confirm) return
      try {
        uni.showLoading({ title: '处理中...' })
        await apiConfirmReceipt(orderNo)
        uni.hideLoading()
        uni.showToast({ title: '已确认收货', icon: 'success' })
        loadOrders(true)
      } catch (e: any) {
        uni.hideLoading()
        uni.showToast({ title: e?.message || '操作失败', icon: 'none' })
      }
    }
  })
}
</script>

<style lang="scss" scoped>
@import "./index.scss";
</style>
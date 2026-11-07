<template>
  <view class="page">
    <q-page-header-back title="我的订单" />

    <q-scroll-list class="page-body" :loading="loading" :has-more="hasMore" @loadmore="loadMore">
      <!-- Search Bar -->
      <view class="search-bar">
        <view class="search-input-wrap">
          <q-icon name="search" size="16" color="#707A6C" />
          <input
            class="search-input"
            placeholder="搜索道地药材品名、订单号..."
            placeholder-class="search-placeholder"
            confirm-type="search"
            @confirm="onSearch"
            v-model="keyword"
          />
        </view>
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
              <text class="store-name">本草国医馆 · 道地药房</text>
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
              <q-cached-image
                v-if="item.productImage"
                class="item-image"
                :src="item.productImage"
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
    </q-scroll-list>
  </view>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { onLoad, onShow } from '@dcloudio/uni-app'
import { getMyOrders, cancelOrder as apiCancelOrder, createPayment, simulatePayment, confirmReceipt as apiConfirmReceipt } from '@/api'
import type { MallOrder, MallOrderItem } from '@/api'
import { UnauthorizedError, resolveFileUrl } from '@/utils/http'

const orders = ref<MallOrder[]>([])
const loading = ref(false)
const activeTab = ref('')
const keyword = ref('')
const currentPage = ref(1)
const hasMore = ref(false)

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
    done: 'Completed'
  } as Record<string, string>)[filter] || ''
})

onShow(() => {
  fetchOrders(true)
})

async function fetchOrders(reset: boolean = false) {
  if (reset) currentPage.value = 1
  loading.value = true
  try {
    const response = await getMyOrders(activeTab.value || undefined)
    const list = response.list ?? response.items ?? []
    orders.value = reset ? list : [...orders.value, ...list]
    const total = response.totalCount ?? response.total ?? 0
    hasMore.value = orders.value.length < total
  } catch (error) {
    console.error('加载订单失败', error)
    if (error instanceof UnauthorizedError) return
    uni.showToast({ title: '订单加载失败', icon: 'none' })
  } finally {
    loading.value = false
  }
}

function loadMore() {
  if (!loading.value && hasMore.value) {
    currentPage.value++
    fetchOrders()
  }
}

function switchTab(value: string) {
  activeTab.value = value
  fetchOrders(true)
}

function onSearch() {
  fetchOrders(true)
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
        fetchOrders(true)
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
    fetchOrders(true)
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
        fetchOrders(true)
      } catch (e: any) {
        uni.hideLoading()
        uni.showToast({ title: e?.message || '操作失败', icon: 'none' })
      }
    }
  })
}
</script>

<style lang="scss" scoped>
.page {
  min-height: 100vh;
  background: var(--page-bg);
}

.page-body {
  height: calc(100vh - var(--page-top-offset));
  box-sizing: border-box;
  padding-bottom: calc(40rpx + env(safe-area-inset-bottom, 0));
}

/* Search Bar */
.search-bar {
  padding: 16rpx 24rpx 12rpx;
  display: flex;
  align-items: center;
  gap: 16rpx;
}

.search-input-wrap {
  flex: 1;
  display: flex;
  align-items: center;
  height: 72rpx;
  padding: 0 24rpx;
  background: #F6F3F2;
  border-radius: 36rpx;
  gap: 12rpx;
}

.search-input {
  flex: 1;
  font-size: 24rpx;
  color: #1B1C1C;
  background: transparent;
}

.search-placeholder {
  color: #707A6C;
  font-size: 24rpx;
}

/* Tabs */
.tabs-scroll {
  white-space: nowrap;
}

.tabs {
  display: flex;
  align-items: center;
  justify-content: space-between;
  box-sizing: border-box;
  width: 100%;
  min-width: 100%;
  padding: 12rpx 35rpx;
}

.tab-item {
  position: relative;
  display: inline-flex;
  align-items: center;
  gap: 6rpx;
  padding: 10rpx 0;
  flex-shrink: 0;
}

.tab-text {
  font-size: 24rpx;
  font-weight: 600;
  color: #40493D;
}

.tab-item.active .tab-text {
  color: #2E7D32;
  font-weight: 700;
}

.tab-badge {
  min-width: 28rpx;
  height: 28rpx;
  border-radius: 14rpx;
  background: #FF9800;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 0 6rpx;
}

.tab-badge-text {
  font-size: 18rpx;
  font-weight: 700;
  color: #ffffff;
}

.tab-indicator {
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  height: 4rpx;
  background: #2E7D32;
  border-radius: 2rpx;
}

/* Orders List */
.orders-list {
  padding: 0 24rpx;
  display: flex;
  flex-direction: column;
  gap: 24rpx;
}

/* Order Card */
.order-card {
  background: #ffffff;
  border-radius: 24rpx;
  padding: 28rpx;
  box-shadow: 0 8rpx 40rpx -4rpx rgba(46, 125, 50, 0.06);
  display: flex;
  flex-direction: column;
  gap: 20rpx;
}

/* Card Header */
.card-header {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
}

.store-info {
  display: flex;
  align-items: center;
  gap: 8rpx;
  min-width: 0;
}

.store-icon {
  width: 36rpx;
  height: 36rpx;
  border-radius: 18rpx;
  background: rgba(46, 125, 50, 0.1);
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.store-name {
  font-size: 24rpx;
  font-weight: 600;
  color: #1B1C1C;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.status-wrap {
  display: flex;
  align-items: center;
  gap: 8rpx;
  flex-shrink: 0;
}

.status-dot {
  width: 10rpx;
  height: 10rpx;
  border-radius: 5rpx;
  background: #2E7D32;
}

.status-text {
  font-size: 24rpx;
  font-weight: 600;
  color: #707A6C;
}

.status-PendingPayment {
  color: #FF9800;
}

.status-Paid {
  color: #2E7D32;
}

.status-Shipped {
  color: #2E7D32;
  font-weight: 700;
}

.status-Completed {
  color: #707A6C;
}

/* Order No */
.order-no-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16rpx;
}

.order-no-text {
  flex: 1;
  min-width: 0;
  font-size: 24rpx;
  color: #707A6C;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.order-no-copy {
  flex-shrink: 0;
  display: flex;
  align-items: center;
  gap: 6rpx;
  padding: 4rpx 0;
}

.copy-icon {
  position: relative;
  width: 24rpx;
  height: 24rpx;
}

.copy-icon::before,
.copy-icon::after {
  content: '';
  position: absolute;
  width: 15rpx;
  height: 15rpx;
  border: 2rpx solid #2E7D32;
  border-radius: 3rpx;
  box-sizing: border-box;
}

.copy-icon::before {
  left: 0;
  bottom: 0;
}

.copy-icon::after {
  right: 0;
  top: 0;
  background: #ffffff;
}

.copy-text {
  font-size: 22rpx;
  color: #2E7D32;
  font-weight: 500;
}

/* Cold-chain Tag */
.cold-tag {
  display: flex;
  align-items: center;
  gap: 10rpx;
  padding: 10rpx 16rpx;
  border-radius: 12rpx;
  background: #F6F3F2;
}

.cold-tag-text {
  font-size: 22rpx;
  font-weight: 500;
  color: #40493D;
  letter-spacing: 1rpx;
}

/* Item Row */
.item-row {
  display: flex;
  gap: 20rpx;
  padding-top: 6rpx;
}

.item-image-wrap {
  width: 144rpx;
  height: 144rpx;
  border-radius: 16rpx;
  overflow: hidden;
  background: #F6F3F2;
  flex-shrink: 0;
  position: relative;
}

.item-image {
  width: 100%;
  height: 100%;
}

.item-image-placeholder {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
}

.item-badge {
  position: absolute;
  top: 6rpx;
  left: 6rpx;
  padding: 2rpx 8rpx;
  border-radius: 6rpx;
  background: rgba(252, 249, 248, 0.9);
}

.item-badge-text {
  font-size: 18rpx;
  font-weight: 500;
  color: #2E7D32;
}

.item-info {
  flex: 1;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  min-width: 0;
  padding: 4rpx 0;
}

.item-name {
  font-size: 26rpx;
  font-weight: 500;
  color: #1B1C1C;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.item-spec {
  font-size: 24rpx;
  color: #40493D;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  margin-top: 4rpx;
}

.item-price-row {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  margin-top: 8rpx;
}

.item-price {
  font-size: 24rpx;
  font-weight: 600;
  color: #1B1C1C;
}

.item-price-left {
  display: flex;
  align-items: baseline;
  gap: 8rpx;
  min-width: 0;
}

.item-price-origin {
  font-size: 20rpx;
  color: #B0ACA2;
  text-decoration: line-through;
}

.item-promo {
  margin-top: 4rpx;
  font-size: 20rpx;
  color: #8A8578;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.item-qty {
  font-size: 24rpx;
  color: #707A6C;
}

/* Card Summary */
.card-summary {
  display: flex;
  justify-content: flex-end;
  align-items: center;
  gap: 10rpx;
  padding-top: 6rpx;
}

.summary-count {
  font-size: 24rpx;
  color: #40493D;
}

.summary-label {
  font-size: 24rpx;
  color: #1B1C1C;
}

.summary-amount {
  font-size: 32rpx;
  font-weight: 700;
  color: #1B1C1C;
  letter-spacing: -1rpx;
}

.amount-accent {
  color: #FF9800;
}

/* Card Actions */
.card-actions {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 12rpx;
  padding-top: 12rpx;
}

.action-btn {
  height: 64rpx;
  padding: 0 24rpx;
  border-radius: 32rpx;
  display: flex;
  align-items: center;
  gap: 6rpx;
  transition: opacity 0.2s;

  &.default {
    background: #F6F3F2;
    color: #1B1C1C;
  }

  &.primary {
    background: #2E7D32;
    color: #ffffff;
  }

  &.accent {
    background: #FF9800;
    color: #ffffff;
    font-weight: 600;
  }

  &.outline {
    background: transparent;
    color: #707A6C;
    border: 1rpx solid #BFCABA;
  }
}

.action-text {
  font-size: 24rpx;
  font-weight: 600;
}

.action-btn.primary .action-text,
.action-btn.accent .action-text {
  font-weight: 600;
}

/* Empty State */
.empty-state {
  padding: 120rpx 0;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 16rpx;
}

.empty-text {
  color: #8A8A8A;
  font-size: 26rpx;
}

/* Reassurance Banner */
.reassurance {
  margin: 32rpx 24rpx 24rpx;
  padding: 24rpx;
  border-radius: 24rpx;
  background: #F6F3F2;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 12rpx;
}

.reassurance-text {
  font-size: 22rpx;
  color: #40493D;
  letter-spacing: 1rpx;
  text-align: center;
}

/* Loading */
.loading-more {
  padding: 24rpx;
  text-align: center;
}

.loading-text {
  font-size: 24rpx;
  color: #707A6C;
}
</style>
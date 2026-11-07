<template>
  <view class="page">
    <q-page-header-back title="订单详情" />

    <scroll-view scroll-y class="page-body" v-if="order">
      <view class="status-banner" :class="'status-' + order.status">
        <view class="status-deco"></view>
        <view class="status-main">
          <view class="status-top">
            <text class="status-label">{{ statusText(order.status) }}</text>
            <view v-if="order.status === 'Shipped'" class="ship-tag">
              <q-icon name="snowflake" size="12" color="#ffffff" />
              <text>恒温达标</text>
            </view>
          </view>
          <text class="status-sub">{{ statusDesc(order) }}</text>
          <view v-if="order.status === 'Shipped'" class="cloud-tag">
            <view class="cloud-dot"></view>
            <text>本草冷链实时云监控中</text>
          </view>
        </view>
      </view>

      <view v-if="afterSaleView" class="card card-aftersale">
        <view class="card-head">
          <view class="head-icon">
            <q-icon name="shield-error" size="16" color="#2E7D32" />
          </view>
          <view class="head-text">
            <text class="head-title">售后进度</text>
            <text class="head-sub">{{ afterSaleView.no }}</text>
          </view>
          <view class="as-status" :class="'as-' + afterSaleView.status">{{ afterSaleView.statusText }}</view>
        </view>
        <view class="ship-info">
          <view class="ship-row">
            <text class="ship-label">售后类型</text>
            <text class="ship-val">{{ afterSaleView.kind === 'refund' ? '仅退款（整单）' : '退货退款（整单）' }}</text>
          </view>
          <view class="ship-row">
            <text class="ship-label">退款金额</text>
            <text class="ship-val price">¥{{ afterSaleView.amount.toFixed(2) }}</text>
          </view>
          <view class="ship-row">
            <text class="ship-label">申请原因</text>
            <text class="ship-val">{{ afterSaleView.reason }}</text>
          </view>
          <view v-if="afterSaleView.description" class="ship-row">
            <text class="ship-label">补充说明</text>
            <text class="ship-val">{{ afterSaleView.description }}</text>
          </view>
          <view v-if="afterSaleView.rejectReason" class="ship-row">
            <text class="ship-label">拒绝原因</text>
            <text class="ship-val reject">{{ afterSaleView.rejectReason }}</text>
          </view>
          <view v-if="afterSaleView.trackingNo" class="ship-row">
            <text class="ship-label">寄回物流</text>
            <text class="ship-val">{{ afterSaleView.expressCompany }} / {{ afterSaleView.trackingNo }}</text>
          </view>
          <view class="ship-row">
            <text class="ship-label">申请时间</text>
            <text class="ship-val">{{ formatTime(afterSaleView.createdAt) }}</text>
          </view>
        </view>
        <view v-if="afterSaleView.evidenceImages.length" class="as-images">
          <image
            v-for="(img, index) in afterSaleView.evidenceImages"
            :key="img + index"
            :src="resolveFileUrl(img)"
            mode="aspectFill"
            @tap="previewEvidence(index)"
          />
        </view>
        <view v-if="needReturnTracking" class="as-action" @tap="goReturnTracking">
          <text>填写寄回运单</text>
          <q-icon name="chevron-right" size="14" color="#2E7D32" />
        </view>
      </view>

      <view v-if="order.shipment" class="card card-logistics">
        <view class="card-head">
          <view class="head-icon">
            <q-icon name="vehicle" size="16" color="#2E7D32" />
          </view>
          <view class="head-text">
            <text class="head-title">物流信息</text>
            <text class="head-sub">{{ order.shipment.trackingNo }}</text>
          </view>
          <view class="head-more" @tap="copyTrackNo">
            <text>复制单号</text>
            <q-icon name="chevron-right" size="14" color="#8A8578" />
          </view>
        </view>
        <view class="ship-info">
          <view class="ship-row">
            <text class="ship-label">快递公司</text>
            <text class="ship-val">{{ order.shipment.expressCompany }}</text>
          </view>
          <view class="ship-row">
            <text class="ship-label">快递单号</text>
            <text class="ship-val mono">{{ order.shipment.trackingNo }}</text>
          </view>
          <view class="ship-row">
            <text class="ship-label">运单状态</text>
            <text class="ship-val">{{ order.shipment.status === 'Delivered' ? '已签收' : '已发货' }}</text>
          </view>
          <view class="ship-row">
            <text class="ship-label">发货时间</text>
            <text class="ship-val">{{ formatTime(order.shipment.shippedAt) }}</text>
          </view>
          <view v-if="order.shipment.deliveredAt" class="ship-row">
            <text class="ship-label">签收时间</text>
            <text class="ship-val">{{ formatTime(order.shipment.deliveredAt) }}</text>
          </view>
          <view v-if="order.shipment.remark" class="ship-row">
            <text class="ship-label">备注</text>
            <text class="ship-val">{{ order.shipment.remark }}</text>
          </view>
        </view>
      </view>

      <view class="card card-address">
        <view class="addr-head">
          <view class="addr-icon">
            <q-icon name="location" size="18" color="#2E7D32" />
          </view>
          <view class="addr-receiver">
            <text class="rc-name">{{ order.receiverName }}</text>
            <text class="rc-phone">{{ maskPhone(order.receiverPhone) }}</text>
          </view>
          <view class="addr-edit" v-if="false">
            <text>修改</text>
            <q-icon name="chevron-right" size="14" color="#8A8578" />
          </view>
        </view>
        <text class="addr-full">{{ order.receiverAddress }}</text>
        <view class="addr-note">
          <q-icon name="bookmark" size="12" color="#8A8578" />
          <text>送货前电话联系，放丰巢快递柜</text>
        </view>
      </view>

      <view class="card card-pharmacy">
        <view class="pharm-head">
          <view class="pharm-left">
            <view class="pharm-avatar">
              <text>草</text>
            </view>
            <view class="pharm-info">
              <view class="pharm-name-row">
                <text class="pharm-name">本草国医馆</text>
                <view class="gap-badge">
                  <text>GAP</text>
                </view>
              </view>
              <text class="pharm-row">
                <q-icon name="location" size="10" color="#8A8578" />
                <text>杭州总仓 · 西湖区文三路 288 号</text>
              </text>
            </view>
          </view>
          <view class="pharm-go" @tap="goShop">
            <text>去逛逛</text>
            <q-icon name="chevron-right" size="12" color="#2E7D32" />
          </view>
        </view>

        <view v-for="item in order.items" :key="item.id" class="prod-item">
          <view class="prod-img">
            <q-cached-image :src="resolveFileUrl(item.productImage) || placeholderImg" mode="aspectFill" />
            <view class="prod-tag" v-if="item.badge">{{ item.badge }}</view>
          </view>
          <view class="prod-info">
            <text class="prod-name">{{ item.productName }}</text>
            <text class="prod-spec">{{ item.specName }}</text>
            <view class="prod-tags">
              <view class="pt-gap">
                <q-icon name="certificate" size="10" color="#2E7D32" />
                <text>GAP道地直采</text>
              </view>
              <view class="pt-cold">
                <q-icon name="snowflake" size="10" color="#0288D1" />
                <text>本草低温速运</text>
              </view>
            </view>
            <view class="prod-bottom">
              <view class="prod-price-left">
                <text class="prod-price">¥{{ item.unitPrice.toFixed(2) }}</text>
                <text
                  v-if="item.originalUnitPrice > item.unitPrice"
                  class="prod-price-origin"
                >¥{{ item.originalUnitPrice.toFixed(2) }}</text>
              </view>
              <view class="prod-bottom-right">
                <text class="prod-qty">×{{ item.quantity }}</text>
                <view
                  v-if="order.status === 'Completed'"
                  class="review-btn"
                  :class="{ done: item.reviewed }"
                  @tap.stop="handleReview(item)"
                >
                  <text>{{ item.reviewed ? '已评价' : '评价' }}</text>
                </view>
              </view>
            </view>
            <text v-if="promotionText(item)" class="prod-promo">{{ promotionText(item) }}</text>
          </view>
        </view>

        <view class="prod-actions">
          <view class="pa-btn ghost" @tap="afterSale">
            <q-icon name="shield-error" size="12" color="#2E7D32" />
            <text>申请售后</text>
          </view>
          <view class="pa-btn ghost" @tap="traceOrigin">
            <q-icon name="tree-round-dot" size="12" color="#2E7D32" />
            <text>查看道地溯源</text>
          </view>
          <view class="pa-btn ghost" @tap="goConsult">
            <q-icon name="service" size="12" color="#2E7D32" />
            <text>用药咨询</text>
          </view>
        </view>
      </view>

      <view class="card card-amount">
        <view class="amt-row"><text>商品金额</text><text>¥{{ order.goodsAmount.toFixed(2) }}</text></view>
        <view class="amt-row"><text>运费</text><text>¥{{ order.freightAmount.toFixed(2) }}</text></view>
        <view v-if="order.promotionDiscountAmount > 0" class="amt-row discount"><text>促销优惠</text><text>-¥{{ order.promotionDiscountAmount.toFixed(2) }}</text></view>
        <view v-if="order.couponDiscountAmount > 0" class="amt-row discount">
          <text>券抵扣{{ order.couponName ? `（${order.couponName}）` : '' }}</text>
          <text>-¥{{ order.couponDiscountAmount.toFixed(2) }}</text>
        </view>
        <view class="amt-row total">
          <text>实付金额</text>
          <text class="payable">¥{{ actualPayAmount.toFixed(2) }}</text>
        </view>
      </view>

      <view class="card card-order-info">
        <view class="oi-row"><text class="oi-label">订单编号</text><text class="oi-val mono">{{ order.orderNo }}</text></view>
        <view class="oi-row"><text class="oi-label">下单时间</text><text class="oi-val">{{ formatTime(order.createdAt) }}</text></view>
        <view v-if="order.paidAt" class="oi-row"><text class="oi-label">支付时间</text><text class="oi-val">{{ formatTime(order.paidAt) }}</text></view>
        <view v-if="order.shippedAt" class="oi-row"><text class="oi-label">发货时间</text><text class="oi-val">{{ formatTime(order.shippedAt) }}</text></view>
        <view v-if="order.completedAt" class="oi-row"><text class="oi-label">完成时间</text><text class="oi-val">{{ formatTime(order.completedAt) }}</text></view>
        <view v-if="order.remark" class="oi-row"><text class="oi-label">订单备注</text><text class="oi-val">{{ order.remark }}</text></view>
      </view>

      <view class="bottom-space"></view>
    </scroll-view>

    <view v-if="order" class="footer-bar">
      <view class="fb-left" @tap="consult">
        <q-icon name="service" size="18" color="#40493D" />
        <text>药师</text>
      </view>
      <view class="fb-left" @tap="afterSale">
        <q-icon name="shield-error" size="18" color="#40493D" />
        <text>售后</text>
      </view>
      <view class="fb-btns">
        <button
          v-if="order.status === 'PendingPayment'"
          class="fb-btn solid"
          @tap="handlePay"
        >立即付款</button>
        <button
          v-if="order.status === 'Shipped'"
          class="fb-btn solid"
          @tap="handleConfirm"
        >确认收货</button>
        <button
          v-if="order.status === 'PendingPayment'"
          class="fb-btn ghost"
          @tap="handleCancel"
        >取消订单</button>
        <button
          v-if="order.status === 'Paid'"
          class="fb-btn ghost"
          @tap="goRefundApply"
        >申请仅退款</button>
        <button
          v-if="(order.status === 'Shipped' || order.status === 'Completed') && !hasActiveReturn"
          class="fb-btn ghost"
          @tap="goReturnApply"
        >退货退款</button>
        <button
          v-if="needReturnTracking"
          class="fb-btn solid"
          @tap="goReturnTracking"
        >填写寄回运单</button>
      </view>
    </view>
    <view v-else class="page-body loading">加载中…</view>
  </view>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { onLoad, onShow } from '@dcloudio/uni-app'
import { getOrderDetail, cancelOrder, confirmReceipt } from '@/api'
import { resolveFileUrl } from '@/utils/http'
import type { MallOrderDetail, MallOrderItem } from '@/api'

const order = ref<MallOrderDetail | null>(null)

/** 实付金额：已支付取实付，未支付/已取消取应付，保证与列表显示口径一致 */
const actualPayAmount = computed(() => {
  const current = order.value
  if (!current) return 0
  return current.paidAmount > 0 ? current.paidAmount : current.payableAmount
})
const orderNo = ref('')
const placeholderImg = '/static/images/hero-herbs.jpg'

interface AfterSaleView {
  kind: 'refund' | 'return'
  no: string
  amount: number
  reason: string
  description: string | null
  evidenceImages: string[]
  status: string
  statusText: string
  rejectReason: string | null
  expressCompany: string | null
  trackingNo: string | null
  createdAt: string
}

const latestReturn = computed(() => {
  const list = order.value?.returns || []
  return list.length ? list[list.length - 1] : null
})

const latestRefund = computed(() => {
  const list = order.value?.refunds || []
  return list.length ? list[list.length - 1] : null
})

function returnStatusText(status: string, hasTracking: boolean) {
  switch (status) {
    case 'Pending': return '待审核'
    case 'Approved': return hasTracking ? '待商家收货' : '待寄回'
    case 'Received': return '已收货'
    case 'Completed': return '已完成'
    case 'Rejected': return '已拒绝'
    default: return status
  }
}

function refundStatusText(status: string) {
  switch (status) {
    case 'Pending': return '处理中'
    case 'Success': return '已退款'
    case 'Failed': return '退款失败'
    default: return status
  }
}

const afterSaleView = computed<AfterSaleView | null>(() => {
  const returned = latestReturn.value
  if (returned) {
    return {
      kind: 'return',
      no: returned.returnNo,
      amount: returned.amount,
      reason: returned.reason,
      description: returned.description,
      evidenceImages: returned.evidenceImages || [],
      status: returned.status,
      statusText: returnStatusText(returned.status, !!returned.trackingNo),
      rejectReason: returned.rejectReason,
      expressCompany: returned.expressCompany,
      trackingNo: returned.trackingNo,
      createdAt: returned.createdAt
    }
  }

  const refund = latestRefund.value
  if (!refund) return null

  return {
    kind: 'refund',
    no: refund.refundNo,
    amount: refund.amount,
    reason: refund.reason,
    description: refund.description,
    evidenceImages: refund.evidenceImages || [],
    status: refund.status,
    statusText: refundStatusText(refund.status),
    rejectReason: null,
    expressCompany: null,
    trackingNo: null,
    createdAt: refund.createdAt
  }
})

const hasActiveReturn = computed(() => {
  const returned = latestReturn.value
  return !!returned && returned.status !== 'Rejected'
})

const needReturnTracking = computed(() => {
  const returned = latestReturn.value
  return !!returned && returned.status === 'Approved' && !returned.trackingNo
})

onLoad(options => {
  orderNo.value = (options?.orderNo as string) || ''
  if (!orderNo.value) {
    uni.showToast({ title: '缺少订单号', icon: 'none' })
  }
})

onShow(() => {
  if (orderNo.value) loadOrder()
})

async function loadOrder() {
  try {
    order.value = await getOrderDetail(orderNo.value)
  } catch (err) {
    console.error(err)
    uni.showToast({ title: '加载失败', icon: 'none' })
  }
}

function goBack() { uni.navigateBack() }
function consult() { uni.showToast({ title: '正在连接药师…', icon: 'none' }) }
function goConsult() { uni.showToast({ title: '正在连接药师…', icon: 'none' }) }
function goShop() { uni.switchTab({ url: '/pages/index/index' }) }

function handleReview(item: MallOrderItem) {
  if (item.reviewed) {
    uni.showToast({ title: '该商品已评价', icon: 'none' })
    return
  }
  uni.navigateTo({
    url: `/pages/review-create/index?orderNo=${encodeURIComponent(orderNo.value)}&itemId=${encodeURIComponent(item.id)}`
  })
}

function afterSale() {
  const current = order.value
  if (!current) return

  if (current.status === 'Paid') {
    goRefundApply()
    return
  }

  if (current.status === 'Shipped' || current.status === 'Completed') {
    if (hasActiveReturn.value) {
      uni.showToast({ title: '已有进行中的售后申请', icon: 'none' })
      return
    }
    goReturnApply()
    return
  }

  if (current.status === 'Returning') {
    if (needReturnTracking.value) {
      goReturnTracking()
      return
    }
    uni.showToast({ title: '售后处理中，请查看售后进度', icon: 'none' })
    return
  }

  if (current.status === 'Refunding') {
    uni.showToast({ title: '退款处理中，请耐心等待', icon: 'none' })
    return
  }

  if (current.status === 'Refunded' || current.status === 'Returned') {
    uni.showToast({ title: '售后已完成', icon: 'none' })
    return
  }

  uni.showToast({ title: '当前订单状态不可申请售后', icon: 'none' })
}

function goRefundApply() {
  uni.navigateTo({
    url: `/pages/after-sale/index?orderNo=${encodeURIComponent(orderNo.value)}&type=refund`
  })
}

function goReturnApply() {
  uni.navigateTo({
    url: `/pages/after-sale/index?orderNo=${encodeURIComponent(orderNo.value)}&type=return`
  })
}

function goReturnTracking() {
  const returned = latestReturn.value
  if (!returned) return
  uni.navigateTo({
    url: `/pages/after-sale/index?type=tracking&returnNo=${encodeURIComponent(returned.returnNo)}`
  })
}

function previewEvidence(index: number) {
  const list = afterSaleView.value?.evidenceImages || []
  if (!list.length) return
  uni.previewImage({
    urls: list.map(resolveFileUrl),
    current: resolveFileUrl(list[index])
  })
}
function traceOrigin() { uni.showToast({ title: '道地溯源功能开发中', icon: 'none' }) }
function copyTrackNo() {
  const no = order.value?.shipment?.trackingNo
  if (!no) return
  uni.setClipboardData({ data: no, success: () => uni.showToast({ title: '已复制', icon: 'success' }) })
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

function maskPhone(p: string) {
  if (!p) return ''
  return p.replace(/(\d{3})\d{4}(\d{4})/, '$1****$2')
}

function formatTime(t?: string | null) {
  if (!t) return ''
  return t.slice(0, 19).replace('T', ' ')
}

function statusText(s: string) {
  const m: Record<string, string> = {
    PendingPayment: '待付款',
    Paid: '待发货',
    Shipped: '待收货',
    Completed: '已完成',
    Cancelled: '已取消',
    Refunding: '退款中',
    Refunded: '已退款',
    Returning: '售后处理中',
    Returned: '售后完成'
  }
  return m[s] || s
}

function statusDesc(o: MallOrderDetail) {
  switch (o.status) {
    case 'PendingPayment': return '请尽快付款，超时订单将自动取消'
    case 'Paid': return '商家正在备货，预计 24 小时内发货'
    case 'Shipped':
      if (o.shipment) return `已发货 · ${o.shipment.expressCompany} ${o.shipment.trackingNo}`
      return '商品已发出，请注意查收'
    case 'Completed': return '订单已完成，期待您的再次光临'
    case 'Cancelled': return '订单已取消'
    case 'Refunding': return '退款申请处理中，请耐心等待'
    case 'Refunded': return `已退款 ¥${o.refundedAmount.toFixed(2)}`
    case 'Returning':
      if (needReturnTracking.value) return '退货申请已通过，请填写寄回运单'
      return '退货申请处理中，请查看售后进度'
    case 'Returned': return `退货完成，已退款 ¥${o.refundedAmount.toFixed(2)}`
    default: return ''
  }
}

async function handlePay() {
  uni.showToast({ title: '正在调起支付…', icon: 'none' })
}
async function handleCancel() {
  uni.showModal({
    title: '取消订单',
    content: '确定取消该订单？',
    success: async r => {
      if (!r.confirm) return
      try {
        await cancelOrder(orderNo.value)
        uni.showToast({ title: '已取消', icon: 'success' })
        order.value = await getOrderDetail(orderNo.value)
      } catch {
        uni.showToast({ title: '操作失败', icon: 'none' })
      }
    }
  })
}
async function handleConfirm() {
  uni.showModal({
    title: '确认收货',
    content: '请确认已收到商品并验收无误',
    success: async r => {
      if (!r.confirm) return
      try {
        await confirmReceipt(orderNo.value)
        uni.showToast({ title: '已确认收货', icon: 'success' })
        order.value = await getOrderDetail(orderNo.value)
      } catch {
        uni.showToast({ title: '操作失败', icon: 'none' })
      }
    }
  })
}
</script>

<style lang="scss" scoped>
$primary: #2E7D32;
$primary-dark: #1B5E20;
$price: #8B5000;
$on-surface: #1B1C1C;
$on-surface-var: #40493D;
$muted: #8A8578;
$card-bg: #ffffff;
$page-bg: #F5F7F5;
$shadow-card: 0 4rpx 20rpx -4rpx rgba(46, 125, 50, 0.08);

.page {
  min-height: 100vh;
  background: $page-bg;
  padding-bottom: 180rpx;
}

.header-right {
  display: flex; align-items: center; gap: 4rpx;
  padding: 8rpx 16rpx;
  background: #F3F8F1;
  border-radius: 100rpx;
  font-size: 22rpx; color: $on-surface-var;
}

.page-body {
  height: calc(100vh - 100rpx);
  padding: 24rpx;
  box-sizing: border-box;
}

.loading { display: flex; align-items: center; justify-content: center; color: $muted; font-size: 26rpx; }

/* ===== 状态横幅 ===== */
.status-banner {
  position: relative;
  padding: 36rpx 28rpx 32rpx;
  border-radius: 24rpx;
  margin-bottom: 20rpx;
  overflow: hidden;
  color: #ffffff;
  background: linear-gradient(135deg, $primary 0%, $primary-dark 100%);

  &.status-PendingPayment { background: linear-gradient(135deg, #FF9800, #F57C00); }
  &.status-Paid { background: linear-gradient(135deg, $primary, #43A047); }
  &.status-Completed { background: linear-gradient(135deg, #546E7A, #78909C); }
  &.status-Cancelled { background: linear-gradient(135deg, #9E9E9E, #BDBDBD); }
  &.status-Refunding, &.status-Refunded { background: linear-gradient(135deg, #E53935, #EF5350); }
  &.status-Returning, &.status-Returned { background: linear-gradient(135deg, #EF6C00, #FB8C00); }
}
.status-deco {
  position: absolute;
  width: 320rpx; height: 320rpx;
  right: -80rpx; top: -80rpx;
  border-radius: 50%;
  background: rgba(255, 255, 255, 0.08);
}
.status-main { position: relative; z-index: 2; }
.status-top { display: flex; align-items: center; gap: 16rpx; margin-bottom: 10rpx; }
.status-label {
  font-size: 38rpx; font-weight: 800;
  letter-spacing: -1rpx;
}
.ship-tag {
  display: flex; align-items: center; gap: 4rpx;
  padding: 6rpx 14rpx;
  background: rgba(255, 255, 255, 0.22);
  border-radius: 100rpx;
  font-size: 20rpx; font-weight: 600;
}
.status-sub {
  display: block;
  font-size: 26rpx; font-weight: 500;
  opacity: 0.92;
  margin-bottom: 14rpx;
}
.cloud-tag {
  display: inline-flex; align-items: center; gap: 8rpx;
  padding: 6rpx 14rpx;
  background: rgba(255, 255, 255, 0.15);
  border-radius: 8rpx;
  font-size: 20rpx;
}
.cloud-dot {
  width: 10rpx; height: 10rpx;
  border-radius: 50%;
  background: #4CAF50;
  box-shadow: 0 0 8rpx #4CAF50;
  animation: pulse 2s infinite;
}
@keyframes pulse {
  0%, 100% { opacity: 1; transform: scale(1); }
  50% { opacity: 0.5; transform: scale(1.2); }
}

/* ===== 通用卡片 ===== */
.card {
  background: $card-bg;
  border-radius: 24rpx;
  padding: 28rpx;
  margin-bottom: 20rpx;
  box-shadow: $shadow-card;
}

.card-head {
  display: flex; align-items: center; gap: 14rpx;
  padding-bottom: 20rpx;
  margin-bottom: 16rpx;
  border-bottom: 1rpx solid #F2F1EC;
}
.head-icon {
  width: 48rpx; height: 48rpx;
  background: #F3F8F1;
  border-radius: 14rpx;
  display: flex; align-items: center; justify-content: center;
}
.head-text { flex: 1; min-width: 0; }
.head-title { font-size: 28rpx; font-weight: 700; color: $on-surface; display: block; }
.head-sub { font-size: 22rpx; color: $muted; font-family: monospace; }
.head-more {
  display: flex; align-items: center; gap: 2rpx;
  font-size: 22rpx; color: $muted;
}

/* ===== 物流卡片：仅展示真实运单信息，无轨迹数据不伪造节点 ===== */
.ship-info { display: flex; flex-direction: column; gap: 14rpx; }
.ship-row { display: flex; align-items: baseline; gap: 20rpx; }
.ship-label {
  width: 130rpx;
  flex-shrink: 0;
  font-size: 24rpx;
  color: $muted;
}
.ship-val {
  font-size: 26rpx;
  color: $on-surface-var;
  word-break: break-all;
}
.mono { font-family: monospace; }

/* ===== 售后进度卡片 ===== */
.card-aftersale {
  border: 1rpx solid rgba(46, 125, 50, 0.12);
}
.as-status {
  padding: 6rpx 16rpx;
  border-radius: 100rpx;
  font-size: 22rpx;
  font-weight: 600;
  background: #FFF8E1;
  color: #B26A00;

  &.as-Approved { background: #E3F2FD; color: #1565C0; }
  &.as-Rejected { background: #F3F3F0; color: $muted; }
  &.as-Received, &.as-Completed, &.as-Success { background: #F3F8F1; color: $primary; }
  &.as-Failed { background: #FFEBEE; color: #C62828; }
}
.ship-val.price { color: $price; font-weight: 700; }
.ship-val.reject { color: #E53935; }
.as-images {
  display: flex;
  flex-wrap: wrap;
  gap: 12rpx;
  margin-top: 16rpx;

  image {
    width: 120rpx;
    height: 120rpx;
    border-radius: 10rpx;
  }
}
.as-action {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 4rpx;
  margin-top: 18rpx;
  padding: 16rpx 0;
  border-radius: 12rpx;
  background: #F3F8F1;
  border: 1rpx solid rgba(46, 125, 50, 0.18);

  text { font-size: 24rpx; font-weight: 600; color: $primary; }
}

/* ===== 地址卡片 ===== */
.addr-head {
  display: flex; align-items: center; gap: 14rpx;
  margin-bottom: 14rpx;
}
.addr-icon {
  width: 52rpx; height: 52rpx;
  background: #F3F8F1;
  border-radius: 14rpx;
  display: flex; align-items: center; justify-content: center;
}
.addr-receiver { flex: 1; display: flex; align-items: center; gap: 12rpx; }
.rc-name { font-size: 28rpx; font-weight: 700; color: $on-surface; }
.rc-phone { font-size: 26rpx; color: $on-surface-var; font-family: monospace; }
.addr-edit { display: flex; align-items: center; font-size: 22rpx; color: $muted; }
.addr-full {
  display: block;
  font-size: 26rpx; color: $on-surface-var;
  line-height: 1.5;
  padding-left: 66rpx;
}
.addr-note {
  display: flex; align-items: center; gap: 8rpx;
  margin-top: 16rpx;
  padding: 12rpx 16rpx;
  margin-left: 66rpx;
  background: #FFF8E1;
  border-radius: 10rpx;
  font-size: 22rpx; color: #B26A00;
}

/* ===== 药房 / 商品卡片 ===== */
.pharm-head {
  display: flex; align-items: center; justify-content: space-between;
  padding-bottom: 20rpx;
  margin-bottom: 12rpx;
  border-bottom: 1rpx solid #F2F1EC;
}
.pharm-left { display: flex; align-items: center; gap: 14rpx; flex: 1; min-width: 0; }
.pharm-avatar {
  width: 56rpx; height: 56rpx;
  background: linear-gradient(135deg, $primary, $primary-dark);
  border-radius: 16rpx;
  display: flex; align-items: center; justify-content: center;
  color: #ffffff; font-size: 28rpx; font-weight: 800;
}
.pharm-info { flex: 1; min-width: 0; }
.pharm-name-row { display: flex; align-items: center; gap: 10rpx; margin-bottom: 6rpx; }
.pharm-name { font-size: 28rpx; font-weight: 700; color: $on-surface; }
.gap-badge {
  padding: 2rpx 10rpx;
  background: $price;
  border-radius: 6rpx;
  font-size: 18rpx; color: #ffffff; font-weight: 700;
  letter-spacing: 1rpx;
}
.pharm-row {
  display: flex; align-items: center; gap: 4rpx;
  font-size: 22rpx; color: $muted;
}
.pharm-go {
  display: flex; align-items: center; gap: 2rpx;
  font-size: 22rpx; color: $primary; font-weight: 600;
}

.prod-item {
  display: flex; gap: 18rpx;
  padding: 20rpx 0;
  border-bottom: 1rpx solid #F2F1EC;

  &:last-of-type { border-bottom: none; }
}
.prod-img {
  position: relative;
  width: 180rpx; height: 180rpx;
  border-radius: 16rpx;
  overflow: hidden;
  flex-shrink: 0;
  background: #F3F3F0;
}
.prod-img image { width: 100%; height: 100%; }
.prod-tag {
  position: absolute; top: 0; left: 0;
  padding: 4rpx 10rpx;
  background: rgba(46, 125, 50, 0.92);
  border-radius: 0 0 12rpx 0;
  font-size: 18rpx; color: #ffffff; font-weight: 600;
}
.prod-info { flex: 1; display: flex; flex-direction: column; gap: 6rpx; min-width: 0; }
.prod-name {
  font-size: 28rpx; font-weight: 600; color: $on-surface;
  line-height: 1.35;
  display: -webkit-box;
  -webkit-box-orient: vertical;
  -webkit-line-clamp: 1;
  overflow: hidden;
}
.prod-spec { font-size: 22rpx; color: $muted; }
.prod-tags { display: flex; gap: 10rpx; flex-wrap: wrap; }
.pt-gap, .pt-cold {
  display: flex; align-items: center; gap: 4rpx;
  padding: 4rpx 10rpx;
  border-radius: 6rpx;
  font-size: 18rpx; font-weight: 500;
}
.pt-gap { background: #F3F8F1; color: $primary; }
.pt-cold { background: #E1F5FE; color: #0288D1; }
.prod-bottom { margin-top: auto; display: flex; align-items: baseline; justify-content: space-between; }
.prod-bottom-right { display: flex; align-items: center; gap: 14rpx; }
.prod-price { font-size: 28rpx; font-weight: 700; color: $price; }
.prod-price-left { display: flex; align-items: baseline; gap: 10rpx; min-width: 0; }
.prod-price-origin { font-size: 22rpx; color: #B0ACA2; text-decoration: line-through; }
.prod-promo { display: block; margin-top: 6rpx; font-size: 22rpx; color: $muted; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.prod-qty { font-size: 24rpx; color: $muted; }
.review-btn {
  padding: 8rpx 22rpx;
  border-radius: 100rpx;
  background: #F3F8F1;
  border: 1rpx solid rgba(46, 125, 50, 0.3);

  text { font-size: 20rpx; font-weight: 600; color: $primary; }

  &.done {
    background: #F3F3F0;
    border-color: #E0DDD6;

    text { color: $muted; }
  }
}

.prod-actions {
  display: flex; gap: 14rpx;
  margin-top: 20rpx;
}
.pa-btn {
  flex: 1;
  display: flex; align-items: center; justify-content: center; gap: 6rpx;
  padding: 16rpx 0;
  border-radius: 12rpx;
  font-size: 24rpx; font-weight: 500;

  &.ghost {
    background: #F3F8F1;
    color: $primary;
    border: 1rpx solid rgba(46, 125, 50, 0.18);
  }
}

/* ===== 金额卡 ===== */
.card-amount { padding: 24rpx 28rpx; }
.amt-row {
  display: flex; justify-content: space-between;
  padding: 10rpx 0;
  font-size: 26rpx; color: $on-surface-var;

  &.discount text:last-child { color: #E53935; }
  &.total {
    padding-top: 16rpx; margin-top: 8rpx;
    border-top: 1rpx solid #F2F1EC;
    font-size: 28rpx; font-weight: 600; color: $on-surface;
  }
}
.payable { font-size: 36rpx; font-weight: 800; color: $price; letter-spacing: -0.5rpx; }

/* ===== 订单信息 ===== */
.card-order-info { padding: 24rpx 28rpx; }
.oi-row {
  display: flex; justify-content: space-between; align-items: flex-start;
  gap: 20rpx;
  padding: 8rpx 0;
  font-size: 24rpx;
}
.oi-label { color: $muted; flex-shrink: 0; }
.oi-val { color: $on-surface; text-align: right; flex: 1; }
.mono { font-family: 'SF Mono', 'Menlo', monospace; }

/* ===== 底部操作栏 ===== */
.bottom-space { height: 40rpx; }

.footer-bar {
  position: fixed; bottom: 0; left: 0; right: 0; z-index: 50;
  background: #ffffff;
  padding: 16rpx 24rpx;
  padding-bottom: calc(16rpx + env(safe-area-inset-bottom));
  display: flex; align-items: center; gap: 16rpx;
  border-top: 1rpx solid #F0EFEB;
}
.fb-left {
  display: flex; flex-direction: column; align-items: center; gap: 2rpx;
  padding: 4rpx 12rpx;
  font-size: 18rpx; color: $on-surface-var;
}
.fb-btns { display: flex; gap: 12rpx; flex: 1; }
.fb-btn {
  flex: 1; height: 76rpx; line-height: 76rpx;
  text-align: center;
  border-radius: 100rpx;
  font-size: 26rpx; font-weight: 600;
  border: none;

  &.solid {
    background: linear-gradient(135deg, $primary 0%, $primary-dark 100%);
    color: #ffffff;
  }
  &.ghost {
    background: #ffffff;
    border: 2rpx solid $primary;
    color: $primary;
  }
}
</style>
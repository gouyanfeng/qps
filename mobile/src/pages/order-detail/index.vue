<template>
  <q-page refresh-enabled :refresher-triggered="refreshing" @refresh="onRefresh">
    <q-page-header-back title="订单详情" />

      <template v-if="order">
        <view class="page-body">
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
          <q-image
            v-for="(img, index) in afterSaleView.evidenceImages"
            :key="img + index"
            :src="img"
            thumb
            :width="120"
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
                <text class="pharm-name">{{ order.items[0]?.storeName || '本草国医馆' }}</text>
                <view class="gap-badge">
                  <text>GAP</text>
                </view>
              </view>
            </view>
          </view>
          <view class="pharm-go" @tap="goShop">
            <text>去逛逛</text>
            <q-icon name="chevron-right" size="12" color="#2E7D32" />
          </view>
        </view>

        <view v-for="item in order.items" :key="item.id" class="prod-item">
          <view class="prod-img">
            <q-image :src="item.productImage || placeholderImg" thumb :width="120" mode="aspectFill" />
            <view class="prod-tag" v-if="item.badge">{{ item.badge }}</view>
          </view>
          <view class="prod-info">
            <text class="prod-name">{{ item.productName }}</text>
            <text class="prod-spec">{{ item.specName }}</text>
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
        </view>
      </template>

      <template v-else>
        <view class="page-body loading">加载中…</view>
      </template>

      <template #footer>
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
      </template>
  </q-page>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { onLoad, onShow } from '@dcloudio/uni-app'
import { getOrderDetail, cancelOrder, confirmReceipt } from '@/api'
import { resolveFileUrl } from '@/utils/http'
import type { MallOrderDetail, MallOrderItem } from '@/api'

const order = ref<MallOrderDetail | null>(null)
const refreshing = ref(false)

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

async function onRefresh() {
  refreshing.value = true
  try {
    await loadOrder()
  } finally {
    refreshing.value = false
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
@import "./index.scss";
</style>
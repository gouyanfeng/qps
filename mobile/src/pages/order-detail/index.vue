<template>
  <view class="page">
    <view class="header">
      <view class="header-left" @tap="goBack">
        <q-icon name="arrow-left" size="22" color="#1B1C1C" />
      </view>
      <text class="header-title">订单详情</text>
      <view class="header-right" @tap="consult">
        <q-icon name="service" size="18" color="#40493D" />
        <text>药师咨询</text>
      </view>
    </view>

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
        <view class="track-line">
          <view class="track-dot"></view>
          <view class="track-bar"></view>
          <view class="track-dot empty"></view>
          <view class="track-bar short"></view>
          <view class="track-dot empty"></view>
          <view class="track-bar short"></view>
          <view class="track-dot empty"></view>
        </view>
        <view class="track-cards">
          <view class="track-card active">
            <text class="tc-time">{{ formatTime(order.shipment.shippedAt) }}</text>
            <text class="tc-text">已发货 · 即将运往中转站</text>
          </view>
          <view class="track-card">
            <text class="tc-text">运输中 · 已到达杭州转运中心</text>
          </view>
          <view class="track-card">
            <text class="tc-text">派送中 · 顺丰快递员正在配送</text>
          </view>
          <view class="track-card">
            <text class="tc-text">签收 · 请及时取件验收</text>
          </view>
        </view>
        <view class="courier-card" @tap="callCourier">
          <view class="courier-left">
            <text class="courier-name">{{ order.shipment.expressCompany || '顺丰速运' }}</text>
            <text class="courier-phone">{{ order.shipment.expressPhone || '95338' }}</text>
          </view>
          <view class="courier-call">
            <q-icon name="send" size="14" color="#ffffff" />
            <text>联系</text>
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
            <image :src="item.productImage || placeholderImg" mode="aspectFill" />
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
              <text class="prod-price">¥{{ item.unitPrice.toFixed(2) }}</text>
              <text class="prod-qty">×{{ item.quantity }}</text>
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
        <view v-if="order.promotionDiscountAmount > 0" class="amt-row discount"><text>优惠</text><text>-¥{{ order.promotionDiscountAmount.toFixed(2) }}</text></view>
        <view class="amt-row total">
          <text>实付金额</text>
          <text class="payable">¥{{ order.payableAmount.toFixed(2) }}</text>
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
          v-if="order.status === 'Shipped' || order.status === 'Completed'"
          class="fb-btn ghost"
          @tap="handleRefund"
        >申请退款</button>
      </view>
    </view>
    <view v-else class="page-body loading">加载中…</view>
  </view>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { onLoad } from '@dcloudio/uni-app'
import { getOrderDetail } from '@/api'
import type { MallOrderDetail } from '@/api'

const order = ref<MallOrderDetail | null>(null)
const orderNo = ref('')
const placeholderImg = 'https://a0ai.marscode.cn/api/ide/v1/text_to_image?prompt=Traditional%20Chinese%20medicine%20herbs%20product%20placeholder&image_size=square'

onLoad(async options => {
  orderNo.value = (options?.orderNo as string) || ''
  if (!orderNo.value) {
    uni.showToast({ title: '缺少订单号', icon: 'none' })
    return
  }
  try {
    order.value = await getOrderDetail(orderNo.value)
  } catch (err) {
    console.error(err)
    uni.showToast({ title: '加载失败', icon: 'none' })
  }
})

function goBack() { uni.navigateBack() }
function consult() { uni.showToast({ title: '正在连接药师…', icon: 'none' }) }
function goConsult() { uni.showToast({ title: '正在连接药师…', icon: 'none' }) }
function goShop() { uni.switchTab({ url: '/pages/index/index' }) }
function afterSale() { uni.showToast({ title: '售后功能开发中', icon: 'none' }) }
function traceOrigin() { uni.showToast({ title: '道地溯源功能开发中', icon: 'none' }) }
function copyTrackNo() {
  const no = order.value?.shipment?.trackingNo
  if (!no) return
  uni.setClipboardData({ data: no, success: () => uni.showToast({ title: '已复制', icon: 'success' }) })
}
function callCourier() {
  const phone = order.value?.shipment?.expressPhone || '95338'
  uni.makePhoneCall({ phoneNumber: phone })
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
    Refunded: '已退款'
  }
  return m[s] || s
}

function statusDesc(o: MallOrderDetail) {
  switch (o.status) {
    case 'PendingPayment': return '请尽快付款，超时订单将自动取消'
    case 'Paid': return '商家正在备货，预计 24 小时内发货'
    case 'Shipped':
      if (o.shipment) return `${o.shipment.expressCompany || '顺丰速运'} · 冷链特快`
      return '商品已发出，请注意查收'
    case 'Completed': return '订单已完成，期待您的再次光临'
    case 'Cancelled': return '订单已取消'
    case 'Refunding': return '退款申请处理中，请耐心等待'
    case 'Refunded': return `已退款 ¥${o.refundedAmount.toFixed(2)}`
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
        await fetch(`/api/mall/orders/${orderNo.value}/cancel`, { method: 'POST' })
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
        await fetch(`/api/mall/orders/${orderNo.value}/confirm-receipt`, { method: 'POST' })
        uni.showToast({ title: '已确认收货', icon: 'success' })
        order.value = await getOrderDetail(orderNo.value)
      } catch {
        uni.showToast({ title: '操作失败', icon: 'none' })
      }
    }
  })
}
function handleRefund() {
  uni.showToast({ title: '售后功能开发中', icon: 'none' })
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

.header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 20rpx 28rpx;
  padding-top: calc(20rpx + env(safe-area-inset-top));
  background: #ffffff;
  border-bottom: 1rpx solid #F0EFEB;
}
.header-left {
  width: 56rpx; height: 56rpx;
  display: flex; align-items: center; justify-content: center;
  border-radius: 50%;
}
.header-title {
  font-size: 32rpx; font-weight: 700; color: $on-surface;
  letter-spacing: -0.5rpx;
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

/* ===== 物流卡片 ===== */
.track-line {
  display: flex; align-items: center;
  padding: 10rpx 0 16rpx;
}
.track-dot {
  width: 16rpx; height: 16rpx; border-radius: 50%;
  background: $primary;
  flex-shrink: 0;
  position: relative;
  &.empty { background: #D7D3CA; }
}
.track-bar {
  flex: 1; height: 4rpx;
  background: linear-gradient(to right, $primary, #A5D6A7);
  margin: 0 8rpx;
  &.short { flex: 0.7; background: #E5E2E1; }
}

.track-cards { display: flex; flex-direction: column; gap: 10rpx; }
.track-card {
  padding: 14rpx 16rpx;
  border-radius: 12rpx;
  background: #F9F8F5;
  border-left: 4rpx solid transparent;

  &.active {
    background: #F3F8F1;
    border-left-color: $primary;
  }
}
.tc-time {
  display: block;
  font-size: 20rpx; color: $primary; font-weight: 600;
  margin-bottom: 4rpx;
}
.tc-text {
  font-size: 24rpx; color: $on-surface-var;
}

.courier-card {
  margin-top: 20rpx;
  padding: 20rpx;
  background: #FAFAF7;
  border-radius: 16rpx;
  display: flex; align-items: center; justify-content: space-between;
}
.courier-left { display: flex; flex-direction: column; gap: 4rpx; }
.courier-name { font-size: 26rpx; font-weight: 600; color: $on-surface; }
.courier-phone { font-size: 22rpx; color: $muted; font-family: monospace; }
.courier-call {
  display: flex; align-items: center; gap: 4rpx;
  padding: 10rpx 20rpx;
  background: $primary;
  border-radius: 100rpx;
  color: #ffffff; font-size: 22rpx; font-weight: 600;
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
.prod-price { font-size: 28rpx; font-weight: 700; color: $price; }
.prod-qty { font-size: 24rpx; color: $muted; }

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
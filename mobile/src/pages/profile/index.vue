﻿﻿<template>
  <view class="page">
    <!-- ============ 顶栏 ============ -->
    <q-page-header background="#ffffff">
      <view class="nav-brand">
        <q-icon name="location" size="26" color="#2E7D32" />
        <text class="nav-title">本草国医馆</text>
        <view class="nav-branch">
          <text class="nav-branch-text">北京总馆</text>
          <q-icon name="chevron-down" size="16" color="#6B6B6B" />
        </view>
      </view>
      <view class="nav-icons">
        <view class="nav-icon">
          <q-icon name="notification" size="26" color="#1B1C1C" />
          <view class="nav-dot"></view>
        </view>
        <q-icon name="qrcode" size="26" color="#1B1C1C" />
        <q-icon name="setting" size="26" color="#1B1C1C" />
      </view>
    </q-page-header>

    <scroll-view scroll-y class="page-body" enhanced>
      <!-- ============ Hero 会员卡片 ============ -->
      <view class="hero-card">
        <view class="hero-top">
          <view class="user-row" @click="openAccount">
            <view class="avatar-ring">
              <image class="avatar-img" :src="'/static/images/avatar-elder.jpg'" mode="aspectFill" />
              <view class="cert-badge">
                <q-icon name="check" size="14" color="#ffffff" />
              </view>
            </view>
            <view class="user-info">
              <view class="user-top">
                <text class="nickname">{{ profile?.nickname || '登录 / 注册' }}</text>
                <view class="member-pill">
                  <q-icon name="user-vip" size="16" color="#5A3E00" />
                  <text class="member-text">{{ profile?.levelName || '会员中心' }}</text>
                </view>
              </view>
              <view class="constitution-pill">
                <view class="constitution-dot"></view>
                <text class="constitution-text">气虚夹湿型 · 道地守候 368 天</text>
              </view>
            </view>
          </view>

          <view class="member-code">
            <q-icon name="qrcode" size="38" color="#ffffff" />
            <text class="member-code-text">会员码</text>
          </view>
        </view>

        <view class="growth-row">
          <text class="growth-label">成长值</text>
          <text class="growth-value">{{ profile?.availablePoints ?? 0 }}</text>
          <text class="growth-tip">可用积分</text>
          <view class="growth-more">
            <text>权益明细</text>
            <q-icon name="chevron-right" size="15" color="rgba(255,255,255,0.9)" />
          </view>
        </view>
        <view class="progress-bar">
          <view class="progress-fill"></view>
        </view>

        <!-- 4 列资产 -->
        <view class="asset-grid">
          <view class="asset-item">
            <view class="asset-value-row">
              <text class="asset-val">{{ couponsCount }}</text>
              <text class="asset-unit">张</text>
            </view>
            <text class="asset-label">专享优惠券</text>
          </view>
          <view class="asset-item">
            <text class="asset-val">{{ profile?.availablePoints ?? 0 }}</text>
            <text class="asset-label">道地积分</text>
          </view>
          <view class="asset-item">
            <text class="asset-val">¥{{ (profile?.totalSpent ?? 0).toFixed(2) }}</text>
            <text class="asset-label">累计消费</text>
          </view>
          <view class="asset-item">
            <view class="asset-value-row">
              <text class="asset-val">{{ profile?.totalOrders ?? 0 }}</text>
              <text class="asset-unit">份</text>
            </view>
            <text class="asset-label">健康档案</text>
          </view>
        </view>
      </view>

      <!-- ============ 我的订单 ============ -->
      <view class="order-card">
        <view class="card-header">
          <view class="card-title-wrap">
            <view class="card-bar"></view>
            <text class="card-title">我的订单</text>
          </view>
          <view class="card-more" @click="goOrder('all')">
            <text>全部订单</text>
            <q-icon name="chevron-right" size="16" color="#6B6B6B" />
          </view>
        </view>

        <view class="order-grid">
          <view class="order-item" @click="goOrder('unpaid')">
            <view class="order-icon">
              <q-icon name="order" size="16" color="#2E7D32" />
              <view v-if="orderBadge.pendingPayment" class="order-badge">{{ orderBadge.pendingPayment }}</view>
            </view>
            <text class="order-label">待付款</text>
          </view>
          <view class="order-item" @click="goOrder('ship')">
            <view class="order-icon">
              <q-icon name="task" size="16" color="#2E7D32" />
              <view v-if="orderBadge.paid" class="order-badge">{{ orderBadge.paid }}</view>
            </view>
            <text class="order-label">待发货</text>
          </view>
          <view class="order-item" @click="goOrder('receive')">
            <view class="order-icon">
              <q-icon name="send" size="16" color="#2E7D32" />
              <view v-if="orderBadge.shipped" class="order-badge">{{ orderBadge.shipped }}</view>
            </view>
            <text class="order-label">待收货</text>
          </view>
          <view class="order-item" @click="goOrder('done')">
            <view class="order-icon">
              <q-icon name="check-circle" size="16" color="#2E7D32" />
              <view v-if="orderBadge.completed" class="order-badge">{{ orderBadge.completed }}</view>
            </view>
            <text class="order-label">已完成</text>
          </view>
          <view class="order-item" @click="goOrder('refund')">
            <view class="order-icon">
              <q-icon name="refresh" size="16" color="#2E7D32" />
              <view v-if="orderBadge.returning" class="order-badge">{{ orderBadge.returning }}</view>
            </view>
            <text class="order-label">退款/售后</text>
          </view>
        </view>

      </view>

      <!-- ============ 节气滋补定制 ============ -->
      <view class="solar-card">
        <view class="solar-head">
          <view class="solar-chip">
            <q-icon name="tree-round-dot" size="18" color="#B26A00" />
            <text class="solar-chip-text">节气滋补 · 白露润肺</text>
          </view>
          <view class="match-chip">
            <view class="match-dot"></view>
            <text class="match-text">定制方匹配度 96%</text>
          </view>
        </view>

        <text class="solar-desc">
          白露秋凉生，宜润燥生津养脾胃。秋燥渐起，为您匹配
          <text class="solar-hl">浙贝母配长白山生晒参</text>。国医馆按方精配无硫炮制，温润肺胃，固护元气。
        </text>

        <view class="formula-card" @click="goService('prescription')">
          <image class="formula-img" :src="'/static/images/herb-ginseng-lily.jpg'" mode="aspectFill" />
          <view class="formula-info">
            <text class="formula-name">长白山鲜人参 &amp; 浙贝母润肺饮</text>
            <view class="formula-tags">
              <view class="formula-tag tag-green">无硫熏蒸</view>
              <view class="formula-tag tag-orange">全链路道地溯源</view>
            </view>
          </view>
          <view class="formula-btn">
            <text class="formula-btn-text">查看配方</text>
            <q-icon name="chevron-right" size="16" color="#ffffff" />
          </view>
        </view>

        <view class="solar-foot">
          <view class="solar-foot-left">
            <q-icon name="institution-checked" size="22" color="#2E7D32" />
            <text class="solar-foot-text">由主任中医师联合审核验方</text>
          </view>
          <view class="solar-foot-right" @click="goService('assess')">
            <text>重新评估体质</text>
            <q-icon name="chevron-right" size="14" color="#2E7D32" />
          </view>
        </view>
      </view>

      <!-- ============ 常用服务 4×2 ============ -->
      <view class="service-card">
        <view class="card-header">
          <view class="card-title-wrap">
            <view class="card-bar"></view>
            <text class="card-title">常用服务 · 国医关怀</text>
          </view>
          <view class="card-more" @click="goService('more')">
            <text>更多服务</text>
            <q-icon name="chevron-right" size="16" color="#6B6B6B" />
          </view>
        </view>

        <view class="service-grid">
          <view class="service-item" v-for="s in services" :key="s.key" @click="goService(s.key)">
            <view class="service-icon" :style="{ backgroundColor: s.bg }">
              <q-icon :name="s.icon" size="18" :color="s.color" />
              <view v-if="s.dot" class="service-dot"></view>
            </view>
            <text class="service-label">{{ s.label }}</text>
            <text class="service-sub" :class="{ 'sub-green': s.highlight }">{{ s.sub }}</text>
          </view>
        </view>
      </view>

      <!-- ============ 底部品质认证 ============ -->
      <view class="footer-card">
        <view class="footer-item">
          <q-icon name="tree-round-dot" size="26" color="#2E7D32" />
          <text class="footer-item-text">核心产地直采</text>
        </view>
        <view class="footer-divider"></view>
        <view class="footer-item">
          <q-icon name="tea" size="26" color="#2E7D32" />
          <text class="footer-item-text">中药炮制古训</text>
        </view>
        <view class="footer-divider"></view>
        <view class="footer-item">
          <q-icon name="task-checked" size="26" color="#2E7D32" />
          <text class="footer-item-text">国标检验入库</text>
        </view>
        <view class="footer-divider"></view>
        <view class="footer-item">
          <q-icon name="snowflake" size="26" color="#2E7D32" />
          <text class="footer-item-text">冷链恒温包邮</text>
        </view>
      </view>

      <view class="license">
        <text class="license-text">本草国医馆 · 严选中国地理标志道地药材</text>
        <text class="license-text license-sub">药品经营质量管理规范 GSP 认证 · 互联网中药房备案许可</text>
      </view>
    </scroll-view>
  </view>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { onShow } from '@dcloudio/uni-app'
import { getMemberProfile, getMemberOrderBadge, getMyCouponCount } from '@/api'
import type { MallMemberOrderBadge, MemberProfile } from '@/api'
import { UnauthorizedError } from '@/utils/http'

interface ServiceItem {
  key: string
  icon: string
  label: string
  sub: string
  bg: string
  color: string
  dot?: boolean
  highlight?: boolean
}

const services: ServiceItem[] = [
  { key: 'trace', icon: 'certificate', label: '一物一码', sub: '溯源真伪', bg: '#E4F1E5', color: '#2E7D32' },
  { key: 'archive', icon: 'task', label: '健康档案', sub: '体质测评', bg: '#E4F1E5', color: '#2E7D32' },
  { key: 'doctor', icon: 'user-search', label: '药师问诊', sub: '7×24h在线', bg: '#FFECC9', color: '#B26A00', dot: true, highlight: true },
  { key: 'recommend', icon: 'thumb-up-1', label: '专属推荐', sub: '体质宜忌', bg: '#E4F1E5', color: '#2E7D32' },
  { key: 'decoct', icon: 'tea', label: '代煎配寄', sub: '古法文武火', bg: '#EFEFEC', color: '#40493D' },
  { key: 'address', icon: 'location', label: '收货地址', sub: '配送管理', bg: '#EFEFEC', color: '#40493D' },
  { key: 'favorite', icon: 'bookmark', label: '常备收藏', sub: '名方药材', bg: '#EFEFEC', color: '#40493D' },
  { key: 'help', icon: 'help-circle', label: '帮助客服', sub: '服务指南', bg: '#EFEFEC', color: '#40493D' }
]

const profile = ref<MemberProfile | null>(null)
const orderBadge = ref<MallMemberOrderBadge>({
  pendingPayment: 0,
  paid: 0,
  shipped: 0,
  completed: 0,
  returning: 0
})
const couponsCount = ref(0)

onShow(async () => {
  try {
    const [member, memberOrderBadge, memberCouponCount] = await Promise.all([
      getMemberProfile(),
      getMemberOrderBadge(),
      getMyCouponCount()
    ])
    profile.value = member
    orderBadge.value = memberOrderBadge
    couponsCount.value = memberCouponCount
  } catch (error) {
    console.error('加载会员资料失败', error)
    if (error instanceof UnauthorizedError) return
    uni.showToast({ title: '会员信息加载失败', icon: 'none' })
  }
})

function openAccount() {
  if (!profile.value) uni.navigateTo({ url: '/pages/account/index' })
}

function goOrder(status: string) {
  uni.navigateTo({ url: `/pages/orders/index?status=${encodeURIComponent(status)}` })
}

function goService(key: string) {
  if (key === 'address') {
    uni.navigateTo({ url: '/pages/addresses/index' })
    return
  }
  uni.showToast({ title: `${key}功能暂未开放`, icon: 'none' })
}
</script>

<style lang="scss" scoped>
$theme-primary: #2E7D32;
$theme-primary-dark: #1B5E20;
$theme-accent: #FF9800;
$theme-bg: #FCF9F8;
$theme-gold: #F5C242;
$on-surface: #1B1C1C;
$on-surface-variant: #40493D;
$outline: #6B6B6B;

.page {
    display: flex;
  flex-direction: column;
  min-height: 100vh;
  background: var(--page-bg);
}

// ============ 顶栏内容 ============
.nav-brand {
  display: flex;
  align-items: center;
  gap: 8rpx;
}
.nav-title {
  font-size: 36rpx;
  font-weight: 800;
  color: $theme-primary;
  letter-spacing: 1rpx;
}
.nav-branch {
  display: flex;
  align-items: center;
  gap: 2rpx;
  margin-left: 8rpx;
  padding: 4rpx 10rpx;
  background: #F1F0EE;
  border-radius: 8rpx;
}
.nav-branch-text {
  font-size: 20rpx;
  color: $outline;
}
.nav-icons {
  display: flex;
  align-items: center;
  gap: 26rpx;
}
.nav-icon {
  position: relative;
  display: flex;
  align-items: center;
}
.nav-dot {
  position: absolute;
  top: -2rpx;
  right: 0;
  width: 14rpx;
  height: 14rpx;
  border-radius: 50%;
  background: #E53935;
}

.page-body {
  flex: 1;
  padding-bottom: 120rpx;
}

// ===== 会员卡 =====
.hero-card {
  position: relative;
  margin: 20rpx 24rpx 0;
  border-radius: 22rpx;
  overflow: hidden;
  background:
    radial-gradient(circle at 14rpx 14rpx, rgba(255, 255, 255, 0.06) 2rpx, transparent 3rpx) 0 0 / 46rpx 46rpx,
    linear-gradient(140deg, #17591E 0%, #206A26 55%, #2C7631 100%);
  box-shadow: 0 10rpx 30rpx -12rpx rgba(23, 89, 30, 0.55);
}
.hero-top {
  display: flex;
  align-items: center;
  gap: 20rpx;
  padding: 30rpx 26rpx 0;
}
.user-row {
  flex: 1;
  display: flex;
  align-items: center;
  gap: 20rpx;
  min-width: 0;
}
.avatar-ring {
  position: relative;
  width: 112rpx;
  height: 112rpx;
  flex-shrink: 0;
  border-radius: 50%;
  padding: 5rpx;
  background: linear-gradient(135deg, #FBE08A, #E9B84C);
  box-shadow: 0 6rpx 18rpx rgba(0, 0, 0, 0.3);
}
.avatar-img {
  width: 100%;
  height: 100%;
  border-radius: 50%;
  display: block;
}
.cert-badge {
  position: absolute;
  right: -6rpx;
  bottom: -6rpx;
  width: 34rpx;
  height: 34rpx;
  border-radius: 50%;
  background: #F7A81B;
  border: 3rpx solid rgba(255, 255, 255, 0.9);
  display: flex;
  align-items: center;
  justify-content: center;
}
.user-info {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 12rpx;
}
.user-top {
  display: flex;
  align-items: center;
  gap: 12rpx;
}
.nickname {
  font-size: 36rpx;
  font-weight: 800;
  color: #ffffff;
}
.member-pill {
  display: inline-flex;
  align-items: center;
  gap: 4rpx;
  padding: 4rpx 12rpx;
  background: linear-gradient(135deg, #F9CE5A, #F2B32E);
  border-radius: 100rpx;
}
.member-text {
  font-size: 17rpx;
  font-weight: 700;
  color: #5A3E00;
}
.constitution-pill {
  display: inline-flex;
  align-items: center;
  gap: 8rpx;
  align-self: flex-start;
  padding: 5rpx 14rpx;
  background: rgba(255, 255, 255, 0.12);
  border: 1rpx solid rgba(255, 255, 255, 0.22);
  border-radius: 100rpx;
}
.constitution-dot {
  width: 10rpx;
  height: 10rpx;
  border-radius: 50%;
  background: #DCE9C8;
}
.constitution-text {
  font-size: 18rpx;
  color: rgba(255, 255, 255, 0.92);
}
.member-code {
  width: 96rpx;
  flex-shrink: 0;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 6rpx;
  padding: 12rpx 0;
  background: rgba(255, 255, 255, 0.12);
  border: 1rpx solid rgba(255, 255, 255, 0.2);
  border-radius: 14rpx;
}
.member-code-text {
  font-size: 17rpx;
  color: rgba(255, 255, 255, 0.92);
}

.growth-row {
  display: flex;
  align-items: baseline;
  padding: 28rpx 26rpx 12rpx;
  gap: 6rpx;
}
.growth-label {
  font-size: 21rpx;
  color: rgba(255, 255, 255, 0.8);
}
.growth-value {
  font-size: 24rpx;
  font-weight: 700;
  color: #ffffff;
}
.growth-tip {
  font-size: 19rpx;
  color: rgba(255, 255, 255, 0.65);
}
.growth-more {
  margin-left: auto;
  display: flex;
  align-items: center;
  gap: 2rpx;
  font-size: 19rpx;
  color: rgba(255, 255, 255, 0.88);
}
.progress-bar {
  margin: 0 26rpx 26rpx;
  height: 10rpx;
  background: rgba(255, 255, 255, 0.18);
  border-radius: 100rpx;
  overflow: hidden;
}
.progress-fill {
  width: 57.6%;
  height: 100%;
  background: linear-gradient(90deg, #F3B93F, #FBE08A);
  border-radius: 100rpx;
}

.asset-grid {
  display: flex;
  align-items: stretch;
  padding: 24rpx 0 28rpx;
  background: rgba(0, 0, 0, 0.14);
  border-top: 1rpx solid rgba(255, 255, 255, 0.12);
}
.asset-item {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 6rpx;
}
.asset-item + .asset-item {
  border-left: 1rpx solid rgba(255, 255, 255, 0.16);
}
.asset-value-row {
  display: flex;
  align-items: baseline;
  gap: 4rpx;
}
.asset-val {
  font-size: 40rpx;
  font-weight: 700;
  color: #ffffff;
  line-height: 1.1;
}
.asset-unit {
  font-size: 20rpx;
  color: rgba(255, 255, 255, 0.85);
}
.asset-label {
  font-size: 18rpx;
  color: rgba(255, 255, 255, 0.8);
}

// ===== 通用卡片 =====
.order-card,
.service-card {
  margin: 20rpx 24rpx 0;
  padding: 26rpx 24rpx;
  background: #ffffff;
  border-radius: 20rpx;
  box-shadow: 0 4rpx 20rpx -8rpx rgba(46, 125, 50, 0.09);
}
.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding-bottom: 20rpx;
  border-bottom: 1rpx solid #F0EEEC;
}
.card-title-wrap {
  display: flex;
  align-items: center;
  gap: 10rpx;
}
.card-bar {
  width: 7rpx;
  height: 30rpx;
  border-radius: 4rpx;
  background: $theme-primary;
}
.card-title {
  font-size: 30rpx;
  font-weight: 800;
  color: $on-surface;
  letter-spacing: 1rpx;
}
.card-more {
  display: flex;
  align-items: center;
  gap: 2rpx;
  font-size: 21rpx;
  color: $outline;
}

// ===== 订单 =====
.order-grid {
  display: flex;
  padding: 26rpx 0 22rpx;
}
.order-item {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 12rpx;
}
.order-icon {
  position: relative;
  width: 72rpx;
  height: 72rpx;
  border-radius: 50%;
  background: #F4F3F1;
  display: flex;
  align-items: center;
  justify-content: center;
}
.order-badge {
  position: absolute;
  top: -6rpx;
  right: -6rpx;
  min-width: 30rpx;
  height: 30rpx;
  padding: 0 6rpx;
  border-radius: 100rpx;
  background: #D32F2F;
  color: #ffffff;
  font-size: 18rpx;
  line-height: 30rpx;
  text-align: center;
  border: 2rpx solid #ffffff;
}
.order-label {
  font-size: 21rpx;
  color: $on-surface-variant;
}

// ===== 节气滋补 =====
.solar-card {
  margin: 20rpx 24rpx 0;
  padding: 26rpx 24rpx;
  background: #FCF7EB;
  border: 2rpx solid #F1E5CC;
  border-radius: 20rpx;
}
.solar-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
}
.solar-chip {
  display: flex;
  align-items: center;
  gap: 6rpx;
  padding: 6rpx 14rpx;
  border: 2rpx solid #E8CB94;
  border-radius: 100rpx;
}
.solar-chip-text {
  font-size: 22rpx;
  font-weight: 700;
  color: #B26A00;
}
.match-chip {
  display: flex;
  align-items: center;
  gap: 6rpx;
  padding: 6rpx 14rpx;
  background: #E3EEDF;
  border-radius: 100rpx;
}
.match-dot {
  width: 10rpx;
  height: 10rpx;
  border-radius: 50%;
  background: $theme-primary;
}
.match-text {
  font-size: 20rpx;
  font-weight: 700;
  color: $theme-primary;
}
.solar-desc {
  display: block;
  margin-top: 18rpx;
  font-size: 23rpx;
  line-height: 1.75;
  color: #54503F;
}
.solar-hl {
  font-weight: 700;
  color: #2F2C22;
}
.formula-card {
  display: flex;
  align-items: center;
  gap: 16rpx;
  margin-top: 18rpx;
  padding: 18rpx;
  background: #ffffff;
  border: 1rpx solid #F0EADD;
  border-radius: 14rpx;
}
.formula-img {
  width: 116rpx;
  height: 116rpx;
  flex-shrink: 0;
  border-radius: 10rpx;
}
.formula-info {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 10rpx;
}
.formula-name {
  font-size: 25rpx;
  font-weight: 700;
  color: $on-surface;
  line-height: 1.35;
}
.formula-tags {
  display: flex;
  align-items: center;
  gap: 8rpx;
  flex-wrap: wrap;
}
.formula-tag {
  padding: 3rpx 10rpx;
  border-radius: 4rpx;
  font-size: 17rpx;
}
.tag-green {
  background: #E1F0DF;
  color: $theme-primary;
}
.tag-orange {
  background: #FAECD6;
  color: #B26A00;
}
.formula-btn {
  display: flex;
  align-items: center;
  gap: 2rpx;
  flex-shrink: 0;
  padding: 14rpx 16rpx;
  background: $theme-primary;
  border-radius: 100rpx;
}
.formula-btn-text {
  font-size: 21rpx;
  font-weight: 700;
  color: #ffffff;
}
.solar-foot {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-top: 18rpx;
  padding-top: 16rpx;
  border-top: 1rpx solid #EFE7D6;
}
.solar-foot-left {
  display: flex;
  align-items: center;
  gap: 8rpx;
}
.solar-foot-text {
  font-size: 19rpx;
  color: $outline;
}
.solar-foot-right {
  display: flex;
  align-items: center;
  gap: 2rpx;
  font-size: 20rpx;
  font-weight: 700;
  color: $theme-primary;
}

// ===== 常用服务 =====
.service-grid {
  display: flex;
  flex-wrap: wrap;
  padding-top: 22rpx;
}
.service-item {
  width: 25%;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8rpx;
  padding: 12rpx 0;
}
.service-icon {
  position: relative;
  width: 72rpx;
  height: 72rpx;
  border-radius: 18rpx;
  display: flex;
  align-items: center;
  justify-content: center;
}
.service-dot {
  position: absolute;
  top: 4rpx;
  right: 4rpx;
  width: 16rpx;
  height: 16rpx;
  border-radius: 50%;
  background: #2E7D32;
}
.service-label {
  font-size: 23rpx;
  font-weight: 700;
  color: $on-surface;
}
.service-sub {
  font-size: 18rpx;
  color: #8A8A8A;
}
.service-sub.sub-green {
  color: $theme-primary;
  font-weight: 700;
}

// ===== 底部认证 =====
.footer-card {
  display: flex;
  align-items: center;
  margin: 20rpx 24rpx 0;
  padding: 22rpx 6rpx;
  background: #F7F5F2;
  border: 1rpx solid #ECE7E1;
  border-radius: 16rpx;
}
.footer-item {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8rpx;
}
.footer-divider {
  width: 1rpx;
  height: 52rpx;
  background: #E3DED7;
}
.footer-item-text {
  font-size: 19rpx;
  font-weight: 600;
  color: $on-surface-variant;
}
.license {
  padding: 26rpx 40rpx 50rpx;
  text-align: center;
  display: flex;
  flex-direction: column;
  gap: 8rpx;
}
.license-text {
  font-size: 20rpx;
  color: #9A958F;
}
.license-sub {
  font-size: 19rpx;
  color: #ADA8A1;
}
</style>

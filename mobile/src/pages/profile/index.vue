﻿﻿﻿﻿<template>
  <q-page refresh-enabled :refresher-triggered="refreshing" @refresh="onRefresh">
    <!-- ============ 顶栏 ============ -->
    <q-page-header-back :show-back="false" title="个人中心" />

      <!-- ============ Hero 会员卡片 ============ -->
      <view class="hero-card">
        <view class="hero-top">
          <view class="user-row" @click="openAccount">
            <view class="avatar-ring">
              <q-image class="avatar-img" :src="'/static/images/avatar-elder.jpg'" mode="aspectFill" />
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
          <view class="asset-item" @tap="goCoupons">
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
          <q-image class="formula-img" :src="'/static/images/herb-ginseng-lily.jpg'" mode="aspectFill" />
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

      <!-- ============ 设置 ============ -->
      <view class="setting-card">
        <view class="setting-cell" @tap="openAccount">
          <view class="setting-icon">
            <q-icon name="edit" size="18" color="#40493D" />
          </view>
          <view class="setting-info">
            <text class="setting-label">编辑个人信息</text>
            <text class="setting-sub">头像、昵称与联系方式</text>
          </view>
          <q-icon name="chevron-right" size="15" color="#6B6B6B" />
        </view>
        <view class="setting-divider"></view>
        <view class="setting-cell" @tap="clearImageCache">
          <view class="setting-icon">
            <q-icon name="delete" size="18" color="#40493D" />
          </view>
          <view class="setting-info">
            <text class="setting-label">清除图片缓存</text>
            <text class="setting-sub">释放本地图片占用的存储空间</text>
          </view>
          <q-icon name="chevron-right" size="15" color="#6B6B6B" />
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
  </q-page>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { onShow } from '@dcloudio/uni-app'
import { getMemberProfile, getMemberOrderBadge, getMyCouponCount } from '@/api'
import type { MallMemberOrderBadge, MemberProfile } from '@/api'
import { UnauthorizedError } from '@/utils/http'
// #ifndef H5
import { clearLocalImageCache } from '@/utils/imageCache'
// #endif

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
const refreshing = ref(false)

async function loadProfile() {
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
}

onShow(loadProfile)

async function onRefresh() {
  refreshing.value = true
  try { await loadProfile() }
  finally { refreshing.value = false }
}

function openAccount() {
  if (!profile.value) uni.navigateTo({ url: '/pages/account/index' })
}

function goOrder(status: string) {
  uni.navigateTo({ url: `/pages/orders/index?status=${encodeURIComponent(status)}` })
}

function goCoupons() {
  uni.navigateTo({ url: '/pages/coupons/index' })
}

function goService(key: string) {
  if (key === 'address') {
    uni.navigateTo({ url: '/pages/addresses/index' })
    return
  }
  uni.showToast({ title: `${key}功能暂未开放`, icon: 'none' })
}

function clearImageCache() {
  // #ifdef H5
  uni.showToast({ title: 'H5 端缓存由浏览器自动管理', icon: 'none' })
  // #endif
  // #ifndef H5
  const count = clearLocalImageCache()
  uni.showToast({
    title: count > 0 ? `已清除 ${count} 张图片缓存` : '暂无图片缓存',
    icon: 'none'
  })
  // #endif
}
</script>

<style lang="scss" scoped>
@import "./index.scss";
</style>
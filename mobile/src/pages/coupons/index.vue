<template>
  <view class="page">
    <q-page-header-back title="优惠券中心" />

    <scroll-view scroll-y class="page-body">
      <!-- 券码兑换区 -->
      <view class="redeem-card">
        <view class="redeem-header">
          <view class="redeem-title-row">
            <q-icon name="coupon" size="20" color="#0D631B" />
            <text class="redeem-title">兑换优惠礼券</text>
          </view>
          <text class="redeem-rules" @tap="showRules">使用规则说明</text>
        </view>
        <view class="redeem-input-row">
          <input
            class="redeem-input"
            placeholder="请输入纸质券码或礼品卡兑换码..."
            placeholder-class="redeem-placeholder"
            v-model="couponCode"
            confirm-type="done"
            @confirm="redeemCode"
          />
          <view class="redeem-btn" @tap="redeemCode">
            <text class="redeem-btn-text">立即兑换</text>
          </view>
        </view>
      </view>

      <!-- Tab 切换 -->
      <view class="tab-bar">
        <view
          v-for="tab in tabs"
          :key="tab.value"
          class="tab-item"
          :class="{ active: activeTab === tab.value }"
          @tap="switchTab(tab.value)"
        >
          <text class="tab-label">{{ tab.label }}</text>
          <view v-if="tab.badge" class="tab-badge">
            <text class="tab-badge-text">{{ tab.badge }}</text>
          </view>
          <view v-if="tab.tag" class="tab-tag">
            <text class="tab-tag-text">{{ tab.tag }}</text>
          </view>
        </view>
      </view>

      <!-- 可使用 -->
      <view v-if="activeTab === 'available'" class="coupon-list">
        <view v-for="item in availableCoupons" :key="item.id" class="coupon-card">
          <view class="coupon-card-body">
            <!-- 左侧金额 -->
            <view class="coupon-left">
              <view class="coupon-amount-row">
                <text class="coupon-yen" :class="amountColorClass(item)">¥</text>
                <text class="coupon-value" :class="amountColorClass(item)">{{ formatAmount(item.discountAmount) }}</text>
              </view>
              <text class="coupon-threshold">{{ thresholdText(item) }}</text>
              <view class="coupon-type-tag" :class="typeTagClass(item)">
                <text class="coupon-type-text" :class="typeTagTextClass(item)">{{ couponTypeLabel(item) }}</text>
              </view>
            </view>
            <!-- 打孔线 -->
            <view class="coupon-perforation">
              <view class="perforation-hole-top"></view>
              <view class="perforation-line"></view>
              <view class="perforation-hole-bottom"></view>
            </view>
            <!-- 右侧信息 -->
            <view class="coupon-right">
              <view class="coupon-info-top">
                <text class="coupon-name">{{ item.couponName }}</text>
                <view class="coupon-scope-tags">
                  <text v-if="item.couponType === 'Shipping'" class="scope-tag scope-tag-neutral">全品类可用</text>
                  <text v-else class="scope-tag scope-tag-neutral">全场通用</text>
                </view>
              </view>
              <view class="coupon-info-bottom">
                <view class="coupon-expire-row">
                  <q-icon name="clock" size="14" color="#923357" />
                  <text class="coupon-expire">{{ formatExpire(item.expireAt) }}</text>
                </view>
                <view class="coupon-use-btn" @tap="goUse">
                  <text class="coupon-use-btn-text">去使用</text>
                </view>
              </view>
            </view>
          </view>
          <view class="coupon-detail-bar">
            <text class="coupon-detail-text">{{ scopeDescription(item) }}</text>
            <q-icon name="chevron-down" size="16" color="#707A6C" />
          </view>
        </view>

        <view v-if="!loading && !availableCoupons.length" class="empty-state">
          <q-icon name="coupon" size="48" color="#BDBDBD" />
          <text class="empty-text">暂无可使用的优惠券</text>
        </view>
      </view>

      <!-- 领券中心 -->
      <view v-if="activeTab === 'claim'" class="coupon-list">
        <!-- 领券推荐标题 -->
        <view class="claim-section-header">
          <view class="claim-title-row">
            <q-icon name="fire" size="20" color="#8B5000" />
            <text class="claim-section-title">领券推荐 · 本周特惠</text>
          </view>
          <text class="claim-update-hint">每日10:00更新</text>
        </view>

        <view v-for="item in claimCoupons" :key="item.id" class="claim-card">
          <view class="claim-card-left">
            <view class="claim-amount-box">
              <text class="claim-amount" :class="claimAmountClass(item)">¥{{ formatAmount(item.discountAmount) }}</text>
              <text class="claim-threshold">{{ claimThresholdText(item) }}</text>
            </view>
            <view class="claim-info">
              <view class="claim-name-row">
                <view v-if="item.remainCount < item.totalCount * 0.3" class="claim-hot-tag">
                  <text class="claim-hot-tag-text">限时抢</text>
                </view>
                <text class="claim-name">{{ item.name }}</text>
              </view>
              <text class="claim-desc">{{ claimScopeDesc(item) }}</text>
              <view class="claim-progress-row" v-if="item.totalCount > 0">
                <view class="claim-progress-bar">
                  <view class="claim-progress-fill" :style="{ width: claimProgress(item) + '%' }"></view>
                </view>
                <text class="claim-progress-text">已抢{{ claimProgress(item) }}%</text>
              </view>
            </view>
          </view>
          <view
            class="claim-btn"
            :class="{ claimed: claimedIds.has(item.id) }"
            @tap="handleClaim(item)"
          >
            <text class="claim-btn-text">{{ claimedIds.has(item.id) ? '已领取' : '立即领取' }}</text>
          </view>
        </view>

        <view v-if="!loading && !claimCoupons.length" class="empty-state">
          <q-icon name="coupon" size="48" color="#BDBDBD" />
          <text class="empty-text">暂无可领取的优惠券</text>
        </view>
      </view>

      <!-- 已用/失效 -->
      <view v-if="activeTab === 'expired'" class="coupon-list">
        <view v-for="item in expiredCoupons" :key="item.id" class="coupon-card coupon-card-disabled">
          <view class="coupon-card-body">
            <view class="coupon-left coupon-left-disabled">
              <view class="coupon-amount-row">
                <text class="coupon-yen coupon-yen-disabled">¥</text>
                <text class="coupon-value coupon-value-disabled">{{ formatAmount(item.discountAmount) }}</text>
              </view>
              <text class="coupon-threshold">{{ thresholdText(item) }}</text>
              <view class="coupon-type-tag coupon-type-tag-disabled">
                <text class="coupon-type-text coupon-type-text-disabled">{{ couponTypeLabel(item) }}</text>
              </view>
            </view>
            <view class="coupon-perforation">
              <view class="perforation-hole-top"></view>
              <view class="perforation-line"></view>
              <view class="perforation-hole-bottom"></view>
            </view>
            <view class="coupon-right">
              <view class="coupon-info-top">
                <text class="coupon-name coupon-name-disabled">{{ item.couponName }}</text>
              </view>
              <view class="coupon-info-bottom">
                <text class="coupon-expire coupon-expire-disabled">{{ expiredStatusText(item) }}</text>
              </view>
            </view>
          </view>
        </view>

        <view v-if="!loading && !expiredCoupons.length" class="empty-state">
          <q-icon name="coupon" size="48" color="#BDBDBD" />
          <text class="empty-text">暂无历史过期券纪录</text>
        </view>
      </view>

      <!-- 温馨提示 -->
      <view class="tips-card">
        <view class="tips-content-row">
          <q-icon name="shield" size="18" color="#0D631B" />
          <text class="tips-content">
            <text class="tips-bold">温馨提示：</text>每笔订单限用1张商品券，可与免运费券及道地积分合并使用；已使用优惠券的订单如退款，优惠券将在24小时内返还至账户。
          </text>
        </view>
        <view class="tips-actions">
          <view class="tips-action" @tap="showFaq">
            <q-icon name="help" size="16" color="#40493D" />
            <text class="tips-action-text">常见用券答疑</text>
          </view>
          <view class="tips-action tips-action-primary" @tap="consultAgent">
            <q-icon name="service" size="16" color="#0D631B" />
            <text class="tips-action-text tips-action-text-primary">咨询在线执业药师</text>
          </view>
        </view>
      </view>

      <view v-if="loading" class="loading-more">
        <text class="loading-text">加载中...</text>
      </view>
    </scroll-view>
  </view>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { onShow } from '@dcloudio/uni-app'
import { getMyCoupons, getAvailableCoupons, claimCoupon, getMyCouponCount } from '@/api'
import type { MemberCoupon, AvailableCoupon } from '@/api'
import { UnauthorizedError } from '@/utils/http'

const activeTab = ref('available')
const loading = ref(false)
const couponCode = ref('')
const couponCount = ref(0)

const availableCoupons = ref<MemberCoupon[]>([])
const claimCoupons = ref<AvailableCoupon[]>([])
const expiredCoupons = ref<MemberCoupon[]>([])
const claimedIds = ref<Set<string>>(new Set())

interface TabItem {
  label: string
  value: string
  badge?: number
  tag?: string
}

const tabs = ref<TabItem[]>([])

function updateTabs() {
  tabs.value = [
    { label: '可使用', value: 'available', badge: availableCoupons.value.length || undefined },
    { label: '领券中心', value: 'claim', tag: '抢好券' },
    { label: '已用/失效', value: 'expired', badge: expiredCoupons.value.length || undefined }
  ]
}

onShow(() => {
  fetchAll()
})

async function fetchAll() {
  loading.value = true
  try {
    const [available, availableList, usedList, expiredList] = await Promise.all([
      getMyCouponCount().catch(() => 0),
      getMyCoupons('claimed').catch(() => [] as MemberCoupon[]),
      getMyCoupons('used').catch(() => [] as MemberCoupon[]),
      getMyCoupons('expired').catch(() => [] as MemberCoupon[])
    ])
    couponCount.value = available
    availableCoupons.value = availableList
    expiredCoupons.value = [...usedList, ...expiredList]
    updateTabs()
  } catch (error) {
    if (error instanceof UnauthorizedError) return
    console.error('加载优惠券失败', error)
  } finally {
    loading.value = false
  }
}

async function fetchClaimCoupons() {
  loading.value = true
  try {
    const list = await getAvailableCoupons()
    claimCoupons.value = list.filter(c => c.isActive && c.remainCount > 0)
  } catch (error) {
    if (error instanceof UnauthorizedError) return
    console.error('加载可领券失败', error)
  } finally {
    loading.value = false
  }
}

function switchTab(value: string) {
  activeTab.value = value
  if (value === 'claim' && !claimCoupons.value.length) {
    fetchClaimCoupons()
  }
}

function formatAmount(amount: number): string {
  if (amount >= 1) return Math.floor(amount).toString()
  return amount.toFixed(1)
}

function thresholdText(item: MemberCoupon): string {
  if (item.thresholdAmount <= 0) return '无门槛立减'
  return `满${Math.floor(item.thresholdAmount)}元可用`
}

function couponTypeLabel(item: MemberCoupon): string {
  const typeMap: Record<string, string> = {
    FullReduction: '满减券',
    Cash: '现金券',
    Discount: '折扣券',
    Shipping: '物流权益',
    Service: '中药服务券'
  }
  return typeMap[item.couponType] || '优惠券'
}

function amountColorClass(item: MemberCoupon): string {
  if (item.couponType === 'Shipping') return 'amount-neutral'
  if (item.couponType === 'Cash' || item.couponType === 'Discount') return 'amount-secondary'
  return 'amount-primary'
}

function typeTagClass(item: MemberCoupon): string {
  if (item.couponType === 'Shipping') return 'type-tag-neutral'
  if (item.couponType === 'Cash' || item.couponType === 'Discount') return 'type-tag-secondary'
  return 'type-tag-primary'
}

function typeTagTextClass(item: MemberCoupon): string {
  if (item.couponType === 'Shipping') return 'type-tag-text-neutral'
  if (item.couponType === 'Cash' || item.couponType === 'Discount') return 'type-tag-text-secondary'
  return 'type-tag-text-primary'
}

function formatExpire(expireAt: string): string {
  if (!expireAt) return ''
  const d = new Date(expireAt)
  const now = new Date()
  const diffDays = Math.ceil((d.getTime() - now.getTime()) / (1000 * 60 * 60 * 24))
  const dateStr = `${d.getFullYear()}.${String(d.getMonth() + 1).padStart(2, '0')}.${String(d.getDate()).padStart(2, '0')}`
  if (diffDays > 0 && diffDays <= 7) return `${dateStr} 到期 (剩${diffDays}天)`
  return `${dateStr} 到期`
}

function scopeDescription(item: MemberCoupon): string {
  if (item.couponType === 'Shipping') return '避光恒温医药箱专送，保障道地草药与鲜药材活性'
  if (item.couponType === 'Service') return '适用于执业中药师古法文武火代煎、名方粉碎破壁定制服务'
  return `限${item.couponName}指定品类使用，可与积分叠加`
}

function expiredStatusText(item: MemberCoupon): string {
  if (item.status === 'used') {
    const usedAt = item.usedAt ? formatDate(item.usedAt) : ''
    return `已使用${usedAt ? ' · ' + usedAt : ''}`
  }
  return '已失效'
}

function formatDate(dateStr: string): string {
  const d = new Date(dateStr)
  return `${d.getFullYear()}.${String(d.getMonth() + 1).padStart(2, '0')}.${String(d.getDate()).padStart(2, '0')}`
}

function claimThresholdText(item: AvailableCoupon): string {
  if (item.thresholdAmount <= 0) return '无门槛'
  return `满${Math.floor(item.thresholdAmount)}可用`
}

function claimScopeDesc(item: AvailableCoupon): string {
  if (item.scopeType === 'all') return '全场道地原药材可用'
  return `限${item.scopeValue || '指定品类'}可用`
}

function claimProgress(item: AvailableCoupon): number {
  if (item.totalCount <= 0) return 0
  return Math.min(99, Math.round(((item.totalCount - item.remainCount) / item.totalCount) * 100))
}

function claimAmountClass(item: AvailableCoupon): string {
  if (item.couponType === 'Discount') return 'claim-amount-primary'
  return 'claim-amount-secondary'
}

async function handleClaim(item: AvailableCoupon) {
  if (claimedIds.value.has(item.id)) return
  try {
    uni.showLoading({ title: '领取中...' })
    await claimCoupon(item.id)
    uni.hideLoading()
    claimedIds.value = new Set([...claimedIds.value, item.id])
    uni.showToast({ title: '领取成功！', icon: 'success' })
  } catch (e: any) {
    uni.hideLoading()
    uni.showToast({ title: e?.message || '领取失败', icon: 'none' })
  }
}

function redeemCode() {
  if (!couponCode.value.trim()) {
    uni.showToast({ title: '请输入有效的兑换码或券号', icon: 'none' })
    return
  }
  uni.showToast({ title: '兑换功能开发中', icon: 'none' })
}

function goUse() {
  uni.switchTab({ url: '/pages/index/index' })
}

function showRules() {
  uni.showToast({ title: '使用规则说明开发中', icon: 'none' })
}

function showFaq() {
  uni.showToast({ title: '常见用券答疑开发中', icon: 'none' })
}

function consultAgent() {
  uni.showToast({ title: '在线执业药师咨询开发中', icon: 'none' })
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
}

.header-action {
  display: flex;
  align-items: center;
}

.header-action-text {
  font-size: 24rpx;
  font-weight: 600;
  color: #0D631B;
}

/* 券码兑换区 */
.redeem-card {
  margin: 16rpx 24rpx;
  padding: 28rpx;
  background: #ffffff;
  border-radius: 24rpx;
  box-shadow: 0 8rpx 40rpx -4rpx rgba(46, 125, 50, 0.06);
}

.redeem-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20rpx;
}

.redeem-title-row {
  display: flex;
  align-items: center;
  gap: 10rpx;
}

.redeem-title {
  font-size: 28rpx;
  font-weight: 600;
  color: #1B1C1C;
}

.redeem-rules {
  font-size: 22rpx;
  font-weight: 500;
  color: #40493D;
}

.redeem-input-row {
  display: flex;
  align-items: center;
  background: #F6F3F2;
  border-radius: 16rpx;
  padding: 12rpx;
  gap: 12rpx;
}

.redeem-input {
  flex: 1;
  font-size: 24rpx;
  color: #1B1C1C;
  background: transparent;
  padding: 0 16rpx;
  height: 64rpx;
}

.redeem-placeholder {
  color: #707A6C;
  font-size: 24rpx;
}

.redeem-btn {
  background: #0D631B;
  border-radius: 16rpx;
  padding: 16rpx 28rpx;
  flex-shrink: 0;
}

.redeem-btn-text {
  font-size: 24rpx;
  font-weight: 600;
  color: #ffffff;
}

/* Tab 切换 */
.tab-bar {
  display: flex;
  align-items: center;
  margin: 0 24rpx;
  padding: 8rpx;
  background: #F6F3F2;
  border-radius: 999rpx;
}

.tab-item {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6rpx;
  padding: 16rpx 0;
  border-radius: 999rpx;
  transition: all 0.25s;
}

.tab-item.active {
  background: #0D631B;
  box-shadow: 0 2rpx 8rpx rgba(13, 99, 27, 0.15);
}

.tab-label {
  font-size: 24rpx;
  font-weight: 600;
  color: #40493D;
}

.tab-item.active .tab-label {
  color: #ffffff;
}

.tab-badge {
  min-width: 28rpx;
  height: 28rpx;
  border-radius: 14rpx;
  background: #88D982;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 0 6rpx;
}

.tab-item.active .tab-badge {
  background: #88D982;
}

.tab-badge-text {
  font-size: 18rpx;
  font-weight: 600;
  color: #002204;
}

.tab-tag {
  padding: 2rpx 10rpx;
  border-radius: 999rpx;
  background: #8B5000;
}

.tab-item.active .tab-tag {
  background: #FFB870;
}

.tab-tag-text {
  font-size: 18rpx;
  font-weight: 600;
  color: #ffffff;
}

.tab-item.active .tab-tag-text {
  color: #2C1600;
}

/* 优惠券列表 */
.coupon-list {
  padding: 20rpx 24rpx;
  display: flex;
  flex-direction: column;
  gap: 20rpx;
}

/* 优惠券卡片 */
.coupon-card {
  background: #ffffff;
  border-radius: 24rpx;
  box-shadow: 0 8rpx 40rpx -4rpx rgba(46, 125, 50, 0.06);
  overflow: hidden;
}

.coupon-card-disabled {
  opacity: 0.65;
}

.coupon-card-body {
  display: flex;
  align-items: stretch;
  min-height: 220rpx;
}

/* 左侧金额区 */
.coupon-left {
  width: 200rpx;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  background: #F6F3F2;
  padding: 20rpx;
  text-align: center;
}

.coupon-left-disabled {
  background: #EAE7E7;
}

.coupon-amount-row {
  display: flex;
  align-items: baseline;
}

.coupon-yen {
  font-size: 28rpx;
  font-weight: 600;
}

.coupon-yen-disabled {
  color: #707A6C !important;
}

.coupon-value {
  font-size: 56rpx;
  font-weight: 600;
  line-height: 1;
}

.coupon-value-disabled {
  color: #707A6C !important;
}

.amount-primary { color: #0D631B; }
.amount-secondary { color: #8B5000; }
.amount-neutral { color: #1B1C1C; }

.coupon-threshold {
  font-size: 22rpx;
  font-weight: 500;
  color: #40493D;
  margin-top: 8rpx;
}

.coupon-type-tag {
  margin-top: 12rpx;
  padding: 4rpx 12rpx;
  border-radius: 999rpx;
  display: inline-flex;
}

.type-tag-primary { background: rgba(13, 99, 27, 0.1); }
.type-tag-secondary { background: rgba(139, 80, 0, 0.1); }
.type-tag-neutral { background: #E5E2E1; }
.type-tag-disabled { background: #E5E2E1; }

.coupon-type-text {
  font-size: 20rpx;
  font-weight: 500;
}

.type-tag-text-primary { color: #0D631B; }
.type-tag-text-secondary { color: #8B5000; }
.type-tag-text-neutral { color: #40493D; }
.type-tag-text-disabled { color: #707A6C; }

/* 打孔线 */
.coupon-perforation {
  width: 24rpx;
  position: relative;
  display: flex;
  flex-direction: column;
  align-items: center;
  background: #ffffff;
}

.perforation-hole-top,
.perforation-hole-bottom {
  width: 24rpx;
  height: 24rpx;
  border-radius: 12rpx;
  background: var(--page-bg);
  position: absolute;
}

.perforation-hole-top { top: -12rpx; }
.perforation-hole-bottom { bottom: -12rpx; }

.perforation-line {
  flex: 1;
  border-right: 2rpx dashed #BFCABA;
  margin: 16rpx 0;
}

/* 右侧信息区 */
.coupon-right {
  flex: 1;
  padding: 24rpx;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
}

.coupon-info-top {
  display: flex;
  flex-direction: column;
  gap: 12rpx;
}

.coupon-name {
  font-size: 28rpx;
  font-weight: 600;
  color: #1B1C1C;
  line-height: 1.3;
}

.coupon-name-disabled {
  color: #707A6C;
}

.coupon-scope-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 8rpx;
}

.scope-tag {
  font-size: 20rpx;
  font-weight: 500;
  padding: 4rpx 12rpx;
  border-radius: 8rpx;
}

.scope-tag-neutral {
  background: #EAE7E7;
  color: #40493D;
}

.scope-tag-accent {
  background: #FFDCBE;
  color: #653900;
}

.coupon-info-bottom {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-top: 16rpx;
  padding-top: 8rpx;
}

.coupon-expire-row {
  display: flex;
  align-items: center;
  gap: 6rpx;
}

.coupon-expire {
  font-size: 22rpx;
  font-weight: 500;
  color: #923357;
}

.coupon-expire-disabled {
  color: #707A6C;
}

.coupon-use-btn {
  background: #0D631B;
  border-radius: 999rpx;
  padding: 12rpx 28rpx;
  box-shadow: 0 2rpx 8rpx rgba(13, 99, 27, 0.15);
}

.coupon-use-btn-text {
  font-size: 24rpx;
  font-weight: 600;
  color: #ffffff;
}

/* 详情展开栏 */
.coupon-detail-bar {
  background: rgba(246, 243, 242, 0.6);
  padding: 12rpx 24rpx;
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.coupon-detail-text {
  font-size: 22rpx;
  color: #40493D;
  flex: 1;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  padding-right: 16rpx;
}

/* 领券推荐区 */
.claim-section-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 16rpx;
}

.claim-title-row {
  display: flex;
  align-items: center;
  gap: 10rpx;
}

.claim-section-title {
  font-size: 28rpx;
  font-weight: 600;
  color: #1B1C1C;
}

.claim-update-hint {
  font-size: 22rpx;
  font-weight: 500;
  color: #40493D;
}

/* 领券卡片 */
.claim-card {
  background: #ffffff;
  border-radius: 24rpx;
  padding: 24rpx;
  box-shadow: 0 8rpx 40rpx -4rpx rgba(46, 125, 50, 0.06);
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 24rpx;
}

.claim-card-left {
  display: flex;
  align-items: center;
  gap: 24rpx;
  min-width: 0;
  flex: 1;
}

.claim-amount-box {
  width: 120rpx;
  height: 120rpx;
  border-radius: 16rpx;
  background: #F6F3F2;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.claim-amount {
  font-size: 32rpx;
  font-weight: 600;
  line-height: 1;
}

.claim-amount-secondary { color: #8B5000; }
.claim-amount-primary { color: #0D631B; }

.claim-threshold {
  font-size: 20rpx;
  font-weight: 500;
  color: #40493D;
  margin-top: 8rpx;
}

.claim-info {
  min-width: 0;
  flex: 1;
}

.claim-name-row {
  display: flex;
  align-items: center;
  gap: 8rpx;
}

.claim-hot-tag {
  background: rgba(139, 80, 0, 0.1);
  padding: 2rpx 8rpx;
  border-radius: 8rpx;
  flex-shrink: 0;
}

.claim-hot-tag-text {
  font-size: 20rpx;
  font-weight: 500;
  color: #8B5000;
}

.claim-name {
  font-size: 26rpx;
  font-weight: 600;
  color: #1B1C1C;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.claim-desc {
  font-size: 22rpx;
  color: #40493D;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  margin-top: 6rpx;
}

.claim-progress-row {
  display: flex;
  align-items: center;
  gap: 12rpx;
  margin-top: 12rpx;
}

.claim-progress-bar {
  width: 160rpx;
  height: 12rpx;
  background: #EAE7E7;
  border-radius: 999rpx;
  overflow: hidden;
}

.claim-progress-fill {
  height: 100%;
  background: #8B5000;
  border-radius: 999rpx;
}

.claim-progress-text {
  font-size: 20rpx;
  font-weight: 500;
  color: #8B5000;
}

.claim-btn {
  flex-shrink: 0;
  background: #8B5000;
  border-radius: 999rpx;
  padding: 16rpx 24rpx;
  box-shadow: 0 2rpx 8rpx rgba(139, 80, 0, 0.15);
}

.claim-btn.claimed {
  background: #E5E2E1;
  box-shadow: none;
}

.claim-btn-text {
  font-size: 24rpx;
  font-weight: 600;
  color: #ffffff;
}

.claim-btn.claimed .claim-btn-text {
  color: #707A6C;
}

/* 温馨提示 */
.tips-card {
  margin: 16rpx 24rpx 24rpx;
  padding: 24rpx;
  border-radius: 24rpx;
  background: #F6F3F2;
}

.tips-content-row {
  display: flex;
  align-items: flex-start;
  gap: 10rpx;
}

.tips-content {
  font-size: 22rpx;
  color: #40493D;
  line-height: 1.6;
}

.tips-bold {
  font-weight: 600;
  color: #1B1C1C;
}

.tips-actions {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-top: 16rpx;
  padding-top: 12rpx;
}

.tips-action {
  display: flex;
  align-items: center;
  gap: 6rpx;
}

.tips-action-text {
  font-size: 24rpx;
  font-weight: 600;
  color: #40493D;
}

.tips-action-primary {
  color: #0D631B;
}

.tips-action-text-primary {
  color: #0D631B;
}

/* 空状态 */
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

/* 加载 */
.loading-more {
  padding: 24rpx;
  text-align: center;
}

.loading-text {
  font-size: 24rpx;
  color: #707A6C;
}
</style>
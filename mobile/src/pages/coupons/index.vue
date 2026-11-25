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
                <text v-if="isPercentCoupon(item.couponType)" class="coupon-value" :class="amountColorClass(item)">
                  {{ couponDiscountRateText(item.discountAmount) }}
                </text>
                <template v-else>
                  <text class="coupon-yen" :class="amountColorClass(item)">¥</text>
                  <text class="coupon-value" :class="amountColorClass(item)">{{ formatAmount(item.discountAmount) }}</text>
                </template>
              </view>
              <text class="coupon-threshold">{{ thresholdText(item) }}</text>
              <view class="coupon-type-tag" :class="typeTagClass(item)">
                <text class="coupon-type-text" :class="typeTagTextClass(item)">{{ couponTypeLabel(item.couponType) }}</text>
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
                  <text class="scope-tag scope-tag-neutral">全场通用</text>
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
              <text class="claim-amount" :class="claimAmountClass(item)">
                {{ isPercentCoupon(item.couponType) ? couponDiscountRateText(item.discountAmount) : `¥${formatAmount(item.discountAmount)}` }}
              </text>
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
                <text v-if="isPercentCoupon(item.couponType)" class="coupon-value coupon-value-disabled">
                  {{ couponDiscountRateText(item.discountAmount) }}
                </text>
                <template v-else>
                  <text class="coupon-yen coupon-yen-disabled">¥</text>
                  <text class="coupon-value coupon-value-disabled">{{ formatAmount(item.discountAmount) }}</text>
                </template>
              </view>
              <text class="coupon-threshold">{{ thresholdText(item) }}</text>
              <view class="coupon-type-tag coupon-type-tag-disabled">
                <text class="coupon-type-text coupon-type-text-disabled">{{ couponTypeLabel(item.couponType) }}</text>
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
import { getMyCoupons, getAvailableCoupons, claimCoupon } from '@/api'
import type { MemberCoupon, AvailableCoupon } from '@/api'
import { UnauthorizedError } from '@/utils/http'
import { couponDiscountRateText, couponTypeLabel, isPercentCoupon } from '@/utils/coupon'

const activeTab = ref('available')
const loading = ref(false)
const couponCode = ref('')

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
  fetchActiveTab()
})

// 每个 tab 独立请求序号，快速切换时只接受最后一次响应，避免旧响应覆盖新数据
const requestSeq = { available: 0, claim: 0, expired: 0 }
let pendingCount = 0

function beginLoading() {
  pendingCount += 1
  loading.value = true
}

function endLoading() {
  pendingCount = Math.max(0, pendingCount - 1)
  if (pendingCount === 0) loading.value = false
}

async function fetchAvailable() {
  const seq = (requestSeq.available += 1)
  beginLoading()
  try {
    const list = await getMyCoupons('claimed')
    if (seq !== requestSeq.available) return
    availableCoupons.value = list
    updateTabs()
  } catch (error) {
    if (error instanceof UnauthorizedError) return
    console.error('加载可使用优惠券失败', error)
  } finally {
    endLoading()
  }
}

async function fetchExpired() {
  const seq = (requestSeq.expired += 1)
  beginLoading()
  try {
    const [usedList, expiredList] = await Promise.all([
      getMyCoupons('used').catch(() => [] as MemberCoupon[]),
      getMyCoupons('expired').catch(() => [] as MemberCoupon[])
    ])
    if (seq !== requestSeq.expired) return
    expiredCoupons.value = [...usedList, ...expiredList]
    updateTabs()
  } catch (error) {
    if (error instanceof UnauthorizedError) return
    console.error('加载已用/失效优惠券失败', error)
  } finally {
    endLoading()
  }
}

async function fetchClaimCoupons() {
  const seq = (requestSeq.claim += 1)
  beginLoading()
  try {
    const list = await getAvailableCoupons()
    if (seq !== requestSeq.claim) return
    claimCoupons.value = list.filter(c => c.isActive && c.remainCount > 0)
  } catch (error) {
    if (error instanceof UnauthorizedError) return
    console.error('加载可领券失败', error)
  } finally {
    endLoading()
  }
}

function fetchActiveTab() {
  if (activeTab.value === 'available') fetchAvailable()
  else if (activeTab.value === 'claim') fetchClaimCoupons()
  else fetchExpired()
}

function switchTab(value: string) {
  activeTab.value = value
  fetchActiveTab()
}

function formatAmount(amount: number): string {
  if (amount >= 1) return Math.floor(amount).toString()
  return amount.toFixed(1)
}

function thresholdText(item: MemberCoupon): string {
  if (item.thresholdAmount <= 0) return '无门槛立减'
  return `满${Math.floor(item.thresholdAmount)}元可用`
}

function amountColorClass(item: MemberCoupon): string {
  if (isPercentCoupon(item.couponType)) return 'amount-secondary'
  return 'amount-primary'
}

function typeTagClass(item: MemberCoupon): string {
  if (isPercentCoupon(item.couponType)) return 'type-tag-secondary'
  return 'type-tag-primary'
}

function typeTagTextClass(item: MemberCoupon): string {
  if (isPercentCoupon(item.couponType)) return 'type-tag-text-secondary'
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
  if (isPercentCoupon(item.couponType)) return 'claim-amount-primary'
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
    // 领券成功后同步刷新「可使用」与「领券中心」，无需退出页面
    await Promise.all([fetchAvailable(), fetchClaimCoupons()])
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
@import "./index.scss";
</style>
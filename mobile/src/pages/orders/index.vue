<template>
  <view class="page">
    <q-page-header-back title="我的订单" subtitle="MY ORDERS" />
    <scroll-view scroll-y class="page-body">
      <view v-for="order in orders" :key="order.orderNo" class="order-card">
        <view class="order-heading">
          <text class="order-no">订单号 {{ order.orderNo }}</text>
          <text class="order-status">{{ orderStatusText(order.status) }}</text>
        </view>
        <text class="order-time">{{ order.createdAt.slice(0, 10) }}</text>
        <view class="order-summary">
          <text>{{ order.receiverName }}</text>
          <text>¥{{ order.payableAmount.toFixed(2) }}</text>
        </view>
      </view>
      <view v-if="!loading && !orders.length" class="empty-state">暂无相关订单</view>
    </scroll-view>
  </view>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { onLoad, onShow } from '@dcloudio/uni-app'
import { getMyOrders, isLoggedIn } from '@/api'
import type { MallOrder } from '@/api'

const orders = ref<MallOrder[]>([])
const loading = ref(false)
const status = ref('')

onLoad(options => {
  const filter = options?.status || ''
  status.value = ({
    unpaid: 'PendingPayment',
    ship: 'Paid',
    receive: 'Shipped',
    done: 'Completed'
  } as Record<string, string>)[filter] || ''
})

onShow(async () => {
  if (!isLoggedIn()) {
    uni.navigateTo({ url: '/pages/account/index' })
    return
  }
  loading.value = true
  try {
    const response = await getMyOrders(status.value || undefined)
    orders.value = response.list ?? response.items ?? []
  } catch (error) {
    console.error('加载订单失败', error)
    uni.showToast({ title: '订单加载失败', icon: 'none' })
  } finally {
    loading.value = false
  }
})

function orderStatusText(value: string) {
  const labels: Record<string, string> = {
    PendingPayment: '待付款',
    Paid: '待发货',
    Shipped: '待收货',
    Completed: '已完成',
    Cancelled: '已取消',
    Refunding: '退款中',
    Refunded: '已退款'
  }
  return labels[value] || value
}
</script>

<style lang="scss" scoped>
.page {
  min-height: 100vh;
  background: var(--page-bg);
}

.page-body {
  height: calc(100vh - var(--page-top-offset));
  padding: 24rpx;
  box-sizing: border-box;
}

.order-card {
  margin-bottom: 18rpx;
  padding: 24rpx;
  background: #ffffff;
  border-radius: 18rpx;
}

.order-heading,
.order-summary {
  display: flex;
  justify-content: space-between;
  gap: 16rpx;
}

.order-no {
  color: #40493D;
  font-size: 22rpx;
}

.order-status {
  color: #2E7D32;
  font-size: 23rpx;
  font-weight: 700;
}

.order-time {
  display: block;
  margin-top: 12rpx;
  color: #999999;
  font-size: 20rpx;
}

.order-summary {
  margin-top: 22rpx;
  color: #1B1C1C;
  font-size: 25rpx;
  font-weight: 600;
}

.empty-state {
  padding: 80rpx 0;
  color: #8A8A8A;
  text-align: center;
  font-size: 24rpx;
}
</style>

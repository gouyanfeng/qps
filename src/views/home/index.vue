<template>
  <div class="dashboard">
    <div class="dashboard-header">
      <div class="header-title">
        <h1>数据概览</h1>
      </div>
      <div class="time-filter">
        <el-select v-model="timeRange" @change="loadData" class="time-select">
          <el-option label="近7天" value="7days" />
          <el-option label="近30天" value="30days" />
          <el-option label="近90天" value="90days" />
        </el-select>
      </div>
    </div>

    <div class="data-section overview-section">
      <div class="stats-grid overview-stats">
        <el-card class="stat-card">
          <div class="stat-content">
            <div class="stat-icon blue">
              <component :is="icons.ShoppingCart" />
            </div>
            <div class="stat-info">
              <p class="stat-label">订单总数</p>
              <p class="stat-value">{{ overviewData.summary?.totalOrders || 0 }}</p>
            </div>
          </div>
        </el-card>

        <el-card class="stat-card">
          <div class="stat-content">
            <div class="stat-icon green">
              <component :is="icons.Wallet" />
            </div>
            <div class="stat-info">
              <p class="stat-label">总收入</p>
              <p class="stat-value">¥{{ overviewData.summary?.totalRevenue || 0 }}</p>
            </div>
          </div>
        </el-card>

        <el-card class="stat-card">
          <div class="stat-content">
            <div class="stat-icon cyan">
              <component :is="icons.CirclePlus" />
            </div>
            <div class="stat-info">
              <p class="stat-label">完成订单</p>
              <p class="stat-value">{{ overviewData.summary?.completedOrders || 0 }}</p>
            </div>
          </div>
        </el-card>

        <el-card class="stat-card">
          <div class="stat-content">
            <div class="stat-icon pink">
              <component :is="icons.ArrowUp" />
            </div>
            <div class="stat-info">
              <p class="stat-label">平均客单价</p>
              <p class="stat-value">¥{{ overviewData.summary?.averageOrderValue || 0 }}</p>
            </div>
          </div>
        </el-card>

        <el-card class="stat-card">
          <div class="stat-content">
            <div class="stat-icon orange">
              <component :is="icons.Clock" />
            </div>
            <div class="stat-info">
              <p class="stat-label">上期收入</p>
              <p class="stat-value">¥{{ overviewData.summary?.previousRevenue || 0 }}</p>
            </div>
          </div>
        </el-card>

        <el-card class="stat-card">
          <div class="stat-content">
            <div class="stat-icon purple">
              <component :is="icons.Refresh" />
            </div>
            <div class="stat-info">
              <p class="stat-label">收入增长率</p>
              <p class="stat-value" :class="growthClass">{{ overviewData.summary?.revenueGrowthRate || 0 }}%</p>
            </div>
          </div>
        </el-card>
      </div>

      <div class="charts-grid overview-charts">
        <el-card class="chart-card">
          <template #header>
            <span class="chart-title">订单趋势</span>
          </template>
          <div id="order-trend-chart" class="chart-container"></div>
        </el-card>

        <el-card class="chart-card">
          <template #header>
            <span class="chart-title">收入趋势</span>
          </template>
          <div id="revenue-trend-chart" class="chart-container"></div>
        </el-card>

        <el-card class="chart-card">
          <template #header>
            <span class="chart-title">门店收入排行</span>
          </template>
          <div id="bar-chart" class="chart-container"></div>
        </el-card>
      </div>
    </div>

    <div class="data-section">
      <div class="charts-grid realtime-charts">
        <el-card class="chart-card full-width">
          <template #header>
            <span class="chart-title">房间状态分布</span>
          </template>
          <div id="pie-chart" class="chart-container"></div>
        </el-card>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts" name="home">
import { ref, onMounted, reactive, computed } from 'vue'
import * as echarts from 'echarts'
import { ShoppingCart, OfficeBuilding, Wallet, Lock, CirclePlus, ArrowUp, PieChart, CircleCheck, Clock, Refresh } from '@element-plus/icons-vue'
import { statisticsApi } from '@/api/modules/statistics'

const icons = { ShoppingCart, OfficeBuilding, Wallet, Lock, CirclePlus, ArrowUp, PieChart, CircleCheck, Clock, Refresh }

const timeRange = ref('7days')
const overviewData = reactive<any>({})
const realtimeData = reactive<any>({})

const growthClass = computed(() => {
  const rate = overviewData.summary?.revenueGrowthRate || 0
  if (rate > 0) return 'positive'
  if (rate < 0) return 'negative'
  return ''
})

let orderTrendChart: echarts.ECharts | null = null
let revenueTrendChart: echarts.ECharts | null = null
let pieChart: echarts.ECharts | null = null
let barChart: echarts.ECharts | null = null

const initCharts = () => {
  orderTrendChart = echarts.init(document.getElementById('order-trend-chart'))
  revenueTrendChart = echarts.init(document.getElementById('revenue-trend-chart'))
  pieChart = echarts.init(document.getElementById('pie-chart'))
  barChart = echarts.init(document.getElementById('bar-chart'))

  window.addEventListener('resize', () => {
    orderTrendChart?.resize()
    revenueTrendChart?.resize()
    pieChart?.resize()
    barChart?.resize()
  })
}

const updateCharts = () => {
  if (orderTrendChart && overviewData.orderTrend) {
    orderTrendChart.setOption({
      backgroundColor: 'transparent',
      tooltip: { trigger: 'axis' },
      legend: { data: overviewData.orderTrend.series?.map((s: any) => s.name) || [], top: 0, left: 'center' },
      grid: { left: '3%', right: '4%', bottom: '3%', top: '15%', containLabel: true },
      xAxis: { type: 'category', data: overviewData.orderTrend.labels || [] },
      yAxis: { type: 'value' },
      series: overviewData.orderTrend.series || []
    })
  }

  if (revenueTrendChart && overviewData.revenueTrend) {
    revenueTrendChart.setOption({
      backgroundColor: 'transparent',
      tooltip: { trigger: 'axis' },
      legend: { data: overviewData.revenueTrend.series?.map((s: any) => s.name) || [], top: 0, left: 'center' },
      grid: { left: '3%', right: '4%', bottom: '3%', top: '15%', containLabel: true },
      xAxis: { type: 'category', data: overviewData.revenueTrend.labels || [] },
      yAxis: { type: 'value' },
      series: overviewData.revenueTrend.series || []
    })
  }

  if (pieChart && realtimeData.roomStatusDistribution) {
    pieChart.setOption({
      backgroundColor: 'transparent',
      tooltip: { trigger: 'item' },
      legend: { orient: 'vertical', right: '5%', top: 'center' },
      series: [{
        type: 'pie',
        radius: ['40%', '70%'],
        center: ['40%', '50%'],
        data: realtimeData.roomStatusDistribution.data?.map((item: any) => ({
          value: item.value,
          name: item.name
        })) || []
      }]
    })
  }

  if (barChart && overviewData.topShopRevenue) {
    barChart.setOption({
      backgroundColor: 'transparent',
      tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
      legend: { data: ['收入'], top: 0, left: 'center' },
      grid: { left: '3%', right: '4%', bottom: '3%', top: '15%', containLabel: true },
      xAxis: { type: 'category', data: overviewData.topShopRevenue.labels || [] },
      yAxis: { type: 'value' },
      series: [{
        name: '收入',
        type: 'bar',
        data: overviewData.topShopRevenue.data || []
      }]
    })
  }
}

const loadData = async () => {
  try {
    const [overviewRes, realtimeRes] = await Promise.all([
      statisticsApi.getOverview({ timeRange: timeRange.value }),
      statisticsApi.getRealtime()
    ])

    Object.assign(overviewData, overviewRes.data)
    Object.assign(realtimeData, realtimeRes.data)

    updateCharts()
  } catch (error) {
    console.error('加载统计数据失败:', error)
  }
}

onMounted(() => {
  initCharts()
  loadData()
})
</script>

<style scoped lang="scss">
.dashboard {
  padding: 15px;
  padding-top: 10px;
  min-height: calc(100vh - 60px);
}

.dashboard-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 10px;
}

.header-title h1 {
  font-size: 24px;
  margin: 0;
}

.subtitle {
  font-size: 14px;
  color: #909399;
  margin: 4px 0 0 0;
}

.time-filter {
  .time-select {
    width: 140px;
  }
}

.data-section {
  margin-bottom: 30px;
}

.section-header {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 16px;
}

.section-title {
  font-size: 16px;
  font-weight: 600;
  color: #303133;
}

.section-badge {
  font-size: 12px;
  color: #60a5fa;
  background: rgba(96, 165, 250, 0.1);
  padding: 4px 10px;
  border-radius: 10px;
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 16px;
  margin-bottom: 20px;
}

.overview-stats {
  grid-template-columns: repeat(6, 1fr);
}

.stat-card {
  .stat-content {
    display: flex;
    align-items: center;
    gap: 16px;
  }

  .stat-icon {
    width: 48px;
    height: 48px;
    border-radius: 12px;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 24px;

    &.blue {
      background: rgba(96, 165, 250, 0.15);
      color: #60a5fa;
    }

    &.purple {
      background: rgba(167, 139, 250, 0.15);
      color: #a78bfa;
    }

    &.green {
      background: rgba(52, 211, 153, 0.15);
      color: #34d399;
    }

    &.orange {
      background: rgba(251, 191, 36, 0.15);
      color: #fbbf24;
    }

    &.cyan {
      background: rgba(20, 184, 166, 0.15);
      color: #14b8a6;
    }

    &.pink {
      background: rgba(236, 72, 153, 0.15);
      color: #ec4899;
    }

    &.red {
      background: rgba(239, 68, 68, 0.15);
      color: #ef4444;
    }

    &.indigo {
      background: rgba(99, 102, 241, 0.15);
      color: #6366f1;
    }
  }

  .stat-info {
    flex: 1;
  }

  .stat-label {
    font-size: 13px;
    color: #909399;
    margin: 0 0 4px 0;
  }

  .stat-value {
    font-size: 24px;
    font-weight: 600;
    margin: 0;
    color: #303133;

    &.positive {
      color: #34d399;
    }

    &.negative {
      color: #ef4444;
    }
  }
}

.charts-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 16px;
}

.chart-card {
  .chart-title {
    font-size: 15px;
    font-weight: 600;
  }

  .chart-container {
    height: 260px;
  }

  &.full-width {
    grid-column: span 3;

    .chart-container {
      height: 280px;
    }
  }
}

@media (max-width: 1200px) {
  .stats-grid {
    grid-template-columns: repeat(2, 1fr);
  }

  .charts-grid {
    grid-template-columns: 1fr;
  }

  .chart-card.full-width {
    grid-column: span 1;
  }
}

@media (max-width: 768px) {
  .stats-grid {
    grid-template-columns: 1fr;
  }
}
</style>
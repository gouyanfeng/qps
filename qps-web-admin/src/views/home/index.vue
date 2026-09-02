<template>
  <div class="home-dashboard">
    <el-alert v-if="errorMessage" :title="errorMessage" type="error" show-icon :closable="false">
      <template #default>
        <el-button type="danger" link @click="loadDashboard">重试</el-button>
      </template>
    </el-alert>

    <div v-loading="loading" class="dashboard-body">
      <div class="dashboard-grid">
        <section class="chart-section">
          <h2 class="section-title">基地数据</h2>
          <div class="chart-grid">
            <FollowFunnelChart title="基地主体状态" :items="dashboard.followFunnel" />
            <FollowTrendChart title="近 7 天基地跟进趋势" :items="dashboard.followTrend" />
            <NewBaseTrendChart :items="dashboard.newBaseTrend" />
            <MainProductDistributionChart title="基地主营品类分布" :items="dashboard.mainProductDistribution" />
          </div>
        </section>

        <section class="chart-section">
          <h2 class="section-title">厂商数据</h2>
          <div class="chart-grid">
            <MainProductDistributionChart title="厂商优先级分布" :items="dashboard.vendorPriorityDistribution" />
            <FollowTrendChart title="近 7 天厂商跟进趋势" :items="dashboard.vendorFollowTrend" />
            <NewPurchaseDemandTrendChart :items="dashboard.newPurchaseDemandTrend" />
            <MainProductDistributionChart
              title="厂商采购品类 Top 9"
              :items="dashboard.vendorPurchaseProductDistribution"
              :total="dashboard.vendorPurchaseProductDistribution.length"
            />
          </div>
        </section>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts" name="home">
import { onMounted, ref } from "vue";
import { ElMessage } from "element-plus";
import dashboardApi, { type CrmDashboardData } from "@/api/modules/dashboard";
import FollowFunnelChart from "./components/FollowFunnelChart.vue";
import MainProductDistributionChart from "./components/MainProductDistributionChart.vue";
import FollowTrendChart from "./components/FollowTrendChart.vue";
import NewBaseTrendChart from "./components/NewBaseTrendChart.vue";
import NewPurchaseDemandTrendChart from "./components/NewPurchaseDemandTrendChart.vue";

const loading = ref(false);
const errorMessage = ref("");
const dashboard = ref<CrmDashboardData>({
  metrics: {
    todayFollowCount: 0,
    overdueFollowCount: 0,
    mySubjectCount: 0,
    highIntentSubjectCount: 0,
  },
  todayFollowSubjects: [],
  recentFollowRecords: [],
  followFunnel: [],
  mainProductDistribution: [],
  followTrend: [],
  newBaseTrend: [],
  vendorPriorityDistribution: [],
  vendorFollowTrend: [],
  newPurchaseDemandTrend: [],
  vendorPurchaseProductDistribution: [],
});

const loadDashboard = async () => {
  loading.value = true;
  errorMessage.value = "";
  try {
    const result = await dashboardApi.getCrmDashboard();
    dashboard.value = result.data;
  } catch (error) {
    errorMessage.value = "首页数据加载失败";
    ElMessage.error(errorMessage.value);
  } finally {
    loading.value = false;
  }
};

onMounted(loadDashboard);
</script>

<style scoped lang="scss">
.home-dashboard {
  min-height: calc(100vh - 96px);
  padding: 18px 20px 28px;
  background: var(--el-bg-color-page);
}

.dashboard-body {
  display: flex;
  flex-direction: column;
  gap: 14px;
  min-height: 420px;
}

.dashboard-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 14px;
}

.chart-section {
  display: flex;
  flex-direction: column;
  gap: 10px;
  min-width: 0;
}

.section-title {
  margin: 0;
  font-size: 16px;
  font-weight: 600;
  color: var(--el-text-color-primary);
}

.chart-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 14px;
}

.chart-grid :deep(.chart-block) {
  display: flex;
  flex-direction: column;
  height: 400px;
}

.chart-grid :deep(.chart) {
  flex: 1;
  min-height: 0;
  height: auto;
}

.chart-grid :deep(.el-empty) {
  flex: 1;
}

@media (max-width: 640px) {
  .home-dashboard {
    padding: 12px;
  }
}
</style>

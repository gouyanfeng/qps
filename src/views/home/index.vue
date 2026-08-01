<template>
  <div class="home-dashboard">
    <div class="page-heading">
      <div>
        <h1>我的 CRM 工作台</h1>
      </div>
      <el-button :icon="Refresh" :loading="loading" @click="loadDashboard">刷新</el-button>
    </div>

    <el-alert v-if="errorMessage" :title="errorMessage" type="error" show-icon :closable="false">
      <template #default>
        <el-button type="danger" link @click="loadDashboard">重试</el-button>
      </template>
    </el-alert>

    <div v-loading="loading" class="dashboard-body">
      <HomeMetricCards :metrics="dashboard.metrics" @metric-click="handleMetricClick" />

      <div class="main-grid">
        <TodayFollowTable
          :customers="dashboard.todayFollowCustomers"
          @open-detail="openHerbBaseDetail"
          @record-follow="recordFollow"
        />
        <div class="side-column">
          <RecentFollowRecords :records="dashboard.recentFollowRecords" @open-detail="openHerbBaseById" />
        </div>
      </div>

      <div class="chart-grid">
        <FollowFunnelChart :items="dashboard.followFunnel" />
        <MainProductDistributionChart :items="dashboard.mainProductDistribution" />
        <FollowTrendChart :items="dashboard.followTrend" />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts" name="home">
import { onMounted, ref } from "vue";
import { useRouter } from "vue-router";
import { ElMessage } from "element-plus";
import { Refresh } from "@element-plus/icons-vue";
import dashboardApi, {
  type CrmDashboardData,
  type CrmDashboardFollowCustomer,
} from "@/api/modules/dashboard";
import HomeMetricCards from "./components/HomeMetricCards.vue";
import TodayFollowTable from "./components/TodayFollowTable.vue";
import RecentFollowRecords from "./components/RecentFollowRecords.vue";
import FollowFunnelChart from "./components/FollowFunnelChart.vue";
import MainProductDistributionChart from "./components/MainProductDistributionChart.vue";
import FollowTrendChart from "./components/FollowTrendChart.vue";

const router = useRouter();
const loading = ref(false);
const errorMessage = ref("");
const dashboard = ref<CrmDashboardData>({
  metrics: {
    todayFollowCount: 0,
    overdueFollowCount: 0,
    myCustomerCount: 0,
    highIntentCustomerCount: 0,
  },
  todayFollowCustomers: [],
  recentFollowRecords: [],
  followFunnel: [],
  mainProductDistribution: [],
  followTrend: [],
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

const goHerbBaseList = (query: Record<string, string> = {}) => {
  router.push({ path: "/crm/herb-base", query });
};

const openHerbBaseDetail = (customer: CrmDashboardFollowCustomer) => {
  goHerbBaseList({ detailId: customer.id });
};

const openHerbBaseById = (customerId: string) => {
  goHerbBaseList({ detailId: customerId });
};

const recordFollow = (customer: CrmDashboardFollowCustomer) => {
  goHerbBaseList({ followId: customer.id });
};

const handleMetricClick = (type: string) => {
  if (type === "today") {
    goHerbBaseList({ followFilter: "today" });
  } else if (type === "overdue") {
    goHerbBaseList({ onlyOverdue: "true" });
  } else if (type === "highIntent") {
    goHerbBaseList({ status: "INTERESTED" });
  } else {
    goHerbBaseList();
  }
};

onMounted(loadDashboard);
</script>

<style scoped lang="scss">
.home-dashboard {
  min-height: calc(100vh - 96px);
  padding: 18px;
  background: var(--el-bg-color-page);
}

.page-heading {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 16px;
}

h1 {
  margin: 0;
  font-size: 22px;
  font-weight: 650;
  color: var(--el-text-color-primary);
}

.dashboard-body {
  display: flex;
  flex-direction: column;
  gap: 16px;
  min-height: 420px;
}

.main-grid {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 360px;
  gap: 16px;
  align-items: start;
}

.side-column {
  display: flex;
  flex-direction: column;
  gap: 16px;
  min-width: 0;
}

.chart-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 16px;
}

@media (max-width: 1280px) {
  .main-grid,
  .chart-grid {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 640px) {
  .home-dashboard {
    padding: 12px;
  }

  .page-heading {
    align-items: flex-start;
    flex-direction: column;
  }
}
</style>

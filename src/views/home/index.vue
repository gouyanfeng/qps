<template>
  <div class="home-dashboard">
    <el-alert v-if="errorMessage" :title="errorMessage" type="error" show-icon :closable="false">
      <template #default>
        <el-button type="danger" link @click="loadDashboard">重试</el-button>
      </template>
    </el-alert>

    <div v-loading="loading" class="dashboard-body">
      <HomeMetricCards :metrics="dashboard.metrics" @metric-click="handleMetricClick" />

      <div class="main-grid">
        <TodayFollowTable
          :customers="dashboard.todayFollowSubjects"
          @open-detail="openHerbBaseDetail"
          @record-follow="recordFollow"
        />
        <div class="side-column">
          <RecentFollowRecords :records="dashboard.recentFollowRecords" @open-detail="openHerbBaseById" />
        </div>
      </div>

      <div class="chart-grid">
        <FollowFunnelChart :items="dashboard.followFunnel" />
        <FollowTrendChart :items="dashboard.followTrend" />
        <NewBaseTrendChart :items="dashboard.newBaseTrend" />
        <MainProductDistributionChart :items="dashboard.mainProductDistribution" />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts" name="home">
import { onMounted, ref } from "vue";
import { useRouter } from "vue-router";
import { ElMessage } from "element-plus";
import dashboardApi, {
  type CrmDashboardData,
  type CrmDashboardFollowSubject,
} from "@/api/modules/dashboard";
import HomeMetricCards from "./components/HomeMetricCards.vue";
import TodayFollowTable from "./components/TodayFollowTable.vue";
import RecentFollowRecords from "./components/RecentFollowRecords.vue";
import FollowFunnelChart from "./components/FollowFunnelChart.vue";
import MainProductDistributionChart from "./components/MainProductDistributionChart.vue";
import FollowTrendChart from "./components/FollowTrendChart.vue";
import NewBaseTrendChart from "./components/NewBaseTrendChart.vue";

const router = useRouter();
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

const openHerbBaseDetail = (subject: CrmDashboardFollowSubject) => {
  goHerbBaseList({ detailId: subject.id });
};

const openHerbBaseById = (herbBaseSubjectId: string) => {
  goHerbBaseList({ detailId: herbBaseSubjectId });
};

const recordFollow = (subject: CrmDashboardFollowSubject) => {
  goHerbBaseList({ followId: subject.id });
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
  padding: 18px 20px 28px;
  background: var(--el-bg-color-page);
}

.dashboard-body {
  display: flex;
  flex-direction: column;
  gap: 14px;
  max-width: 1680px;
  min-height: 420px;
  margin: 0 auto;
}

.main-grid {
  display: grid;
  grid-template-columns: minmax(0, 2.25fr) minmax(300px, 0.75fr);
  gap: 14px;
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
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 14px;
}

@media (max-width: 1280px) {
  .chart-grid {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 1080px) {
  .main-grid {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 640px) {
  .home-dashboard {
    padding: 12px;
  }
}
</style>

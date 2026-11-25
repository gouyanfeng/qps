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
            <FollowFunnelChart title="基地主体状态" :items="followFunnel" />
            <FollowTrendChart title="近 7 天基地跟进趋势" :items="followTrend" />
            <NewBaseTrendChart :items="newBaseTrend" />
            <ProductDistributionChart title="基地供应品类分布" :items="supplyProductDistribution" />
          </div>
        </section>

        <section class="chart-section">
          <h2 class="section-title">厂商数据</h2>
          <div class="chart-grid">
            <ProductDistributionChart title="厂商优先级分布" :items="vendorPriorityDistribution" />
            <FollowTrendChart title="近 7 天厂商跟进趋势" :items="vendorFollowTrend" />
            <NewPurchaseDemandTrendChart :items="newPurchaseDemandTrend" />
            <ProductDistributionChart
              title="厂商采购品类 Top 10"
              :items="vendorPurchaseProductDistribution"
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
import dashboardApi, {
  type CrmDashboardChartItem,
  type CrmDashboardNewBaseTrendItem,
  type CrmDashboardNewPurchaseDemandTrendItem,
  type CrmDashboardTrendItem,
} from "@/api/modules/crm/crmDashboard";
import FollowFunnelChart from "./components/FollowFunnelChart.vue";
import ProductDistributionChart from "./components/ProductDistributionChart.vue";
import FollowTrendChart from "./components/FollowTrendChart.vue";
import NewBaseTrendChart from "./components/NewBaseTrendChart.vue";
import NewPurchaseDemandTrendChart from "./components/NewPurchaseDemandTrendChart.vue";

const loading = ref(false);
const errorMessage = ref("");
const followFunnel = ref<CrmDashboardChartItem[]>([]);
const supplyProductDistribution = ref<CrmDashboardChartItem[]>([]);
const followTrend = ref<CrmDashboardTrendItem[]>([]);
const newBaseTrend = ref<CrmDashboardNewBaseTrendItem[]>([]);
const vendorPriorityDistribution = ref<CrmDashboardChartItem[]>([]);
const vendorFollowTrend = ref<CrmDashboardTrendItem[]>([]);
const newPurchaseDemandTrend = ref<CrmDashboardNewPurchaseDemandTrendItem[]>([]);
const vendorPurchaseProductDistribution = ref<CrmDashboardChartItem[]>([]);

const normalizeVendorPriority = (items: CrmDashboardChartItem[]) => {
  const labelMap: Record<string, string> = {
    High: "高",
    Medium: "中",
    Low: "低",
    high: "高",
    medium: "中",
    low: "低",
  };
  const counts = new Map<string, number>();
  items.forEach(item => {
    const name = labelMap[item.name] || item.name || labelMap[item.code] || item.code || "中";
    counts.set(name, (counts.get(name) || 0) + item.value);
  });
  return ["高", "中", "低"]
    .filter(name => counts.has(name))
    .map(name => ({ code: name, name, value: counts.get(name) || 0 }));
};

const loadDashboard = async () => {
  loading.value = true;
  errorMessage.value = "";
  try {
    const [followFunnelResult, supplyProductResult, followTrendResult, newBaseTrendResult, vendorPriorityResult, vendorFollowTrendResult, newPurchaseDemandTrendResult, vendorPurchaseProductResult] = await Promise.all([
      dashboardApi.getFollowFunnel(),
      dashboardApi.getSupplyProductDistribution(),
      dashboardApi.getFollowTrend(),
      dashboardApi.getNewBaseTrend(),
      dashboardApi.getVendorPriorityDistribution(),
      dashboardApi.getVendorFollowTrend(),
      dashboardApi.getNewPurchaseDemandTrend(),
      dashboardApi.getVendorPurchaseProductDistribution(),
    ]);
    followFunnel.value = followFunnelResult.data;
    supplyProductDistribution.value = supplyProductResult.data;
    followTrend.value = followTrendResult.data;
    newBaseTrend.value = newBaseTrendResult.data;
    vendorPriorityDistribution.value = normalizeVendorPriority(vendorPriorityResult.data);
    vendorFollowTrend.value = vendorFollowTrendResult.data;
    newPurchaseDemandTrend.value = newPurchaseDemandTrendResult.data;
    vendorPurchaseProductDistribution.value = vendorPurchaseProductResult.data;
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
@import "./index.scss";
</style>

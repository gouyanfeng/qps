<template>
  <Permission code="MALL_DASHBOARD_VIEW">
    <div class="mall-dashboard">
      <el-alert v-if="errorMessage" :title="errorMessage" type="error" show-icon :closable="false">
        <template #default>
          <el-button type="danger" link @click="loadDashboard">重试</el-button>
        </template>
      </el-alert>

      <div v-loading="loading" class="dashboard-body">
        <div class="section-heading">
          <h2>核心经营</h2>
          <span>近 7 天</span>
        </div>
        <SummaryCards :summary="summary" />
        <div class="chart-grid">
          <OrderTrendChart :items="orderTrend" />
          <SalesTrendChart :items="salesTrend" />
          <AovTrendChart :items="aovTrend" />
          <ConversionFunnelChart :items="conversionFunnel" />
          <OrderStatusDonutChart :items="orderStatusDistribution" />
          <TopProductsChart :items="topProducts" />
        </div>
      </div>
    </div>
  </Permission>
</template>

<script setup lang="ts" name="MallDashboard">
import { onMounted, ref } from "vue";
import { ElMessage } from "element-plus";
import mallDashboardApi, {
  type MallAovTrendItem,
  type MallConversionFunnelItem,
  type MallDashboardSummary,
  type MallOrderStatusItem,
  type MallOrderTrendItem,
  type MallSalesTrendItem,
  type MallTopProductItem
} from "@/api/modules/mall/mallDashboard";
import SummaryCards from "./components/SummaryCards.vue";
import OrderTrendChart from "./components/OrderTrendChart.vue";
import SalesTrendChart from "./components/SalesTrendChart.vue";
import AovTrendChart from "./components/AovTrendChart.vue";
import ConversionFunnelChart from "./components/ConversionFunnelChart.vue";
import OrderStatusDonutChart from "./components/OrderStatusDonutChart.vue";
import TopProductsChart from "./components/TopProductsChart.vue";

const loading = ref(false);
const errorMessage = ref("");
const summary = ref<MallDashboardSummary | null>(null);
const orderTrend = ref<MallOrderTrendItem[]>([]);
const salesTrend = ref<MallSalesTrendItem[]>([]);
const aovTrend = ref<MallAovTrendItem[]>([]);
const conversionFunnel = ref<MallConversionFunnelItem[]>([]);
const orderStatusDistribution = ref<MallOrderStatusItem[]>([]);
const topProducts = ref<MallTopProductItem[]>([]);

const loadDashboard = async () => {
  loading.value = true;
  errorMessage.value = "";
  try {
    const [summaryResult, orderTrendResult, salesTrendResult, aovTrendResult, funnelResult, statusResult, topProductsResult] =
      await Promise.all([
        mallDashboardApi.getSummary(),
        mallDashboardApi.getOrderTrend(),
        mallDashboardApi.getSalesTrend(),
        mallDashboardApi.getAovTrend(),
        mallDashboardApi.getConversionFunnel(),
        mallDashboardApi.getOrderStatusDistribution(),
        mallDashboardApi.getTopProducts()
      ]);
    summary.value = summaryResult.data;
    orderTrend.value = orderTrendResult.data;
    salesTrend.value = salesTrendResult.data;
    aovTrend.value = aovTrendResult.data;
    conversionFunnel.value = funnelResult.data;
    orderStatusDistribution.value = statusResult.data;
    topProducts.value = topProductsResult.data;
  } catch (error) {
    errorMessage.value = "商城数据看板加载失败";
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

<template>
  <div class="chart-block">
    <div class="block-header">
      <h2>近 7 天新增采购列表</h2>
    </div>
    <el-empty v-if="isEmpty" description="暂无新增采购列表" :image-size="72" />
    <VChart v-else class="chart" :option="option" autoresize />
  </div>
</template>

<script setup lang="ts">
import { computed } from "vue";
import { use } from "echarts/core";
import { CanvasRenderer } from "echarts/renderers";
import { BarChart } from "echarts/charts";
import { GridComponent, TooltipComponent } from "echarts/components";
import VChart from "vue-echarts";
import type { CrmDashboardNewPurchaseDemandTrendItem } from "@/api/modules/crm/crmDashboard";

use([CanvasRenderer, BarChart, GridComponent, TooltipComponent]);

const props = defineProps<{
  items: CrmDashboardNewPurchaseDemandTrendItem[];
}>();

const isEmpty = computed(() => props.items.length === 0 || props.items.every(item => item.newPurchaseDemandCount === 0));

const option = computed(() => ({
  tooltip: { trigger: "axis" },
  grid: { left: 36, right: 16, top: 24, bottom: 32 },
  xAxis: {
    type: "category",
    data: props.items.map(item => new Date(item.date).toLocaleDateString("zh-CN", { month: "2-digit", day: "2-digit" })),
  },
  yAxis: { type: "value", minInterval: 1 },
  series: [
    {
      name: "新增采购列表",
      type: "bar",
      barMaxWidth: 28,
      data: props.items.map(item => item.newPurchaseDemandCount),
    },
  ],
}));
</script>

<style scoped lang="scss">
@import "./NewPurchaseDemandTrendChart.scss";
</style>

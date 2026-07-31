<template>
  <div class="chart-block">
    <div class="block-header">
      <h2>主营品类分布</h2>
    </div>
    <el-empty v-if="isEmpty" description="暂无品类数据" :image-size="72" />
    <VChart v-else class="chart" :option="option" autoresize />
  </div>
</template>

<script setup lang="ts">
import { computed } from "vue";
import { use } from "echarts/core";
import { CanvasRenderer } from "echarts/renderers";
import { PieChart } from "echarts/charts";
import { LegendComponent, TooltipComponent } from "echarts/components";
import VChart from "vue-echarts";
import type { CrmDashboardChartItem } from "@/api/modules/dashboard";

use([CanvasRenderer, PieChart, LegendComponent, TooltipComponent]);

const props = defineProps<{
  items: CrmDashboardChartItem[];
}>();

const isEmpty = computed(() => props.items.length === 0 || props.items.every(item => item.value === 0));

const option = computed(() => ({
  tooltip: { trigger: "item" },
  legend: { bottom: 0, type: "scroll" },
  series: [
    {
      type: "pie",
      radius: ["48%", "70%"],
      center: ["50%", "45%"],
      label: { formatter: "{b} {c}" },
      data: props.items.map(item => ({ name: item.name, value: item.value })),
    },
  ],
}));
</script>

<style scoped lang="scss">
.chart-block {
  min-width: 0;
  border: 1px solid var(--el-border-color-light);
  border-radius: 8px;
  background: var(--el-bg-color);
}

.block-header {
  display: flex;
  align-items: center;
  height: 48px;
  padding: 0 16px;
  border-bottom: 1px solid var(--el-border-color-lighter);
}

h2 {
  margin: 0;
  font-size: 16px;
  font-weight: 600;
}

.chart {
  width: 100%;
  height: 260px;
}
</style>

<template>
  <div class="chart-block">
    <div class="block-header">
      <h2>近 7 天新增基地</h2>
    </div>
    <el-empty v-if="isEmpty" description="暂无新增基地" :image-size="72" />
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
import type { CrmDashboardNewBaseTrendItem } from "@/api/modules/dashboard";

use([CanvasRenderer, BarChart, GridComponent, TooltipComponent]);

const props = defineProps<{
  items: CrmDashboardNewBaseTrendItem[];
}>();

const isEmpty = computed(() => props.items.length === 0 || props.items.every(item => item.newBaseCount === 0));

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
      name: "新增基地",
      type: "bar",
      barMaxWidth: 28,
      data: props.items.map(item => item.newBaseCount),
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
  overflow: hidden;
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
  height: 300px;
}
</style>

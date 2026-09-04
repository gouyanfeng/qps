<template>
  <div class="chart-block">
    <div class="block-header">
      <h2>{{ props.title || "品类分布" }}</h2>
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
import { LegendComponent, TitleComponent, TooltipComponent } from "echarts/components";
import VChart from "vue-echarts";
import type { CrmDashboardChartItem } from "@/api/modules/dashboard";

use([CanvasRenderer, PieChart, LegendComponent, TitleComponent, TooltipComponent]);

const props = defineProps<{
  items: CrmDashboardChartItem[];
  title?: string;
  total?: number;
}>();

const chartItems = computed(() => {
  return props.items
    .filter(item => item.value > 0)
    .slice()
    .sort((a, b) => b.value - a.value)
    .slice(0, 10);
});

const isEmpty = computed(() => chartItems.value.length === 0);

const option = computed(() => ({
  title:
    props.total === undefined
      ? undefined
      : {
          text: String(props.total),
          subtext: "品类总数",
          left: "center",
          top: "39%",
          textAlign: "center",
          textStyle: { fontSize: 22, fontWeight: 600 },
          subtextStyle: { fontSize: 12, color: "#909399" },
        },
  tooltip: { trigger: "item" },
  legend: { top: 8, left: "center", type: "scroll" },
  series: [
    {
      type: "pie",
      radius: ["48%", "70%"],
      center: ["50%", "56%"],
      label: { formatter: "{b} {c}" },
      data: chartItems.value.map(item => ({ name: item.name, value: item.value })),
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
  height: 310px;
}
</style>

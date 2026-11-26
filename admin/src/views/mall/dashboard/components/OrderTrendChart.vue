<template>
  <div class="chart-block">
    <div class="block-header">
      <h2>订单趋势</h2>
      <span class="block-hint">近 7 天</span>
    </div>
    <el-empty v-if="isEmpty" description="暂无订单数据" :image-size="72" />
    <VChart v-else class="chart" :option="option" autoresize />
  </div>
</template>

<script setup lang="ts">
import { computed } from "vue";
import { use } from "echarts/core";
import { CanvasRenderer } from "echarts/renderers";
import { LineChart } from "echarts/charts";
import { GridComponent, TooltipComponent } from "echarts/components";
import VChart from "vue-echarts";
import type { MallOrderTrendItem } from "@/api/modules/mall/mallDashboard";
import { formatDay, getChartTheme } from "../chartTheme";

use([CanvasRenderer, LineChart, GridComponent, TooltipComponent]);

const props = defineProps<{ items: MallOrderTrendItem[] }>();

const isEmpty = computed(() => props.items.length === 0 || props.items.every(item => item.orderCount === 0));

const option = computed(() => {
  const theme = getChartTheme();
  const days = props.items.map(item => formatDay(item.date));
  const counts = props.items.map(item => item.orderCount);
  return {
    color: [theme.palette[0]],
    tooltip: {
      trigger: "axis",
      backgroundColor: theme.tooltipBg,
      textStyle: { color: theme.textColor },
      formatter: (params: Array<{ axisValue: string; value: number }>) =>
        `${params[0]?.axisValue ?? ""}<br/>订单数：${params[0]?.value ?? 0}`
    },
    grid: { left: 36, right: 16, top: 24, bottom: 28 },
    xAxis: {
      type: "category",
      boundaryGap: false,
      data: days,
      axisLine: { lineStyle: { color: theme.axisLine } },
      axisLabel: { color: theme.textSecondary }
    },
    yAxis: {
      type: "value",
      minInterval: 1,
      axisLine: { show: false },
      axisLabel: { color: theme.textSecondary },
      splitLine: { lineStyle: { color: theme.splitLine } }
    },
    series: [
      {
        name: "订单数",
        type: "line",
        smooth: true,
        symbol: "circle",
        areaStyle: { opacity: 0.1 },
        data: counts
      }
    ]
  };
});
</script>

<style scoped lang="scss">
@import "./OrderTrendChart.scss";
</style>

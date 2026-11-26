<template>
  <div class="chart-block">
    <div class="block-header">
      <h2>客单价趋势</h2>
      <span class="block-hint">近 7 天 · 实收 / 成交订单</span>
    </div>
    <el-empty v-if="isEmpty" description="暂无成交数据" :image-size="72" />
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
import type { MallAovTrendItem } from "@/api/modules/mall/mallDashboard";
import { formatAmount, formatDay, getChartTheme } from "../chartTheme";

use([CanvasRenderer, BarChart, GridComponent, TooltipComponent]);

const props = defineProps<{ items: MallAovTrendItem[] }>();

const isEmpty = computed(() => props.items.length === 0 || props.items.every(item => item.averageOrderValue === 0));

const option = computed(() => {
  const theme = getChartTheme();
  const days = props.items.map(item => formatDay(item.date));
  return {
    color: [theme.palette[3]],
    tooltip: {
      trigger: "axis",
      axisPointer: { type: "shadow" },
      backgroundColor: theme.tooltipBg,
      textStyle: { color: theme.textColor },
      formatter: (params: Array<{ axisValue: string; value: number }>) =>
        `${params[0]?.axisValue ?? ""}<br/>客单价：${formatAmount(params[0]?.value ?? 0)}`
    },
    grid: { left: 48, right: 16, top: 24, bottom: 28 },
    xAxis: {
      type: "category",
      data: days,
      axisLine: { lineStyle: { color: theme.axisLine } },
      axisLabel: { color: theme.textSecondary }
    },
    yAxis: {
      type: "value",
      axisLine: { show: false },
      axisLabel: { color: theme.textSecondary, formatter: (value: number) => `¥${value}` },
      splitLine: { lineStyle: { color: theme.splitLine } }
    },
    series: [
      {
        name: "客单价",
        type: "bar",
        barMaxWidth: 28,
        data: props.items.map(item => item.averageOrderValue)
      }
    ]
  };
});
</script>

<style scoped lang="scss">
@import "./AovTrendChart.scss";
</style>

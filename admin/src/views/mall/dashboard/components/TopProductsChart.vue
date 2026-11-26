<template>
  <div class="chart-block">
    <div class="block-header">
      <h2>热销商品 Top 10</h2>
      <span class="block-hint">近 7 天 · 按销量</span>
    </div>
    <el-empty v-if="isEmpty" description="暂无销售数据" :image-size="72" />
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
import type { MallTopProductItem } from "@/api/modules/mall/mallDashboard";
import { formatAmount, getChartTheme } from "../chartTheme";

use([CanvasRenderer, BarChart, GridComponent, TooltipComponent]);

const props = defineProps<{ items: MallTopProductItem[] }>();

// 后端已按销量降序返回；ECharts 横向柱状图 y 轴从下往上排，反转后销量最大者显示在顶部
const chartItems = computed(() => props.items.slice(0, 10).slice().reverse());

const isEmpty = computed(() => chartItems.value.length === 0);

const option = computed(() => {
  const theme = getChartTheme();
  const names = chartItems.value.map(item => item.productName);
  return {
    color: [theme.palette[0]],
    tooltip: {
      trigger: "axis",
      axisPointer: { type: "shadow" },
      backgroundColor: theme.tooltipBg,
      textStyle: { color: theme.textColor },
      formatter: (params: Array<{ dataIndex: number }>) => {
        const item = chartItems.value[params[0]?.dataIndex ?? 0];
        if (!item) return "";
        return `${item.productName}<br/>销量：${item.quantity} 件<br/>销售额：${formatAmount(item.salesAmount)}`;
      }
    },
    grid: { left: 8, right: 24, top: 12, bottom: 12, containLabel: true },
    xAxis: {
      type: "value",
      minInterval: 1,
      axisLine: { show: false },
      axisLabel: { color: theme.textSecondary },
      splitLine: { lineStyle: { color: theme.splitLine } }
    },
    yAxis: {
      type: "category",
      data: names,
      axisLine: { lineStyle: { color: theme.axisLine } },
      axisLabel: { color: theme.textSecondary, width: 120, overflow: "truncate" }
    },
    series: [
      {
        name: "销量",
        type: "bar",
        barMaxWidth: 18,
        label: { show: true, position: "right", color: theme.textSecondary },
        data: chartItems.value.map(item => item.quantity)
      }
    ]
  };
});
</script>

<style scoped lang="scss">
@import "./TopProductsChart.scss";
</style>

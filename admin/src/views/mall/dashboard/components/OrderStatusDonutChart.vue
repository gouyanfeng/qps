<template>
  <div class="chart-block">
    <div class="block-header">
      <h2>订单状态分布</h2>
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
import { PieChart } from "echarts/charts";
import { LegendComponent, TitleComponent, TooltipComponent } from "echarts/components";
import VChart from "vue-echarts";
import type { MallOrderStatusItem } from "@/api/modules/mall/mallDashboard";
import { getChartTheme } from "../chartTheme";

use([CanvasRenderer, PieChart, LegendComponent, TitleComponent, TooltipComponent]);

const props = defineProps<{ items: MallOrderStatusItem[] }>();

const chartItems = computed(() => props.items.filter(item => item.value > 0));

const isEmpty = computed(() => chartItems.value.length === 0);

const option = computed(() => {
  const theme = getChartTheme();
  const total = chartItems.value.reduce((sum, item) => sum + item.value, 0);
  return {
    color: theme.palette,
    tooltip: {
      trigger: "item",
      backgroundColor: theme.tooltipBg,
      textStyle: { color: theme.textColor },
      formatter: (params: { name: string; value: number; percent: number }) =>
        `${params.name}<br/>${params.value} 单（${params.percent}%）`
    },
    legend: {
      type: "scroll",
      bottom: 0,
      left: "center",
      textStyle: { color: theme.textSecondary }
    },
    title: {
      text: String(total),
      subtext: "订单总数",
      left: "center",
      top: "38%",
      textAlign: "center",
      textStyle: { color: theme.textColor, fontSize: 22, fontWeight: 600 },
      subtextStyle: { color: theme.textSecondary, fontSize: 12 }
    },
    series: [
      {
        name: "订单状态",
        type: "pie",
        radius: ["45%", "68%"],
        center: ["50%", "52%"],
        avoidLabelOverlap: true,
        label: { show: false },
        emphasis: { label: { show: true, fontSize: 14, fontWeight: 600, color: theme.textColor } },
        data: chartItems.value.map(item => ({ name: item.name, value: item.value }))
      }
    ]
  };
});
</script>

<style scoped lang="scss">
@import "./OrderStatusDonutChart.scss";
</style>

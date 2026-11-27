<template>
  <div class="chart-block">
    <div class="block-header">
      <h2>订单转化漏斗</h2>
      <span class="block-hint">近 7 天 · 按下单批次</span>
    </div>
    <el-empty v-if="isEmpty" description="暂无订单数据" :image-size="72" />
    <VChart v-else class="chart" :option="option" autoresize />
  </div>
</template>

<script setup lang="ts">
import { computed } from "vue";
import { use } from "echarts/core";
import { CanvasRenderer } from "echarts/renderers";
import { FunnelChart } from "echarts/charts";
import { TooltipComponent } from "echarts/components";
import VChart from "vue-echarts";
import type { MallConversionFunnelItem } from "@/api/modules/mall/mallDashboard";
import { getChartTheme } from "../chartTheme";

use([CanvasRenderer, FunnelChart, TooltipComponent]);

const props = defineProps<{ items: MallConversionFunnelItem[] }>();

const isEmpty = computed(() => props.items.length === 0 || props.items[0]?.value === 0);

const option = computed(() => {
  const theme = getChartTheme();
  const data = props.items.map(item => ({ name: item.name, value: item.value }));
  return {
    color: theme.palette,
    tooltip: {
      trigger: "item",
      backgroundColor: theme.tooltipBg,
      textStyle: { color: theme.textColor },
      formatter: (params: { name: string; value: number }) => `${params.name}：${params.value} 单`
    },
    series: [
      {
        name: "订单转化",
        type: "funnel",
        left: "12%",
        right: "12%",
        top: 16,
        bottom: 16,
        minSize: "24%",
        maxSize: "100%",
        sort: "descending",
        gap: 2,
        label: {
          show: true,
          position: "inside",
          color: "#ffffff",
          fontSize: 13,
          formatter: (params: { name: string; value: number }) => `${params.name}  ${params.value}`
        },
        labelLine: { show: false },
        itemStyle: { borderColor: theme.tooltipBg, borderWidth: 1 },
        emphasis: { label: { fontSize: 15, fontWeight: 600 } },
        data
      }
    ]
  };
});
</script>

<style scoped lang="scss">
@import "./ConversionFunnelChart.scss";
</style>

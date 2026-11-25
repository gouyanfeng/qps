<template>
  <div class="chart-block">
    <div class="block-header">
      <h2>{{ props.title || "跟进漏斗" }}</h2>
    </div>
    <el-empty v-if="isEmpty" description="暂无漏斗数据" :image-size="72" />
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
import type { CrmDashboardChartItem } from "@/api/modules/crm/crmDashboard";

use([CanvasRenderer, FunnelChart, TooltipComponent]);

const props = defineProps<{
  items: CrmDashboardChartItem[];
  title?: string;
}>();

const isEmpty = computed(() => props.items.length === 0 || props.items.every(item => item.value === 0));

const option = computed(() => ({
  tooltip: { trigger: "item" },
  series: [
    {
      type: "funnel",
      left: "8%",
      right: "8%",
      top: 16,
      bottom: 16,
      minSize: "20%",
      maxSize: "90%",
      sort: "none",
      label: { formatter: "{b} {c}" },
      data: props.items.map(item => ({ name: item.name, value: item.value })),
    },
  ],
}));
</script>

<style scoped lang="scss">
@import "./FollowFunnelChart.scss";
</style>

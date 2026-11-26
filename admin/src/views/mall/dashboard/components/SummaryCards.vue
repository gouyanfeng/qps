<template>
  <div class="summary-cards">
    <div v-for="card in cards" :key="card.label" class="summary-card">
      <div class="card-label">{{ card.label }}</div>
      <div class="card-value">
        <span class="value-number">{{ card.value }}</span>
        <span v-if="card.unit" class="value-unit">{{ card.unit }}</span>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from "vue";
import type { MallDashboardSummary } from "@/api/modules/mall/mallDashboard";
import { formatAmount } from "../chartTheme";

const props = defineProps<{ summary: MallDashboardSummary | null }>();

const cards = computed(() => {
  const s = props.summary;
  if (!s) return [];
  return [
    { label: "订单总数", value: s.totalOrders, unit: "单" },
    { label: "成交订单", value: s.paidOrders, unit: "单" },
    { label: "支付转化率", value: s.conversionRate, unit: "%" },
    { label: "销售额", value: formatAmount(s.salesAmount), unit: "" },
    { label: "客单价", value: formatAmount(s.averageOrderValue), unit: "" },
    { label: "商品销量", value: s.itemsSold, unit: "件" },
    { label: "退款金额", value: formatAmount(s.refundAmount), unit: "" },
    { label: "待发货", value: s.pendingShipment, unit: "单" }
  ];
});
</script>

<style scoped lang="scss">
@import "./SummaryCards.scss";
</style>

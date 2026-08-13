<template>
  <div class="metric-grid">
    <button
      v-for="item in metricItems"
      :key="item.type"
      class="metric-card"
      type="button"
      @click="$emit('metric-click', item.type)"
    >
      <span class="metric-icon" :class="item.tone">
        <component :is="item.icon" />
      </span>
      <span class="metric-content">
        <span class="metric-label">{{ item.label }}</span>
        <strong>{{ item.value }}</strong>
      </span>
    </button>
  </div>
</template>

<script setup lang="ts">
import { computed } from "vue";
import { AlarmClock, Star, User, Warning } from "@element-plus/icons-vue";
import type { CrmDashboardMetrics } from "@/api/modules/dashboard";

const props = defineProps<{
  metrics: CrmDashboardMetrics;
}>();

defineEmits<{
  (event: "metric-click", type: string): void;
}>();

const metricItems = computed(() => [
  {
    type: "today",
    label: "今日待跟进",
    value: props.metrics.todayFollowCount,
    icon: AlarmClock,
    tone: "is-blue",
  },
  {
    type: "overdue",
    label: "逾期未跟进",
    value: props.metrics.overdueFollowCount,
    icon: Warning,
    tone: "is-red",
  },
  {
    type: "subjects",
    label: "我的基地主体",
    value: props.metrics.mySubjectCount,
    icon: User,
    tone: "is-green",
  },
  {
    type: "highIntent",
    label: "高意向主体",
    value: props.metrics.highIntentSubjectCount,
    icon: Star,
    tone: "is-amber",
  },
]);
</script>

<style scoped lang="scss">
.metric-grid {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 12px;
}

.metric-card {
  display: flex;
  align-items: center;
  min-width: 0;
  min-height: 76px;
  padding: 14px;
  border: 1px solid var(--el-border-color-light);
  border-radius: 8px;
  background: var(--el-bg-color);
  color: var(--el-text-color-primary);
  text-align: left;
  cursor: pointer;
  transition: border-color 0.2s ease, box-shadow 0.2s ease, transform 0.2s ease;
}

.metric-card:hover {
  border-color: var(--el-color-primary-light-5);
  box-shadow: 0 8px 18px rgb(31 45 61 / 8%);
  transform: translateY(-1px);
}

.metric-icon {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex: 0 0 48px;
  width: 48px;
  height: 48px;
  border-radius: 8px;
  margin-right: 12px;
  font-size: 28px;

  :deep(svg) {
    width: 28px;
    height: 28px;
  }
}

.metric-icon.is-blue {
  color: #2563eb;
  background: #eff6ff;
}

.metric-icon.is-red {
  color: #dc2626;
  background: #fef2f2;
}

.metric-icon.is-green {
  color: #059669;
  background: #ecfdf5;
}

.metric-icon.is-amber {
  color: #d97706;
  background: #fffbeb;
}

.metric-content {
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.metric-label {
  font-size: 13px;
  color: var(--el-text-color-regular);
}

strong {
  margin-top: 5px;
  font-size: 24px;
  line-height: 1;
}

@media (max-width: 1200px) {
  .metric-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 640px) {
  .metric-grid {
    grid-template-columns: 1fr;
  }
}
</style>

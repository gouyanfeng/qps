<template>
  <div class="side-block recent-block">
    <div class="block-header">
      <h2>最近沟通</h2>
      <span>最近 5 条</span>
    </div>

    <el-empty v-if="records.length === 0" description="暂无沟通记录" :image-size="72" />
    <div v-else class="record-list">
      <button v-for="record in records" :key="record.id" class="record-item" type="button" @click="$emit('open-detail', record.herbBaseSubjectId)">
        <span class="record-title">
          <strong>{{ record.subjectName || "-" }}</strong>
          <el-tag size="small" :type="getFollowResultType(record.followResult)">
            {{ formatFollowResult(record.followResult) }}
          </el-tag>
        </span>
        <span class="record-content">{{ record.content || "-" }}</span>
        <span class="record-meta">
          {{ formatFollowType(record.followType) }}
          <span>{{ formatDate(record.createdAt) }}</span>
        </span>
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { CrmDashboardRecentFollowRecord } from "@/api/modules/dashboard";

defineProps<{
  records: CrmDashboardRecentFollowRecord[];
}>();

defineEmits<{
  (event: "open-detail", herbBaseSubjectId: string): void;
}>();

const followTypeLabels: Record<string, string> = {
  PHONE: "电话",
  WECHAT: "微信",
  VISIT: "拜访",
};

const followResultLabels: Record<string, string> = {
  CONNECTED: "已接通",
  MISSED: "未接",
  EMPTY_NUMBER: "空号",
  INTERESTED: "有意向",
  NOT_INTERESTED: "无意向",
};

const formatFollowType = (value?: string | null) => (value ? followTypeLabels[value] || value : "沟通");
const formatFollowResult = (value?: string | null) => (value ? followResultLabels[value] || value : "沟通");

const formatDate = (date?: string | null) => {
  if (!date) return "-";
  return new Date(date).toLocaleString("zh-CN", {
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  });
};

const getFollowResultType = (value?: string | null) => {
  if (value === "INTERESTED" || value === "有意向") return "success";
  if (value === "MISSED" || value === "未接") return "warning";
  if (value === "EMPTY_NUMBER" || value === "空号") return "danger";
  return "info";
};
</script>

<style scoped lang="scss">
.side-block {
  min-width: 0;
  border: 1px solid var(--el-border-color-light);
  border-radius: 8px;
  background: var(--el-bg-color);
  overflow: hidden;
}

.recent-block {
  height: 350px;
}

.block-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  height: 48px;
  padding: 0 16px;
  border-bottom: 1px solid var(--el-border-color-lighter);
}

h2 {
  margin: 0;
  font-size: 16px;
  font-weight: 600;
}

.block-header span {
  color: var(--el-text-color-secondary);
  font-size: 12px;
}

.record-list {
  display: flex;
  flex-direction: column;
  max-height: 300px;
  padding: 8px 14px 14px;
  overflow: auto;
}

.record-item {
  display: flex;
  flex-direction: column;
  gap: 6px;
  width: 100%;
  padding: 10px 0;
  border: 0;
  border-bottom: 1px solid var(--el-border-color-lighter);
  background: transparent;
  color: var(--el-text-color-primary);
  text-align: left;
  cursor: pointer;
}

.record-item:last-child {
  border-bottom: 0;
}

.record-title,
.record-meta {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
}

.record-title strong {
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.record-content {
  color: var(--el-text-color-regular);
  font-size: 13px;
  line-height: 1.5;
}

.record-meta {
  color: var(--el-text-color-secondary);
  font-size: 12px;
}
</style>

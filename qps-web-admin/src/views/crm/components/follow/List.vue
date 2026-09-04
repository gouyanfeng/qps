<template>
  <section class="detail-card follow-card">
    <div class="section-title section-title-first">
      <h3>沟通记录</h3>
      <Permission v-if="addPermission" :code="addPermission">
        <el-button type="primary" link :icon="Phone" @click="$emit('add')">记录</el-button>
      </Permission>
    </div>
    <el-timeline v-if="records.length" class="follow-timeline">
      <el-timeline-item v-for="record in records" :key="record.id" :timestamp="formatDate(record.createdAt)" placement="top">
        <div class="follow-item">
          <div class="follow-title">
            <strong>{{ record.followResult || "沟通" }}</strong>
            <el-tag size="small">{{ record.followType || "-" }}</el-tag>
          </div>
          <p>{{ record.content || "-" }}</p>
          <span class="muted">{{ record.contactName || "未指定联系人" }} · 下次 {{ formatDate(record.nextFollowAt) }}</span>
        </div>
      </el-timeline-item>
    </el-timeline>
    <el-empty v-else description="暂无沟通记录" />
  </section>
</template>

<script setup lang="ts">
import { Phone } from "@element-plus/icons-vue";
import Permission from "@/components/Permission/index.vue";

defineProps<{
  records: any[];
  addPermission?: string;
}>();

defineEmits<{
  (event: "add"): void;
}>();

const formatDate = (value?: string | null) => {
  if (!value) return "-";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  const pad = (num: number) => `${num}`.padStart(2, "0");
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())} ${pad(date.getHours())}:${pad(date.getMinutes())}`;
};
</script>

<style scoped lang="scss">
.detail-card {
  min-width: 0;
  padding: 16px;
  border: 1px solid var(--el-border-color-light);
  border-radius: 8px;
  background: #ffffff;
}

.section-title {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
  margin: 0 -16px 14px;
  padding: 0 16px 10px;
  border-bottom: 1px solid var(--el-border-color-lighter);
}

.section-title h3 {
  margin: 0;
  font-size: 16px;
}

.follow-item {
  display: flex;
  flex-direction: column;
  gap: 6px;
  padding-bottom: 12px;
  border-bottom: 1px solid var(--el-border-color-lighter);
}

.follow-title {
  display: flex;
  align-items: center;
  gap: 8px;
}

.follow-item p {
  margin: 0;
  line-height: 1.55;
}

.muted {
  color: var(--el-text-color-secondary);
  font-size: 12px;
}
</style>

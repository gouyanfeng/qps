<template>
  <section class="detail-card transfer-card">
    <div class="section-title section-title-first">
      <div class="section-heading">
        <h3>流转记录</h3>
        <el-tag size="small" effect="plain">{{ records.length }} 条</el-tag>
      </div>
    </div>
    <el-timeline v-if="records.length">
      <el-timeline-item v-for="record in records" :key="record.id" :timestamp="formatDate(record.createdAt)" placement="top">
        <div class="transfer-item">
          <strong>{{ record.actionType || "流转" }}：{{ record.fromOwnerUserName || "未分配" }} 至 {{ record.toOwnerUserName || "未分配" }}</strong>
          <p v-if="record.remark">{{ record.remark }}</p>
          <span class="muted">操作人 {{ record.operatorUserName || "-" }}</span>
        </div>
      </el-timeline-item>
    </el-timeline>
    <el-empty v-else description="暂无流转记录" class="compact-empty" :image-size="56" />
  </section>
</template>

<script setup lang="ts">
defineProps<{
  records: any[];
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

.section-heading {
  display: flex;
  align-items: center;
  gap: 8px;
}

.section-title h3 {
  margin: 0;
  font-size: 16px;
}

.transfer-item {
  display: flex;
  flex-direction: column;
  gap: 6px;
  padding-bottom: 12px;
  border-bottom: 1px solid var(--el-border-color-lighter);
}

.transfer-item p {
  margin: 0;
  line-height: 1.55;
}

.muted {
  color: var(--el-text-color-secondary);
  font-size: 12px;
}

.compact-empty {
  padding: 12px 0;

  :deep(.el-empty__description) {
    margin-top: 6px;
  }
}
</style>

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
@import "./List.scss";
</style>

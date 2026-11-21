<template>
  <section class="entity-card" :style="cardStyle">
    <slot />
    <div v-if="$slots.actions" class="entity-card-actions">
      <slot name="actions" />
    </div>
  </section>
</template>

<script setup lang="ts">
const cardStyle = { flex: "0 0 auto", width: "100%" };
</script>

<style scoped>
.entity-card {
  margin: 0;
  background: var(--el-bg-color);
  display: flex;
  flex-direction: column;
  overflow: hidden;
}
.entity-card :deep(.el-descriptions__table) { table-layout: fixed; width: 100%; }
.entity-card :deep(.el-descriptions__cell) { overflow: hidden; }
.entity-card :deep(.el-descriptions__label),
.entity-card :deep(.el-descriptions__content) {
  font-size: 14px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  word-break: keep-all;
}
.entity-card :deep(.el-descriptions__label) { width: 88px; }
.entity-card-actions { display: flex; justify-content: flex-end; gap: 8px; padding-top: 8px; }
</style>

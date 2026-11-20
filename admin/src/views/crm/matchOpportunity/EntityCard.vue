<template>
  <section class="entity-card" :style="cardStyle">
    <slot />
    <div v-if="$slots.actions" class="entity-card-actions">
      <slot name="actions" />
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed } from "vue";

const props = withDefaults(defineProps<{ borderColor?: string; width?: number; height?: number }>(), {
  borderColor: "",
  width: 430,
  height: 290
});

// 状态色边框统一由这里输出，卡片只关心传什么颜色
// 不能写 flex 简写：左列为纵向 flex，flex-basis 会变成高度基准覆盖 height
const cardStyle = computed(() => ({
  flex: "0 0 auto",
  width: `${props.width}px`,
  height: `${props.height}px`,
  ...(props.borderColor ? { borderColor: props.borderColor, borderWidth: "2px" } : {})
}));
</script>

<style scoped>
.entity-card {
  margin: 0;
  padding: 12px;
  border: 1px solid var(--el-border-color-lighter);
  border-radius: var(--el-border-radius-base);
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
.entity-card-actions { display: flex; justify-content: flex-end; gap: 8px; margin-top: auto; padding-top: 8px; }
</style>

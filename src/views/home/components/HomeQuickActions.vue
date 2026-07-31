<template>
  <div class="side-block">
    <div class="block-header">
      <h2>快捷操作</h2>
    </div>
    <div class="action-list">
      <el-button :icon="Plus" type="primary" @click="$emit('go', 'addHerbBase')">新增药材基地</el-button>
      <el-button :icon="Phone" @click="$emit('go', 'recordFollow')">记录沟通</el-button>
      <el-button :icon="Shop" @click="$emit('go', 'vendors')">查看供应商</el-button>
      <el-button :icon="Collection" @click="$emit('go', 'dictionary')">数据字典</el-button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { Collection, Phone, Plus, Shop } from "@element-plus/icons-vue";

defineEmits<{
  (event: "go", action: string): void;
}>();
</script>

<style scoped lang="scss">
.side-block {
  border: 1px solid var(--el-border-color-light);
  border-radius: 8px;
  background: var(--el-bg-color);
}

.block-header {
  display: flex;
  align-items: center;
  height: 48px;
  padding: 0 16px;
  border-bottom: 1px solid var(--el-border-color-lighter);
}

h2 {
  margin: 0;
  font-size: 16px;
  font-weight: 600;
}

.action-list {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 10px;
  padding: 14px;
}

.action-list :deep(.el-button) {
  width: 100%;
  margin: 0;
}
</style>

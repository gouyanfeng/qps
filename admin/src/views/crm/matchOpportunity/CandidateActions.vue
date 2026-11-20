<template>
  <Permission code="CRM_MATCH_OPPORTUNITY_MANAGE">
    <div class="candidate-actions">
      <el-button v-if="label !== '中选'" size="small" type="primary" :disabled="disabled" @click="emit('select')">中选</el-button>
      <el-button size="small" type="danger" plain @click="emit('remove')">移除</el-button>
    </div>
  </Permission>
</template>

<script setup lang="ts">
import { computed } from "vue";
import Permission from "@/components/Permission/index.vue";
import { candidateStatusLabel } from "./matchStatus";

const props = withDefaults(defineProps<{ status?: string | null; disabled?: boolean }>(), {
  status: "",
  disabled: false
});
const emit = defineEmits<{
  select: [];
  remove: [];
}>();

// 基地候选存中文、药厂候选存英文，按钮显隐统一按中文标签判断
const label = computed(() => candidateStatusLabel(props.status));
</script>

<style scoped>
.candidate-actions { display: flex; flex-wrap: wrap; justify-content: flex-end; gap: 8px; }
.candidate-actions :deep(.el-button + .el-button) { margin-left: 0; }
</style>

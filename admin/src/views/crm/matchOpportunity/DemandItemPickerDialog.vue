<template>
  <el-dialog :model-value="modelValue" title="选择采购明细" width="920px" destroy-on-close @update:model-value="handleVisibleChange">
    <el-table :data="demand?.items || []" border highlight-current-row @current-change="handleCurrentChange">
      <el-table-column prop="productName" label="品类" min-width="160" />
      <el-table-column label="数量" min-width="120">
        <template #default="{ row }">{{ row.quantity || "-" }}{{ row.quantity ? row.quantityUnit || "" : "" }}</template>
      </el-table-column>
      <el-table-column prop="specification" label="规格" min-width="160" show-overflow-tooltip />
      <el-table-column prop="qualityRequirement" label="质量" min-width="160" show-overflow-tooltip />
      <el-table-column label="交期" min-width="180">
        <template #default="{ row }">{{ formatDate(row.expectedDeliveryAt || demand?.expectedDeliveryAt) }}</template>
      </el-table-column>
    </el-table>
    <template #footer>
      <el-button @click="handleVisibleChange(false)">取消</el-button>
      <el-button type="primary" :disabled="!selectedItem" @click="confirm">继续</el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, watch } from "vue";

const props = defineProps<{ modelValue: boolean; demand?: any }>();
const emit = defineEmits<{ "update:modelValue": [value: boolean]; confirm: [item: any] }>();

const selectedItem = ref<any>(null);

watch(() => [props.modelValue, props.demand], () => { selectedItem.value = null; });

const handleVisibleChange = (value: boolean) => emit("update:modelValue", value);
const handleCurrentChange = (item: any) => { selectedItem.value = item; };
const confirm = () => {
  if (!selectedItem.value) return;
  emit("confirm", selectedItem.value);
  handleVisibleChange(false);
};
const formatDate = (value?: string | null) => value ? new Date(value).toLocaleString("zh-CN") : "-";
</script>

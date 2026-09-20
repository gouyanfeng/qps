<template>
  <el-dialog :model-value="modelValue" title="选择采购明细" width="920px" destroy-on-close @update:model-value="handleVisibleChange">
    <el-table :data="demand?.items || []" border highlight-current-row @current-change="handleCurrentChange">
      <el-table-column prop="productName" label="品类" min-width="130" />
      <el-table-column label="数量" min-width="100">
        <template #default="{ row }">{{ row.quantity || "-" }}{{ row.quantity ? row.quantityUnit || "" : "" }}</template>
      </el-table-column>
      <el-table-column prop="specification" label="规格" min-width="130" show-overflow-tooltip />
      <el-table-column prop="qualityRequirement" label="质量" min-width="130" show-overflow-tooltip />
      <el-table-column label="撮合状态" min-width="100" align="center">
        <template #default="{ row }"><el-tag size="small" :type="statusType(row.matchDisplayStatus)">{{ row.matchDisplayStatus || "待撮合" }}</el-tag></template>
      </el-table-column>
      <el-table-column label="交期" min-width="150">
        <template #default="{ row }">{{ formatDate(row.expectedDeliveryAt || demand?.expectedDeliveryAt) }}</template>
      </el-table-column>
    </el-table>
    <template #footer>
      <el-button @click="handleVisibleChange(false)">取消</el-button>
      <el-button type="primary" :disabled="!selectedItem" @click="confirm">{{ confirmLabel }}</el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { computed, ref, watch } from "vue";

const props = defineProps<{ modelValue: boolean; demand?: any }>();
const emit = defineEmits<{ "update:modelValue": [value: boolean]; confirm: [item: any] }>();

const selectedItem = ref<any>(null);
const confirmLabel = computed(() => {
  if (!selectedItem.value) return "继续";
  if (["推进中", "暂停"].includes(selectedItem.value.matchOpportunityStatus)) return "查看撮合";
  return selectedItem.value.matchOpportunityStatus === "已关闭" ? "重新撮合" : "继续";
});

watch(() => [props.modelValue, props.demand], () => { selectedItem.value = null; });

const handleVisibleChange = (value: boolean) => emit("update:modelValue", value);
const handleCurrentChange = (item: any) => { selectedItem.value = item; };
const confirm = () => {
  if (!selectedItem.value) return;
  emit("confirm", selectedItem.value);
  handleVisibleChange(false);
};
const formatDate = (value?: string | null) => value ? new Date(value).toLocaleString("zh-CN") : "-";
const statusType = (status?: string) => ({ "撮合中": "success", "已暂停": "warning", "已结束": "info" }[status || ""] || undefined);
</script>

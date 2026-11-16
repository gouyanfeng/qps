<template>
  <el-dialog :model-value="modelValue" :title="dialogTitle" width="920px" destroy-on-close @update:model-value="handleVisibleChange">
    <el-table :data="demand?.items || []" border highlight-current-row :row-class-name="rowClassName" @current-change="handleCurrentChange" @selection-change="handleSelectionChange">
      <el-table-column v-if="isMulti" type="selection" width="52" :selectable="rowSelectable" />
      <el-table-column prop="demandName" label="采购列表" min-width="180" show-overflow-tooltip />
      <el-table-column prop="productName" label="品类" min-width="120" />
      <el-table-column label="需求数量" min-width="100">
        <template #default="{ row }">{{ row.quantity || "-" }}{{ row.quantity ? row.quantityUnit || "" : "" }}</template>
      </el-table-column>
      <el-table-column label="已采购 / 待撮合" min-width="140">
        <template #default="{ row }">
          <template v-if="hasQuantity(row)">
            <span class="matched">{{ row.matchedQuantity || 0 }}</span> / <span :class="{ 'no-remaining': remainingOf(row) <= 0 }">{{ remainingOf(row) }}</span>{{ row.quantityUnit || "" }}
          </template>
          <span v-else>-</span>
        </template>
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
      <el-button type="primary" :disabled="isMulti ? !selectedItems.length : !selectedItem" @click="confirm">{{ confirmLabel }}</el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { computed, ref, watch } from "vue";
import { ElMessage } from "element-plus";

const props = defineProps<{ modelValue: boolean; demand?: any; source?: string }>();
const emit = defineEmits<{ "update:modelValue": [value: boolean]; confirm: [item: any] }>();

const isMulti = computed(() => props.source === "supply");
const selectedItems = ref<any[]>([]);
const dialogTitle = computed(() => props.source === "supply" ? "选择采购列表明细（供应 → 需求）" : "选择采购明细（需求 → 供应）");
const selectedItem = ref<any>(null);
const hasQuantity = (item: any) => Number(item?.quantity || 0) > 0;
const remainingOf = (item: any) => hasQuantity(item) ? Number(item?.remainingQuantity ?? 0) : 1;
const isCompleted = (item: any) => hasQuantity(item) && remainingOf(item) <= 0;
const rowClassName = ({ row }: any) => isCompleted(row) ? "completed-demand-item" : "";
const confirmLabel = computed(() => {
  if (isMulti.value) return selectedItems.value.length ? `发起撮合（${selectedItems.value.length} 家药厂）` : "选择药厂需求";
  if (!selectedItem.value) return "继续";
  if (isCompleted(selectedItem.value)) return "已采购完成";
  if (["推进中", "暂停"].includes(selectedItem.value.matchOpportunityStatus)) return "查看撮合";
  if (["撮合中", "已暂停"].includes(selectedItem.value.matchDisplayStatus)) return "查看撮合";
  return selectedItem.value.matchOpportunityStatus === "已关闭" || selectedItem.value.matchDisplayStatus === "已结束"
    ? "继续撮合剩余量"
    : "继续";
});

watch(() => [props.modelValue, props.demand], () => { selectedItem.value = null; selectedItems.value = []; });

const rowSelectable = (row: any) => !isCompleted(row);
const handleSelectionChange = (rows: any[]) => { selectedItems.value = rows; };
const handleVisibleChange = (value: boolean) => emit("update:modelValue", value);
const handleCurrentChange = (item: any) => {
  if (isMulti.value) return;
  if (isCompleted(item)) {
    selectedItem.value = null;
    ElMessage.warning("该采购明细已采购完成，如需追加数量请先修改需求");
    return;
  }
  selectedItem.value = item;
};
const confirm = () => {
  if (isMulti.value) {
    if (!selectedItems.value.length) return;
    emit("confirm", selectedItems.value);
  } else {
    if (!selectedItem.value) return;
    emit("confirm", selectedItem.value);
  }
  handleVisibleChange(false);
};
const formatDate = (value?: string | null) => value ? new Date(value).toLocaleString("zh-CN") : "-";
const statusType = (status?: string) => ({ "撮合中": "success", "已暂停": "warning", "已结束": "info", "已采购完成": "success" }[status || ""] || undefined);
</script>

<style scoped>
.matched { color: var(--el-color-success); }
.no-remaining { color: var(--el-text-color-placeholder); }
:deep(.completed-demand-item) { color: var(--el-text-color-placeholder); }
</style>

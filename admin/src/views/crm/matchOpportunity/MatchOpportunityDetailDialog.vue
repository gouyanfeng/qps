<template>
  <el-dialog
    :model-value="candidateSelectorVisible"
    title="候选筛选"
    width="1200px"
    top="6vh"
    destroy-on-close
    append-to-body
    @update:model-value="handleCandidateDialogVisible"
  >
    <CandidateSelector
      :demand-item-id="selectedDemandItem?.id || ''"
      :demand-item="selectedDemandItem"
      :opportunity-id="appendCandidateOpportunityId"
      :preselected-supply-ids="preselectedSupplyIds"
      :preselected-demand-item-ids="preselectedDemandItemIds"
      :demand-item-ids="selectedDemandItems.map(item => item.id)"
      :source="candidateSource"
      :supply-id="appendSupplyId"
      @created="handleOpportunityCreated"
      @appended="handleCandidatesAppended"
    />
  </el-dialog>

  <DetailDrawer
    ref="detailDrawerRef"
    v-model="detailDrawerVisible"
    :opportunity-id="currentOpportunityId"
    @refresh-list="emit('refresh')"
    @append-candidates="openCandidateAppend"
  />
</template>

<script setup lang="ts" name="matchOpportunityDetailDialog">
import { reactive, ref } from "vue";
import CandidateSelector from "./CandidateSelector.vue";
import DetailDrawer from "./DetailDrawer.vue";

const emit = defineEmits<{ refresh: [] }>();

const detailDrawerRef = ref();
const detailDrawerVisible = ref(false);
const currentOpportunityId = ref("");

const candidateSelectorVisible = ref(false);
const candidateSource = ref("demand");
const appendCandidateOpportunityId = ref("");
const appendSupplyId = ref("");
const selectedDemandItem = ref<any>(null);
const selectedDemandItems = ref<any[]>([]);
const preselectedSupplyIds = ref<string[]>([]);
const preselectedDemandItemIds = ref<string[]>([]);

const open = (opportunityId: string) => {
  currentOpportunityId.value = opportunityId;
  detailDrawerVisible.value = true;
};

const buildSelectorDemandItem = (opportunity: any) => {
  const initiator = (opportunity.items || []).find((i: any) => i.role === "initiator" && i.itemType === "demand")
    || (opportunity.items || []).find((i: any) => i.role === "initiator");
  return {
    id: initiator?.entityId || initiator?.demandItemId,
    productName: initiator?.productName || opportunity.productName,
    quantity: initiator?.quantity,
    remainingQuantity: initiator?.quantity,
    quantityUnit: initiator?.quantityUnit,
    specification: initiator?.specification,
    qualityRequirement: initiator?.qualityRequirement,
  };
};

const closeCandidateSelector = () => {
  selectedDemandItem.value = null;
  selectedDemandItems.value = [];
  candidateSelectorVisible.value = false;
  if (appendCandidateOpportunityId.value) {
    appendCandidateOpportunityId.value = "";
  }
};

const handleCandidateDialogVisible = (visible: boolean) => {
  if (!visible) closeCandidateSelector();
};

const handleOpportunityCreated = async (opportunityId: string) => {
  closeCandidateSelector();
  appendSupplyId.value = "";
  open(opportunityId);
  emit("refresh");
};

const handleCandidatesAppended = async (opportunityId: string) => {
  closeCandidateSelector();
  appendSupplyId.value = "";
  open(opportunityId);
  // 同一机会的详情抽屉一直开着，props 未变化不会触发重新加载，需主动重拉
  await detailDrawerRef.value?.reload();
  emit("refresh");
};

const openCandidateAppend = async (opportunity: any) => {
  // 追加不需要回溯采购单明细，直接用机会自带的品类信息构造筛选条件。
  selectedDemandItem.value = buildSelectorDemandItem(opportunity);
  selectedDemandItems.value = [];
  preselectedSupplyIds.value = [];
  preselectedDemandItemIds.value = [];
  appendSupplyId.value = (opportunity.items || []).find((c: any) => c.role === "initiator" && c.itemType === "supply")?.entityId || "";
  appendCandidateOpportunityId.value = opportunity.id;
  candidateSource.value = opportunity.source === "supply" ? "supply" : "demand";
  candidateSelectorVisible.value = true;
};

defineExpose({ open });
</script>

<template>
  <section class="candidate-selector">
    <el-form :model="filters" inline class="filter-form">
      <el-form-item label="品类"><ProductSelect v-model="filters.productName" class="product-filter" placeholder="默认需求品类，可调整" /></el-form-item>
      <el-form-item label="地区"><el-input v-model="filters.region" clearable /></el-form-item>
      <el-form-item label="可供量"><el-input-number v-model="filters.minAvailableQuantity" :min="0" controls-position="right" /></el-form-item>
      <el-form-item label="规格"><el-input v-model="filters.specification" clearable /></el-form-item>
      <el-form-item label="质量"><el-input v-model="filters.qualityRequirement" clearable /></el-form-item>
      <el-form-item label="供货周期"><el-input v-model="filters.supplyCycle" clearable /></el-form-item>
      <el-form-item label="预期价格不高于"><el-input-number v-model="filters.maxExpectedPrice" :min="0" controls-position="right" /></el-form-item>
      <el-form-item><el-button type="primary" @click="search">查询</el-button><el-button @click="reset">重置</el-button></el-form-item>
    </el-form>

    <el-table ref="supplyTableRef" :data="candidates" v-loading="loading" border row-key="herbBaseSupplyId" @selection-change="handleSelectionChange">
      <Permission code="CRM_MATCH_OPPORTUNITY_MANAGE"><el-table-column type="selection" width="52" /></Permission>
      <el-table-column prop="productName" label="供应品类" min-width="130" show-overflow-tooltip />
      <el-table-column prop="subjectName" label="主体" min-width="180" show-overflow-tooltip />
      <el-table-column prop="herbBaseName" label="基地" min-width="160" show-overflow-tooltip />
      <el-table-column label="可供量" min-width="120"><template #default="{ row }">{{ row.availableQuantity }}{{ row.quantityUnit || "" }}</template></el-table-column>
      <el-table-column prop="specification" label="规格" min-width="140" show-overflow-tooltip />
      <el-table-column prop="qualityRequirement" label="质量" min-width="140" show-overflow-tooltip />
      <el-table-column label="预期价格" min-width="130"><template #default="{ row }">{{ row.expectedPrice }}{{ row.priceUnit || "" }}</template></el-table-column>
      <el-table-column prop="supplyCycle" label="供货周期" min-width="130" show-overflow-tooltip />
      <el-table-column label="有效期" min-width="170"><template #default="{ row }">{{ formatDate(row.validUntil) }}</template></el-table-column>
    </el-table>
    <div class="selector-footer">
      <el-pagination v-model:current-page="page" v-model:page-size="pageSize" :total="total" layout="total, prev, pager, next" @current-change="loadCandidates" />
      <Permission code="CRM_MATCH_OPPORTUNITY_MANAGE"><el-button type="primary" :disabled="selectedSupplyIds.length === 0" :loading="creating" @click="submit">{{ opportunityId ? "确定" : "创建撮合机会" }}</el-button></Permission>
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed, nextTick, onMounted, reactive, ref, watch } from "vue";
import { ElMessage } from "element-plus";
import crmMatchOpportunityApi from "@/api/modules/crm/crmMatchOpportunity";
import Permission from "@/components/Permission/index.vue";
import ProductSelect from "@/views/crm/components/ProductSelect/index.vue";

const props = defineProps<{ demandItemId: string; demandItem?: any; opportunityId?: string; preselectedSupplyIds?: string[]; demandItemIds?: string[]; source?: string; replace?: boolean }>();
const emit = defineEmits<{ created: [opportunityId: string]; appended: [opportunityId: string] }>();

const supplyTableRef = ref<any>();
const loading = ref(false);
const creating = ref(false);
const candidates = ref<any[]>([]);
const selectedSupplyIds = ref<string[]>([]);
const page = ref(1);
const pageSize = ref(10);
const total = ref(0);
const progressOptionsLoading = ref(false);
const progressOptions = ref<any>({ nextActionsByStage: {} });
const nextActionForm = reactive({ nextAction: "", nextActionRemark: "", nextActionAt: "" });
const filters = reactive<any>({ productName: props.demandItem?.productName || "", region: "", minAvailableQuantity: undefined, specification: "", qualityRequirement: "", supplyCycle: "", maxExpectedPrice: undefined });
const summaryTitle = computed(() => props.source === "supply" ? "需求摘要（供应 → 需求）" : "需求摘要（需求 → 供应）");
const quantityLabel = computed(() => props.demandItem?.quantity ? `${props.demandItem.quantity}${props.demandItem.quantityUnit || ""}` : "-");
const remainingLabel = computed(() => {
  if (!props.demandItem?.quantity) return "-";
  const remaining = Number(props.demandItem.remainingQuantity ?? props.demandItem.quantity);
  return `${remaining}${props.demandItem.quantityUnit || ""}`;
});
const initialStage = "匹配核实";
const otherAction = "其他";
const initialNextActions = computed(() => progressOptions.value.nextActionsByStage?.[initialStage] || []);

const loadProgressOptions = async () => {
  progressOptionsLoading.value = true;
  try {
    const result = await crmMatchOpportunityApi.getProgressOptions();
    progressOptions.value = result.data || { nextActionsByStage: {} };
  } finally {
    progressOptionsLoading.value = false;
  }
};

const loadCandidates = async () => {
  if (!props.demandItemId) return;
  loading.value = true;
  try {
    const params = Object.fromEntries(Object.entries(filters).filter(([, value]) => value !== "" && value !== undefined && value !== null));
    const result = await crmMatchOpportunityApi.getCandidates(props.demandItemId, {
      ...params,
      ...(props.opportunityId ? { excludeOpportunityId: props.opportunityId } : {}),
      page: page.value,
      pageSize: pageSize.value
    });
    candidates.value = result.data?.list || [];
    total.value = result.data?.totalCount || 0;
    selectedSupplyIds.value = [];
    if (props.preselectedSupplyIds?.length) {
      const rows = candidates.value.filter((row: any) => props.preselectedSupplyIds!.includes(row.herbBaseSupplyId));
      selectedSupplyIds.value = rows.map((row: any) => row.herbBaseSupplyId);
      await nextTick();
      rows.forEach((row: any) => supplyTableRef.value?.toggleRowSelection(row, true));
    }
  } finally {
    loading.value = false;
  }
};

const search = () => { page.value = 1; loadCandidates(); };
const reset = () => { Object.assign(filters, { productName: props.demandItem?.productName || "", region: "", minAvailableQuantity: undefined, specification: "", qualityRequirement: "", supplyCycle: "", maxExpectedPrice: undefined }); search(); };
const handleSelectionChange = (rows: any[]) => { selectedSupplyIds.value = rows.map(row => row.herbBaseSupplyId); };
const submit = async () => {
  if (!selectedSupplyIds.value.length) return;
  if (!props.opportunityId && (!nextActionForm.nextAction || !nextActionForm.nextActionAt)) {
    ElMessage.warning("请选择下一步动作和计划时间");
    return;
  }
  if (!props.opportunityId && nextActionForm.nextAction === otherAction && !nextActionForm.nextActionRemark.trim()) {
    ElMessage.warning("请填写其他待办说明");
    return;
  }
  creating.value = true;
  try {
    if (props.opportunityId && props.replace) {
      await crmMatchOpportunityApi.replaceCandidates(props.opportunityId, { supplyIds: selectedSupplyIds.value });
      ElMessage.success("候选已重新选择");
      emit("appended", props.opportunityId);
    } else if (props.opportunityId) {
      await crmMatchOpportunityApi.addCandidates(props.opportunityId, { supplyIds: selectedSupplyIds.value });
      ElMessage.success("候选已追加");
      emit("appended", props.opportunityId);
    } else {
      const result = await crmMatchOpportunityApi.create(props.demandItemId, {
        supplyIds: selectedSupplyIds.value,
        nextAction: nextActionForm.nextAction,
        nextActionRemark: nextActionForm.nextActionRemark.trim(),
        nextActionAt: nextActionForm.nextActionAt,
        source: props.source === "supply" ? "supply" : "demand",
        demandItemIds: props.demandItemIds?.length ? props.demandItemIds : [props.demandItemId]
      });
      ElMessage.success("撮合机会已创建");
      emit("created", result.data);
    }
  } finally {
    creating.value = false;
  }
};
const formatDate = (value?: string | null) => value ? new Date(value).toLocaleString("zh-CN") : "-";

watch(() => props.demandItemId, () => {
  filters.productName = props.demandItem?.productName || "";
  page.value = 1;
  total.value = 0;
  candidates.value = [];
  selectedSupplyIds.value = [];
  Object.assign(nextActionForm, { nextAction: "", nextActionRemark: "", nextActionAt: "" });
  void loadCandidates();
});
watch(() => nextActionForm.nextAction, action => {
  if (action !== otherAction) nextActionForm.nextActionRemark = "";
});
onMounted(() => { void loadProgressOptions(); void loadCandidates(); });
</script>

<style scoped>
.candidate-selector { display: grid; gap: 16px; }
.filter-form { margin: 0; }
.product-filter { width: 240px; }
.next-action-form { display: flex; align-items: flex-start; gap: 16px; }
.next-action-form :deep(.el-form-item) { flex: 1; margin-bottom: 0; }
.selector-footer { display: flex; justify-content: space-between; align-items: center; gap: 16px; }
</style>

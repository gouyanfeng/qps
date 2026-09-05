<template>
  <section class="candidate-selector">
    <el-descriptions class="demand-summary" title="需求摘要" :column="4" border>
      <el-descriptions-item label="品类">{{ demandItem?.productName || "-" }}</el-descriptions-item>
      <el-descriptions-item label="数量">{{ quantityLabel }}</el-descriptions-item>
      <el-descriptions-item label="规格">{{ demandItem?.specification || "-" }}</el-descriptions-item>
      <el-descriptions-item label="质量">{{ demandItem?.qualityRequirement || "-" }}</el-descriptions-item>
      <el-descriptions-item label="交期">{{ formatDate(demandItem?.expectedDeliveryAt) }}</el-descriptions-item>
    </el-descriptions>

    <el-form :model="filters" inline class="filter-form">
      <el-form-item label="地区"><el-input v-model="filters.region" clearable /></el-form-item>
      <el-form-item label="可供量"><el-input-number v-model="filters.minAvailableQuantity" :min="0" controls-position="right" /></el-form-item>
      <el-form-item label="规格"><el-input v-model="filters.specification" clearable /></el-form-item>
      <el-form-item label="质量"><el-input v-model="filters.qualityRequirement" clearable /></el-form-item>
      <el-form-item label="供货周期"><el-input v-model="filters.supplyCycle" clearable /></el-form-item>
      <el-form-item label="预期价格不高于"><el-input-number v-model="filters.maxExpectedPrice" :min="0" controls-position="right" /></el-form-item>
      <el-form-item><el-button type="primary" @click="search">查询</el-button><el-button @click="reset">重置</el-button></el-form-item>
    </el-form>

    <el-table :data="candidates" v-loading="loading" border row-key="herbBaseSupplyId" @selection-change="handleSelectionChange">
      <el-table-column type="selection" width="52" />
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
      <el-button type="primary" :disabled="selectedSupplyIds.length === 0" :loading="creating" @click="createOpportunity">创建撮合机会</el-button>
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch } from "vue";
import { ElMessage } from "element-plus";
import crmMatchOpportunityApi from "@/api/modules/crmMatchOpportunity";

const props = defineProps<{ demandItemId: string; demandItem?: any }>();
const emit = defineEmits<{ created: [opportunityId: string] }>();

const loading = ref(false);
const creating = ref(false);
const candidates = ref<any[]>([]);
const selectedSupplyIds = ref<string[]>([]);
const page = ref(1);
const pageSize = ref(10);
const total = ref(0);
const filters = reactive<any>({ region: "", minAvailableQuantity: undefined, specification: "", qualityRequirement: "", supplyCycle: "", maxExpectedPrice: undefined });
const quantityLabel = computed(() => props.demandItem?.quantity ? `${props.demandItem.quantity}${props.demandItem.quantityUnit || ""}` : "-");

const loadCandidates = async () => {
  if (!props.demandItemId) return;
  loading.value = true;
  try {
    const result = await crmMatchOpportunityApi.getCandidates(props.demandItemId, { ...filters, page: page.value, pageSize: pageSize.value });
    candidates.value = result.data?.list || [];
    total.value = result.data?.totalCount || 0;
    selectedSupplyIds.value = [];
  } finally {
    loading.value = false;
  }
};

const search = () => { page.value = 1; loadCandidates(); };
const reset = () => { Object.assign(filters, { region: "", minAvailableQuantity: undefined, specification: "", qualityRequirement: "", supplyCycle: "", maxExpectedPrice: undefined }); search(); };
const handleSelectionChange = (rows: any[]) => { selectedSupplyIds.value = rows.map(row => row.herbBaseSupplyId); };
const createOpportunity = async () => {
  if (!selectedSupplyIds.value.length) return;
  creating.value = true;
  try {
    const result = await crmMatchOpportunityApi.create(props.demandItemId, { supplyIds: selectedSupplyIds.value });
    ElMessage.success("撮合机会已创建");
    emit("created", result.data);
  } finally {
    creating.value = false;
  }
};
const formatDate = (value?: string | null) => value ? new Date(value).toLocaleString("zh-CN") : "-";

watch(() => props.demandItemId, () => { page.value = 1; loadCandidates(); }, { immediate: true });
</script>

<style scoped>
.candidate-selector { display: grid; gap: 16px; }
.filter-form { margin: 0; }
.selector-footer { display: flex; justify-content: space-between; align-items: center; gap: 16px; }
</style>

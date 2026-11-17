<template>
  <section class="candidate-selector">
    <el-form :model="filters" inline class="filter-form">
      <el-form-item label="品类"><ProductSelect v-model="filters.productName" class="product-filter" :placeholder="isSupply ? '按品类筛选采购需求' : '默认需求品类，可调整'" /></el-form-item>
      <el-form-item v-if="!isSupply" label="地区"><el-input v-model="filters.region" clearable /></el-form-item>
      <el-form-item v-if="!isSupply" label="可供量"><el-input-number v-model="filters.minAvailableQuantity" :min="0" controls-position="right" /></el-form-item>
      <el-form-item label="规格"><el-input v-model="filters.specification" clearable /></el-form-item>
      <el-form-item label="质量"><el-input v-model="filters.qualityRequirement" clearable /></el-form-item>
      <el-form-item v-if="!isSupply" label="供货周期"><el-input v-model="filters.supplyCycle" clearable /></el-form-item>
      <el-form-item v-if="!isSupply" label="预期价格不高于"><el-input-number v-model="filters.maxExpectedPrice" :min="0" controls-position="right" /></el-form-item>
      <el-form-item><el-button type="primary" @click="search">查询</el-button><el-button @click="reset">重置</el-button></el-form-item>
    </el-form>

    <!-- 需求侧机会：候选是基地供应 -->
    <el-table v-if="!isSupply" ref="supplyTableRef" :data="candidates" v-loading="loading" border row-key="herbBaseSupplyId" @selection-change="handleSelectionChange">
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

    <!-- 供应侧机会：候选是药厂采购需求 -->
    <el-table v-else ref="demandTableRef" :data="candidates" v-loading="loading" border row-key="demandItemId" @selection-change="handleDemandSelectionChange">
      <Permission code="CRM_MATCH_OPPORTUNITY_MANAGE"><el-table-column type="selection" width="52" :selectable="rowSelectable" /></Permission>
      <el-table-column prop="vendorName" label="需求药厂" min-width="200" show-overflow-tooltip />
      <el-table-column prop="demandName" label="采购单" min-width="180" show-overflow-tooltip />
      <el-table-column prop="productName" label="需求品类" min-width="120" show-overflow-tooltip />
      <el-table-column label="需求数量" min-width="120"><template #default="{ row }">{{ row.quantity }}{{ row.quantityUnit || "" }}</template></el-table-column>
      <el-table-column label="目标价" min-width="120"><template #default="{ row }">{{ row.targetPrice }}{{ row.priceUnit || "" }}</template></el-table-column>
      <el-table-column prop="specification" label="规格" min-width="140" show-overflow-tooltip />
      <el-table-column prop="qualityRequirement" label="质量" min-width="140" show-overflow-tooltip />
      <el-table-column label="期望到货" min-width="170"><template #default="{ row }">{{ formatDate(row.expectedDeliveryAt) }}</template></el-table-column>
    </el-table>
    <div class="selector-footer">
      <el-pagination v-model:current-page="page" v-model:page-size="pageSize" :total="total" layout="total, prev, pager, next" @current-change="loadCandidates" />
      <Permission code="CRM_MATCH_OPPORTUNITY_MANAGE"><el-button type="primary" :disabled="!selectedCount" :loading="creating" @click="submit">{{ opportunityId ? "确定" : "创建撮合机会" }}</el-button></Permission>
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed, nextTick, onMounted, reactive, ref, watch } from "vue";
import { ElMessage } from "element-plus";
import crmMatchOpportunityApi from "@/api/modules/crm/crmMatchOpportunity";
import { crmVendorDemandApi } from "@/api/modules/crm/crmVendorDemand";
import { crmVendorApi } from "@/api/modules/crm/crmVendor";
import Permission from "@/components/Permission/index.vue";
import ProductSelect from "@/views/crm/components/ProductSelect/index.vue";

const props = defineProps<{
  demandItemId: string;
  demandItem?: any;
  opportunityId?: string;
  preselectedSupplyIds?: string[];
  preselectedDemandItemIds?: string[];
  demandItemIds?: string[];
  source?: string;
  replace?: boolean;
  supplyId?: string;
}>();
const emit = defineEmits<{ created: [opportunityId: string]; appended: [opportunityId: string] }>();

const isSupply = computed(() => props.source === "supply");
const supplyTableRef = ref<any>();
const demandTableRef = ref<any>();
const loading = ref(false);
const creating = ref(false);
const candidates = ref<any[]>([]);
const selectedSupplyIds = ref<string[]>([]);
const selectedDemandItemIds = ref<string[]>([]);
const selectedCount = computed(() => isSupply.value ? selectedDemandItemIds.value.length : selectedSupplyIds.value.length);
const page = ref(1);
const pageSize = ref(10);
const total = ref(0);
const vendorNames = ref<Record<string, string>>({});
const nextActionForm = reactive({ nextAction: "", nextActionRemark: "", nextActionAt: "" });
const filters = reactive<any>({ productName: props.demandItem?.productName || "", region: "", minAvailableQuantity: undefined, specification: "", qualityRequirement: "", supplyCycle: "", maxExpectedPrice: undefined });
const otherAction = "其他";

const rowSelectable = (row: any) => {
  // 剩余量为 0 的采购明细不可再撮合
  if (row.quantity && Number(row.remainingQuantity ?? row.quantity) <= 0) return false;
  return true;
};

const loadVendorNames = async (ids: string[]) => {
  const missing = ids.filter(id => id && !vendorNames.value[id]);
  if (!missing.length) return;
  const results = await Promise.all(missing.map(id => crmVendorApi.getVendor(id).catch(() => null)));
  const map = { ...vendorNames.value };
  missing.forEach((id, index) => {
    const data = (results[index] as any)?.data;
    if (data) map[id] = data.vendorName || data.name || "";
  });
  vendorNames.value = { ...map };
};

// 需求侧：按需求明细查基地供应（现有匹配接口）
const loadSupplyCandidates = async () => {
  if (!props.demandItemId) { candidates.value = []; total.value = 0; return; }
  const params = Object.fromEntries(Object.entries(filters).filter(([, value]) => value !== "" && value !== undefined && value !== null));
  const result = await crmMatchOpportunityApi.getCandidates(props.demandItemId, {
    ...params,
    // 重新选择时要看得到当前候选以便回显；追加时排除已有候选
    ...(props.opportunityId && !props.replace ? { excludeOpportunityId: props.opportunityId } : {}),
    page: page.value,
    pageSize: pageSize.value
  });
  candidates.value = result.data?.list || [];
  total.value = result.data?.totalCount || 0;
  const preselect = (props.preselectedSupplyIds || []).map(String);
  selectedSupplyIds.value = candidates.value.filter((row: any) => preselect.includes(String(row.herbBaseSupplyId))).map((row: any) => row.herbBaseSupplyId);
  if (preselect.length) {
    await nextTick();
    candidates.value.forEach((row: any) => supplyTableRef.value?.toggleRowSelection(row, preselect.includes(String(row.herbBaseSupplyId))));
  }
};

// 供应侧：直接查采购列表（现有接口），把有效明细展开成候选
const loadDemandCandidates = async () => {
  const params: any = { pageIndex: page.value, pageSize: pageSize.value, status: "有效" };
  if (filters.productName) params.productName = filters.productName;
  const result = await crmVendorDemandApi.getList(params);
  const demands = result.data?.list || [];
  total.value = result.data?.totalCount || 0;

  const rows: any[] = [];
  for (const demand of demands) {
    for (const item of demand.items || []) {
      if (filters.productName && item.productName !== filters.productName) continue;
      if (filters.specification && !(item.specification || "").includes(filters.specification)) continue;
      if (filters.qualityRequirement && !(item.qualityRequirement || "").includes(filters.qualityRequirement)) continue;
      rows.push({
        ...item,
        demandItemId: item.id,
        demandId: demand.id,
        demandName: demand.demandName,
        vendorId: demand.vendorId,
        vendorName: vendorNames.value[demand.vendorId] || demand.vendorName || "",
        expectedDeliveryAt: item.expectedDeliveryAt || demand.expectedDeliveryAt,
      });
    }
  }
  // 采购列表按单分页，明细展开后条数可能超过 pageSize，前端不再二次分页
  candidates.value = rows;
  await loadVendorNames(rows.map(row => row.vendorId).filter(Boolean));
  rows.forEach(row => { if (vendorNames.value[row.vendorId]) row.vendorName = vendorNames.value[row.vendorId]; });

  const preselect = (props.preselectedDemandItemIds || []).map(String);
  selectedDemandItemIds.value = rows.filter(row => preselect.includes(String(row.demandItemId))).map(row => row.demandItemId);
  if (preselect.length) {
    await nextTick();
    rows.forEach((row: any) => demandTableRef.value?.toggleRowSelection(row, preselect.includes(String(row.demandItemId))));
  }
};

const loadCandidates = async () => {
  loading.value = true;
  try {
    if (isSupply.value) await loadDemandCandidates();
    else await loadSupplyCandidates();
  } catch (error: any) {
    if (error?.code !== "ERR_CANCELED") ElMessage.error("加载候选列表失败");
  } finally {
    loading.value = false;
  }
};

const search = () => { page.value = 1; void loadCandidates(); };
const reset = () => { Object.assign(filters, { productName: props.demandItem?.productName || "", region: "", minAvailableQuantity: undefined, specification: "", qualityRequirement: "", supplyCycle: "", maxExpectedPrice: undefined }); search(); };
const handleSelectionChange = (rows: any[]) => { selectedSupplyIds.value = rows.map(row => row.herbBaseSupplyId); };
const handleDemandSelectionChange = (rows: any[]) => { selectedDemandItemIds.value = rows.map(row => row.demandItemId); };

const submit = async () => {
  if (!selectedCount.value) return;
  creating.value = true;
  try {
    if (props.opportunityId && props.replace) {
      await crmMatchOpportunityApi.replaceCandidates(props.opportunityId, isSupply.value
        ? { demandItemIds: selectedDemandItemIds.value }
        : { supplyIds: selectedSupplyIds.value });
      ElMessage.success("候选已重新选择");
      emit("appended", props.opportunityId);
    } else if (props.opportunityId) {
      await crmMatchOpportunityApi.addCandidates(props.opportunityId, isSupply.value
        ? { demandItemIds: selectedDemandItemIds.value }
        : { supplyIds: selectedSupplyIds.value });
      ElMessage.success("候选已追加");
      emit("appended", props.opportunityId);
    } else {
      const result = await crmMatchOpportunityApi.create(props.demandItemId, {
        supplyIds: selectedSupplyIds.value,
        nextAction: nextActionForm.nextAction,
        nextActionRemark: nextActionForm.nextActionRemark.trim(),
        nextActionAt: nextActionForm.nextActionAt,
        source: isSupply.value ? "supply" : "demand",
        demandItemIds: isSupply.value ? selectedDemandItemIds.value : (props.demandItemIds?.length ? props.demandItemIds : [props.demandItemId])
      });
      ElMessage.success("撮合机会已创建");
      emit("created", result.data);
    }
  } finally {
    creating.value = false;
  }
};
const formatDate = (value?: string | null) => value ? new Date(value).toLocaleString("zh-CN") : "-";

watch(() => [props.demandItemId, props.opportunityId, props.replace, props.preselectedSupplyIds, props.preselectedDemandItemIds], () => {
  filters.productName = props.demandItem?.productName || "";
  page.value = 1;
  total.value = 0;
  candidates.value = [];
  selectedSupplyIds.value = [];
  selectedDemandItemIds.value = [];
  void loadCandidates();
}, { deep: true });
onMounted(() => { void loadCandidates(); });
</script>

<style scoped>
.candidate-selector { display: grid; gap: 16px; }
.filter-form { margin: 0; }
.product-filter { width: 240px; }
.selector-footer { display: flex; justify-content: space-between; align-items: center; gap: 16px; }
</style>

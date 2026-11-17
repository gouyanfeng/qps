<template>
  <div class="match-opportunity-page">
    <el-dialog
      :model-value="candidateSelectorVisible"
      :title="replaceCandidateMode ? '重新选择候选' : '候选筛选'"
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
        :replace="replaceCandidateMode"
        :supply-id="appendSupplyId"
        @created="handleOpportunityCreated"
        @appended="handleCandidatesAppended"
      />
    </el-dialog>

    <QueryPage ref="queryPageRef" api="/admin/crm/match-opportunities" :search-param="searchForm" @reset="handleReset">
      <template #searchConditions>
        <el-form :model="searchForm" :inline="true">
          <el-form-item label="状态">
            <el-select v-model="searchForm.status" clearable placeholder="全部状态">
              <el-option v-for="status in statuses" :key="status" :label="status" :value="status" />
            </el-select>
          </el-form-item>
          <el-form-item label="发起方向">
            <el-select v-model="searchForm.source" clearable placeholder="全部方向">
              <el-option label="药厂" value="demand" />
              <el-option label="基地" value="supply" />
            </el-select>
          </el-form-item>
          <el-form-item label="品类"><el-input v-model="searchForm.productName" clearable placeholder="请输入品类" /></el-form-item>
          <el-form-item label="需求厂商">
            <el-select v-model="searchForm.vendorId" filterable remote clearable placeholder="请选择厂商" :remote-method="loadVendorOptions">
              <el-option v-for="vendor in vendorOptions" :key="vendor.id" :label="vendor.vendorName" :value="vendor.id" />
            </el-select>
          </el-form-item>
          <el-form-item label="基地主体">
            <el-select v-model="searchForm.herbBaseSubjectId" filterable remote clearable placeholder="请选择主体" :remote-method="loadSubjectOptions">
              <el-option v-for="subject in subjectOptions" :key="subject.id" :label="subject.subjectName" :value="subject.id" />
            </el-select>
          </el-form-item>
          <el-form-item label="撮合跟进人">
            <el-select v-model="searchForm.ownerUserId" filterable remote clearable placeholder="请选择跟进人" :remote-method="loadOwnerOptions">
              <el-option v-for="user in ownerOptions" :key="user.id" :label="getUserDisplayName(user)" :value="user.id" />
            </el-select>
          </el-form-item>
          <el-form-item label="下一步时间">
            <el-date-picker v-model="nextActionAtRange" type="datetimerange" value-format="YYYY-MM-DDTHH:mm:ss" range-separator="至" start-placeholder="开始时间" end-placeholder="结束时间" />
          </el-form-item>
        </el-form>
      </template>

      <template #table="{ tableData }">
        <el-table :data="tableData" :row-key="'id'" class="wide-list-table" style="--table-min-width: 1410px" border>
  
          <el-table-column label="发起方" min-width="220" show-overflow-tooltip>
            <template #default="{ row }">      
               <el-tag size="small" effect="dark" style="margin-left: 8px" :style="row.source === 'supply'
                ? { background: '#f59e0b', borderColor: '#f59e0b', color: '#fff' }
                : { background: '#16a34a', borderColor: '#16a34a', color: '#fff' }">{{ directionLabel(row) }}</el-tag>
              <span>{{ row.initiatorName || "-" }}</span>
       
            </template>
          </el-table-column>
                  <el-table-column prop="demandProductName" label="品类" min-width="130" show-overflow-tooltip />
          <el-table-column prop="candidateNames" label="候选名单" min-width="220" show-overflow-tooltip />
          <el-table-column label="需求数量" width="130"><template #default="{ row }">{{ quantityLabel(row) }}</template></el-table-column>
          <el-table-column label="候选数" width="90" align="center"><template #default="{ row }">{{ row.candidateCount }}</template></el-table-column>
          <el-table-column label="状态" width="100" align="center"><template #default="{ row }"><el-tag size="small" :type="statusType(row)">{{ displayStatus(row) }}</el-tag></template></el-table-column>
          <el-table-column prop="stage" label="撮合阶段" width="120" show-overflow-tooltip />
          <el-table-column prop="ownerUserName" label="撮合跟进人" width="120" show-overflow-tooltip />
          <el-table-column label="当前待办" min-width="220" show-overflow-tooltip>
            <template #default="{ row }">
              <span>{{ row.nextAction || '-' }}</span>
              <span v-if="row.nextActionRemark">（{{ row.nextActionRemark }}）</span>
              <span v-if="row.nextActionAt" class="next-action-time">{{ formatDate(row.nextActionAt) }}</span>
            </template>
          </el-table-column>
          <el-table-column label="更新时间" width="180"><template #default="{ row }">{{ formatDate(row.updatedAt) }}</template></el-table-column>
          <el-table-column label="操作" width="150" fixed="right" class-name="actions-column" header-class-name="actions-column">
            <template #default="{ row }">
              <Permission code="CRM_MATCH_OPPORTUNITY_MANAGE"><el-button v-if="row.status !== '已关闭'" link type="primary" @click="openProgress(row)">推进</el-button></Permission>
              <Permission code="CRM_MATCH_OPPORTUNITY_VIEW"><el-button link type="primary" @click="openDetail(row.id)">详情</el-button></Permission>
            </template>
          </el-table-column>
        </el-table>
      </template>
    </QueryPage>

    <DemandItemPickerDialog v-model="demandItemPickerVisible" :demand="currentDemand" :source="matchSource" @confirm="handleDemandItemSelected" />
    <DetailDrawer v-model="detailDrawerVisible" :opportunity-id="currentOpportunityId" @refresh-list="reloadList" @append-candidates="openCandidateAppend" @replace-candidates="openCandidateReplace" />
    <ProgressDialog v-model="progressDialogVisible" :opportunity="progressOpportunity" @submitted="reloadList" />
  </div>
</template>

<script setup lang="ts">
import { computed, onActivated, onMounted, reactive, ref, watch } from "vue";
import { useRoute, useRouter } from "vue-router";
import { ElMessage } from "element-plus";
import QueryPage from "@/components/QueryPage/index.vue";
import Permission from "@/components/Permission/index.vue";
import crmVendorDemandApi from "@/api/modules/crm/crmVendorDemand";
import { crmVendorApi } from "@/api/modules/crm/crmVendor";
import { crmHerbBaseApi } from "@/api/modules/crm/crmHerbBase";
import userApi from "@/api/modules/system/user";
import DemandItemPickerDialog from "./DemandItemPickerDialog.vue";
import CandidateSelector from "./CandidateSelector.vue";
import DetailDrawer from "./DetailDrawer.vue";
import ProgressDialog from "./ProgressDialog.vue";

const route = useRoute();
const router = useRouter();
const queryPageRef = ref();
const demandItemPickerVisible = ref(false);
const candidateSelectorVisible = ref(false);
const detailDrawerVisible = ref(false);
const progressDialogVisible = ref(false);
const currentDemand = ref<any>(null);
const selectedDemandItem = ref<any>(null);
const selectedDemandItems = ref<any[]>([]);
const currentOpportunityId = ref("");
const progressOpportunity = ref<any>(null);
const appendCandidateOpportunityId = ref("");
const replaceCandidateMode = ref(false);
const preselectedSupplyIds = ref<string[]>([]);
const preselectedDemandItemIds = ref<string[]>([]);
const appendSupplyId = ref("");
const candidateSource = ref("demand");
const nextActionAtRange = ref<string[]>([]);
const vendorOptions = ref<any[]>([]);
const subjectOptions = ref<any[]>([]);
const ownerOptions = ref<any[]>([]);
const statuses = ["推进中", "暂停", "已关闭"];
const matchSource = computed(() => (String(route.query.source || "") === "supply" || route.query.supplyId ? "supply" : "demand"));
const searchForm = reactive<any>({ status: "", source: "", productName: "", vendorId: "", herbBaseSubjectId: "", ownerUserId: "", nextActionAtFrom: "", nextActionAtTo: "" });
let loadingDemandKey = "";

const getUserDisplayName = (user: any) => user.realName || user.username || user.name || "-";
const quantityLabel = (row: any) => row.demandQuantity === null || row.demandQuantity === undefined ? "-" : `${row.demandQuantity}${row.quantityUnit || ""}`;
const directionLabel = (row: any) => row?.source === "supply" ? "基地" : "药厂";
const formatDate = (value?: string | null) => value ? new Date(value).toLocaleString("zh-CN") : "-";
// 已关闭 + 撮合成功 展示为「已成交」，避免把成交数据看成失败关闭
const isDeal = (row: any) => row?.status === "已关闭" && row?.closeResult === "撮合成功";
const displayStatus = (row: any) => isDeal(row) ? "已成交" : (row?.status || "-");
const statusType = (row: any) => isDeal(row)
  ? "success"
  : ({ "推进中": "success", "暂停": "warning", "已关闭": "info" }[row?.status || ""] || undefined);
const reloadList = async () => await queryPageRef.value?.getTableList();

const loadVendorOptions = async (keyword = "") => {
  const result = await crmVendorApi.getVendorList({ page: 1, pageSize: 50, keyword });
  vendorOptions.value = result.data?.list || [];
};
const loadSubjectOptions = async (keyword = "") => {
  const result = await crmHerbBaseApi.getSubjectList({ page: 1, pageSize: 50, keyword });
  subjectOptions.value = result.data?.list || [];
};
const loadOwnerOptions = async () => {
  const result = await userApi.getUserList({ page: 1, pageSize: 100, username: "", realName: "", roleId: "", isActive: true });
  ownerOptions.value = (result.data?.list || []).filter((user: any) => user.isActive);
};

const loadDemandFromRoute = async () => {
  const demandId = String(route.query.demandId || "");
  const supplyId = String(route.query.supplyId || "");
  if (!demandId && !supplyId) {
    demandItemPickerVisible.value = false;
    currentDemand.value = null;
    selectedDemandItem.value = null;
    candidateSelectorVisible.value = false;
    appendCandidateOpportunityId.value = "";
    preselectedSupplyIds.value = [];
    return;
  }

  const key = supplyId ? `supply:${supplyId}` : `demand:${demandId}`;
  if (loadingDemandKey === key) return;
  loadingDemandKey = key;

  demandItemPickerVisible.value = false;
  selectedDemandItem.value = null;
  candidateSelectorVisible.value = false;
  appendCandidateOpportunityId.value = "";
  preselectedSupplyIds.value = supplyId ? [supplyId] : [];
  try {
    if (supplyId) {
      // 从供应侧发起：列出仍有剩余量的采购明细，优先展示同品类。
      const result = await crmVendorDemandApi.getList({ page: 1, pageSize: 100, status: "有效" });
      if (String(route.query.supplyId || "") !== supplyId) return;
      const productName = String(route.query.productName || "");
      const items = ((result.data?.list || []) as any[]).flatMap(demand =>
        (demand.items || [])
          .filter((item: any) => Number(item.quantity || 0) <= 0 || Number(item.remainingQuantity ?? 0) > 0)
          .map((item: any) => ({ ...item, demandName: demand.demandName, demandNo: demand.demandNo, vendorName: demand.vendorName })));
      items.sort((a, b) => Number(b.productName === productName) - Number(a.productName === productName));
      currentDemand.value = { items };
      demandItemPickerVisible.value = items.length > 0;
      if (!items.length) ElMessage.warning("暂无待撮合的采购列表");
      return;
    }

    const result = await crmVendorDemandApi.getDetail(demandId);
    if (String(route.query.demandId || "") !== demandId) return;
    const demand = result.data?.list?.[0] || null;
    if (demand?.items) {
      demand.items = demand.items.map((item: any) => ({ ...item, demandName: item.demandName || demand.demandName }));
    }
    currentDemand.value = demand;
    demandItemPickerVisible.value = !!currentDemand.value;
  } catch (error: any) {
    if (error?.code !== "ERR_CANCELED") {
      console.error("加载采购列表失败:", error);
    }
  } finally {
    if (loadingDemandKey === key) loadingDemandKey = "";
  }
};

const handleDemandItemSelected = (items: any | any[]) => {
  const list = Array.isArray(items) ? items : [items];
  if (!list.length) return;
  if (list.length === 1) {
    const item = list[0];
    if (["推进中", "暂停", "撮合中", "已暂停"].includes(item.matchOpportunityStatus || item.matchDisplayStatus) && item.matchOpportunityId) {
      openDetail(item.matchOpportunityId);
      return;
    }
    if (Number(item.quantity || 0) > 0 && Number(item.remainingQuantity ?? 0) <= 0) {
      ElMessage.warning("该采购明细已采购完成，无需继续撮合");
      return;
    }
  }

  selectedDemandItems.value = list;
  selectedDemandItem.value = list[0];
  candidateSelectorVisible.value = true;
};
const closeCandidateSelector = async () => {
  selectedDemandItem.value = null;
  selectedDemandItems.value = [];
  candidateSelectorVisible.value = false;
  if (appendCandidateOpportunityId.value) {
    const opportunityId = appendCandidateOpportunityId.value;
    appendCandidateOpportunityId.value = "";
    replaceCandidateMode.value = false;
    openDetail(opportunityId);
    return;
  }
  // 弹窗直接关闭时不做路由跳转，仅清理选择态
};
const handleCandidateDialogVisible = (visible: boolean) => {
  if (!visible) void closeCandidateSelector();
};
const handleOpportunityCreated = async (opportunityId: string) => {
  selectedDemandItem.value = null;
  selectedDemandItems.value = [];
  candidateSelectorVisible.value = false;
  appendCandidateOpportunityId.value = "";
  currentDemand.value = null;
  openDetail(opportunityId);
  await reloadList();
};
const handleCandidatesAppended = async (opportunityId: string) => {
  selectedDemandItem.value = null;
  selectedDemandItems.value = [];
  candidateSelectorVisible.value = false;
  appendCandidateOpportunityId.value = "";
  replaceCandidateMode.value = false;
  await reloadList();
  openDetail(opportunityId);
};
const buildSelectorDemandItem = (opportunity: any) => ({
  id: opportunity.demandItemId,
  productName: opportunity.demandProductName,
  quantity: opportunity.demandQuantity,
  remainingQuantity: opportunity.demandQuantity,
  quantityUnit: opportunity.quantityUnit,
  specification: opportunity.specification,
  qualityRequirement: opportunity.qualityRequirement,
});
const openCandidateAppend = async (opportunity: any) => {
  // 追加不需要回溯采购单明细，直接用机会自带的品类信息构造筛选条件。
  // 空机会没有 demandItemId/demandId，不能再去查采购单明细，否则弹窗打不开。
  selectedDemandItem.value = buildSelectorDemandItem(opportunity);
  selectedDemandItems.value = [];
  preselectedSupplyIds.value = [];
  preselectedDemandItemIds.value = [];
  appendSupplyId.value = opportunity.candidates?.find((c: any) => c.role === "initiator")?.herbBaseSupplyId || "";
  appendCandidateOpportunityId.value = opportunity.id;
  candidateSource.value = opportunity.source === "supply" ? "supply" : "demand";
  replaceCandidateMode.value = false;
  candidateSelectorVisible.value = true;
};
const openCandidateReplace = (opportunity: any) => {
  currentOpportunityId.value = opportunity.id;
  selectedDemandItem.value = buildSelectorDemandItem(opportunity);
  selectedDemandItems.value = [];
  // 重选回显当前候选：需求侧带出候选基地供应，供应侧带出候选药厂需求明细
  preselectedSupplyIds.value = (opportunity.candidates || [])
    .filter((c: any) => c.role === "candidate")
    .map((c: any) => c.herbBaseSupplyId);
  preselectedDemandItemIds.value = (opportunity.demands || [])
    .filter((d: any) => d.role === "candidate")
    .map((d: any) => d.demandItemId);
  appendSupplyId.value = (opportunity.candidates || [])
    .find((c: any) => c.role === "initiator")?.herbBaseSupplyId || "";
  appendCandidateOpportunityId.value = opportunity.id;
  candidateSource.value = opportunity.source === "supply" ? "supply" : "demand";
  replaceCandidateMode.value = true;
  candidateSelectorVisible.value = true;
};
const openDetail = (opportunityId: string) => {
  currentOpportunityId.value = opportunityId;
  detailDrawerVisible.value = true;
};
const openProgress = (opportunity: any) => {
  progressOpportunity.value = opportunity;
  progressDialogVisible.value = true;
};
const replaceWithoutRouteParams = () => {
  const { demandId: _demandId, detailId: _detailId, supplyId: _supplyId, productName: _productName, source: _source, ...query } = route.query;
  return router.replace({ path: route.path, query });
};
const handleReset = () => {
  Object.assign(searchForm, { status: "", source: "", productName: "", vendorId: "", herbBaseSubjectId: "", ownerUserId: "", nextActionAtFrom: "", nextActionAtTo: "" });
  nextActionAtRange.value = [];
};

watch(nextActionAtRange, range => {
  searchForm.nextActionAtFrom = range?.[0] || "";
  searchForm.nextActionAtTo = range?.[1] || "";
});
watch(detailDrawerVisible, visible => {
  if (!visible && !candidateSelectorVisible.value && (route.query.demandId || route.query.detailId)) {
    void replaceWithoutRouteParams();
  }
});
watch(() => ({ path: route.path, detailId: route.query.detailId }), ({ path, detailId }) => {
  if (path !== "/crm/match-opportunity") return;
  if (typeof detailId === "string" && detailId) openDetail(detailId);
}, { immediate: true });
watch(() => [route.query.demandId, route.query.supplyId], () => { void loadDemandFromRoute(); }, { immediate: true });
onActivated(() => { void loadDemandFromRoute(); });
onMounted(() => { void loadVendorOptions(); void loadSubjectOptions(); void loadOwnerOptions(); });
</script>

<style scoped>
.candidate-panel { padding: 20px; }
.candidate-panel__head { display: flex; align-items: center; justify-content: space-between; margin-bottom: 16px; font-weight: 600; }
.candidate-direction { display: inline-flex; align-items: center; gap: 8px; }
.next-action-time { display: block; margin-top: 4px; color: var(--el-text-color-secondary); font-size: 12px; }
</style>
<template>
  <div class="match-opportunity-page">
    <div v-if="candidateSelectorVisible" class="card candidate-panel">
      <div class="candidate-panel__head">
        <span>候选筛选</span>
        <el-button link @click="closeCandidateSelector">返回机会列表</el-button>
      </div>
      <CandidateSelector
        :demand-item-id="selectedDemandItem?.id || ''"
        :demand-item="selectedDemandItem"
        :opportunity-id="appendCandidateOpportunityId"
        @created="handleOpportunityCreated"
        @appended="handleCandidatesAppended"
      />
    </div>

    <QueryPage v-show="!candidateSelectorVisible" ref="queryPageRef" api="/admin/crm/match-opportunities" :search-param="searchForm" @reset="handleReset">
      <template #searchConditions>
        <el-form :model="searchForm" :inline="true">
          <el-form-item label="状态">
            <el-select v-model="searchForm.status" clearable placeholder="全部状态">
              <el-option v-for="status in statuses" :key="status" :label="status" :value="status" />
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
          <el-table-column prop="demandProductName" label="品类" min-width="130" show-overflow-tooltip />
          <el-table-column prop="vendorName" label="需求厂商" min-width="180" show-overflow-tooltip />
          <el-table-column label="需求数量" width="130"><template #default="{ row }">{{ quantityLabel(row) }}</template></el-table-column>
          <el-table-column label="候选数" width="90" align="center"><template #default="{ row }">{{ row.candidateCount }}</template></el-table-column>
          <el-table-column label="状态" width="100" align="center"><template #default="{ row }"><el-tag size="small" :type="statusType(row.status)">{{ row.status || '-' }}</el-tag></template></el-table-column>
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

    <DemandItemPickerDialog v-model="demandItemPickerVisible" :demand="currentDemand" @confirm="handleDemandItemSelected" />
    <DetailDrawer v-model="detailDrawerVisible" :opportunity-id="currentOpportunityId" @refresh-list="reloadList" @append-candidates="openCandidateAppend" />
    <ProgressDialog v-model="progressDialogVisible" :opportunity="progressOpportunity" @submitted="reloadList" />
  </div>
</template>

<script setup lang="ts">
import { onActivated, onMounted, reactive, ref, watch } from "vue";
import { useRoute, useRouter } from "vue-router";
import QueryPage from "@/components/QueryPage/index.vue";
import Permission from "@/components/Permission/index.vue";
import crmVendorDemandApi from "@/api/modules/crmVendorDemand";
import { crmVendorApi } from "@/api/modules/crmVendor";
import { crmHerbBaseApi } from "@/api/modules/crmHerbBase";
import userApi from "@/api/modules/user";
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
const currentOpportunityId = ref("");
const progressOpportunity = ref<any>(null);
const appendCandidateOpportunityId = ref("");
const nextActionAtRange = ref<string[]>([]);
const vendorOptions = ref<any[]>([]);
const subjectOptions = ref<any[]>([]);
const ownerOptions = ref<any[]>([]);
const statuses = ["推进中", "暂停", "已关闭"];
const searchForm = reactive<any>({ status: "", productName: "", vendorId: "", herbBaseSubjectId: "", ownerUserId: "", nextActionAtFrom: "", nextActionAtTo: "" });
let loadingDemandId = "";

const getUserDisplayName = (user: any) => user.realName || user.username || user.name || "-";
const quantityLabel = (row: any) => row.demandQuantity === null || row.demandQuantity === undefined ? "-" : `${row.demandQuantity}${row.quantityUnit || ""}`;
const formatDate = (value?: string | null) => value ? new Date(value).toLocaleString("zh-CN") : "-";
const statusType = (status?: string) => ({ "推进中": "success", "暂停": "warning", "已关闭": "info" }[status || ""] || undefined);
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
  if (!demandId) {
    demandItemPickerVisible.value = false;
    currentDemand.value = null;
    selectedDemandItem.value = null;
    candidateSelectorVisible.value = false;
    appendCandidateOpportunityId.value = "";
    return;
  }

  if (loadingDemandId === demandId) return;
  loadingDemandId = demandId;

  demandItemPickerVisible.value = false;
  selectedDemandItem.value = null;
  candidateSelectorVisible.value = false;
  appendCandidateOpportunityId.value = "";
  try {
    const result = await crmVendorDemandApi.getDetail(demandId);
    if (String(route.query.demandId || "") !== demandId) return;
    currentDemand.value = result.data?.list?.[0] || null;
    demandItemPickerVisible.value = !!currentDemand.value;
  } catch (error: any) {
    if (error?.code !== "ERR_CANCELED") {
      console.error("加载采购需求失败:", error);
    }
  } finally {
    if (loadingDemandId === demandId) loadingDemandId = "";
  }
};

const handleDemandItemSelected = (item: any) => {
  if (item.matchOpportunityId && ["推进中", "暂停"].includes(item.matchOpportunityStatus)) {
    openDetail(item.matchOpportunityId);
    return;
  }

  selectedDemandItem.value = item;
  candidateSelectorVisible.value = true;
};
const closeCandidateSelector = async () => {
  selectedDemandItem.value = null;
  candidateSelectorVisible.value = false;
  if (appendCandidateOpportunityId.value) {
    const opportunityId = appendCandidateOpportunityId.value;
    appendCandidateOpportunityId.value = "";
    openDetail(opportunityId);
    return;
  }
  await replaceWithoutRouteParams();
};
const handleOpportunityCreated = async (opportunityId: string) => {
  selectedDemandItem.value = null;
  candidateSelectorVisible.value = false;
  appendCandidateOpportunityId.value = "";
  currentDemand.value = null;
  openDetail(opportunityId);
  await reloadList();
};
const handleCandidatesAppended = async (opportunityId: string) => {
  selectedDemandItem.value = null;
  candidateSelectorVisible.value = false;
  appendCandidateOpportunityId.value = "";
  await reloadList();
  openDetail(opportunityId);
};
const openCandidateAppend = async (opportunity: any) => {
  const result = await crmVendorDemandApi.getDetail(opportunity.demandId);
  const demand = result.data?.list?.[0] || null;
  const demandItem = demand?.items?.find((item: any) => item.id === opportunity.demandItemId);
  if (!demandItem) return;

  currentDemand.value = demand;
  selectedDemandItem.value = demandItem;
  appendCandidateOpportunityId.value = opportunity.id;
  detailDrawerVisible.value = false;
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
  const { demandId: _demandId, detailId: _detailId, ...query } = route.query;
  return router.replace({ path: route.path, query });
};
const handleReset = () => {
  Object.assign(searchForm, { status: "", productName: "", vendorId: "", herbBaseSubjectId: "", ownerUserId: "", nextActionAtFrom: "", nextActionAtTo: "" });
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
watch(() => route.query.demandId, () => { void loadDemandFromRoute(); }, { immediate: true });
onActivated(() => { void loadDemandFromRoute(); });
onMounted(() => { void loadVendorOptions(); void loadSubjectOptions(); void loadOwnerOptions(); });
</script>

<style scoped>
.candidate-panel { padding: 20px; }
.candidate-panel__head { display: flex; align-items: center; justify-content: space-between; margin-bottom: 16px; font-weight: 600; }
.next-action-time { display: block; margin-top: 4px; color: var(--el-text-color-secondary); font-size: 12px; }
</style>
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
        @created="handleOpportunityCreated"
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
        <el-table :data="tableData" :row-key="'id'" class="wide-list-table" style="--table-min-width: 1230px" border>
          <el-table-column prop="demandProductName" label="品类" min-width="130" show-overflow-tooltip />
          <el-table-column prop="vendorName" label="需求厂商" min-width="180" show-overflow-tooltip />
          <el-table-column label="需求数量" width="130"><template #default="{ row }">{{ quantityLabel(row) }}</template></el-table-column>
          <el-table-column label="候选数" width="90" align="center"><template #default="{ row }">{{ row.candidateCount }}</template></el-table-column>
          <el-table-column label="状态" width="100" align="center"><template #default="{ row }"><el-tag size="small" :type="statusType(row.status)">{{ row.status || '-' }}</el-tag></template></el-table-column>
          <el-table-column prop="ownerUserName" label="撮合跟进人" width="120" show-overflow-tooltip />
          <el-table-column label="下一步时间" width="180"><template #default="{ row }">{{ formatDate(row.nextActionAt) }}</template></el-table-column>
          <el-table-column label="更新时间" width="180"><template #default="{ row }">{{ formatDate(row.updatedAt) }}</template></el-table-column>
          <el-table-column label="操作" width="90" fixed="right">
            <template #default="{ row }"><Permission code="CRM_MATCH_OPPORTUNITY_VIEW"><el-button link type="primary" @click="openDetail(row.id)">详情</el-button></Permission></template>
          </el-table-column>
        </el-table>
      </template>
    </QueryPage>

    <DemandItemPickerDialog v-model="demandItemPickerVisible" :demand="currentDemand" @confirm="handleDemandItemSelected" />
    <DetailDrawer v-model="detailDrawerVisible" :opportunity-id="currentOpportunityId" @refresh-list="reloadList" />
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref, watch } from "vue";
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

const route = useRoute();
const router = useRouter();
const queryPageRef = ref();
const demandItemPickerVisible = ref(false);
const candidateSelectorVisible = ref(false);
const detailDrawerVisible = ref(false);
const currentDemand = ref<any>(null);
const selectedDemandItem = ref<any>(null);
const currentOpportunityId = ref("");
const nextActionAtRange = ref<string[]>([]);
const vendorOptions = ref<any[]>([]);
const subjectOptions = ref<any[]>([]);
const ownerOptions = ref<any[]>([]);
const statuses = ["推进中", "暂停", "已关闭"];
const searchForm = reactive<any>({ status: "", productName: "", vendorId: "", herbBaseSubjectId: "", ownerUserId: "", nextActionAtFrom: "", nextActionAtTo: "" });

const getUserDisplayName = (user: any) => user.realName || user.username || user.name || "-";
const quantityLabel = (row: any) => row.demandQuantity === null || row.demandQuantity === undefined ? "-" : `${row.demandQuantity}${row.quantityUnit || ""}`;
const formatDate = (value?: string | null) => value ? new Date(value).toLocaleString("zh-CN") : "-";
const statusType = (status?: string) => ({ "推进中": "success", "暂停": "warning", "已关闭": "info" }[status || ""] || "");
const reloadList = () => queryPageRef.value?.getTableList();

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
  if (!demandId) return;
  const result = await crmVendorDemandApi.getDetail(demandId);
  currentDemand.value = result.data;
  selectedDemandItem.value = null;
  candidateSelectorVisible.value = false;
  demandItemPickerVisible.value = true;
};

const handleDemandItemSelected = (item: any) => {
  selectedDemandItem.value = item;
  candidateSelectorVisible.value = true;
};
const closeCandidateSelector = async () => {
  selectedDemandItem.value = null;
  candidateSelectorVisible.value = false;
  await replaceWithoutDemandId();
};
const handleOpportunityCreated = async (opportunityId: string) => {
  selectedDemandItem.value = null;
  candidateSelectorVisible.value = false;
  currentDemand.value = null;
  await replaceWithoutDemandId();
  await reloadList();
  openDetail(opportunityId);
};
const openDetail = (opportunityId: string) => {
  currentOpportunityId.value = opportunityId;
  detailDrawerVisible.value = true;
};
const replaceWithoutDemandId = () => {
  const { demandId: _demandId, ...query } = route.query;
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
watch(() => route.query.demandId, () => { void loadDemandFromRoute(); }, { immediate: true });
onMounted(() => { void loadVendorOptions(); void loadSubjectOptions(); void loadOwnerOptions(); });
</script>

<style scoped>
.candidate-panel { padding: 20px; }
.candidate-panel__head { display: flex; align-items: center; justify-content: space-between; margin-bottom: 16px; font-weight: 600; }
</style>

<template>
  <el-drawer :model-value="modelValue" size="82%" :with-header="false" @update:model-value="handleVisibleChange">
    <div v-loading="loading" class="detail-drawer">
      <template v-if="currentOpportunity">
        <section class="drawer-head">
          <div>
            <div class="detail-kicker">撮合机会详情</div>
            <div class="title-row">
              <h2>{{ currentOpportunity.demandProductName || "撮合机会" }}</h2>
              <el-tag :type="statusType(currentOpportunity)">{{ displayStatus(currentOpportunity) }}</el-tag>
              <el-tag effect="plain" :type="currentOpportunity.source === 'supply' ? 'warning' : 'primary'">{{ directionLabel }}</el-tag>
            </div>
            <div class="head-meta"><span>需求厂商 {{ currentOpportunity.vendorName || "-" }}</span><span>跟进人 {{ currentOpportunity.ownerUserName || "-" }}</span></div>
          </div>
          <div class="head-actions">
            <Permission code="CRM_MATCH_OPPORTUNITY_MANAGE"><el-button v-if="canMaintainCandidates" @click="requestReplaceCandidates">重新选择候选</el-button></Permission>
            <Permission code="CRM_MATCH_OPPORTUNITY_MANAGE"><el-button v-if="canMaintainCandidates" @click="emit('append-candidates', currentOpportunity)">追加候选</el-button></Permission>
            <Permission code="CRM_MATCH_OPPORTUNITY_MANAGE"><el-button v-if="canAdvance" type="primary" @click="progressDialogVisible = true">推进机会</el-button></Permission>
            <Permission code="CRM_MATCH_OPPORTUNITY_MANAGE"><el-button v-if="currentOpportunity.status === '推进中'" type="warning" @click="updateStatus('暂停')">暂停</el-button></Permission>
            <Permission code="CRM_MATCH_OPPORTUNITY_MANAGE"><el-button v-if="currentOpportunity.status !== '已关闭'" type="danger" @click="closeOpportunity">关闭</el-button></Permission>
            <el-button :icon="Refresh" @click="refreshDetail">刷新</el-button>
            <el-button :icon="Close" circle title="关闭详情" @click="handleVisibleChange(false)" />
          </div>
        </section>

        <section class="detail-section detail-columns">
          <div class="detail-column">
            <div class="section-title"><h3>{{ leftTitle }}</h3></div>
            <div class="card-stack">
              <template v-if="isSupplySource">
                <EntityCard v-for="row in initiatorBases" :key="row.id" :border-color="initiatorBorderColor">
                  <el-descriptions :column="2" border>
                    <el-descriptions-item label="基地" :span="2">{{ row.herbBaseName || "-" }}</el-descriptions-item>
                    <el-descriptions-item label="供应品类">{{ row.productName || "-" }}</el-descriptions-item>
                    <el-descriptions-item label="可供量">{{ quantityLabelOf(row) }}</el-descriptions-item>
                    <el-descriptions-item label="规格" :span="2">{{ row.specification || "-" }}</el-descriptions-item>
                    <el-descriptions-item label="质量" :span="2">{{ row.qualityRequirement || "-" }}</el-descriptions-item>
                    <el-descriptions-item label="预期价格">{{ priceLabel(row) }}</el-descriptions-item>
                    <el-descriptions-item label="有效期">{{ formatDate(row.validUntil) }}</el-descriptions-item>
                  </el-descriptions>
                </EntityCard>
              </template>
              <template v-else>
                <EntityCard v-for="row in initiatorDemands" :key="row.id" :border-color="initiatorBorderColor">
                  <el-descriptions :column="2" border>
                    <el-descriptions-item label="发起药厂" :span="2">{{ row.vendorName || "-" }}</el-descriptions-item>
                    <el-descriptions-item label="品类">{{ row.productName || "-" }}</el-descriptions-item>
                    <el-descriptions-item label="需求数量">{{ quantityLabelOfDemand(row) }}</el-descriptions-item>
                    <el-descriptions-item label="规格" :span="2">{{ row.specification || "-" }}</el-descriptions-item>
                    <el-descriptions-item label="质量" :span="2">{{ row.qualityRequirement || "-" }}</el-descriptions-item>
                    <el-descriptions-item label="目标价">{{ priceLabelOfDemand(row) }}</el-descriptions-item>
                    <el-descriptions-item label="期望到货">{{ formatDate(row.expectedDeliveryAt) }}</el-descriptions-item>
                    <el-descriptions-item label="收货地址" :span="2">{{ row.deliveryAddress || "-" }}</el-descriptions-item>
                  </el-descriptions>
                </EntityCard>
              </template>
            </div>
            <el-empty v-if="isSupplySource ? !initiatorBases.length : !initiatorDemands.length" :description="isSupplySource ? '暂无发起基地' : '暂无发起药厂'" :image-size="60" />
          </div>

          <div class="detail-column">
            <div class="section-title"><h3>{{ rightTitle }}</h3></div>
            <div class="card-row">
              <template v-if="isSupplySource">
                <EntityCard v-for="row in candidateDemands" :key="row.id" :border-color="candidateStatusColor(row.status)">
                  <el-descriptions :column="2" border>
                    <el-descriptions-item label="候选药厂" :span="2">{{ row.vendorName || "-" }}</el-descriptions-item>
                    <el-descriptions-item label="需求品类">{{ row.productName || "-" }}</el-descriptions-item>
                    <el-descriptions-item label="需求数量">{{ quantityLabelOfDemand(row) }}</el-descriptions-item>
                    <el-descriptions-item label="目标价">{{ priceLabelOfDemand(row) }}</el-descriptions-item>
                    <el-descriptions-item label="规格">{{ row.specification || "-" }}</el-descriptions-item>
                    <el-descriptions-item label="质量" :span="2">{{ row.qualityRequirement || "-" }}</el-descriptions-item>
                    <el-descriptions-item label="期望到货">{{ formatDate(row.expectedDeliveryAt) }}</el-descriptions-item>
                    <el-descriptions-item label="状态">
                      <el-tag size="small" :type="candidateStatusTagType(row.status)">{{ candidateStatusLabel(row.status) }}</el-tag>
                    </el-descriptions-item>
                    <el-descriptions-item v-if="row.eliminationReason" label="淘汰原因" :span="2">{{ row.eliminationReason }}</el-descriptions-item>
                  </el-descriptions>
                  <template #actions>
                    <CandidateActions
                      v-if="canMaintainCandidates"
                      :status="row.status"
                      @edit="openDemandEdit(row)"
                      @keep="updateDemandStatus(row, '保留')"
                      @select="updateDemandStatus(row, '中选')"
                      @restore="updateDemandStatus(row, '待评估')"
                      @eliminate="eliminateDemand(row)"
                    />
                  </template>
                </EntityCard>
              </template>
              <template v-else>
                <EntityCard v-for="row in candidateBases" :key="row.id" :border-color="candidateStatusColor(row.status)">
                  <el-descriptions :column="2" border>
                    <el-descriptions-item label="基地" :span="2">{{ row.herbBaseName || "-" }}</el-descriptions-item>
                    <el-descriptions-item label="可供量">{{ quantityLabelOf(row) }}</el-descriptions-item>
                    <el-descriptions-item label="预期价格">{{ priceLabel(row) }}</el-descriptions-item>
                    <el-descriptions-item label="规格" :span="2">{{ row.specification || "-" }}</el-descriptions-item>
                    <el-descriptions-item label="质量" :span="2">{{ row.qualityRequirement || "-" }}</el-descriptions-item>
                    <el-descriptions-item label="当前状态">{{ row.currentState || "已失效" }}</el-descriptions-item>
                    <el-descriptions-item label="状态">
                      <el-tag size="small" :type="candidateStatusTagType(row.status)">{{ candidateStatusLabel(row.status) }}</el-tag>
                    </el-descriptions-item>
                    <el-descriptions-item v-if="row.eliminationReason" label="淘汰原因" :span="2">{{ row.eliminationReason }}</el-descriptions-item>
                  </el-descriptions>
                  <template #actions>
                    <CandidateActions
                      v-if="canMaintainCandidates"
                      :status="row.status"
                      :disabled="row.currentState === '已失效'"
                      @edit="openCandidateEdit(row)"
                      @keep="updateCandidateStatus(row, '保留')"
                      @select="updateCandidateStatus(row, '中选')"
                      @restore="updateCandidateStatus(row, '待评估')"
                      @eliminate="eliminateCandidate(row)"
                    />
                  </template>
                </EntityCard>
              </template>
            </div>
            <el-empty v-if="isSupplySource ? !candidateDemands.length : !candidateBases.length" :description="isSupplySource ? '暂无候选药厂' : '暂无候选基地'" :image-size="60" />
          </div>
        </section>

        <section class="detail-section">
          <div class="section-title"><h3>过程时间线</h3></div>
          <el-timeline v-if="reversedActivities.length" class="activity-timeline">
            <el-timeline-item v-for="activity in reversedActivities" :key="activity.id" :timestamp="formatDate(activity.createdAt)" placement="top">
              <strong>{{ activity.actionType }}</strong>
              <div>{{ activity.content }}</div>
              <small>{{ activity.createdBy || "-" }}</small>
            </el-timeline-item>
          </el-timeline>
          <el-empty v-else description="暂无过程记录" :image-size="72" />
        </section>
      </template>
    </div>
  </el-drawer>

  <ProgressDialog v-model="progressDialogVisible" :opportunity="currentOpportunity" @submitted="refreshAfterOperation" />

  <el-dialog v-model="closeDialogVisible" title="关闭撮合机会" width="460px">
    <el-form :model="closeForm" label-width="96px">
      <el-form-item label="关闭结果" required>
        <el-select v-model="closeForm.closeResult" placeholder="请选择关闭结果" style="width: 100%">
          <el-option label="撮合成功" value="撮合成功" />
          <el-option label="未成交" value="未成交" />
          <el-option label="需求取消" value="需求取消" />
        </el-select>
      </el-form-item>
      <el-form-item v-if="closeForm.closeResult !== '撮合成功'" label="关闭原因" required><el-input v-model="closeForm.closedReason" type="textarea" :rows="3" maxlength="500" show-word-limit /></el-form-item>
      <el-alert v-else title="撮合成功需要至少一条当前有效的中选候选。" type="info" :closable="false" />
    </el-form>
    <template #footer><el-button @click="closeDialogVisible = false">取消</el-button><el-button type="primary" :loading="saving" @click="saveCloseOpportunity">确认关闭</el-button></template>
  </el-dialog>

  <el-dialog v-model="candidateEditVisible" title="修改候选" width="560px" destroy-on-close>
    <el-form :model="candidateEditForm" label-width="100px">
      <el-form-item label="可供量"><el-input-number v-model="candidateEditForm.availableQuantity" :min="0" controls-position="right" style="width: 100%" /></el-form-item>
      <el-form-item label="规格"><el-input v-model="candidateEditForm.specification" clearable /></el-form-item>
      <el-form-item label="质量"><el-input v-model="candidateEditForm.qualityRequirement" clearable /></el-form-item>
      <el-form-item label="预期价格"><el-input-number v-model="candidateEditForm.expectedPrice" :min="0" controls-position="right" style="width: 100%" /></el-form-item>
      <el-form-item label="供货周期"><el-input v-model="candidateEditForm.supplyCycle" clearable /></el-form-item>
      <el-form-item label="有效期"><el-date-picker v-model="candidateEditForm.validUntil" type="datetime" value-format="YYYY-MM-DDTHH:mm:ss" style="width: 100%" /></el-form-item>
    </el-form>
    <template #footer><el-button @click="candidateEditVisible = false">取消</el-button><el-button type="primary" :loading="saving" @click="saveCandidateEdit">保存</el-button></template>
  </el-dialog>

  <el-dialog v-model="demandEditVisible" title="修改候选药厂需求" width="560px" destroy-on-close>
    <el-form :model="demandEditForm" label-width="100px">
      <el-form-item label="需求数量"><el-input-number v-model="demandEditForm.quantity" :min="0" controls-position="right" style="width: 100%" /></el-form-item>
      <el-form-item label="规格"><el-input v-model="demandEditForm.specification" clearable /></el-form-item>
      <el-form-item label="质量"><el-input v-model="demandEditForm.qualityRequirement" clearable /></el-form-item>
      <el-form-item label="目标价"><el-input-number v-model="demandEditForm.targetPrice" :min="0" controls-position="right" style="width: 100%" /></el-form-item>
      <el-form-item label="期望到货"><el-date-picker v-model="demandEditForm.expectedDeliveryAt" type="datetime" value-format="YYYY-MM-DDTHH:mm:ss" style="width: 100%" /></el-form-item>
    </el-form>
    <template #footer><el-button @click="demandEditVisible = false">取消</el-button><el-button type="primary" :loading="saving" @click="saveDemandEdit">保存</el-button></template>
  </el-dialog>
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch } from "vue";
import { Close, Refresh } from "@element-plus/icons-vue";
import { ElMessage, ElMessageBox } from "element-plus";
import http from "@/api";
import Permission from "@/components/Permission/index.vue";
import ProgressDialog from "./ProgressDialog.vue";
import EntityCard from "./EntityCard.vue";
import CandidateActions from "./CandidateActions.vue";
import { candidateStatusColor, candidateStatusLabel, candidateStatusTagType, opportunityStatusColor } from "./matchStatus";

const props = defineProps<{ modelValue: boolean; opportunityId?: string }>();
const emit = defineEmits<{ "update:modelValue": [value: boolean]; "refresh-list": []; "append-candidates": [opportunity: any]; "replace-candidates": [opportunity: any] }>();
const currentOpportunity = ref<any>(null);
const loading = ref(false);
const saving = ref(false);
const progressDialogVisible = ref(false);
const closeDialogVisible = ref(false);
const closeForm = reactive({ closeResult: "", closedReason: "" });
const candidateEditVisible = ref(false);
const editingCandidate = ref<any>(null);
const candidateEditForm = reactive<any>({
  availableQuantity: null, specification: "", qualityRequirement: "", expectedPrice: null, supplyCycle: "", validUntil: "",
});
const demandEditVisible = ref(false);
const editingDemand = ref<any>(null);
const demandEditForm = reactive<any>({ quantity: null, specification: "", qualityRequirement: "", targetPrice: null, expectedDeliveryAt: "" });
const canMaintainCandidates = computed(() => currentOpportunity.value?.status === "推进中");
const canAdvance = computed(() => currentOpportunity.value?.status !== "已关闭");
const quantityLabel = computed(() => quantityLabelOf(currentOpportunity.value));
const directionLabel = computed(() => currentOpportunity.value?.source === "supply" ? "供应 → 需求" : "需求 → 供应");
const isSupplySource = computed(() => currentOpportunity.value?.source === "supply");
const leftTitle = computed(() => isSupplySource.value ? "供给信息" : "需求信息");
const rightTitle = computed(() => isSupplySource.value ? "候选药厂" : "候选基地");
const candidatesOf = (role: string) => (currentOpportunity.value?.candidates || []).filter((item: any) => (item.role || "candidate") === role);
const demandsOf = (role: string) => (currentOpportunity.value?.demands || []).filter((item: any) => item.role === role);
const initiatorBases = computed(() => candidatesOf("initiator"));
const candidateBases = computed(() => candidatesOf("candidate"));
const initiatorDemands = computed(() => demandsOf("initiator"));
const candidateDemands = computed(() => demandsOf("candidate"));

const formatDate = (value?: string | null) => value ? new Date(value).toLocaleString("zh-CN") : "-";
const quantityLabelOf = (row?: any) => row?.demandQuantity === null || row?.demandQuantity === undefined
  ? row?.availableQuantity === null || row?.availableQuantity === undefined ? "-" : `${row.availableQuantity}${row.quantityUnit || ""}`
  : `${row.demandQuantity}${row.quantityUnit || ""}`;
const priceLabel = (row: any) => row.expectedPrice === null || row.expectedPrice === undefined ? "-" : `${row.expectedPrice}${row.priceUnit || ""}`;
const quantityLabelOfDemand = (row: any) => row?.quantity === null || row?.quantity === undefined ? "-" : `${row.quantity}${row.quantityUnit || ""}`;
const priceLabelOfDemand = (row: any) => row?.targetPrice === null || row?.targetPrice === undefined ? "-" : `${row.targetPrice}${row.priceUnit || ""}`;
// 已关闭 + 撮合成功 展示为「已成交」
const isDeal = (row: any) => row?.status === "已关闭" && row?.closeResult === "撮合成功";
const displayStatus = (row: any) => isDeal(row) ? "已成交" : (row?.status || "-");
const statusType = (row: any) => isDeal(row)
  ? "success"
  : ({ "推进中": "success", "暂停": "warning", "已关闭": "info" }[row?.status || ""] || "");
// 发起方卡片按撮合机会整体状态上色
const initiatorBorderColor = computed(() => opportunityStatusColor(currentOpportunity.value?.status, isDeal(currentOpportunity.value)));
const reversedActivities = computed(() => [...(currentOpportunity.value?.activities || [])].reverse());

const handleVisibleChange = (value: boolean) => emit("update:modelValue", value);
const loadDetail = async () => {
  if (!props.opportunityId) return;
  loading.value = true;
  try {
    const result = await http.get<any>(`/admin/crm/match-opportunities/${props.opportunityId}`);
    currentOpportunity.value = result.data;
  } finally {
    loading.value = false;
  }
};
const refreshDetail = async () => {
  await loadDetail();
  ElMessage.success("详情已刷新");
};
const refreshAfterOperation = async () => {
  await loadDetail();
  emit("refresh-list");
};
const STATUS_MESSAGES: Record<string, string> = {
  "保留": "候选已保留",
  "中选": "候选已中选",
  "淘汰": "候选已淘汰",
  "待评估": "候选已恢复为待评估"
};
const patchCandidateStatus = async (path: string, status: string, eliminationReason = "") => {
  if (!props.opportunityId) return;
  saving.value = true;
  try {
    await http.patch(path, { status, eliminationReason });
    ElMessage.success(STATUS_MESSAGES[status] || "候选状态已更新");
    await refreshAfterOperation();
  } finally {
    saving.value = false;
  }
};
// 基地候选
const updateCandidateStatus = (candidate: any, status: string, eliminationReason = "") =>
  patchCandidateStatus(`/admin/crm/match-opportunities/${props.opportunityId}/candidates/${candidate.id}/status`, status, eliminationReason);
// 药厂候选
const updateDemandStatus = (demand: any, status: string, eliminationReason = "") =>
  patchCandidateStatus(`/admin/crm/match-opportunities/${props.opportunityId}/demands/${demand.id}/status`, status, eliminationReason);
const requestReplaceCandidates = () => {
  if (!currentOpportunity.value) return;
  if (currentOpportunity.value.status === "已关闭") {
    ElMessage.warning("已关闭的撮合机会不能重新选择候选");
    return;
  }
  emit("replace-candidates", currentOpportunity.value);
};
const openCandidateEdit = (candidate: any) => {
  editingCandidate.value = candidate;
  Object.assign(candidateEditForm, {
    availableQuantity: candidate.availableQuantity ?? null,
    specification: candidate.specification || "",
    qualityRequirement: candidate.qualityRequirement || "",
    expectedPrice: candidate.expectedPrice ?? null,
    supplyCycle: candidate.supplyCycle || "",
    validUntil: candidate.validUntil || "",
  });
  candidateEditVisible.value = true;
};
const saveCandidateEdit = async () => {
  if (!props.opportunityId || !editingCandidate.value) return;
  saving.value = true;
  try {
    await http.patch(`/admin/crm/match-opportunities/${props.opportunityId}/candidates/${editingCandidate.value.id}`, { ...candidateEditForm });
    ElMessage.success("候选已修改");
    candidateEditVisible.value = false;
    await refreshAfterOperation();
  } finally {
    saving.value = false;
  }
};
const openDemandEdit = (row: any) => {
  editingDemand.value = row;
  Object.assign(demandEditForm, {
    quantity: row.quantity ?? null,
    specification: row.specification || "",
    qualityRequirement: row.qualityRequirement || "",
    targetPrice: row.targetPrice ?? null,
    expectedDeliveryAt: row.expectedDeliveryAt || "",
  });
  demandEditVisible.value = true;
};
const saveDemandEdit = async () => {
  if (!props.opportunityId || !editingDemand.value) return;
  saving.value = true;
  try {
    await http.patch(`/admin/crm/match-opportunities/${props.opportunityId}/demands/${editingDemand.value.id}`, { ...demandEditForm });
    ElMessage.success("候选药厂需求已修改");
    demandEditVisible.value = false;
    await refreshAfterOperation();
  } finally {
    saving.value = false;
  }
};
const eliminateCandidate = async (candidate: any) => {
  try {
    const { value } = await ElMessageBox.prompt("淘汰原因", "淘汰候选", { inputPattern: /\S+/, inputErrorMessage: "请填写淘汰原因" });
    await updateCandidateStatus(candidate, "淘汰", value);
  } catch (error: any) {
    if (error !== "cancel" && error !== "close") throw error;
  }
};
const eliminateDemand = async (demand: any) => {
  try {
    const { value } = await ElMessageBox.prompt("淘汰原因", "淘汰候选药厂", { inputPattern: /\S+/, inputErrorMessage: "请填写淘汰原因" });
    await updateDemandStatus(demand, "淘汰", value);
  } catch (error: any) {
    if (error !== "cancel" && error !== "close") throw error;
  }
};
const updateStatus = async (status: string) => {
  if (!props.opportunityId) return;
  saving.value = true;
  try {
    await http.patch(`/admin/crm/match-opportunities/${props.opportunityId}/status`, { status });
    ElMessage.success("撮合机会已暂停");
    await refreshAfterOperation();
  } finally {
    saving.value = false;
  }
};
const closeOpportunity = async () => {
  Object.assign(closeForm, { closeResult: "", closedReason: "" });
  closeDialogVisible.value = true;
};
const saveCloseOpportunity = async () => {
  if (!props.opportunityId || !closeForm.closeResult) {
    ElMessage.warning("请选择关闭结果");
    return;
  }
  if (closeForm.closeResult !== "撮合成功" && !closeForm.closedReason.trim()) {
    ElMessage.warning("请填写关闭原因");
    return;
  }
  saving.value = true;
  try {
    await http.patch(`/admin/crm/match-opportunities/${props.opportunityId}/status`, {
      status: "已关闭",
      closeResult: closeForm.closeResult,
      closedReason: closeForm.closedReason.trim()
    });
    ElMessage.success("撮合机会已关闭");
    closeDialogVisible.value = false;
    await refreshAfterOperation();
  } finally {
    saving.value = false;
  }
};
watch(() => [props.modelValue, props.opportunityId], ([visible, opportunityId]) => {
  if (visible && opportunityId) void loadDetail();
}, { immediate: true });
</script>

<style scoped>
.detail-drawer { min-height: 100%; padding: 24px; }
.drawer-head { display: flex; align-items: flex-start; justify-content: space-between; gap: 20px; margin-bottom: 24px; }
.detail-kicker { color: var(--el-text-color-secondary); font-size: 13px; }
.title-row, .head-actions, .head-meta { display: flex; align-items: center; gap: 10px; }
.title-row h2 { margin: 6px 0; font-size: 22px; }
.head-meta { color: var(--el-text-color-secondary); font-size: 14px; }
.head-actions { flex-wrap: wrap; justify-content: flex-end; }
.detail-section { margin-top: 24px; }
.detail-columns { display: grid; grid-template-columns: 470px minmax(0, 1fr); gap: 20px; align-items: start; }
.detail-column { min-width: 0; }
.card-row { display: flex; gap: 12px; overflow-x: auto; overflow-y: hidden; padding-bottom: 8px; }
.card-stack { display: flex; flex-direction: column; gap: 12px; }
.section-title { display: flex; align-items: center; gap: 12px; }
.section-title h3 { margin: 0 0 12px; font-size: 16px; }
.activity-timeline { margin: 0; padding-left: 8px; }
.activity-timeline small { display: block; margin-top: 4px; color: var(--el-text-color-secondary); }
.current-supply { margin-top: 8px; color: var(--el-text-color-secondary); }
</style>

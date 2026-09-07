<template>
  <el-drawer :model-value="modelValue" size="82%" :with-header="false" @update:model-value="handleVisibleChange">
    <div v-loading="loading" class="detail-drawer">
      <template v-if="currentOpportunity">
        <section class="drawer-head">
          <div>
            <div class="detail-kicker">撮合机会详情</div>
            <div class="title-row"><h2>{{ currentOpportunity.demandProductName || "撮合机会" }}</h2><el-tag :type="statusType(currentOpportunity.status)">{{ currentOpportunity.status || "-" }}</el-tag></div>
            <div class="head-meta"><span>需求厂商 {{ currentOpportunity.vendorName || "-" }}</span><span>跟进人 {{ currentOpportunity.ownerUserName || "-" }}</span></div>
          </div>
          <div class="head-actions">
            <Permission code="CRM_MATCH_OPPORTUNITY_MANAGE"><el-button v-if="canMaintainCandidates" @click="emit('append-candidates', currentOpportunity)">追加候选</el-button></Permission>
            <Permission code="CRM_MATCH_OPPORTUNITY_MANAGE"><el-button v-if="canUpdateNextAction" @click="openNextActionDialog">更新下一步</el-button></Permission>
            <Permission code="CRM_MATCH_OPPORTUNITY_MANAGE"><el-button v-if="currentOpportunity.status === '推进中'" type="warning" @click="updateStatus('暂停')">暂停</el-button></Permission>
            <Permission code="CRM_MATCH_OPPORTUNITY_MANAGE"><el-button v-if="currentOpportunity.status === '暂停'" type="success" @click="resumeOpportunity">恢复</el-button></Permission>
            <Permission code="CRM_MATCH_OPPORTUNITY_MANAGE"><el-button v-if="currentOpportunity.status !== '已关闭'" type="danger" @click="closeOpportunity">关闭</el-button></Permission>
            <el-button :icon="Refresh" @click="refreshDetail">刷新</el-button>
            <el-button :icon="Close" circle title="关闭详情" @click="handleVisibleChange(false)" />
          </div>
        </section>

        <section class="detail-section">
          <div class="section-title"><h3>需求快照</h3></div>
          <el-descriptions :column="3" border>
            <el-descriptions-item label="品类">{{ currentOpportunity.demandProductName || "-" }}</el-descriptions-item>
            <el-descriptions-item label="需求数量">{{ quantityLabel }}</el-descriptions-item>
            <el-descriptions-item label="规格">{{ currentOpportunity.specification || "-" }}</el-descriptions-item>
            <el-descriptions-item label="质量">{{ currentOpportunity.qualityRequirement || "-" }}</el-descriptions-item>
            <el-descriptions-item label="期望到货">{{ formatDate(currentOpportunity.expectedDeliveryAt) }}</el-descriptions-item>
            <el-descriptions-item label="收货地址">{{ currentOpportunity.deliveryAddress || "-" }}</el-descriptions-item>
            <el-descriptions-item label="下一步">{{ currentOpportunity.nextAction || "-" }}</el-descriptions-item>
            <el-descriptions-item label="下一步时间">{{ formatDate(currentOpportunity.nextActionAt) }}</el-descriptions-item>
            <el-descriptions-item label="关闭结果">{{ currentOpportunity.closeResult || "-" }}</el-descriptions-item>
            <el-descriptions-item label="关闭原因">{{ currentOpportunity.closedReason || "-" }}</el-descriptions-item>
          </el-descriptions>
        </section>

        <section class="detail-section">
          <div class="section-title"><h3>候选</h3></div>
          <el-table :data="currentOpportunity.candidates || []" border>
            <el-table-column prop="subjectName" label="基地主体" min-width="170" show-overflow-tooltip />
            <el-table-column prop="herbBaseName" label="基地" min-width="140" show-overflow-tooltip />
            <el-table-column label="可供量" width="110"><template #default="{ row }">{{ quantityLabelOf(row) }}</template></el-table-column>
            <el-table-column prop="specification" label="规格" min-width="120" show-overflow-tooltip />
            <el-table-column prop="qualityRequirement" label="质量" min-width="120" show-overflow-tooltip />
            <el-table-column label="预期价格" width="130"><template #default="{ row }">{{ priceLabel(row) }}</template></el-table-column>
            <el-table-column label="当前状态" min-width="150">
              <template #default="{ row }">
                <el-popover v-if="row.currentState === '条件已变更'" width="320" trigger="hover">
                  <template #reference><el-tag size="small" type="warning">条件已变更</el-tag></template>
                  <div>变化字段：{{ row.currentChangedFields?.join("、") }}</div>
                  <div v-if="row.currentSupply" class="current-supply">当前：{{ quantityLabelOf(row.currentSupply) }}，{{ row.currentSupply.specification || "无规格" }}，{{ priceLabel(row.currentSupply) }}</div>
                </el-popover>
                <el-tag v-else size="small" :type="row.currentState === '当前正常' ? 'success' : 'danger'">{{ row.currentState || "已失效" }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column label="状态" width="100"><template #default="{ row }"><el-tag size="small" :type="candidateStatusType(row.status)">{{ row.status || "-" }}</el-tag></template></el-table-column>
            <el-table-column prop="eliminationReason" label="淘汰原因" min-width="150" show-overflow-tooltip />
            <el-table-column label="操作" width="130" fixed="right">
              <template #default="{ row }">
                <Permission code="CRM_MATCH_OPPORTUNITY_MANAGE">
                  <el-button v-if="canMaintainCandidates && row.status !== '保留'" link type="success" @click="updateCandidateStatus(row, '保留')">保留</el-button>
                  <el-button v-if="canMaintainCandidates && row.status !== '中选'" link type="primary" :disabled="row.currentState === '已失效'" @click="updateCandidateStatus(row, '中选')">中选</el-button>
                  <el-button v-if="canMaintainCandidates && row.status !== '淘汰'" link type="danger" @click="eliminateCandidate(row)">淘汰</el-button>
                </Permission>
              </template>
            </el-table-column>
          </el-table>
        </section>

        <section class="detail-section">
          <div class="section-title"><h3>过程时间线</h3></div>
          <el-timeline v-if="currentOpportunity.activities?.length" class="activity-timeline">
            <el-timeline-item v-for="activity in currentOpportunity.activities" :key="activity.id" :timestamp="formatDate(activity.createdAt)" placement="top">
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

  <el-dialog v-model="nextActionDialogVisible" title="更新下一步" width="520px">
    <el-form :model="nextActionForm" label-width="96px">
      <el-form-item label="下一步" required><el-input v-model="nextActionForm.nextAction" type="textarea" :rows="3" maxlength="500" show-word-limit /></el-form-item>
      <el-form-item label="计划时间" required><el-date-picker v-model="nextActionForm.nextActionAt" type="datetime" value-format="YYYY-MM-DDTHH:mm:ss" /></el-form-item>
    </el-form>
    <template #footer><el-button @click="nextActionDialogVisible = false">取消</el-button><el-button type="primary" :loading="saving" @click="saveNextAction">保存</el-button></template>
  </el-dialog>

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
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch } from "vue";
import { Close, Refresh } from "@element-plus/icons-vue";
import { ElMessage, ElMessageBox } from "element-plus";
import http from "@/api";
import Permission from "@/components/Permission/index.vue";

const props = defineProps<{ modelValue: boolean; opportunityId?: string }>();
const emit = defineEmits<{ "update:modelValue": [value: boolean]; "refresh-list": []; "append-candidates": [opportunity: any] }>();
const currentOpportunity = ref<any>(null);
const loading = ref(false);
const saving = ref(false);
const nextActionDialogVisible = ref(false);
const nextActionForm = reactive({ nextAction: "", nextActionAt: "" });
const resumePending = ref(false);
const closeDialogVisible = ref(false);
const closeForm = reactive({ closeResult: "", closedReason: "" });
const canMaintainCandidates = computed(() => currentOpportunity.value?.status === "推进中");
const canUpdateNextAction = computed(() => currentOpportunity.value?.status !== "已关闭");
const quantityLabel = computed(() => quantityLabelOf(currentOpportunity.value));

const formatDate = (value?: string | null) => value ? new Date(value).toLocaleString("zh-CN") : "-";
const quantityLabelOf = (row?: any) => row?.demandQuantity === null || row?.demandQuantity === undefined
  ? row?.availableQuantity === null || row?.availableQuantity === undefined ? "-" : `${row.availableQuantity}${row.quantityUnit || ""}`
  : `${row.demandQuantity}${row.quantityUnit || ""}`;
const priceLabel = (row: any) => row.expectedPrice === null || row.expectedPrice === undefined ? "-" : `${row.expectedPrice}${row.priceUnit || ""}`;
const statusType = (status?: string) => ({ "推进中": "success", "暂停": "warning", "已关闭": "info" }[status || ""] || "");
const candidateStatusType = (status?: string) => ({ "保留": "success", "中选": "primary", "淘汰": "danger", "待评估": "warning" }[status || ""] || "");

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
const updateCandidateStatus = async (candidate: any, status: string, eliminationReason = "") => {
  if (!props.opportunityId) return;
  saving.value = true;
  try {
    await http.patch(`/admin/crm/match-opportunities/${props.opportunityId}/candidates/${candidate.id}/status`, { status, eliminationReason });
    ElMessage.success(status === "保留" ? "候选已保留" : status === "中选" ? "候选已中选" : "候选已淘汰");
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
const openNextActionDialog = () => {
  resumePending.value = false;
  Object.assign(nextActionForm, { nextAction: currentOpportunity.value?.nextAction || "", nextActionAt: toDateTimeInputValue(currentOpportunity.value?.nextActionAt) });
  nextActionDialogVisible.value = true;
};
const saveNextAction = async () => {
  if (!props.opportunityId || !nextActionForm.nextAction.trim() || !nextActionForm.nextActionAt) {
    ElMessage.warning("请填写下一步和计划时间");
    return;
  }
  saving.value = true;
  try {
    if (resumePending.value) {
      await http.patch(`/admin/crm/match-opportunities/${props.opportunityId}/status`, { status: "推进中", nextAction: nextActionForm.nextAction.trim(), nextActionAt: nextActionForm.nextActionAt });
      ElMessage.success("撮合机会已恢复");
    } else {
      await http.patch(`/admin/crm/match-opportunities/${props.opportunityId}/next-action`, { nextAction: nextActionForm.nextAction.trim(), nextActionAt: nextActionForm.nextActionAt });
      ElMessage.success("下一步已更新");
    }
    nextActionDialogVisible.value = false;
    await refreshAfterOperation();
  } finally {
    saving.value = false;
  }
};
const updateStatus = async (status: string) => {
  if (!props.opportunityId) return;
  saving.value = true;
  try {
    await http.patch(`/admin/crm/match-opportunities/${props.opportunityId}/status`, { status });
    ElMessage.success(status === "暂停" ? "撮合机会已暂停" : "撮合机会已恢复");
    await refreshAfterOperation();
  } finally {
    saving.value = false;
  }
};
const resumeOpportunity = () => {
  if (new Date(currentOpportunity.value?.nextActionAt || 0).getTime() < Date.now()) {
    resumePending.value = true;
    Object.assign(nextActionForm, { nextAction: currentOpportunity.value?.nextAction || "", nextActionAt: "" });
    nextActionDialogVisible.value = true;
    return;
  }
  void updateStatus("推进中");
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
const toDateTimeInputValue = (value?: string | null) => value ? value.replace(" ", "T").slice(0, 19) : "";

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
.section-title h3 { margin: 0 0 12px; font-size: 16px; }
.activity-timeline { margin: 0; padding-left: 8px; }
.activity-timeline small { display: block; margin-top: 4px; color: var(--el-text-color-secondary); }
.current-supply { margin-top: 8px; color: var(--el-text-color-secondary); }
</style>

<template>
  <el-drawer :model-value="modelValue" size="82%" :with-header="false" @update:model-value="handleVisibleChange">
    <div v-loading="loading" class="detail-drawer">
      <template v-if="currentOpportunity">
        <section class="drawer-head">
          <div>
            <div class="detail-kicker">撮合机会详情</div>
            <div class="title-row">
              <h2>{{ currentOpportunity.productName || "撮合机会" }}</h2>
              <el-tag :type="statusType(currentOpportunity)">{{ displayStatus(currentOpportunity) }}</el-tag>
              <el-tag effect="plain" :type="currentOpportunity.source === 'supply' ? 'warning' : 'primary'">{{ directionLabel }}</el-tag>
            </div>
            <div class="head-meta"><span>{{ isSupplySource ? '发起基地' : '需求厂商' }} {{ currentOpportunity.initiatorName || "-" }}</span><span>跟进人 {{ currentOpportunity.ownerUserName || "-" }}</span></div>
          </div>
          <div class="head-actions">
            <Permission code="CRM_MATCH_OPPORTUNITY_MANAGE"><el-button v-if="canMaintainCandidates" @click="emit('append-candidates', currentOpportunity)">追加候选</el-button></Permission>
            <Permission code="CRM_MATCH_OPPORTUNITY_MANAGE"><el-button v-if="canAdvance" type="primary" @click="progressDialogVisible = true">推进机会</el-button></Permission>
            <Permission code="CRM_MATCH_OPPORTUNITY_MANAGE"><el-button v-if="currentOpportunity.status === '推进中'" type="warning" @click="updateStatus('暂停')">暂停</el-button></Permission>
            <Permission code="CRM_MATCH_OPPORTUNITY_MANAGE"><el-button v-if="currentOpportunity.status !== '已关闭'" type="danger" @click="closeOpportunity">关闭</el-button></Permission>
            <el-button :icon="Refresh" @click="refreshDetail">刷新</el-button>
            <el-button :icon="Close" circle title="关闭详情" @click="handleVisibleChange(false)" />
          </div>
        </section>

        <section class="flow-section">
          <el-steps :active="flowActive" :status="flowStatus" align-center finish-status="success">
            <el-step title="发起撮合" :description="flowDescriptionOf(0)" />
            <el-step v-for="(stage, index) in flowStages" :key="stage" :title="stage" :description="flowDescriptionOf(index + 1)" />
            <el-step :title="flowCloseTitle" :description="flowDescriptionOf(flowStages.length + 1)" />
          </el-steps>
        </section>

        <section class="detail-body">
          <section class="detail-card">
            <div class="section-title"><div class="section-heading"><h3>{{ leftTitle }}</h3></div></div>
            <div class="card-stack">
              <template v-if="isSupplySource">
                <EntityCard v-for="row in initiatorBases" :key="row.id">
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
                <EntityCard v-for="row in initiatorDemands" :key="row.id">
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
              <el-empty v-if="isSupplySource ? !initiatorBases.length : !initiatorDemands.length" :description="isSupplySource ? '暂无发起基地' : '暂无发起药厂'" :image-size="60" />
            </div>
          </section>

          <section class="detail-card">
            <div class="section-title"><div class="section-heading"><h3>{{ rightTitle }}</h3></div></div>
            <div class="card-row">
              <template v-if="isSupplySource">
                <EntityCard v-for="row in candidateDemands" :key="row.id">
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
                      @select="updateItemStatus(row, '中选')"
                      @remove="removeItem(row)"
                    />
                  </template>
                </EntityCard>
              </template>
              <template v-else>
                <EntityCard v-for="row in candidateBases" :key="row.id">
                  <el-descriptions :column="2" border>
                    <el-descriptions-item label="基地" :span="2">{{ row.herbBaseName || "-" }}</el-descriptions-item>
                    <el-descriptions-item label="可供量">{{ quantityLabelOf(row) }}</el-descriptions-item>
                    <el-descriptions-item label="预期价格">{{ priceLabel(row) }}</el-descriptions-item>
                    <el-descriptions-item label="规格" :span="2">{{ row.specification || "-" }}</el-descriptions-item>
                    <el-descriptions-item label="质量" :span="2">{{ row.qualityRequirement || "-" }}</el-descriptions-item>
                    <el-descriptions-item label="状态">
                      <el-tag size="small" :type="candidateStatusTagType(row.status)">{{ candidateStatusLabel(row.status) }}</el-tag>
                    </el-descriptions-item>
                    <el-descriptions-item v-if="row.eliminationReason" label="淘汰原因" :span="2">{{ row.eliminationReason }}</el-descriptions-item>
                  </el-descriptions>
                  <template #actions>
                    <CandidateActions
                      v-if="canMaintainCandidates"
                      :status="row.status"
                      @select="updateItemStatus(row, '中选')"
                      @remove="removeItem(row)"
                    />
                  </template>
                </EntityCard>
              </template>
            </div>
            <el-empty v-if="isSupplySource ? !candidateDemands.length : !candidateBases.length" :description="isSupplySource ? '暂无候选药厂' : '暂无候选基地'" :image-size="60" />
          </section>

          <section class="detail-card">
            <div class="section-title"><div class="section-heading"><h3>过程时间线</h3></div></div>
            <el-timeline v-if="reversedActivities.length" class="activity-timeline">
              <el-timeline-item v-for="activity in reversedActivities" :key="activity.id" :timestamp="formatDate(activity.createdAt)" placement="top">
                <strong>{{ activity.actionType }}</strong>
                <div>{{ activity.content }}</div>
                <small>{{ activity.createdBy || "-" }}</small>
              </el-timeline-item>
            </el-timeline>
            <el-empty v-else description="暂无过程记录" :image-size="72" />
          </section>
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
import { candidateStatusLabel, candidateStatusTagType } from "./matchStatus";

const props = defineProps<{ modelValue: boolean; opportunityId?: string }>();
const emit = defineEmits<{ "update:modelValue": [value: boolean]; "refresh-list": []; "append-candidates": [opportunity: any] }>();
const currentOpportunity = ref<any>(null);
const loading = ref(false);
const saving = ref(false);
const progressDialogVisible = ref(false);
const closeDialogVisible = ref(false);
const closeForm = reactive({ closeResult: "", closedReason: "" });
const canMaintainCandidates = computed(() => currentOpportunity.value?.status === "推进中");
const canAdvance = computed(() => currentOpportunity.value?.status !== "已关闭");
const directionLabel = computed(() => currentOpportunity.value?.source === "supply" ? "供应 → 需求" : "需求 → 供应");
const isSupplySource = computed(() => currentOpportunity.value?.source === "supply");
const leftTitle = computed(() => isSupplySource.value ? "供给信息" : "需求信息");
const rightTitle = computed(() => isSupplySource.value ? "候选药厂" : "候选基地");

// 统一子表：items 按 itemType(supply/demand) + role 分组
const itemsOf = (itemType: string, role: string) =>
  (currentOpportunity.value?.items || []).filter((item: any) => item.itemType === itemType && item.role === role);
const initiatorBases = computed(() => itemsOf("supply", "initiator"));
const candidateBases = computed(() => itemsOf("supply", "candidate"));
const initiatorDemands = computed(() => itemsOf("demand", "initiator"));
const candidateDemands = computed(() => itemsOf("demand", "candidate"));

const formatDate = (value?: string | null) => value ? new Date(value).toLocaleString("zh-CN") : "-";
const quantityLabelOf = (row?: any) => row?.availableQuantity === null || row?.availableQuantity === undefined ? "-" : `${row.availableQuantity}${row.quantityUnit || ""}`;
const priceLabel = (row: any) => row.expectedPrice === null || row.expectedPrice === undefined ? "-" : `${row.expectedPrice}${row.priceUnit || ""}`;
const quantityLabelOfDemand = (row: any) => row?.quantity === null || row?.quantity === undefined ? "-" : `${row.quantity}${row.quantityUnit || ""}`;
const priceLabelOfDemand = (row: any) => row?.targetPrice === null || row?.targetPrice === undefined ? "-" : `${row.targetPrice}${row.priceUnit || ""}`;
const isDeal = (row: any) => row?.status === "已关闭" && row?.closeResult === "撮合成功";
const displayStatus = (row: any) => isDeal(row) ? "已成交" : (row?.status || "-");
const statusType = (row: any) => isDeal(row)
  ? "success"
  : ({ "推进中": "success", "暂停": "warning", "已关闭": "info" }[row?.status || ""] || "");
const reversedActivities = computed(() => [...(currentOpportunity.value?.activities || [])].reverse());

// 详情顶部横向流程：发起 -> 三阶段 -> 关闭，按机会状态高亮当前节点
const flowStages = ["匹配核实", "商务沟通", "意向确认"];
const flowStepCount = flowStages.length + 2; // 发起撮合 + 三阶段 + 关闭
const flowStageIndex = computed(() => flowStages.indexOf(currentOpportunity.value?.stage));
const flowClosed = computed(() => currentOpportunity.value?.status === "已关闭");
// active = 已完成步骤数（0 起算的当前步骤索引）；关闭时等于总步数，全部显示完成
const flowActive = computed(() => flowClosed.value ? flowStepCount : Math.max(1, flowStageIndex.value + 1));
const flowStatus = computed(() => flowClosed.value ? "finish" : "process");
const flowCloseTitle = computed(() => flowClosed.value ? (currentOpportunity.value?.closeResult || "已关闭") : "关闭机会");
const flowDescriptionOf = (index: number) => {
  const opportunity = currentOpportunity.value;
  if (!opportunity) return "";
  if (index === 0) {
    const initiator = itemsOf(isSupplySource.value ? "supply" : "demand", "initiator")[0];
    return opportunity.initiatorName || initiator?.herbBaseName || initiator?.vendorName || "";
  }
  if (index === flowStages.length + 1) return flowClosed.value ? (opportunity.closedReason || "") : "";
  if (index !== flowStageIndex.value + 1) return "";
  if (opportunity.status === "暂停") return "已暂停";
  return opportunity.nextAction || "";
};

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
const updateItemStatus = async (item: any, status: string) => {
  if (!props.opportunityId) return;
  saving.value = true;
  try {
    await http.patch(`/admin/crm/match-opportunities/${props.opportunityId}/items/${item.id}/status`, { status });
    ElMessage.success(status === "中选" ? "候选已中选" : "候选状态已更新");
    await refreshAfterOperation();
  } finally {
    saving.value = false;
  }
};
const removeItem = async (item: any) => {
  if (!props.opportunityId) return;
  try {
    await ElMessageBox.confirm("确认移除该候选？移除后该明细可重新参与其他撮合。", "移除候选", { type: "warning" });
  } catch {
    return;
  }
  saving.value = true;
  try {
    await http.delete(`/admin/crm/match-opportunities/${props.opportunityId}/items/${item.id}`);
    ElMessage.success("候选已移除");
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

defineExpose({ reload: loadDetail });
</script>

<style scoped>
.detail-drawer { min-height: 100%; padding: 24px; }
.drawer-head { display: flex; align-items: flex-start; justify-content: space-between; gap: 20px; margin-bottom: 24px; }
.detail-kicker { color: var(--el-text-color-secondary); font-size: 13px; }
.title-row, .head-actions, .head-meta { display: flex; align-items: center; gap: 10px; }
.title-row h2 { margin: 6px 0; font-size: 22px; }
.head-meta { color: var(--el-text-color-secondary); font-size: 14px; }
.head-actions { flex-wrap: wrap; justify-content: flex-end; }
.flow-section { margin-bottom: 20px; padding: 12px 20px 4px; border: 1px solid var(--el-border-color-light); }
.flow-section :deep(.el-step__description) { min-height: 18px; font-size: 12px; }
.detail-body { display: grid; grid-template-columns: minmax(0, 1fr) minmax(0, 1fr) minmax(0, 1fr); gap: 16px; align-items: start; }
.detail-card { min-width: 0; padding: 16px; border: 1px solid var(--el-border-color-light); border-radius: 8px; }
.section-title { display: flex; align-items: center; justify-content: space-between; gap: 10px; margin: 0 -16px 18px; padding: 0 16px 12px; border-bottom: 1px solid var(--el-border-color-lighter); }
.section-heading { display: flex; align-items: center; flex-wrap: wrap; gap: 8px; }
.section-title h3 { margin: 0; font-size: 16px; }
.card-row { display: flex; flex-wrap: wrap; gap: 12px; }
.card-stack { display: flex; flex-direction: column; gap: 12px; }
.activity-timeline { margin: 0; padding-left: 8px; }
.activity-timeline small { display: block; margin-top: 4px; color: var(--el-text-color-secondary); }
</style>

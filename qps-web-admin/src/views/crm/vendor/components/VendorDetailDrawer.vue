<template>
  <el-drawer :model-value="modelValue" size="82%" :with-header="false" class="vendor-drawer" @update:model-value="handleVisibleChange">
    <div v-if="currentVendor" class="drawer-layout">
      <section class="drawer-head">
        <div>
          <div class="detail-kicker">厂商详情</div>
          <div class="title-row">
            <h2>{{ currentVendor.vendorName }}</h2>
            <el-tooltip :content="getPriorityRule(currentVendor.priorityLevel)">
              <el-tag :type="getPriorityType(currentVendor.priorityLevel)" effect="dark">{{ currentVendor.priorityLevel || "-" }}</el-tag>
            </el-tooltip>
          </div>
          <div class="head-meta">
            <span>采购需求 {{ currentVendor.purchaseDemandCount || 0 }}</span>
            <span>品类 {{ currentVendor.productCount || 0 }}</span>
            <span>联系人 {{ currentVendor.contactCount || 0 }}</span>
          </div>
        </div>
        <div class="head-actions">
          <el-button v-if="canManageTransfer" :icon="Edit" @click="openTransferDialog(currentVendor.ownerUserId ? 'TRANSFER' : 'ASSIGN')">
            {{ currentVendor.ownerUserId ? "转交" : "分配" }}
          </el-button>
          <el-button v-if="canManageTransfer || canReturn(currentVendor)" :icon="Edit" @click="openTransferDialog('RETURN')">退回</el-button>
          <Permission code="CRM_VENDOR_EDIT"><el-button :icon="Edit" @click="openEditDialog">编辑</el-button></Permission>
          <el-button :icon="Refresh" @click="refreshDetail()">刷新</el-button>
        </div>
      </section>

      <section class="summary-band">
        <div><span>采购概况</span><strong>{{ currentVendor.purchaseDemandCount || 0 }} 条需求</strong><span>品类 {{ currentVendor.productCount || 0 }} · 联系人 {{ currentVendor.contactCount || 0 }}</span></div>
        <div><span>主联系人</span><strong>{{ currentVendor.primaryContactName || "-" }}</strong><span>{{ currentVendor.primaryContactPhone || "-" }}</span></div>
        <div><span>最近采购</span><strong>{{ formatDate(currentVendor.latestPurchaseTime) }}</strong><span>{{ currentVendor.latestPurchaseDemandName || "-" }}</span></div>
        <div><span>更新时间</span><strong>{{ formatDate(currentVendor.updatedAt) }}</strong><span>{{ currentVendor.remark || "-" }}</span></div>
      </section>

      <section class="detail-grid">
        <ContactList
          :contacts="currentVendor.contacts || []"
          add-permission="CRM_VENDOR_EDIT"
          edit-permission="CRM_VENDOR_EDIT"
          primary-permission="CRM_VENDOR_EDIT"
          status-permission="CRM_VENDOR_EDIT"
          show-status-action
          @add="openContactDialog()"
          @edit="openContactDialog"
          @set-primary="setPrimaryContact"
          @toggle-status="toggleContactStatus"
        />

        <div class="detail-content">
          <section class="detail-card purchase-demand-card">
            <div class="section-title">
              <h3>采购需求</h3>
              <Permission code="CRM_PURCHASE_DEMAND_MANAGE"><el-button type="primary" link :icon="Plus" @click="openPurchaseDemandDialog()">新增采购需求</el-button></Permission>
            </div>
            <el-table :data="purchaseDemands" v-loading="purchaseDemandLoading" border class="purchase-demand-table">
              <el-table-column label="采购需求" min-width="230">
                <template #default="{ row }">
                  <div class="demand-title-cell">
                    <strong>{{ row.demandName || "未命名采购需求" }}</strong>
                    <span>{{ row.demandNo || "-" }}</span>
                  </div>
                </template>
              </el-table-column>
              <el-table-column label="采购明细" min-width="250">
                <template #default="{ row }">
                  <div v-if="row.items?.length" class="demand-item-list">
                    <el-tag v-for="item in row.items" :key="item.id || item.productName" size="small" effect="plain" class="demand-item-tag">
                      <span>{{ item.productName || "未填写品类" }}</span>
                      <b v-if="item.quantity">{{ item.quantity }}{{ item.quantityUnit || "" }}</b>
                    </el-tag>
                  </div>
                  <span v-else class="empty-value">-</span>
                </template>
              </el-table-column>
              <el-table-column label="提出日期" width="164"><template #default="{ row }">{{ formatDate(row.demandAt) }}</template></el-table-column>
              <el-table-column label="期望到货" width="164"><template #default="{ row }">{{ formatDate(row.expectedDeliveryAt) }}</template></el-table-column>
              <el-table-column prop="receivingAddress" label="收货地" min-width="180" show-overflow-tooltip />
              <el-table-column label="状态" width="96" align="center">
                <template #default="{ row }">
                  <el-tag :type="getPurchaseDemandStatusType(row.status)" size="small" effect="light">{{ row.status || "-" }}</el-tag>
                </template>
              </el-table-column>
              <el-table-column label="操作" width="205" fixed="right">
                <template #default="{ row }">
                  <Permission code="CRM_PURCHASE_DEMAND_MANAGE">
                    <div class="demand-actions">
                      <el-button type="primary" link :icon="Edit" @click="openPurchaseDemandDialog(row)">编辑</el-button>
                      <el-button v-if="row.status === '待确认'" type="success" link @click="changePurchaseDemandStatus(row, '有效')">确认有效</el-button>
                      <el-button v-if="row.status !== '已完成' && row.status !== '已关闭'" type="danger" link @click="closePurchaseDemand(row)">关闭</el-button>
                    </div>
                  </Permission>
                </template>
              </el-table-column>
            </el-table>
            <div class="table-footer">
              <el-pagination
                :current-page="purchaseDemandPage"
                :page-size="purchaseDemandPageSize"
                :page-sizes="[5, 10, 20, 50, 100]"
                :total="purchaseDemandTotal"
                layout="total, sizes, prev, pager, next, jumper"
                background
                @size-change="handlePurchaseDemandSizeChange"
                @current-change="handlePurchaseDemandPageChange"
              />
            </div>
          </section>

          <div class="activity-row">
            <FollowList :records="followRecords" add-permission="CRM_FOLLOW" @add="openFollowDialog" />
            <TransferList :records="currentVendor.transferRecords || []" />
          </div>
        </div>
      </section>
    </div>
  </el-drawer>

  <el-dialog v-model="vendorDialogVisible" title="编辑厂商" width="560px">
    <el-form :model="vendorForm" label-width="110px">
      <el-form-item label="厂商名称" required>
        <el-input v-model="vendorForm.vendorName" clearable placeholder="请输入厂商名称" />
      </el-form-item>
      <el-form-item label="优先级">
        <el-select v-model="vendorForm.priorityLevel" placeholder="请选择优先级">
          <el-option label="高" value="高" />
          <el-option label="中" value="中" />
          <el-option label="低" value="低" />
        </el-select>
      </el-form-item>
      <el-form-item label="备注">
        <el-input v-model="vendorForm.remark" type="textarea" :rows="3" placeholder="请输入备注" />
      </el-form-item>
    </el-form>
    <template #footer>
      <el-button @click="vendorDialogVisible = false">取消</el-button>
      <el-button type="primary" @click="submitVendorForm">保存</el-button>
    </template>
  </el-dialog>

  <ContactDialog
    v-model="contactDialogVisible"
    entity-type="CRM_VENDOR"
    :entity-id="currentVendor?.id"
    :contact="editingContact"
    @saved="handleRecordSaved"
  />
  <FollowDialog
    v-model="followDialogVisible"
    entity-type="CRM_VENDOR"
    :entity-id="currentVendor?.id"
    :contacts="currentVendor?.contacts || []"
    @saved="handleRecordSaved"
  />
  <TransferDialog
    v-model="transferDialogVisible"
    entity-type="CRM_VENDOR"
    :entity-ids="currentVendor ? [currentVendor.id] : []"
    :mode="transferMode"
    selected-label="已选厂商"
    @saved="handleRecordSaved"
  />

  <CrmVendorDemandEditor
    v-model="purchaseDemandDialogVisible"
    :vendor-id="currentVendor?.id"
    :demand="editingPurchaseDemand"
    lock-vendor
    @saved="handlePurchaseDemandSaved"
  />
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch } from "vue";
import { Edit, Plus, Refresh } from "@element-plus/icons-vue";
import { ElMessage, ElMessageBox } from "element-plus";
import CrmVendorDemandEditor from "@/components/CrmVendorDemandEditor/index.vue";
import { crmVendorApi } from "@/api/modules/crmVendor";
import crmVendorDemandApi from "@/api/modules/crmVendorDemand";
import Permission from "@/components/Permission/index.vue";
import ContactList from "@/views/crm/components/contact/List.vue";
import ContactDialog from "@/views/crm/components/contact/Dialog.vue";
import FollowList from "@/views/crm/components/follow/List.vue";
import FollowDialog from "@/views/crm/components/follow/Dialog.vue";
import TransferList from "@/views/crm/components/transfer/List.vue";
import TransferDialog from "@/views/crm/components/transfer/Dialog.vue";
import { useAuthStore } from "@/stores/modules/auth";
import { useUserStore } from "@/stores/modules/user";

interface VendorDetail {
  id: string;
  vendorName: string;
  normalizedVendorName: string;
  priorityLevel: string;
  latestPurchaseTime?: string | null;
  latestPurchaseDemandName: string;
  remark: string;
  ownerUserId?: string | null;
  ownerUserName?: string | null;
  primaryContactName: string;
  primaryContactPhone: string;
  purchaseDemandCount: number;
  productCount: number;
  contactCount: number;
  createdAt: string;
  updatedAt: string;
  contacts: any[];
  transferRecords: any[];
}

const props = defineProps<{
  modelValue: boolean;
  vendorId?: string;
}>();

const emit = defineEmits<{
  (event: "update:modelValue", value: boolean): void;
  (event: "refresh-list"): void;
}>();

const authStore = useAuthStore();
const userStore = useUserStore();

const currentVendor = ref<VendorDetail | null>(null);
const vendorDialogVisible = ref(false);
const transferDialogVisible = ref(false);
const contactDialogVisible = ref(false);
const purchaseDemandDialogVisible = ref(false);
const editingPurchaseDemand = ref<any>(null);
const followDialogVisible = ref(false);
const editingContact = ref<any>(null);
const purchaseDemands = ref<any[]>([]);
const purchaseDemandLoading = ref(false);
const purchaseDemandPage = ref(1);
const purchaseDemandPageSize = ref(5);
const purchaseDemandTotal = ref(0);
const followRecords = ref<any[]>([]);

const vendorForm = reactive({
  id: "",
  vendorName: "",
  priorityLevel: "中",
  remark: "",
});

const transferMode = ref<"ASSIGN" | "TRANSFER" | "RETURN">("TRANSFER");
const canManageTransfer = computed(() => authStore.userPermissions.includes("CRM_TRANSFER"));

const priorityRules: Record<string, string> = {
  高: "高：有电话、有联系人、90天内有采购、品类数 >= 3",
  中: "中：有电话、有品类",
  低: "低：干净可导入，但缺电话、缺品类、采购时间较旧或联系人信息较弱",
};

watch(
  () => [props.modelValue, props.vendorId],
  async ([visible, vendorId]) => {
    if (visible && vendorId) await openDetail(String(vendorId));
  },
  { immediate: true }
);

const handleVisibleChange = (value: boolean) => {
  emit("update:modelValue", value);
};

const notifyListChanged = () => {
  emit("refresh-list");
};

const openDetail = async (vendorId: string) => {
  currentVendor.value = null;
  purchaseDemands.value = [];
  followRecords.value = [];
  purchaseDemandPage.value = 1;
  const result = await crmVendorApi.getVendor(vendorId);
  currentVendor.value = result.data;
  await loadPurchaseDemands();
  await loadFollowRecords();
};

const refreshDetail = async (showMessage = true) => {
  if (!currentVendor.value) return;
  const result = await crmVendorApi.getVendor(currentVendor.value.id);
  currentVendor.value = result.data;
  await loadPurchaseDemands();
  await loadFollowRecords();
  if (showMessage) ElMessage.success("厂商详情已刷新");
};

const openEditDialog = async () => {
  if (!currentVendor.value) return;
  Object.assign(vendorForm, {
    id: currentVendor.value.id,
    vendorName: currentVendor.value.vendorName || "",
    priorityLevel: currentVendor.value.priorityLevel || "中",
    remark: currentVendor.value.remark || "",
  });
  vendorDialogVisible.value = true;
};

const submitVendorForm = async () => {
  if (!vendorForm.vendorName.trim()) {
    ElMessage.warning("请输入厂商名称");
    return;
  }

  await crmVendorApi.updateVendor(vendorForm.id, {
    vendorName: vendorForm.vendorName,
    priorityLevel: vendorForm.priorityLevel,
    remark: vendorForm.remark,
  });
  ElMessage.success("编辑成功");
  vendorDialogVisible.value = false;
  await refreshDetail(false);
  notifyListChanged();
};

const openTransferDialog = (mode: "ASSIGN" | "TRANSFER" | "RETURN" = "TRANSFER") => {
  if (!currentVendor.value) return;
  transferMode.value = mode;
  transferDialogVisible.value = true;
};

const openContactDialog = (row?: any) => {
  editingContact.value = row || null;
  contactDialogVisible.value = true;
};

const setPrimaryContact = async (row: any) => {
  if (!currentVendor.value) return;
  await crmVendorApi.setPrimaryVendorContact(currentVendor.value.id, row.id);
  ElMessage.success("主联系人已更新");
  await refreshDetail(false);
  notifyListChanged();
};

const toggleContactStatus = async (row: any) => {
  if (!currentVendor.value) return;
  const status = row.status === "无效" ? "有效" : "无效";
  await crmVendorApi.updateVendorContactStatus(currentVendor.value.id, row.id, {
    status,
    remark: row.remark || "",
  });
  ElMessage.success(status === "无效" ? "联系人已停用" : "联系人已启用");
  await refreshDetail(false);
  notifyListChanged();
};

const openPurchaseDemandDialog = (row?: any) => {
  editingPurchaseDemand.value = row || null;
  purchaseDemandDialogVisible.value = true;
};

const handlePurchaseDemandSaved = async () => {
  purchaseDemandPage.value = 1;
  await refreshDetail(false);
  notifyListChanged();
};

const changePurchaseDemandStatus = async (row: any, status: string) => {
  await crmVendorDemandApi.changeStatus(row.id, { status });
  ElMessage.success("状态已更新");
  await refreshDetail(false);
  notifyListChanged();
};

const closePurchaseDemand = async (row: any) => {
  const { value } = await ElMessageBox.prompt("关闭原因", "关闭采购需求", {
    inputPattern: /.+/,
    inputErrorMessage: "请填写关闭原因",
  });
  await crmVendorDemandApi.changeStatus(row.id, { status: "已关闭", closedReason: value });
  ElMessage.success("采购需求已关闭");
  await refreshDetail(false);
  notifyListChanged();
};

const openFollowDialog = () => {
  if (!currentVendor.value) return;
  followDialogVisible.value = true;
};

const handleRecordSaved = async () => {
  await refreshDetail(false);
  notifyListChanged();
};

const loadPurchaseDemands = async () => {
  if (!currentVendor.value) return;

  purchaseDemandLoading.value = true;
  try {
    const result = await crmVendorApi.getVendorPurchaseDemands(currentVendor.value.id, {
      page: purchaseDemandPage.value,
      pageSize: purchaseDemandPageSize.value,
      sortField: "DemandAt",
      sortDirection: "Descending",
    });
    purchaseDemands.value = result.data?.list || [];
    purchaseDemandTotal.value = result.data?.totalCount || 0;
  } finally {
    purchaseDemandLoading.value = false;
  }
};

const loadFollowRecords = async () => {
  if (!currentVendor.value) return;
  const result = await crmVendorApi.getVendorFollowRecords(currentVendor.value.id);
  followRecords.value = result.data || [];
};

const handlePurchaseDemandPageChange = async (page: number) => {
  purchaseDemandPage.value = page;
  await loadPurchaseDemands();
};

const handlePurchaseDemandSizeChange = async (pageSize: number) => {
  purchaseDemandPageSize.value = pageSize;
  purchaseDemandPage.value = 1;
  await loadPurchaseDemands();
};

const getPriorityRule = (value?: string | null) => {
  if (!value) return "优先级规则：高=电话、联系人、近90天采购、品类数>=3；中=有电话、有品类；低=信息较弱但可导入";
  return priorityRules[value] || value;
};

const getPriorityType = (value?: string | null) => {
  if (value === "高") return "danger";
  if (value === "中") return "warning";
  if (value === "低") return "info";
  return "";
};

const getPurchaseDemandStatusType = (value?: string | null) => {
  if (value === "有效" || value === "已完成") return "success";
  if (value === "待确认") return "warning";
  if (value === "已关闭") return "info";
  return "";
};

const canReturn = (row?: Partial<VendorDetail> | null) =>
  !!row?.ownerUserId && row.ownerUserId === userStore.userInfo.userId;

const formatDate = (value?: string | null) => {
  if (!value) return "-";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;

  const pad = (num: number) => `${num}`.padStart(2, "0");
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())} ${pad(date.getHours())}:${pad(date.getMinutes())}`;
};
</script>

<style scoped lang="scss">
.drawer-layout { min-height: 100%; background: #fff; }.drawer-head { display:grid; grid-template-columns:minmax(0,1fr) auto; gap:20px; padding:22px 0 18px; border-bottom:1px solid var(--el-border-color-light); }.detail-kicker, .head-meta, .summary-band span, .follow-item span, .demand-title-cell span, .empty-value { color:var(--el-text-color-secondary); font-size:13px; }.title-row, .head-meta, .head-actions { display:flex; flex-wrap:wrap; align-items:center; gap:8px; }.title-row h2 { margin:0; font-size:25px; }.head-meta { margin-top:10px; }.head-actions { justify-content:flex-end; }.summary-band { display:grid; grid-template-columns:repeat(4,1fr); gap:12px; padding:14px 0; }.summary-band > div, .detail-card { display:flex; flex-direction:column; gap:5px; padding:16px; border:1px solid var(--el-border-color-light); border-radius:8px; }.detail-grid { display:grid; grid-template-columns:minmax(360px,.9fr) minmax(0,1.75fr); gap:16px; }.detail-content { display:grid; gap:16px; }.section-title { display:flex; align-items:center; justify-content:space-between; gap:10px; margin:0 -16px 14px; padding:0 16px 10px; border-bottom:1px solid var(--el-border-color-lighter); }.section-title h3 { margin:0; font-size:16px; }.activity-row { display:flex; gap:16px; }.activity-row > .detail-card { flex:1; min-width:0; }.follow-item p { margin:6px 0; }.item-tag,.ml8 { margin-right:4px; }.purchase-demand-card { gap:0; }.purchase-demand-table :deep(.el-table__cell) { padding:11px 0; }.purchase-demand-table :deep(.el-table__header th) { background:#fafcfc; color:var(--el-text-color-secondary); font-weight:500; }.demand-title-cell { display:grid; gap:5px; line-height:1.35; }.demand-title-cell strong { color:var(--el-text-color-primary); font-weight:600; }.demand-item-list, .demand-actions { display:flex; flex-wrap:wrap; align-items:center; gap:6px; }.demand-item-tag { margin:0; border-color:#bfe7e2; background:#f0fbf9; color:#177f78; }.demand-item-tag b { margin-left:5px; color:#0f766e; font-weight:600; }.demand-actions { flex-wrap:nowrap; white-space:nowrap; }.table-footer { display:flex; justify-content:flex-end; padding-top:12px; } @media(max-width:1200px){.summary-band,.detail-grid{grid-template-columns:1fr}.activity-row{flex-direction:column}} 
</style>

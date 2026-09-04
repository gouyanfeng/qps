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
        <section class="detail-card contacts-card">
          <div class="section-title">
            <h3>联系人</h3>
            <Permission code="CRM_VENDOR_EDIT"><el-button type="primary" link :icon="Plus" @click="openContactDialog()">新增联系人</el-button></Permission>
          </div>
          <el-table :data="currentVendor.contacts || []" border>
            <el-table-column label="姓名" width="130">
              <template #default="{ row }">
                {{ row.contactName || "-" }}<el-tag v-if="row.isPrimary" size="small" type="success" class="ml8">主</el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="phone" label="电话" width="150" />
            <el-table-column label="类型" width="90"><template #default="{ row }">{{ row.phoneType || "-" }}</template></el-table-column>
            <el-table-column label="角色" min-width="110"><template #default="{ row }">{{ row.roleName || "-" }}</template></el-table-column>
            <el-table-column prop="remark" label="备注" min-width="180" />
            <el-table-column label="状态" width="90">
              <template #default="{ row }"><el-tag :type="row.status === '无效' ? 'danger' : 'info'" size="small">{{ row.status || "-" }}</el-tag></template>
            </el-table-column>
            <el-table-column label="操作" width="210">
              <template #default="{ row }">
                <Permission code="CRM_VENDOR_EDIT">
                  <el-button type="primary" link :icon="Edit" @click="openContactDialog(row)">编辑</el-button>
                  <el-button v-if="!row.isPrimary && row.status !== '无效'" type="primary" link @click="setPrimaryContact(row)">设为主</el-button>
                  <el-button type="primary" link @click="toggleContactStatus(row)">{{ row.status === "无效" ? "启用" : "停用" }}</el-button>
                </Permission>
              </template>
            </el-table-column>
          </el-table>
        </section>

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
            <section class="detail-card">
              <div class="section-title">
                <h3>沟通记录</h3>
                <Permission code="CRM_FOLLOW"><el-button type="primary" link :icon="Phone" @click="openFollowDialog">记录</el-button></Permission>
              </div>
              <el-timeline v-if="followRecords.length">
                <el-timeline-item v-for="record in followRecords" :key="record.id" :timestamp="formatDate(record.createdAt)">
                  <div class="follow-item">
                    <strong>{{ record.followResult || "沟通" }}</strong> <el-tag size="small">{{ record.followType || "-" }}</el-tag>
                    <p>{{ record.content || "-" }}</p>
                    <span>{{ record.contactName || "未指定联系人" }} · 下次 {{ formatDate(record.nextFollowAt) }}</span>
                  </div>
                </el-timeline-item>
              </el-timeline>
              <el-empty v-else description="暂无沟通记录" />
            </section>
            <section class="detail-card">
              <div class="section-title"><h3>流转记录</h3></div>
              <el-timeline v-if="currentVendor.transferRecords?.length">
                <el-timeline-item v-for="record in currentVendor.transferRecords" :key="record.id" :timestamp="formatDate(record.createdAt)">
                  <div class="follow-item">
                    <strong>{{ record.actionType || "流转" }}：{{ record.fromOwnerUserName || "未分配" }} 至 {{ record.toOwnerUserName || "未分配" }}</strong>
                    <p v-if="record.remark">{{ record.remark }}</p>
                    <span>操作人 {{ record.operatorUserName || "-" }}</span>
                  </div>
                </el-timeline-item>
              </el-timeline>
              <el-empty v-else description="暂无流转记录" />
            </section>
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

  <el-dialog v-model="transferDialogVisible" :title="transferDialogTitle" width="520px">
    <el-form :model="transferForm" label-width="90px">
      <el-form-item label="已选厂商">
        <span>{{ transferForm.entityIds.length }} 个</span>
      </el-form-item>
      <el-form-item v-if="transferMode !== 'RETURN'" label="跟进人">
        <el-select v-model="transferForm.ownerUserId" filterable placeholder="请选择跟进人">
          <el-option v-for="user in ownerOptions" :key="user.id" :label="getUserDisplayName(user)" :value="user.id" />
        </el-select>
      </el-form-item>
      <el-form-item label="备注">
        <el-input v-model="transferForm.remark" type="textarea" :rows="3" placeholder="可选" />
      </el-form-item>
    </el-form>
    <template #footer>
      <el-button @click="transferDialogVisible = false">取消</el-button>
      <el-button type="primary" @click="submitTransfer">保存</el-button>
    </template>
  </el-dialog>

  <el-dialog v-model="contactDialogVisible" :title="contactForm.id ? '编辑联系人' : '新增联系人'" width="520px">
    <el-form :model="contactForm" label-width="100px">
      <el-form-item label="姓名"><el-input v-model="contactForm.contactName" clearable placeholder="联系人姓名" /></el-form-item>
      <el-form-item label="电话"><el-input v-model="contactForm.phone" clearable placeholder="联系电话" /></el-form-item>
      <el-form-item label="电话类型">
        <el-select v-model="contactForm.phoneType">
          <el-option label="手机" value="手机" />
          <el-option label="座机" value="座机" />
          <el-option label="未知" value="未知" />
        </el-select>
      </el-form-item>
      <el-form-item label="微信"><el-input v-model="contactForm.wechat" clearable placeholder="微信号" /></el-form-item>
      <el-form-item label="角色">
        <el-select v-model="contactForm.roleName" clearable placeholder="请选择角色">
          <el-option label="负责人" value="负责人" />
          <el-option label="采购" value="采购" />
          <el-option label="财务" value="财务" />
          <el-option label="其他" value="其他" />
        </el-select>
      </el-form-item>
      <el-form-item label="主联系人"><el-switch v-model="contactForm.isPrimary" /></el-form-item>
      <el-form-item label="备注"><el-input v-model="contactForm.remark" type="textarea" :rows="3" /></el-form-item>
    </el-form>
    <template #footer>
      <el-button @click="contactDialogVisible = false">取消</el-button>
      <el-button type="primary" @click="submitContact">保存</el-button>
    </template>
  </el-dialog>

  <el-dialog v-model="followDialogVisible" title="记录沟通" width="560px">
    <el-form :model="followForm" label-width="100px">
      <el-form-item label="联系人">
        <el-select v-model="followForm.contactId" clearable placeholder="可不指定">
          <el-option v-for="contact in followContacts" :key="contact.id" :label="contact.contactName || contact.phone" :value="contact.id" />
        </el-select>
      </el-form-item>
      <el-form-item label="沟通方式">
        <el-select v-model="followForm.followType">
          <el-option label="电话" value="电话" />
          <el-option label="微信" value="微信" />
          <el-option label="拜访" value="拜访" />
        </el-select>
      </el-form-item>
      <el-form-item label="沟通结果" required>
        <el-select v-model="followForm.followResult" placeholder="请选择结果">
          <el-option label="已接通" value="已接通" />
          <el-option label="未接" value="未接" />
          <el-option label="空号" value="空号" />
          <el-option label="有意向" value="有意向" />
          <el-option label="无意向" value="无意向" />
        </el-select>
      </el-form-item>
      <el-form-item label="意向等级">
        <el-select v-model="followForm.intentLevel" clearable placeholder="意向等级">
          <el-option label="A" value="A" />
          <el-option label="B" value="B" />
          <el-option label="C" value="C" />
        </el-select>
      </el-form-item>
      <el-form-item label="沟通内容"><el-input v-model="followForm.content" type="textarea" :rows="4" placeholder="记录沟通要点" /></el-form-item>
      <el-form-item label="下次跟进">
        <el-date-picker
          v-model="followForm.nextFollowAt"
          type="datetime"
          value-format="YYYY-MM-DDTHH:mm:ss"
          :disabled-date="disablePastFollowDate"
          placeholder="请选择时间"
        />
      </el-form-item>
    </el-form>
    <template #footer>
      <el-button @click="followDialogVisible = false">取消</el-button>
      <el-button type="primary" @click="submitFollowRecord">保存</el-button>
    </template>
  </el-dialog>

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
import { Edit, Phone, Plus, Refresh } from "@element-plus/icons-vue";
import { ElMessage, ElMessageBox } from "element-plus";
import CrmVendorDemandEditor from "@/components/CrmVendorDemandEditor/index.vue";
import { crmVendorApi } from "@/api/modules/crmVendor";
import crmVendorDemandApi from "@/api/modules/crmVendorDemand";
import { userApi } from "@/api/modules/user";
import Permission from "@/components/Permission/index.vue";
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
const ownerOptions = ref<any[]>([]);
const purchaseDemands = ref<any[]>([]);
const purchaseDemandLoading = ref(false);
const purchaseDemandPage = ref(1);
const purchaseDemandPageSize = ref(5);
const purchaseDemandTotal = ref(0);
const followRecords = ref<any[]>([]);
const followContacts = computed(() => (currentVendor.value?.contacts || []).filter((contact: any) => contact.status !== "无效"));

const vendorForm = reactive({
  id: "",
  vendorName: "",
  priorityLevel: "中",
  remark: "",
});

const transferForm = reactive({
  entityIds: [] as string[],
  ownerUserId: "",
  remark: "",
});
const transferMode = ref<"ASSIGN" | "TRANSFER" | "RETURN">("TRANSFER");
const canManageTransfer = computed(() => authStore.userPermissions.includes("CRM_TRANSFER"));
const transferDialogTitle = computed(() => ({
  ASSIGN: "分配跟进人",
  TRANSFER: "转交跟进人",
  RETURN: "退回待分配池",
})[transferMode.value]);

const contactForm = reactive({
  id: "",
  contactName: "",
  phone: "",
  phoneType: "未知",
  wechat: "",
  roleName: "",
  isPrimary: false,
  remark: "",
});

const followForm = reactive({
  contactId: undefined as string | undefined,
  followType: "电话",
  followResult: "",
  intentLevel: "",
  content: "",
  nextFollowAt: "",
});

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

const getUserDisplayName = (user: any) => user.realName || user.username || user.name || "-";

const loadOwnerOptions = async () => {
  const res = await userApi.getUserList({ page: 1, pageSize: 100, username: "", realName: "", roleId: "", isActive: true });
  ownerOptions.value = (res.data?.list || []).filter((user: any) => user.isActive);
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
  await loadOwnerOptions();
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

const openTransferDialog = async (mode: "ASSIGN" | "TRANSFER" | "RETURN" = "TRANSFER") => {
  if (!currentVendor.value) return;
  transferMode.value = mode;
  if (mode !== "RETURN") await loadOwnerOptions();
  Object.assign(transferForm, {
    entityIds: [currentVendor.value.id],
    ownerUserId: "",
    remark: "",
  });
  transferDialogVisible.value = true;
};

const submitTransfer = async () => {
  if (transferMode.value !== "RETURN" && !transferForm.ownerUserId) {
    ElMessage.warning("请选择跟进人");
    return;
  }

  await crmVendorApi.changeOwner({
    entityIds: transferForm.entityIds,
    toOwnerUserId: transferMode.value === "RETURN" ? null : transferForm.ownerUserId,
    remark: transferForm.remark,
  });
  ElMessage.success("流转成功");
  transferDialogVisible.value = false;
  await refreshDetail(false);
  notifyListChanged();
};

const resetContactForm = () => {
  Object.assign(contactForm, {
    id: "",
    contactName: "",
    phone: "",
    phoneType: "未知",
    wechat: "",
    roleName: "",
    isPrimary: false,
    remark: "",
  });
};

const openContactDialog = (row?: any) => {
  resetContactForm();
  if (row) Object.assign(contactForm, { ...row });
  contactDialogVisible.value = true;
};

const isValidPhone = (phone: string) => /^1[3-9]\d{9}$/.test(phone) || /^0\d{2,3}-?\d{7,8}(-\d{1,6})?$/.test(phone);

const submitContact = async () => {
  if (!currentVendor.value) return;
  const phone = contactForm.phone.trim();
  if (!contactForm.contactName.trim() && !phone) {
    ElMessage.error("请填写联系人姓名或电话");
    return;
  }
  if (phone && !isValidPhone(phone)) {
    ElMessage.error("联系电话格式不正确");
    return;
  }

  const request = {
    ...contactForm,
    contactName: contactForm.contactName.trim(),
    phone,
  };

  if (contactForm.id) await crmVendorApi.updateContact(contactForm.id, request);
  else await crmVendorApi.createContact(currentVendor.value.id, request);

  ElMessage.success("联系人已保存");
  contactDialogVisible.value = false;
  await refreshDetail(false);
  notifyListChanged();
};

const setPrimaryContact = async (row: any) => {
  if (!currentVendor.value) return;
  await crmVendorApi.setPrimaryContact(row.id);
  ElMessage.success("主联系人已更新");
  await refreshDetail(false);
  notifyListChanged();
};

const toggleContactStatus = async (row: any) => {
  if (!currentVendor.value) return;
  const status = row.status === "无效" ? "有效" : "无效";
  await crmVendorApi.updateContactStatus(row.id, {
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

const resetFollowForm = () => {
  Object.assign(followForm, {
    contactId: undefined,
    followType: "电话",
    followResult: "",
    intentLevel: "",
    content: "",
    nextFollowAt: "",
  });
};

const openFollowDialog = () => {
  if (!currentVendor.value) return;
  resetFollowForm();
  const primaryContact = followContacts.value.find((contact: any) => contact.isPrimary);
  followForm.contactId = primaryContact?.id;
  followDialogVisible.value = true;
};

const submitFollowRecord = async () => {
  if (!currentVendor.value) return;
  if (!followForm.followResult) {
    ElMessage.error("请选择沟通结果");
    return;
  }
  if (followForm.nextFollowAt && new Date(followForm.nextFollowAt).getTime() <= Date.now()) {
    ElMessage.error("下次跟进时间必须晚于当前时间");
    return;
  }

  await crmVendorApi.createFollowRecord(currentVendor.value.id, {
    ...followForm,
    contactId: followForm.contactId || null,
    nextFollowAt: followForm.nextFollowAt || null,
  });
  ElMessage.success("沟通记录已保存");
  followDialogVisible.value = false;
  await refreshDetail(false);
  notifyListChanged();
};

const disablePastFollowDate = (date: Date) => date.getTime() < new Date().setHours(0, 0, 0, 0);

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
  const result = await crmVendorApi.getFollowRecords(currentVendor.value.id);
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

<template>
  <div class="vendor-page">
    <QueryPage api="/admin/crm/vendors" :searchParam="searchForm" @reset="handleReset" ref="queryPageRef">
      <template #searchConditions>
        <el-form :model="searchForm" :inline="true">
          <el-form-item label="关键词">
            <el-input v-model="searchForm.keyword" clearable placeholder="厂商 / 联系人 / 电话 / 品类" />
          </el-form-item>
          <el-form-item>
            <template #label>
              <span class="filter-label-with-help">
                优先级
                <el-tooltip :content="getPriorityRule()" placement="top">
                  <el-icon class="filter-help-icon" :title="getPriorityRule()">
                    <QuestionFilled />
                  </el-icon>
                </el-tooltip>
              </span>
            </template>
            <el-select v-model="searchForm.priorityLevel" clearable placeholder="优先级">
              <el-option label="高" value="High" />
              <el-option label="中" value="Medium" />
              <el-option label="低" value="Low" />
            </el-select>
          </el-form-item>
          <el-form-item label="电话">
            <el-select v-model="searchForm.hasPhone" clearable placeholder="电话状态">
              <el-option label="有电话" :value="true" />
              <el-option label="无电话" :value="false" />
            </el-select>
          </el-form-item>
          <el-form-item label="品类">
            <el-select v-model="searchForm.hasProduct" clearable placeholder="品类状态">
              <el-option label="有品类" :value="true" />
              <el-option label="无品类" :value="false" />
            </el-select>
          </el-form-item>
        </el-form>
      </template>

      <template #headerButtons>
        <Permission code="CRM_VENDOR_ASSIGN"><el-button :icon="Edit" @click="openAssignDialog()">分配</el-button></Permission>
        <Permission code="CRM_VENDOR_ADD"><el-button type="primary" :icon="Plus" @click="openCreateDialog">新增厂商</el-button></Permission>
      </template>

      <template #table="{ tableData }">
        <el-table
          :data="tableData"
          :row-key="'id'"
          :fit="true"
          class="wide-list-table"
          style="--table-min-width: 1500px"
          border
          @selection-change="handleSelectionChange"
        >
          <el-table-column type="selection" width="44" fixed="left" />
          <el-table-column label="厂商名称" min-width="200" fixed="left" show-overflow-tooltip>
            <template #default="{ row }">
              <el-button type="primary" link class="vendor-link" @click="openDetail(row)">
                {{ row.vendorName || "-" }}
              </el-button>
            </template>
          </el-table-column>
          <el-table-column label="优先级" width="92">
            <template #default="{ row }">
              <el-tooltip :content="getPriorityRule(row.priorityLevel)" placement="top">
                <el-tag :type="getPriorityType(row.priorityLevel)" :title="getPriorityRule(row.priorityLevel)">
                  {{ formatPriority(row.priorityLevel) }}
                </el-tag>
              </el-tooltip>
            </template>
          </el-table-column>
          <el-table-column label="主联系人 / 电话" width="156">
            <template #default="{ row }">
              <div class="cell-main">
                <span>{{ row.primaryContactName || "-" }}</span>
                <span class="phone-text">{{ row.primaryContactPhone || "-" }}</span>
              </div>
            </template>
          </el-table-column>
          <el-table-column label="负责人" width="104" show-overflow-tooltip>
            <template #default="{ row }">{{ row.ownerUserName || "未分配" }}</template>
          </el-table-column>
          <el-table-column label="采购计划" width="88" align="right">
            <template #default="{ row }">{{ row.purchasePlanCount || 0 }}</template>
          </el-table-column>
          <el-table-column label="品类" width="72" align="right">
            <template #default="{ row }">{{ row.productCount || 0 }}</template>
          </el-table-column>
          <el-table-column label="最近采购时间" width="160">
            <template #default="{ row }">{{ formatDate(row.latestPurchaseTime) }}</template>
          </el-table-column>
          <el-table-column label="最近采购计划" min-width="280" show-overflow-tooltip>
            <template #default="{ row }">{{ row.latestPurchasePlanName || "-" }}</template>
          </el-table-column>
          <el-table-column label="更新时间" width="160">
            <template #default="{ row }">{{ formatDate(row.updatedAt) }}</template>
          </el-table-column>
          <el-table-column label="操作" width="220" fixed="right" class-name="actions-column" header-class-name="actions-column">
            <template #default="{ row }">
              <Permission code="CRM_VENDOR_ASSIGN"><el-button type="primary" link :icon="Edit" @click="openAssignDialog([row])">分配</el-button></Permission>
              <Permission code="CRM_VENDOR_EDIT"><el-button type="primary" link :icon="Edit" @click="openEditDialog(row)">编辑</el-button></Permission>
              <el-button type="primary" link :icon="View" @click="openDetail(row)">详情</el-button>
            </template>
          </el-table-column>
        </el-table>
      </template>
    </QueryPage>

    <el-drawer v-model="detailDrawerVisible" size="82%" :with-header="false" class="vendor-drawer">
      <div v-if="currentVendor" class="drawer-layout">
        <section class="drawer-head">
          <div class="head-main">
            <div class="detail-kicker">厂商详情</div>
            <div class="title-row">
              <h2>{{ currentVendor.vendorName }}</h2>
              <el-tooltip :content="getPriorityRule(currentVendor.priorityLevel)" placement="top">
                <el-tag :type="getPriorityType(currentVendor.priorityLevel)" effect="dark" :title="getPriorityRule(currentVendor.priorityLevel)">
                  {{ formatPriority(currentVendor.priorityLevel) }}
                </el-tag>
              </el-tooltip>
            </div>
            <div class="head-meta">
              <span>采购计划 {{ currentVendor.purchasePlanCount || 0 }}</span>
              <span>品类 {{ currentVendor.productCount || 0 }}</span>
              <span>联系人 {{ currentVendor.contactCount || 0 }}</span>
            </div>
          </div>
          <div class="head-actions">
            <Permission code="CRM_VENDOR_ASSIGN"><el-button :icon="Edit" @click="openAssignDialog([currentVendor])">分配</el-button></Permission>
            <Permission code="CRM_VENDOR_EDIT"><el-button :icon="Edit" @click="openEditDialog(currentVendor)">编辑</el-button></Permission>
            <el-button :icon="Refresh" @click="refreshDetail">刷新</el-button>
          </div>
        </section>

        <section class="summary-band">
          <div class="summary-card">
            <span class="label">采购概况</span>
            <strong>{{ currentVendor.purchasePlanCount || 0 }} 个计划</strong>
            <span>品类 {{ currentVendor.productCount || 0 }} · 联系人 {{ currentVendor.contactCount || 0 }}</span>
          </div>
          <div class="summary-card">
            <span class="label">主联系人</span>
            <strong>{{ currentVendor.primaryContactName || "-" }}</strong>
            <span>{{ currentVendor.primaryContactPhone || "-" }}</span>
          </div>
          <div class="summary-card">
            <span class="label">最近采购</span>
            <strong>{{ formatDate(currentVendor.latestPurchaseTime) }}</strong>
            <span>{{ currentVendor.latestPurchasePlanName || "-" }}</span>
          </div>
          <div class="summary-card">
            <span class="label">更新时间</span>
            <strong>{{ formatDate(currentVendor.updatedAt) }}</strong>
            <span>{{ currentVendor.remark || "-" }}</span>
          </div>
        </section>

        <section class="detail-grid">
          <div class="detail-column detail-card">
            <div class="section-title section-title-first">
              <h3>联系人</h3>
            </div>
            <el-table :data="currentVendor.contacts || []" border>
              <el-table-column label="姓名" width="130" show-overflow-tooltip>
                <template #default="{ row }">
                  <span>{{ row.contactName || "-" }}</span>
                  <el-tag v-if="row.isPrimary" size="small" type="success" class="ml8">主</el-tag>
                </template>
              </el-table-column>
              <el-table-column prop="phone" label="电话" width="150" show-overflow-tooltip />
              <el-table-column label="类型" width="90">
                <template #default="{ row }">{{ formatPhoneType(row.phoneType) }}</template>
              </el-table-column>
              <el-table-column label="角色" min-width="110" show-overflow-tooltip>
                <template #default="{ row }">{{ formatRole(row.roleName) }}</template>
              </el-table-column>
              <el-table-column prop="remark" label="备注" min-width="180" show-overflow-tooltip />
              <el-table-column label="状态" width="90">
                <template #default="{ row }">
                  <el-tag :type="row.status === 'INVALID' ? 'danger' : 'info'" size="small">
                    {{ formatContactStatus(row.status) }}
                  </el-tag>
                </template>
              </el-table-column>
            </el-table>

            <div class="section-title">
              <h3>采购品类</h3>
            </div>
            <div v-if="currentVendor.products?.length" class="product-tags">
              <el-tooltip v-for="item in currentVendor.products" :key="item.id" :content="item.remark || item.productName" placement="top">
                <el-tag effect="plain">{{ item.productName }}</el-tag>
              </el-tooltip>
            </div>
            <el-empty v-else description="暂无品类" />
          </div>

          <div class="detail-column detail-card">
            <div class="section-title section-title-first">
              <h3>采购计划</h3>
            </div>
            <el-table :data="purchasePlans" v-loading="purchasePlanLoading" border>
              <el-table-column label="计划名称" min-width="220" show-overflow-tooltip>
                <template #default="{ row }">{{ row.purchasePlanName || "-" }}</template>
              </el-table-column>
              <el-table-column label="采购时间" width="150">
                <template #default="{ row }">{{ formatDate(row.purchaseTime) }}</template>
              </el-table-column>
              <el-table-column label="品类数量" min-width="240" show-overflow-tooltip>
                <template #default="{ row }">{{ row.products || "-" }}</template>
              </el-table-column>
              <el-table-column label="网页" width="92">
                <template #default="{ row }">
                  <el-button v-if="row.pageUrl" type="primary" link :icon="Link" @click="openPage(row.pageUrl)">打开</el-button>
                  <span v-else>-</span>
                </template>
              </el-table-column>
            </el-table>
            <div class="table-footer">
              <el-pagination
                v-model:current-page="purchasePlanPage"
                v-model:page-size="purchasePlanPageSize"
                :page-sizes="[10, 20, 50, 100]"
                :total="purchasePlanTotal"
                layout="total, sizes, prev, pager, next, jumper"
                background
                @size-change="handlePurchasePlanSizeChange"
                @current-change="handlePurchasePlanPageChange"
              />
            </div>
          </div>
        </section>
      </div>
    </el-drawer>

    <el-dialog v-model="vendorDialogVisible" :title="isEdit ? '编辑厂商' : '新增厂商'" width="560px">
      <el-form :model="vendorForm" label-width="110px">
        <el-form-item label="厂商名称" required>
          <el-input v-model="vendorForm.vendorName" clearable placeholder="请输入厂商名称" />
        </el-form-item>
        <el-form-item label="优先级">
          <el-select v-model="vendorForm.priorityLevel" placeholder="请选择优先级">
            <el-option label="高" value="High" />
            <el-option label="中" value="Medium" />
            <el-option label="低" value="Low" />
          </el-select>
        </el-form-item>
        <el-form-item label="负责人">
          <el-select v-model="vendorForm.ownerUserId" clearable filterable placeholder="请选择负责人">
            <el-option v-for="user in ownerOptions" :key="user.id" :label="getUserDisplayName(user)" :value="user.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="最近采购时间">
          <el-date-picker v-model="vendorForm.latestPurchaseTime" type="datetime" value-format="YYYY-MM-DDTHH:mm:ss" placeholder="请选择时间" />
        </el-form-item>
        <el-form-item label="最近采购计划">
          <el-input v-model="vendorForm.latestPurchasePlanName" clearable placeholder="请输入最近采购计划" />
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

    <el-dialog v-model="assignDialogVisible" title="分配厂商" width="520px">
      <el-form :model="assignForm" label-width="90px">
        <el-form-item label="已选厂商">
          <span>{{ assignForm.vendorIds.length }} 个</span>
        </el-form-item>
        <el-form-item label="负责人">
          <el-select v-model="assignForm.ownerUserId" clearable filterable placeholder="清空则取消负责人">
            <el-option v-for="user in ownerOptions" :key="user.id" :label="getUserDisplayName(user)" :value="user.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="assignForm.remark" type="textarea" :rows="3" placeholder="可选" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="assignDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="submitAssignOwner">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts" name="vendor">
import { reactive, ref } from "vue";
import { Edit, Link, Plus, QuestionFilled, Refresh, View } from "@element-plus/icons-vue";
import { ElMessage } from "element-plus";
import QueryPage from "@/components/QueryPage/index.vue";
import { crmVendorApi } from "@/api/modules/crmVendor";
import { userApi } from "@/api/modules/user";
import Permission from "@/components/Permission/index.vue";

interface VendorDetail {
  id: string;
  vendorName: string;
  normalizedVendorName: string;
  priorityLevel: string;
  latestPurchaseTime?: string | null;
  latestPurchasePlanName: string;
  remark: string;
  ownerUserId?: string | null;
  ownerUserName?: string | null;
  primaryContactName: string;
  primaryContactPhone: string;
  purchasePlanCount: number;
  productCount: number;
  contactCount: number;
  createdAt: string;
  updatedAt: string;
  contacts: any[];
  products: any[];
  purchasePlans: any[];
}

const queryPageRef = ref();

const detailDrawerVisible = ref(false);
const vendorDialogVisible = ref(false);
const assignDialogVisible = ref(false);
const isEdit = ref(false);
const currentVendor = ref<VendorDetail | null>(null);
const selectedVendors = ref<VendorDetail[]>([]);
const ownerOptions = ref<any[]>([]);
const purchasePlans = ref<any[]>([]);
const purchasePlanLoading = ref(false);
const purchasePlanPage = ref(1);
const purchasePlanPageSize = ref(10);
const purchasePlanTotal = ref(0);

const searchForm = reactive({
  keyword: "",
  priorityLevel: "",
  hasPhone: undefined as boolean | undefined,
  hasProduct: undefined as boolean | undefined,
});

const vendorForm = reactive({
  id: "",
  vendorName: "",
  priorityLevel: "Medium",
  latestPurchaseTime: "",
  latestPurchasePlanName: "",
  ownerUserId: "",
  remark: "",
});

const assignForm = reactive({
  vendorIds: [] as string[],
  ownerUserId: "",
  remark: "",
});

const priorityLabels: Record<string, string> = {
  High: "高",
  Medium: "中",
  Low: "低",
};

const priorityRules: Record<string, string> = {
  High: "高：有电话、有联系人、90天内有采购、品类数 >= 3",
  Medium: "中：有电话、有品类",
  Low: "低：干净可导入，但缺电话、缺品类、采购时间较旧或联系人信息较弱",
};

const phoneTypeLabels: Record<string, string> = {
  MOBILE: "手机",
  LANDLINE: "座机",
  UNKNOWN: "未知",
};

const roleLabels: Record<string, string> = {
  OWNER: "负责人",
  PURCHASE: "采购",
  FINANCE: "财务",
  BASE_OWNER: "基地负责人",
  COOPERATIVE_OWNER: "合作社负责人",
  OTHER: "其他",
  采购: "采购",
};

const contactStatusLabels: Record<string, string> = {
  UNVERIFIED: "未验证",
  VALID: "有效",
  INVALID: "无效",
};

const handleReset = () => {
  searchForm.keyword = "";
  searchForm.priorityLevel = "";
  searchForm.hasPhone = undefined;
  searchForm.hasProduct = undefined;
};

const reloadList = () => {
  queryPageRef.value?.getTableList();
};

const handleSelectionChange = (rows: VendorDetail[]) => {
  selectedVendors.value = rows;
};

const getUserDisplayName = (user: any) => user.realName || user.username || user.name || "-";

const loadOwnerOptions = async () => {
  const res = await userApi.getUserList({ page: 1, pageSize: 100, username: "", realName: "", roleId: "", isActive: true });
  ownerOptions.value = (res.data?.list || []).filter((user: any) => user.isActive);
};

const resetVendorForm = () => {
  Object.assign(vendorForm, {
    id: "",
    vendorName: "",
    priorityLevel: "Medium",
    latestPurchaseTime: "",
    latestPurchasePlanName: "",
    ownerUserId: "",
    remark: "",
  });
};

const openCreateDialog = async () => {
  isEdit.value = false;
  resetVendorForm();
  await loadOwnerOptions();
  vendorDialogVisible.value = true;
};

const toDateTimeInputValue = (value?: string | null) => {
  if (!value) return "";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;

  const pad = (num: number) => `${num}`.padStart(2, "0");
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`;
};

const openEditDialog = async (row: VendorDetail) => {
  isEdit.value = true;
  await loadOwnerOptions();
  Object.assign(vendorForm, {
    id: row.id,
    vendorName: row.vendorName || "",
    priorityLevel: row.priorityLevel || "Medium",
    latestPurchaseTime: toDateTimeInputValue(row.latestPurchaseTime),
    latestPurchasePlanName: row.latestPurchasePlanName || "",
    ownerUserId: row.ownerUserId || "",
    remark: row.remark || "",
  });
  vendorDialogVisible.value = true;
};

const submitVendorForm = async () => {
  if (!vendorForm.vendorName.trim()) {
    ElMessage.warning("请输入厂商名称");
    return;
  }

  const payload = {
    vendorName: vendorForm.vendorName,
    priorityLevel: vendorForm.priorityLevel,
    latestPurchaseTime: vendorForm.latestPurchaseTime || null,
    latestPurchasePlanName: vendorForm.latestPurchasePlanName,
    ownerUserId: vendorForm.ownerUserId || null,
    remark: vendorForm.remark,
  };

  if (isEdit.value) {
    await crmVendorApi.updateVendor(vendorForm.id, payload);
    ElMessage.success("编辑成功");
    if (currentVendor.value?.id === vendorForm.id) {
      const result = await crmVendorApi.getVendor(vendorForm.id);
      currentVendor.value = result.data;
    }
  } else {
    await crmVendorApi.createVendor(payload);
    ElMessage.success("新增成功");
  }

  vendorDialogVisible.value = false;
  reloadList();
};

const openAssignDialog = async (vendors?: VendorDetail[]) => {
  const rows = vendors || selectedVendors.value;
  if (rows.length === 0) {
    ElMessage.warning("请选择要分配的厂商");
    return;
  }

  await loadOwnerOptions();
  Object.assign(assignForm, {
    vendorIds: rows.map(item => item.id),
    ownerUserId: rows.length === 1 ? rows[0].ownerUserId || "" : "",
    remark: "",
  });
  assignDialogVisible.value = true;
};

const submitAssignOwner = async () => {
  await crmVendorApi.assignOwner({
    vendorIds: assignForm.vendorIds,
    ownerUserId: assignForm.ownerUserId || null,
    remark: assignForm.remark,
  });
  ElMessage.success("分配成功");
  assignDialogVisible.value = false;
  reloadList();
  if (currentVendor.value && assignForm.vendorIds.includes(currentVendor.value.id)) {
    await refreshDetail();
  }
};

const openDetail = async (row: any) => {
  const result = await crmVendorApi.getVendor(row.id);
  currentVendor.value = result.data;
  detailDrawerVisible.value = true;
  purchasePlanPage.value = 1;
  await loadPurchasePlans();
};

const refreshDetail = async () => {
  if (!currentVendor.value) return;
  const result = await crmVendorApi.getVendor(currentVendor.value.id);
  currentVendor.value = result.data;
  await loadPurchasePlans();
  ElMessage.success("厂商详情已刷新");
};

const loadPurchasePlans = async () => {
  if (!currentVendor.value) return;

  purchasePlanLoading.value = true;
  try {
    const result = await crmVendorApi.getVendorPurchasePlans(currentVendor.value.id, {
      page: purchasePlanPage.value,
      pageSize: purchasePlanPageSize.value,
      sortField: "PurchaseTime",
      sortDirection: "Descending",
    });
    purchasePlans.value = result.data?.list || [];
    purchasePlanTotal.value = result.data?.totalCount || 0;
  } finally {
    purchasePlanLoading.value = false;
  }
};

const handlePurchasePlanPageChange = async (page: number) => {
  purchasePlanPage.value = page;
  await loadPurchasePlans();
};

const handlePurchasePlanSizeChange = async (pageSize: number) => {
  purchasePlanPageSize.value = pageSize;
  purchasePlanPage.value = 1;
  await loadPurchasePlans();
};

const openPage = (url: string) => {
  window.open(url, "_blank", "noopener,noreferrer");
};

const formatPriority = (value?: string | null) => {
  if (!value) return "-";
  return priorityLabels[value] || value;
};

const getPriorityRule = (value?: string | null) => {
  if (!value) return "优先级规则：高=电话、联系人、近90天采购、品类数>=3；中=有电话、有品类；低=信息较弱但可导入";
  return priorityRules[value] || value;
};

const getPriorityType = (value?: string | null) => {
  if (value === "High") return "danger";
  if (value === "Medium") return "warning";
  if (value === "Low") return "info";
  return "";
};

const formatPhoneType = (value?: string | null) => {
  if (!value) return "-";
  return phoneTypeLabels[value] || value;
};

const formatRole = (value?: string | null) => {
  if (!value) return "-";
  return roleLabels[value] || value;
};

const formatContactStatus = (value?: string | null) => {
  if (!value) return "-";
  return contactStatusLabels[value] || value;
};

const formatDate = (value?: string | null) => {
  if (!value) return "-";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;

  const pad = (num: number) => `${num}`.padStart(2, "0");
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())} ${pad(date.getHours())}:${pad(date.getMinutes())}`;
};
</script>

<style scoped lang="scss">
.vendor-page {
  .vendor-link {
    padding: 0;
    font-weight: 600;
  }

  .cell-main {
    display: flex;
    min-width: 0;
    flex-direction: column;
    gap: 2px;
    line-height: 1.35;
  }

  .phone-text {
    color: var(--el-text-color-secondary);
    font-size: 12px;
  }

  .filter-label-with-help {
    display: inline-flex;
    align-items: center;
    gap: 4px;
  }

  .filter-help-icon {
    color: var(--el-text-color-secondary);
    cursor: help;
    font-size: 15px;
  }

  .drawer-layout {
    min-height: 100%;
    padding: 0;
    background: #ffffff;
  }

  .drawer-head {
    display: grid;
    grid-template-columns: minmax(0, 1fr) auto;
    gap: 20px;
    align-items: flex-start;
    padding: 22px 0 18px;
    border-bottom: 1px solid var(--el-border-color-light);
    background: #ffffff;

    h2 {
      margin: 0;
      color: #111827;
      font-size: 25px;
      font-weight: 700;
      line-height: 1.25;
    }
  }

  .head-main {
    min-width: 0;
  }

  .title-row {
    display: flex;
    min-width: 0;
    flex-wrap: wrap;
    align-items: center;
    gap: 10px;
    margin-top: 6px;
  }

  .detail-kicker {
    margin-bottom: 6px;
    color: var(--el-text-color-secondary);
    font-size: 12px;
    font-weight: 600;
  }

  .eyebrow {
    color: var(--el-text-color-secondary);
    font-size: 12px;
  }

  .head-meta,
  .head-actions {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 8px;
  }

  .head-meta {
    margin-top: 10px;
    line-height: 1.7;

    > span {
      display: inline-flex;
      align-items: center;
      min-height: 24px;
      padding-right: 10px;
      border-right: 1px solid var(--el-border-color-lighter);
      color: var(--el-text-color-secondary);
      font-size: 13px;

      &:last-child {
        border-right: 0;
      }
    }
  }

  .head-actions {
    justify-content: flex-end;

    :deep(.el-button) {
      margin-left: 0;
    }
  }

  .head-meta span {
    color: var(--el-text-color-secondary);
    font-size: 13px;
  }

  .summary-band {
    display: grid;
    grid-template-columns: repeat(4, minmax(0, 1fr));
    gap: 12px;
    padding: 14px 0;
    background: #ffffff;

    > div {
      display: flex;
      min-width: 0;
      min-height: 76px;
      flex-direction: column;
      justify-content: center;
      gap: 5px;
      padding: 13px 16px;
      border: 1px solid var(--el-border-color-light);
      border-radius: 8px;
      background: #ffffff;
    }

    .label {
      color: var(--el-text-color-secondary);
      font-size: 12px;
    }

    strong {
      color: #111827;
      font-size: 15px;
      line-height: 1.35;
    }

    span:last-child {
      overflow-wrap: anywhere;
      color: var(--el-text-color-secondary);
    }
  }

  .detail-grid {
    display: grid;
    grid-template-columns: minmax(360px, 0.9fr) minmax(0, 1.75fr);
    gap: 16px;
    padding: 0 0 24px;
    background: #ffffff;
  }

  .detail-column {
    min-width: 0;
  }

  .detail-card {
    padding: 16px;
    border: 1px solid var(--el-border-color-light);
    border-radius: 8px;
    background: #ffffff;

    :deep(.el-table) {
      border-radius: 6px;
    }

    :deep(.el-table th.el-table__cell) {
      background: #f8fafc;
      color: var(--el-text-color-secondary);
      font-weight: 600;
    }
  }

  .section-title {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin: 18px -16px 14px;
    padding: 0 16px 10px;
    border-bottom: 1px solid var(--el-border-color-lighter);

    h3 {
      margin: 0;
      color: #111827;
      font-size: 16px;
      font-weight: 700;
    }
  }

  .section-title-first {
    margin-top: 0;
  }

  .product-tags {
    display: flex;
    flex-wrap: wrap;
    gap: 8px;
    min-height: 40px;
    padding: 2px 0 4px;
  }

  .table-footer {
    display: flex;
    justify-content: flex-end;
    padding-top: 12px;
  }

  .ml8 {
    margin-left: 8px;
  }

  :deep(.actions-column .cell) {
    display: flex;
    justify-content: center;
  }
}

@media (max-width: 1200px) {
  .vendor-page {
    .summary-band,
    .detail-grid {
      grid-template-columns: 1fr;
    }
  }
}
</style>











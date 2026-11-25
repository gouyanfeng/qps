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
              <el-option label="高" value="高" />
              <el-option label="中" value="中" />
              <el-option label="低" value="低" />
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
        <Permission code="CRM_TRANSFER"><el-button :icon="Edit" @click="openTransferDialog()">批量流转</el-button></Permission>
        <Permission code="CRM_VENDOR_ADD"><el-button type="primary" :icon="Plus" @click="openCreateDialog">新增厂商</el-button></Permission>
      </template>

      <template #table="{ tableData }">
        <el-table
          :data="tableData"
          :row-key="'id'"
          :fit="true"
          class="wide-list-table"
          style="--table-min-width: 1720px"
          border
          @selection-change="handleSelectionChange"
          @sort-change="handleSortChange"
        >
          <el-table-column type="selection" width="44" fixed="left" />
          <el-table-column label="厂商名称" min-width="240" fixed="left" show-overflow-tooltip>
            <template #default="{ row }">
              <el-button type="primary" link class="vendor-link" @click="openDetail(row)">
                {{ row.vendorName || "-" }}
              </el-button>
            </template>
          </el-table-column>
          <el-table-column label="优先级" width="88">
            <template #default="{ row }">
              <el-tooltip :content="getPriorityRule(row.priorityLevel)" placement="top">
                <el-tag :type="getPriorityType(row.priorityLevel)" :title="getPriorityRule(row.priorityLevel)">
                  {{ row.priorityLevel || "-" }}
                </el-tag>
              </el-tooltip>
            </template>
          </el-table-column>
          <el-table-column label="主联系人 / 电话" width="160">
            <template #default="{ row }">
              <div class="cell-main">
                <span>{{ row.primaryContactName || "-" }}</span>
                <span class="phone-text">{{ row.primaryContactPhone || "-" }}</span>
              </div>
            </template>
          </el-table-column>
          <el-table-column label="跟进人" width="96" show-overflow-tooltip>
            <template #default="{ row }">{{ row.ownerUserName || "未分配" }}</template>
          </el-table-column>
          <el-table-column label="采购次数" width="120" align="right">
            <template #default="{ row }">{{ row.purchaseDemandCount || 0 }}</template>
          </el-table-column>
          <el-table-column label="采购品类" width="280">
            <template #default="{ row }">
              <el-tooltip
                v-if="row.productName?.length"
                :content="getProductNamesTooltip(row.productName)"
                placement="top"
                :disabled="hiddenProductCount(row.productName) === 0"
              >
                <div class="main-product-tags main-product-tags--compact">
                  <el-tag v-for="productName in visibleProductNames(row.productName)" :key="productName" size="small" type="info" effect="plain">
                    {{ productName }}
                  </el-tag>
                  <el-tag v-if="hiddenProductCount(row.productName) > 0" size="small" type="info" effect="plain">
                    +{{ hiddenProductCount(row.productName) }}
                  </el-tag>
                </div>
              </el-tooltip>
              <span v-else class="muted">-</span>
            </template>
          </el-table-column>
          <el-table-column label="最近采购时间" width="150">
            <template #default="{ row }">{{ formatDate(row.latestPurchaseTime) }}</template>
          </el-table-column>
          <el-table-column label="最近采购列表" min-width="280" show-overflow-tooltip>
            <template #default="{ row }">{{ row.latestPurchaseDemandName || "-" }}</template>
          </el-table-column>
          <el-table-column prop="updatedAt" label="更新时间" width="150" sortable="custom">
            <template #default="{ row }">{{ formatDate(row.updatedAt) }}</template>
          </el-table-column>
          <el-table-column label="操作" width="390" fixed="right" class-name="actions-column" header-class-name="actions-column">
            <template #default="{ row }">
              <el-button type="primary" link :icon="View" @click="openDetail(row)">详情</el-button>
              <Permission code="CRM_FOLLOW"><el-button type="primary" link :icon="Phone" @click="openFollowDialog(row)">记录沟通</el-button></Permission>
              <Permission code="CRM_VENDOR_EDIT"><el-button type="primary" link :icon="Edit" @click="openEditDialog(row)">编辑</el-button></Permission>
              <Permission code="CRM_TRANSFER">
                <el-button type="primary" link :icon="Edit" @click="openTransferDialog([row], row.ownerUserId ? 'TRANSFER' : 'ASSIGN')">
                  {{ row.ownerUserId ? "转交" : "分配" }}
                </el-button>
              </Permission>
              <el-button v-if="canManageTransfer || canReturn(row)" type="primary" link :icon="Edit" @click="openTransferDialog([row], 'RETURN')">退回</el-button>
            </template>
          </el-table-column>
        </el-table>
      </template>
    </QueryPage>

    <VendorDetailDrawer v-model="detailDrawerVisible" :vendor-id="currentVendorId" @refresh-list="reloadList" />

    <FollowDialog
      v-model="followDialogVisible"
      entity-type="CRM_VENDOR"
      :entity-id="followVendorId"
      @saved="reloadList"
    />


    <el-dialog v-model="vendorDialogVisible" :title="isEdit ? '编辑厂商' : '新增厂商'" width="560px">
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

    <TransferDialog
      v-model="transferDialogVisible"
      entity-type="CRM_VENDOR"
      :entity-ids="transferEntityIds"
      :mode="transferMode"
      selected-label="已选厂商"
      @saved="reloadList"
    />

  </div>
</template>

<script setup lang="ts" name="vendor">
import { computed, onMounted, reactive, ref, watch } from "vue";
import { Edit, Phone, Plus, QuestionFilled, View } from "@element-plus/icons-vue";
import { ElMessage } from "element-plus";
import { useRoute, useRouter } from "vue-router";
import QueryPage from "@/components/QueryPage/index.vue";
import { crmVendorApi } from "@/api/modules/crm/crmVendor";
import Permission from "@/components/Permission/index.vue";
import TransferDialog from "@/views/crm/components/transfer/Dialog.vue";
import FollowDialog from "@/views/crm/components/follow/Dialog.vue";
import VendorDetailDrawer from "./components/VendorDetailDrawer.vue";
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
  productName: string[];
  contactCount: number;
  createdAt: string;
  updatedAt: string;
  contacts: any[];
  transferRecords: any[];
}

const queryPageRef = ref();
const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();
const userStore = useUserStore();

const detailDrawerVisible = ref(false);
const followDialogVisible = ref(false);
const vendorDialogVisible = ref(false);
const transferDialogVisible = ref(false);
const isEdit = ref(false);
const currentVendorId = ref("");
const followVendorId = ref("");
const selectedVendors = ref<VendorDetail[]>([]);
const transferEntityIds = ref<string[]>([]);

const searchForm = reactive({
  keyword: "",
  priorityLevel: "",
  hasPhone: undefined as boolean | undefined,
  hasProduct: undefined as boolean | undefined,
  sortField: "UpdatedAt",
  sortDirection: "Descending",
});

const vendorForm = reactive({
  id: "",
  vendorName: "",
  priorityLevel: "中",
  remark: "",
});

const transferMode = ref<"ASSIGN" | "TRANSFER" | "RETURN">("TRANSFER");
const canManageTransfer = computed(() => authStore.hasPermissionGet("CRM_TRANSFER"));

const priorityRules: Record<string, string> = {
  高: "高：有电话、有联系人、90天内有采购、品类数 >= 3",
  中: "中：有电话、有品类",
  低: "低：干净可导入，但缺电话、缺品类、采购时间较旧或联系人信息较弱",
};

const handleReset = () => {
  searchForm.keyword = "";
  searchForm.priorityLevel = "";
  searchForm.hasPhone = undefined;
  searchForm.hasProduct = undefined;
  searchForm.sortField = "UpdatedAt";
  searchForm.sortDirection = "Descending";
};

const maxProductTagCount = 8;

const visibleProductNames = (productNames: string[]) => {
  if (productNames.length <= maxProductTagCount) return productNames;
  return productNames.slice(0, maxProductTagCount - 1);
};

const hiddenProductCount = (productNames: string[]) => productNames.length - visibleProductNames(productNames).length;

const getProductNamesTooltip = (productNames: string[]) => `采购品类：${productNames.join("、")}`;

const reloadList = () => {
  queryPageRef.value?.getTableList();
};

const handleSelectionChange = (rows: VendorDetail[]) => {
  selectedVendors.value = rows;
};

const handleSortChange = ({ prop, order }: { prop: "updatedAt"; order: "ascending" | "descending" | null }) => {
  searchForm.sortField = order ? "UpdatedAt" : "UpdatedAt";
  searchForm.sortDirection = order === "ascending" ? "Ascending" : "Descending";
  reloadList();
};

const resetVendorForm = () => {
  Object.assign(vendorForm, {
    id: "",
    vendorName: "",
    priorityLevel: "中",
    remark: "",
  });
};

const openCreateDialog = async () => {
  isEdit.value = false;
  resetVendorForm();
  vendorDialogVisible.value = true;
};

const openEditDialog = async (row: VendorDetail) => {
  isEdit.value = true;
  Object.assign(vendorForm, {
    id: row.id,
    vendorName: row.vendorName || "",
    priorityLevel: row.priorityLevel || "中",
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
    remark: vendorForm.remark,
  };

  if (isEdit.value) {
    await crmVendorApi.updateVendor(vendorForm.id, payload);
    ElMessage.success("编辑成功");
  } else {
    await crmVendorApi.createVendor(payload);
    ElMessage.success("新增成功");
  }

  vendorDialogVisible.value = false;
  reloadList();
};

const openTransferDialog = async (vendors?: VendorDetail[], mode: "ASSIGN" | "TRANSFER" | "RETURN" = "TRANSFER") => {
  const rows = vendors || selectedVendors.value;
  if (rows.length === 0) {
    ElMessage.warning("请选择要流转的厂商");
    return;
  }

  transferMode.value = mode;
  transferEntityIds.value = rows.map(item => item.id);
  transferDialogVisible.value = true;
};

const openDetail = (row: any) => {
  currentVendorId.value = row.id;
  detailDrawerVisible.value = true;
};

const openFollowDialog = (row: VendorDetail) => {
  followVendorId.value = row.id;
  followDialogVisible.value = true;
};


const getQueryValue = (value: unknown) => {
  if (Array.isArray(value)) return value[0] || "";
  return typeof value === "string" ? value : "";
};

const applyRouteEntrypoint = async () => {
  const followId = getQueryValue(route.query.followId);
  const detailId = getQueryValue(route.query.detailId);

  if (followId) {
    openDetail({ id: followId });
  } else if (detailId) {
    openDetail({ id: detailId });
  }
};

// 关抽屉时清 URL 里的 detailId，保证下次 push 能触发导航
watch(detailDrawerVisible, visible => {
  if (!visible && (route.query.detailId || route.query.followId)) {
    const { detailId, followId, ...query } = route.query;
    void router.replace({ path: route.path, query });
  }
});

// detailId 变化时重新打开抽屉
watch(() => route.query.detailId, detailId => {
  if (route.path !== "/crm/vendor") return;
  if (typeof detailId === "string" && detailId) openDetail({ id: detailId });
});

onMounted(() => {
  void applyRouteEntrypoint();
});

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
@import "./index.scss";
</style>
<template>
  <div class="customer-page">
    <QueryPage api="/admin/crm/herb-base-subjects" :searchParam="searchForm" @reset="handleReset" ref="queryPageRef">
      <template #searchConditions>
        <el-form :model="searchForm" :inline="true">
          <el-form-item label="关键词">
            <el-input v-model="searchForm.keyword" clearable placeholder="基地 / 主体 / 联系人 / 电话" />
          </el-form-item>
          <el-form-item label="供应品类">
            <ProductSelect
              v-model="searchForm.productName"
              multiple
              collapse-tags
              collapse-tags-tooltip
              placeholder="供应品类"
            />
          </el-form-item>
          <el-form-item>
            <template #label>
              <span class="grade-filter-label">
                <span>等级</span>
                <el-tooltip placement="top" effect="light" :show-after="150">
                  <template #content>
                    <div class="grade-rule-tooltip">
                      <div>高：80-100 分</div>
                      <div>中：60-79 分</div>
                      <div>低：30-59 分</div>
                      <div>无效：0-29 分</div>
                    </div>
                  </template>
                  <el-icon class="grade-help-icon" title="查看等级分数区间"><QuestionFilled /></el-icon>
                </el-tooltip>
              </span>
            </template>
            <el-select v-model="searchForm.grade" clearable placeholder="等级">
              <el-option label="高" value="高" />
              <el-option label="中" value="中" />
              <el-option label="低" value="低" />
              <el-option label="无效" value="无效" />
            </el-select>
          </el-form-item>
          <el-form-item label="状态">
            <el-select v-model="searchForm.status" clearable placeholder="状态">
              <el-option label="待联系" value="待联系" />
              <el-option label="跟进中" value="跟进中" />
              <el-option label="有意向" value="有意向" />
              <el-option label="已成交" value="已成交" />
              <el-option label="已流失" value="已流失" />
            </el-select>
          </el-form-item>
          <el-form-item label="跟进">
            <el-select v-model="followFilter" clearable placeholder="跟进节奏" @change="applyFollowFilter">
              <el-option label="已逾期" value="overdue" />
              <el-option label="今天" value="today" />
              <el-option label="未来7天" value="next7" />
              <el-option label="未设置" value="none" />
            </el-select>
          </el-form-item>
        </el-form>
      </template>

      <template #headerButtons>
        <Permission code="CRM_TRANSFER"><el-button :icon="Edit" @click="openTransferDialog()">批量流转</el-button></Permission>
        <Permission code="CRM_HERB_BASE_ADD"><el-button type="primary" :icon="Plus" @click="handleAdd">新增药材基地</el-button></Permission>
      </template>

      <template #table="{ tableData }">
        <el-table
          :data="tableData"
          :row-key="'id'"
          :row-class-name="getRowClassName"
          :fit="true"
          class="wide-list-table"
          style="--table-min-width: 2060px"
          border
          @selection-change="handleSelectionChange"
          @sort-change="handleSortChange"
        >
          <el-table-column type="selection" width="44" fixed="left" />
          <el-table-column label="基地主体" min-width="220" fixed="left" show-overflow-tooltip>
            <template #default="{ row }">
              <div class="cell-main">
                <el-button type="primary" link class="customer-link" @click="openDetail(row)">
                  {{ row.subjectName || "-" }}
                </el-button>
              </div>
            </template>
          </el-table-column>
          <el-table-column label="基地数" width="82" align="right">
            <template #default="{ row }">
              <span class="count-cell">{{ row.baseCount || 0 }}</span>
            </template>
          </el-table-column>
          <el-table-column label="主联系人 / 电话" width="150">
            <template #default="{ row }">
              <div class="cell-main">
                <span>{{ row.primaryContactName || "-" }}</span>
                <span class="phone-text">{{ row.primaryContactPhone || "-" }}</span>
              </div>
            </template>
          </el-table-column>
          <el-table-column label="供应品类" width="148">
            <template #default="{ row }">
              <div v-if="row.productName?.length" class="main-product-tags">
                <el-tag
                  v-for="value in row.productName"
                  :key="value"
                  size="small"
                  type="info"
                  effect="plain"
                >
                  {{ value }}
                </el-tag>
              </div>
              <span v-else class="muted">-</span>
            </template>
          </el-table-column>
          <el-table-column prop="totalScale" label="总规模(亩)" width="118" align="right" sortable="custom">
            <template #default="{ row }">
              <span class="scale-cell">{{ row.totalScale ?? "-" }}</span>
            </template>
          </el-table-column>
          <el-table-column label="地区" min-width="180" show-overflow-tooltip>
            <template #default="{ row }">{{ formatRegions(row.regions) }}</template>
          </el-table-column>
          <el-table-column prop="score" width="160" sortable="custom">
            <template #header>
              <span class="score-column-header">
                <span>评分 / 等级</span>
                <el-tooltip placement="top" effect="light" :show-after="150">
                  <template #content>
                    <div class="score-rule-tooltip">
                      <strong>主体评分规则（满分 100 分）</strong>
                      <div>规模：&gt;0 为 10 分，&ge;100 为 15 分，&ge;200 为 20 分，&ge;500 为 25 分</div>
                      <div>基地数：1 个 5 分，2 个 8 分，&ge;3 个 10 分</div>
                      <div>供应品类：1 个 10 分，&ge;2 个 15 分</div>
                      <div>联系人：姓名 4 分，主体电话 12 分，有效联系人电话 4 分，无电话 2 分，最高 20 分</div>
                      <div>跟进：已成交 20 分，有意向 18 分，近 30 天有效跟进 14 分，跟进中 10 分</div>
                      <div>资料：地区 2 分，地址 2 分，备注 1 分</div>
                      <div>来源：人工录入/政府公示 5 分，行业平台 4 分，百度地图 3 分，其他 2 分（取最高来源分）</div>
                      <div>等级：高 80-100 分，中 60-79 分，低 30-59 分，无效 0-29 分</div>
                      <div>限制：无基地最高 59 分；无主体主要联系人电话最高 79 分；已流失为 0 分/无效</div>
                    </div>
                  </template>
                  <el-icon class="score-help-icon" title="查看评分规则"><QuestionFilled /></el-icon>
                </el-tooltip>
              </span>
            </template>
            <template #default="{ row }">
              <div class="score-cell">
                <strong>{{ row.score ?? 0 }}</strong>
                <el-tag size="small">{{ row.grade || "-" }}</el-tag>
              </div>
            </template>
          </el-table-column>
          <el-table-column label="状态" width="88">
            <template #default="{ row }">
              <el-tag>{{ row.status || "-" }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column label="最近沟通" min-width="170">
            <template #default="{ row }">
              <div class="cell-main">
                <span>{{ row.lastFollowResult || "未沟通" }}</span>
                <span class="muted">{{ formatNullableDate(row.lastFollowAt) }}</span>
              </div>
            </template>
          </el-table-column>
          <el-table-column label="下次跟进" width="138">
            <template #default="{ row }">
              <span :class="{ overdue: isOverdue(row.nextFollowAt) }">
                {{ formatNullableDate(row.nextFollowAt) }}
              </span>
            </template>
          </el-table-column>
          <el-table-column label="跟进人" width="96" show-overflow-tooltip>
            <template #default="{ row }">{{ row.ownerUserName || "-" }}</template>
          </el-table-column>
          <el-table-column prop="createdAt" label="创建时间" width="146" sortable="custom">
            <template #default="{ row }">{{ formatNullableDate(row.createdAt) }}</template>
          </el-table-column>
          <el-table-column label="操作" width="320" fixed="right" class-name="actions-column" header-class-name="actions-column">
            <template #default="{ row }">
              <div class="table-actions">
                <el-button type="primary" link :icon="View" @click="openDetail(row)">详情</el-button>
                <el-button v-if="canManageTransfer" type="primary" link :icon="Edit" @click="openTransferDialog([row], row.ownerUserId ? 'TRANSFER' : 'ASSIGN')">
                  {{ row.ownerUserId ? "转交" : "分配" }}
                </el-button>
                <el-button v-if="canManageTransfer || canReturn(row)" type="primary" link :icon="Edit" @click="openTransferDialog([row], 'RETURN')">退回</el-button>
                <Permission code="CRM_FOLLOW"><el-button type="primary" link :icon="Phone" @click="openDetail(row)">记录沟通</el-button></Permission>
              </div>
            </template>
          </el-table-column>
        </el-table>
      </template>
    </QueryPage>

    <el-dialog v-model="dialogVisible" :title="isEdit ? '编辑基地' : '新增基地'" width="680px">
      <el-form :model="form" label-width="110px">
        <el-form-item label="基地名称">
          <el-input v-model="form.baseName" placeholder="请输入基地名称" />
        </el-form-item>
        <el-form-item label="规模(亩)">
          <el-input-number v-model="form.scale" :min="0" :precision="2" />
        </el-form-item>
        <el-form-item label="地区">
          <ChinaRegionCascader v-model="regionPath" @change="handleRegionChange" />
        </el-form-item>
        <el-form-item label="详细地址">
          <el-input v-model="form.address" placeholder="请输入详细地址" />
        </el-form-item>
        <el-form-item label="来源">
          <el-select v-model="form.sourcePlatform" placeholder="请选择来源">
            <el-option v-for="item in sourcePlatforms" :key="item" :label="item" :value="item" />
          </el-select>
        </el-form-item>
        <el-form-item label="备注">
          <el-input type="textarea" v-model="form.remark" :rows="3" placeholder="请输入备注" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit">{{ isEdit ? "保存" : "创建" }}</el-button>
      </template>
    </el-dialog>

    <HerbBaseDetailDrawer v-model="detailDrawerVisible" :subject-id="currentHerbBaseSubjectId" @refresh-list="reloadList" />

    <el-dialog v-model="transferDialogVisible" :title="transferDialogTitle" width="520px">
      <el-form :model="transferForm" label-width="100px">
        <el-form-item v-if="transferMode !== 'RETURN'" label="跟进人">
          <el-select v-model="transferForm.ownerUserId" placeholder="请选择跟进人">
            <el-option v-for="user in ownerOptions" :key="user.id" :label="user.realName" :value="user.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="transferForm.remark" type="textarea" :rows="3" placeholder="请输入流转备注" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="transferDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="submitTransfer">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts" name="customer">
import { computed, onMounted, reactive, ref } from "vue";
import { useRoute } from "vue-router";
import { Edit, Phone, Plus, QuestionFilled, View } from "@element-plus/icons-vue";
import { ElMessage } from "element-plus";
import QueryPage from "@/components/QueryPage/index.vue";
import ChinaRegionCascader from "@/components/ChinaRegionCascader/index.vue";
import ProductSelect from "@/components/ProductSelect/index.vue";
import { crmHerbBaseApi } from "@/api/modules/crmHerbBase";
import { userApi } from "@/api/modules/user";
import Permission from "@/components/Permission/index.vue";
import HerbBaseDetailDrawer from "./components/HerbBaseDetailDrawer.vue";
import { useAuthStore } from "@/stores/modules/auth";
import { useUserStore } from "@/stores/modules/user";

interface HerbBaseSubjectDetail {
  id: string;
  subjectName?: string;
  subjectType?: string;
  productName: string[];
  grade: string;
  score: number;
  status: string;
  ownerUserId?: string | null;
  ownerUserName?: string | null;
  remark: string;
  primaryContactName: string;
  primaryContactPhone: string;
  lastFollowAt?: string | null;
  lastFollowResult: string;
  nextFollowAt?: string | null;
  baseCount?: number;
  totalScale?: number | null;
  regions?: string[];
  herbBases?: any[];
  transferRecords?: any[];
  createdAt: string;
  updatedAt: string;
}

// 页面状态
const queryPageRef = ref();
const route = useRoute();
const authStore = useAuthStore();
const userStore = useUserStore();

const dialogVisible = ref(false);
const detailDrawerVisible = ref(false);
const transferDialogVisible = ref(false);
const isEdit = ref(false);
const followFilter = ref("");
const currentHerbBaseSubjectId = ref("");
const selectedHerbBases = ref<HerbBaseSubjectDetail[]>([]);
const ownerOptions = ref<any[]>([]);
const regionPath = ref<string[]>([]);

// 列表筛选
const searchForm = reactive({
  keyword: "",
  grade: "",
  status: "",
  productName: [] as string[],
  onlyOverdue: undefined as boolean | undefined,
  onlyNoNextFollow: undefined as boolean | undefined,
  nextFollowFrom: "",
  nextFollowTo: "",
  sortField: "CreatedAt",
  sortDirection: "Descending",
});

// 基地编辑
const form = reactive({
  id: "",
  baseName: "",
  herbBaseSubjectId: undefined as string | undefined,
  subjectName: "",
  scale: undefined as number | undefined,
  province: "",
  city: "",
  area: "",
  address: "",
  lat: undefined as number | undefined,
  lng: undefined as number | undefined,
  sourcePlatform: "百度地图",
  sourceId: undefined as number | undefined,
  status: "待联系",
  remark: "",
  primaryContactName: "",
  primaryContactPhone: "",
});

// 流转
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

// 下拉选项
const sourcePlatforms = ["百度地图", "政府网站", "手工录入", "Excel导入", "其他"];

// 通用展示与校验
const canReturn = (row?: Partial<HerbBaseSubjectDetail> | null) =>
  !!row?.ownerUserId && row.ownerUserId === userStore.userInfo.userId;

const formatNullableDate = (date?: string | null) => {
  if (!date) return "-";
  return new Date(date).toLocaleString("zh-CN");
};

const formatRegions = (regions?: string[]) => regions?.filter(Boolean).join(" / ") || "-";

const handleRegionChange = (value: string[] | string) => {
  const path = Array.isArray(value) ? value : [];
  form.province = path[0] || "";
  form.city = path[1] || "";
  form.area = path[2] || "";
};

const isOverdue = (date?: string | null) => {
  if (!date) return false;
  return new Date(date).getTime() < Date.now();
};

const formatDateParam = (date: Date) => {
  const pad = (num: number) => String(num).padStart(2, "0");
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`;
};

// 列表操作
const getRowClassName = ({ row }: any) => {
  if (isOverdue(row.nextFollowAt)) return "row-overdue";
  return "";
};

const applyFollowFilter = () => {
  searchForm.onlyOverdue = undefined;
  searchForm.onlyNoNextFollow = undefined;
  searchForm.nextFollowFrom = "";
  searchForm.nextFollowTo = "";

  const now = new Date();
  const todayStart = new Date(now.getFullYear(), now.getMonth(), now.getDate());
  const todayEnd = new Date(todayStart);
  todayEnd.setDate(todayEnd.getDate() + 1);
  todayEnd.setSeconds(todayEnd.getSeconds() - 1);

  if (followFilter.value === "overdue") {
    searchForm.onlyOverdue = true;
  } else if (followFilter.value === "none") {
    searchForm.onlyNoNextFollow = true;
  } else if (followFilter.value === "today") {
    searchForm.nextFollowFrom = formatDateParam(todayStart);
    searchForm.nextFollowTo = formatDateParam(todayEnd);
  } else if (followFilter.value === "next7") {
    const sevenDays = new Date(todayStart);
    sevenDays.setDate(sevenDays.getDate() + 7);
    searchForm.nextFollowFrom = formatDateParam(todayStart);
    searchForm.nextFollowTo = formatDateParam(sevenDays);
  }
};

const handleReset = () => {
  Object.assign(searchForm, {
    keyword: "",
    sourcePlatform: "",
    grade: "",
    status: "",
    productName: [],
    onlyOverdue: undefined,
    onlyNoNextFollow: undefined,
    nextFollowFrom: "",
    nextFollowTo: "",
    sortField: "CreatedAt",
    sortDirection: "Descending",
  });
  followFilter.value = "";
};

// 基地编辑操作
const resetCustomerForm = () => {
  Object.assign(form, {
    id: "",
    baseName: "",
    herbBaseSubjectId: undefined,
    subjectName: "",
    scale: undefined,
    province: "",
    city: "",
    area: "",
    address: "",
    lat: undefined,
    lng: undefined,
    sourcePlatform: "百度地图",
    sourceId: undefined,
    status: "待联系",
    remark: "",
    primaryContactName: "",
    primaryContactPhone: "",
  });
  regionPath.value = [];
};

const handleAdd = async () => {
  isEdit.value = false;
  resetCustomerForm();
  dialogVisible.value = true;
};

const reloadList = () => {
  queryPageRef.value?.getTableList();
};

const handleSortChange = ({ prop, order }: { prop: "totalScale" | "score" | "createdAt"; order: "ascending" | "descending" | null }) => {
  const sortFieldMap = {
    totalScale: "TotalScale",
    score: "Score",
    createdAt: "CreatedAt",
  };

  searchForm.sortField = order ? sortFieldMap[prop] : "CreatedAt";
  searchForm.sortDirection = order === "ascending" ? "Ascending" : "Descending";
  reloadList();
};

const handleSelectionChange = (rows: HerbBaseSubjectDetail[]) => {
  selectedHerbBases.value = rows;
};

// 主体流转
const loadOwnerOptions = async () => {
  const res = await userApi.getUserList({ page: 1, pageSize: 100, username: "", realName: "", roleId: "", isActive: true });
  ownerOptions.value = (res.data?.list || []).filter((user: any) => user.isActive);
};

const openDetail = (row: any) => {
  currentHerbBaseSubjectId.value = row.id;
  detailDrawerVisible.value = true;
};

const handleSubmit = async () => {
  if (!form.baseName) {
    ElMessage.error("请输入基地名称");
    return;
  }

  const request = {
    ...form,
    herbBaseSubjectId: form.herbBaseSubjectId,
    subjectName: form.subjectName || form.baseName || "",
    sourcePlatform: form.sourcePlatform || "百度地图",
    status: form.status || "待联系",
  };

  if (isEdit.value) {
    await crmHerbBaseApi.updateHerbBase(form.id, request);
    ElMessage.success("保存成功");
  } else {
    await crmHerbBaseApi.createHerbBase(request);
    ElMessage.success("创建成功");
  }

  dialogVisible.value = false;
  reloadList();
};

const openTransferDialog = async (rows?: HerbBaseSubjectDetail[], mode: "ASSIGN" | "TRANSFER" | "RETURN" = "TRANSFER") => {
  const customers = rows?.length ? rows : selectedHerbBases.value;
  if (customers.length === 0) {
    ElMessage.warning("请选择要流转的基地主体");
    return;
  }

  transferMode.value = mode;
  Object.assign(transferForm, {
    entityIds: customers.map(customer => customer.id),
    ownerUserId: "",
    remark: "",
  });
  if (mode !== "RETURN") await loadOwnerOptions();
  transferDialogVisible.value = true;
};

const submitTransfer = async () => {
  if (transferMode.value !== "RETURN" && !transferForm.ownerUserId) {
    ElMessage.warning("请选择跟进人");
    return;
  }

  await crmHerbBaseApi.changeOwner({
    entityIds: [...transferForm.entityIds],
    toOwnerUserId: transferMode.value === "RETURN" ? null : transferForm.ownerUserId,
    remark: transferForm.remark || undefined,
  });
  ElMessage.success("流转成功");
  transferDialogVisible.value = false;
  reloadList();
};

// 路由入口
const getQueryValue = (value: unknown) => {
  if (Array.isArray(value)) return value[0] || "";
  return typeof value === "string" ? value : "";
};

const applyRouteEntrypoint = async () => {
  const followQuery = getQueryValue(route.query.followFilter);
  const gradeQuery = getQueryValue(route.query.grade);
  const onlyOverdueQuery = getQueryValue(route.query.onlyOverdue);
  const actionQuery = getQueryValue(route.query.action);
  const detailId = getQueryValue(route.query.detailId);
  const followId = getQueryValue(route.query.followId);

  if (followQuery) {
    followFilter.value = followQuery;
    applyFollowFilter();
    reloadList();
  } else if (onlyOverdueQuery === "true") {
    followFilter.value = "overdue";
    applyFollowFilter();
    reloadList();
  }

  if (gradeQuery) {
    searchForm.grade = gradeQuery;
    reloadList();
  }

  if (actionQuery === "add") {
    await handleAdd();
  }

  if (followId) {
    currentHerbBaseSubjectId.value = followId;
    detailDrawerVisible.value = true;
  } else if (detailId) {
    currentHerbBaseSubjectId.value = detailId;
    detailDrawerVisible.value = true;
  }
};

onMounted(() => {
  void applyRouteEntrypoint();
});
</script>

<style scoped lang="scss">
.customer-page {
  .cell-main {
    display: flex;
    flex-direction: column;
    gap: 4px;
    line-height: 1.35;
  }

  .customer-link {
    align-self: flex-start;
    max-width: 100%;
    padding: 0;
    overflow: hidden;
    font-weight: 600;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .table-actions {
    display: flex;
    align-items: center;
    gap: 6px;
    width: 100%;
    white-space: nowrap;

    :deep(.el-button) {
      margin-left: 0;
    }
  }

  :deep(.actions-column .cell) {
    padding-right: 10px;
    padding-left: 10px;
  }

  .muted {
    color: var(--el-text-color-secondary);
    font-size: 12px;
  }

  .phone-text {
    color: var(--el-color-primary);
    font-family: Consolas, monospace;
  }

  .score-cell {
    display: flex;
    align-items: center;
    gap: 8px;
  }

  .score-column-header {
    display: inline-flex;
    align-items: center;
    gap: 4px;
  }

  .grade-filter-label {
    display: inline-flex;
    align-items: center;
    gap: 3px;
  }

  .grade-help-icon {
    color: var(--el-text-color-secondary);
    cursor: help;
    font-size: 14px;
  }

  .score-help-icon {
    color: var(--el-text-color-secondary);
    cursor: help;
    font-size: 18px;
  }

  .score-rule-tooltip {
    max-width: 520px;
    line-height: 1.7;
    white-space: normal;
  }

  .score-rule-tooltip strong {
    display: block;
    margin-bottom: 4px;
    color: var(--el-text-color-primary);
  }

  .grade-rule-tooltip {
    line-height: 1.8;
    white-space: nowrap;
  }

  .scale-cell {
    font-weight: 700;
  }

  .count-cell {
    font-weight: 700;
  }

  .overdue {
    color: var(--el-color-danger);
    font-weight: 600;
  }

  .inline-fields {
    display: flex;
    width: 100%;
    gap: 10px;

    > * {
      flex: 1;
    }
  }
  :deep(.row-overdue) {
    background: #fff7f7;
  }
}
</style>

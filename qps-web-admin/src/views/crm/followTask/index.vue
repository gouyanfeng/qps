<template>
  <div class="follow-task page-content">
    <section class="task-overview" aria-label="跟进任务概览">
      <div
        v-for="item in metrics"
        :key="item.category"
        class="overview-item"
        :class="item.tone"
      >
        <span class="overview-icon">
          <el-icon><component :is="item.icon" /></el-icon>
        </span>
        <div class="overview-content">
          <span>{{ item.label }}</span>
          <strong>{{ item.value }}</strong>
        </div>
      </div>
    </section>

    <QueryPage
      ref="queryPageRef"
      api="/admin/crm/follow-tasks"
      :searchParam="searchForm"
      @reset="handleReset"
    >
      <template #searchConditions>
        <el-form :model="searchForm" :inline="true">
          <el-form-item label="任务类型" class="task-category-form-item">
            <el-select v-model="searchForm.category" placeholder="全部任务">
              <el-option label="全部任务" value="" />
              <el-option label="已逾期" value="OVERDUE" />
              <el-option label="今日" value="TODAY" />
              <el-option label="未来计划" value="FUTURE" />
              <el-option label="未设计划" value="NO_PLAN" />
            </el-select>
          </el-form-item>
          <el-form-item label="对象类型">
            <el-select v-model="searchForm.entityType" clearable placeholder="全部对象">
              <el-option label="基地主体" value="CRM_HERB_BASE_SUBJECT" />
              <el-option label="厂商" value="CRM_VENDOR" />
            </el-select>
          </el-form-item>
          <el-form-item label="关键词">
            <el-input v-model="searchForm.keyword" clearable placeholder="名称、联系人或电话" @keyup.enter="reloadList" />
          </el-form-item>
        </el-form>
      </template>

      <template #table="{ tableData }">
        <el-table :data="tableData" border class="follow-task-table">
          <el-table-column label="跟进对象" min-width="220">
            <template #default="{ row }">
              <b>{{ row.entityName }}</b>
            </template>
          </el-table-column>
          <el-table-column label="类型" width="100">
            <template #default="{ row }">
              <el-tag size="small">{{ entityTypeText(row.entityType) }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column prop="contactName" label="联系人" width="150" show-overflow-tooltip />
          <el-table-column prop="contactPhone" label="电话" width="140" />
          <el-table-column label="上次跟进" width="170">
            <template #default="{ row }">
              {{ formatDate(row.lastFollowAt) }}
            </template>
          </el-table-column>
          <el-table-column label="下次跟进" width="170">
            <template #default="{ row }">
              <span :class="row.category.toLowerCase()">{{ taskText(row) }}</span>
            </template>
          </el-table-column>
          <el-table-column label="最近结果" width="110">
            <template #default="{ row }">
              {{ formatFollowResult(row.lastFollowResult) }}
            </template>
          </el-table-column>
          <el-table-column label="操作" width="150" fixed="right">
            <template #default="{ row }">
              <el-button type="primary" link @click="openFollowDialog(row)">记录沟通</el-button>
              <el-button link @click="detail(row)">详情</el-button>
            </template>
          </el-table-column>
        </el-table>
      </template>
    </QueryPage>

    <el-dialog v-model="followDialogVisible" title="记录沟通" width="560px">
      <el-form :model="followForm" label-width="100px">
        <el-form-item label="联系人">
          <el-select v-model="followForm.contactId" clearable placeholder="可不指定">
            <el-option
              v-for="contact in followContacts"
              :key="contact.id"
              :label="contact.contactName || contact.phone"
              :value="contact.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="沟通方式">
          <el-select v-model="followForm.followType">
            <el-option label="电话" value="PHONE" />
            <el-option label="微信" value="WECHAT" />
            <el-option label="拜访" value="VISIT" />
          </el-select>
        </el-form-item>
        <el-form-item label="沟通结果" required>
          <el-select v-model="followForm.followResult" placeholder="请选择结果">
            <el-option label="已接通" value="CONNECTED" />
            <el-option label="未接" value="MISSED" />
            <el-option label="空号" value="EMPTY_NUMBER" />
            <el-option label="有意向" value="INTERESTED" />
            <el-option label="无意向" value="NOT_INTERESTED" />
          </el-select>
        </el-form-item>
        <el-form-item label="意向等级">
          <el-select v-model="followForm.intentLevel" clearable placeholder="意向等级">
            <el-option label="A" value="A" />
            <el-option label="B" value="B" />
            <el-option label="C" value="C" />
          </el-select>
        </el-form-item>
        <el-form-item label="沟通内容">
          <el-input v-model="followForm.content" type="textarea" :rows="4" placeholder="记录沟通要点" />
        </el-form-item>
        <el-form-item label="下次跟进">
          <el-date-picker
            v-model="followForm.nextFollowAt"
            type="datetime"
            value-format="YYYY-MM-DDTHH:mm:ss"
            placeholder="请选择时间"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="followDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="followSubmitting" @click="submitFollowRecord">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from "vue";
import { Calendar, CircleCheckFilled, DocumentDelete, WarningFilled } from "@element-plus/icons-vue";
import { ElMessage } from "element-plus";
import { useRouter } from "vue-router";
import { crmHerbBaseApi } from "@/api/modules/crmHerbBase";
import { crmFollowTaskApi } from "@/api/modules/crmFollowTask";
import { crmVendorApi } from "@/api/modules/crmVendor";
import QueryPage from "@/components/QueryPage/index.vue";

interface FollowTask {
  entityId: string;
  entityType: string;
  entityName: string;
  contactName: string;
  contactPhone: string;
  lastFollowAt?: string;
  lastFollowResult: string;
  nextFollowAt?: string;
  category: string;
}

interface FollowTaskOverview {
  overdueCount: number;
  todayCount: number;
  noPlanCount: number;
  completedLast7DaysCount: number;
}

interface FollowContact {
  id: string;
  contactName?: string;
  phone?: string;
  isPrimary?: boolean;
  status?: string;
}

const router = useRouter();
const queryPageRef = ref<InstanceType<typeof QueryPage>>();
const followDialogVisible = ref(false);
const followSubmitting = ref(false);
const followTarget = ref<FollowTask>();
const followContacts = ref<FollowContact[]>([]);
const overview = ref<FollowTaskOverview>({
  overdueCount: 0,
  todayCount: 0,
  noPlanCount: 0,
  completedLast7DaysCount: 0,
});
const searchForm = reactive({
  category: "",
  entityType: "",
  keyword: "",
});
const followForm = reactive({
  contactId: undefined as string | undefined,
  followType: "PHONE",
  followResult: "",
  intentLevel: "",
  content: "",
  nextFollowAt: "",
});

const metrics = computed(() => [
  { label: "已逾期", value: overview.value.overdueCount, category: "OVERDUE", tone: "danger", icon: WarningFilled },
  { label: "今日待跟进", value: overview.value.todayCount, category: "TODAY", tone: "warning", icon: Calendar },
  { label: "未设下次跟进", value: overview.value.noPlanCount, category: "NO_PLAN", tone: "neutral", icon: DocumentDelete },
  { label: "近 7 天已完成", value: overview.value.completedLast7DaysCount, category: "", tone: "success", icon: CircleCheckFilled },
]);

const followResultLabels: Record<string, string> = {
  CONNECTED: "已接通",
  MISSED: "未接",
  EMPTY_NUMBER: "空号",
  INTERESTED: "有意向",
  NOT_INTERESTED: "无意向",
  已接通: "已接通",
  未接: "未接",
  空号: "空号",
  有意向: "有意向",
  无意向: "无意向",
};

const loadOverview = async () => {
  const { data } = await crmFollowTaskApi.getList({ page: 1, pageSize: 1 });
  overview.value = data.overview || overview.value;
};

const reloadList = () => {
  queryPageRef.value?.getList();
};

const handleReset = () => {
  Object.assign(searchForm, {
    category: "",
    entityType: "",
    keyword: "",
  });
};

const entityTypeText = (entityType: string) => (entityType === "CRM_VENDOR" ? "厂商" : "基地主体");

const detail = (row: FollowTask) => {
  router.push(
    row.entityType === "CRM_VENDOR"
      ? { path: "/crm/vendor", query: { detailId: row.entityId } }
      : { path: "/crm/herb-base", query: { detailId: row.entityId } },
  );
};

const resetFollowForm = () => {
  Object.assign(followForm, {
    contactId: undefined,
    followType: "PHONE",
    followResult: "",
    intentLevel: "",
    content: "",
    nextFollowAt: "",
  });
};

const openFollowDialog = async (row: FollowTask) => {
  followTarget.value = row;
  resetFollowForm();

  const response = row.entityType === "CRM_VENDOR"
    ? await crmVendorApi.getVendor(row.entityId)
    : await crmHerbBaseApi.getSubject(row.entityId);

  followContacts.value = (response.data?.contacts || []).filter((contact: FollowContact) => contact.status !== "INVALID");
  followForm.contactId = followContacts.value.find(contact => contact.isPrimary)?.id;
  followDialogVisible.value = true;
};

const submitFollowRecord = async () => {
  if (!followTarget.value) return;
  if (!followForm.followResult) {
    ElMessage.error("请选择沟通结果");
    return;
  }
  if (followForm.nextFollowAt && new Date(followForm.nextFollowAt).getTime() <= Date.now()) {
    ElMessage.error("下次跟进时间必须晚于当前时间");
    return;
  }

  followSubmitting.value = true;
  try {
    const request = {
      ...followForm,
      contactId: followForm.contactId || null,
      nextFollowAt: followForm.nextFollowAt || null,
    };

    if (followTarget.value.entityType === "CRM_VENDOR") {
      await crmVendorApi.createFollowRecord(followTarget.value.entityId, request);
    } else {
      await crmHerbBaseApi.createSubjectFollowRecord(followTarget.value.entityId, request);
    }

    ElMessage.success("沟通记录已保存");
    followDialogVisible.value = false;
    reloadList();
    await loadOverview();
  } finally {
    followSubmitting.value = false;
  }
};

const formatDate = (value?: string) => (value ? value.replace("T", " ").slice(0, 16) : "-");

const formatFollowResult = (value?: string) => (value ? followResultLabels[value] || value : "-");

const taskText = (row: FollowTask) => {
  if (row.category === "OVERDUE") return "已逾期";
  if (row.category === "TODAY") return "今天";
  if (row.category === "NO_PLAN") return "未设计划";
  return formatDate(row.nextFollowAt);
};

onMounted(() => {
  void loadOverview();
});
</script>

<style scoped lang="scss">
.task-overview {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 10px;
  margin-bottom: 14px;
}

.overview-item {
  position: relative;
  display: flex;
  min-height: 76px;
  flex-direction: row;
  align-items: center;
  justify-content: center;
  gap: 12px;
  padding: 12px 16px;
  border: 1px solid var(--el-border-color-lighter);
  border-left-width: 3px;
  border-radius: 6px;
  background: var(--el-bg-color);
  color: var(--el-text-color-regular);
  font: inherit;
  text-align: left;
}

.overview-icon {
  display: inline-flex;
  width: 34px;
  height: 34px;
  flex: 0 0 34px;
  align-items: center;
  justify-content: center;
  border-radius: 4px;
  font-size: 18px;
}

.overview-item.danger {
  border-left-color: var(--el-color-danger);
}

.overview-item.warning {
  border-left-color: var(--el-color-warning);
}

.overview-item.neutral {
  border-left-color: var(--el-color-info);
}

.overview-item.success {
  border-left-color: var(--el-color-success);
}

.overview-item.danger .overview-icon {
  background: var(--el-color-danger-light-9);
  color: var(--el-color-danger);
}

.overview-item.warning .overview-icon {
  background: var(--el-color-warning-light-9);
  color: var(--el-color-warning);
}

.overview-item.neutral .overview-icon {
  background: var(--el-color-info-light-9);
  color: var(--el-color-info);
}

.overview-item.success .overview-icon {
  background: var(--el-color-success-light-9);
  color: var(--el-color-success);
}

.overview-content {
  display: flex;
  min-width: 0;
  align-items: baseline;
  gap: 10px;
  white-space: nowrap;
}

.overview-content strong {
  color: var(--el-text-color-primary);
  font-size: 23px;
  line-height: 1;
}

.follow-task :deep(.query-page .table-header) {
  display: none;
}

.follow-task :deep(.follow-task-table .el-table__cell) {
  padding-top: 7px;
  padding-bottom: 7px;
}

.follow-task :deep(.follow-task-table .cell) {
  font-size: 13px;
}

.overdue {
  color: var(--el-color-danger);
}

.today {
  color: var(--el-color-warning);
}

.no_plan {
  color: var(--el-text-color-secondary);
}
</style>

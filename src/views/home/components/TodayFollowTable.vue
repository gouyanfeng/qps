<template>
  <div class="workbench-block">
    <div class="block-header">
      <h2>今日待跟进</h2>
      <span>{{ customers.length }} 条</span>
    </div>

    <el-table :data="customers" height="360" border empty-text="今天没有待跟进客户">
      <el-table-column label="客户名称" min-width="170" show-overflow-tooltip>
        <template #default="{ row }">
          <el-button type="primary" link class="base-link" @click="$emit('open-detail', row)">
            {{ row.baseName || "-" }}
          </el-button>
          <div class="sub-text">{{ row.subjectName || "-" }}</div>
        </template>
      </el-table-column>
      <el-table-column label="主营品类" width="120" show-overflow-tooltip>
        <template #default="{ row }">{{ formatMainProducts(row) }}</template>
      </el-table-column>
      <el-table-column label="地区" min-width="150" show-overflow-tooltip>
        <template #default="{ row }">{{ formatRegion(row) }}</template>
      </el-table-column>
      <el-table-column label="主联系人 / 电话" width="150">
        <template #default="{ row }">
          <div>{{ row.primaryContactName || "-" }}</div>
          <div class="sub-text">{{ row.primaryContactPhone || "-" }}</div>
        </template>
      </el-table-column>
      <el-table-column label="最近沟通结果" width="118">
        <template #default="{ row }">
          <el-tag size="small" :type="getFollowResultType(row.lastFollowResult)">
            {{ formatFollowResult(row.lastFollowResult) }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column label="下次跟进时间" width="168">
        <template #default="{ row }">
          <span :class="{ overdue: isOverdue(row.nextFollowAt) }">{{ formatDate(row.nextFollowAt) }}</span>
        </template>
      </el-table-column>
      <el-table-column label="操作" width="160" fixed="right" header-class-name="actions-column" class-name="actions-column">
        <template #default="{ row }">
          <div class="table-actions">
            <el-button type="primary" link @click="$emit('open-detail', row)">详情</el-button>
            <el-button type="primary" link @click="$emit('record-follow', row)">记录沟通</el-button>
          </div>
        </template>
      </el-table-column>
    </el-table>
  </div>
</template>

<script setup lang="ts">
import type { CrmDashboardFollowCustomer } from "@/api/modules/dashboard";

defineProps<{
  customers: CrmDashboardFollowCustomer[];
}>();

defineEmits<{
  (event: "open-detail", customer: CrmDashboardFollowCustomer): void;
  (event: "record-follow", customer: CrmDashboardFollowCustomer): void;
}>();

const mainProductLabels: Record<string, string> = {
  HUANG_QI: "黄芪",
  DANG_GUI: "当归",
  DANG_SHEN: "党参",
  TIAN_MA: "天麻",
  OTHER: "其他",
};

const followResultLabels: Record<string, string> = {
  CONNECTED: "已接通",
  MISSED: "未接",
  EMPTY_NUMBER: "空号",
  INTERESTED: "有意向",
  NOT_INTERESTED: "无意向",
};

const formatMainProducts = (row: CrmDashboardFollowCustomer) => {
  const values = row.mainProducts || [];
  return values.map(value => mainProductLabels[value] || value).join("、") || "-";
};

const formatRegion = (row: CrmDashboardFollowCustomer) =>
  [row.province, row.city, row.area].filter(Boolean).join(" / ") || "-";

const formatDate = (date?: string | null) => {
  if (!date) return "-";
  return new Date(date).toLocaleString("zh-CN", {
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  });
};

const isOverdue = (date?: string | null) => Boolean(date && new Date(date).getTime() < Date.now());

const formatFollowResult = (value?: string | null) => (value ? followResultLabels[value] || value : "未沟通");

const getFollowResultType = (value?: string | null) => {
  if (value === "INTERESTED" || value === "有意向") return "success";
  if (value === "MISSED" || value === "未接") return "warning";
  if (value === "EMPTY_NUMBER" || value === "空号") return "danger";
  return "info";
};
</script>

<style scoped lang="scss">
.workbench-block {
  border: 1px solid var(--el-border-color-light);
  border-radius: 8px;
  background: var(--el-bg-color);
}

.block-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  height: 48px;
  padding: 0 16px;
  border-bottom: 1px solid var(--el-border-color-lighter);
}

h2 {
  margin: 0;
  font-size: 16px;
  font-weight: 600;
}

.block-header span,
.sub-text {
  color: var(--el-text-color-secondary);
  font-size: 12px;
}

.base-link {
  max-width: 100%;
  padding: 0;
}

.table-actions {
  display: flex;
  align-items: center;
  gap: 8px;
  white-space: nowrap;
}

.overdue {
  color: var(--el-color-danger);
  font-weight: 600;
}
</style>

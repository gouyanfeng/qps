<template>
  <div class="purchase-demand-page">
    <QueryPage ref="queryPageRef" api="/admin/crm/purchase-demands" :searchParam="searchForm" @reset="handleReset">
      <template #searchConditions>
        <el-form :model="searchForm" :inline="true">
          <el-form-item label="关键词">
            <el-input v-model="searchForm.keyword" clearable placeholder="编号 / 需求名称 / 厂商" />
          </el-form-item>
          <el-form-item label="状态">
            <el-select v-model="searchForm.status" clearable placeholder="全部状态">
              <el-option v-for="status in statuses" :key="status" :label="status" :value="status" />
            </el-select>
          </el-form-item>
        </el-form>
      </template>

      <template #table="{ tableData }">
        <el-table :data="tableData" :row-key="'id'" class="wide-list-table" style="--table-min-width: 1450px" border>
          <el-table-column prop="demandNo" label="编号" min-width="180" />
          <el-table-column prop="demandName" label="需求名称" min-width="200" show-overflow-tooltip />
          <el-table-column label="采购明细" width="280">
            <template #default="{ row }">
              <el-tooltip
                v-if="row.items?.length"
                :content="getDemandItemsTooltip(row.items)"
                placement="top"
                :disabled="hiddenDemandItemCount(row.items) === 0"
              >
                <div class="demand-item-tags demand-item-tags--compact">
                  <el-tag v-for="item in visibleDemandItems(row.items)" :key="item.id || item.productName" size="small">
                    {{ getDemandItemLabel(item) }}
                  </el-tag>
                  <el-tag v-if="hiddenDemandItemCount(row.items) > 0" size="small">+{{ hiddenDemandItemCount(row.items) }}</el-tag>
                </div>
              </el-tooltip>
              <span v-if="!row.items?.length">-</span>
            </template>
          </el-table-column>
          <el-table-column label="提出日期" width="170">
            <template #default="{ row }">{{ formatDate(row.demandAt) }}</template>
          </el-table-column>
          <el-table-column label="期望到货" width="170">
            <template #default="{ row }">{{ formatDate(row.expectedDeliveryAt) }}</template>
          </el-table-column>
          <el-table-column prop="receivingAddress" label="收货地" min-width="160" show-overflow-tooltip />
          <el-table-column prop="status" label="状态" width="100">
            <template #default="{ row }"><el-tag size="small">{{ row.status }}</el-tag></template>
          </el-table-column>
          <el-table-column prop="sourceType" label="来源" width="100" />
          <el-table-column label="撮合进度" width="230" show-overflow-tooltip>
            <template #default="{ row }">
              <el-button v-if="canStartMatch(row)" class="match-progress" link type="primary" @click="startMatch(row)">{{ matchProgressLabel(row) }}</el-button>
              <span v-else>{{ matchProgressLabel(row) }}</span>
            </template>
          </el-table-column>
          <el-table-column label="操作" width="110" fixed="right" class-name="actions-column" header-class-name="actions-column">
            <template #default="{ row }">
              <Permission code="CRM_MATCH_OPPORTUNITY_MANAGE"><el-button v-if="canStartMatch(row)" link type="primary" @click="startMatch(row)">开始撮合</el-button></Permission>
            </template>
          </el-table-column>
        </el-table>
      </template>
    </QueryPage>
  </div>
</template>

<script setup lang="ts">
import { reactive } from "vue";
import { useRouter } from "vue-router";
import { useAuthStore } from "@/stores/modules/auth";
import Permission from "@/components/Permission/index.vue";
import QueryPage from "@/components/QueryPage/index.vue";
import crmMatchOpportunityApi from "@/api/modules/crm/crmMatchOpportunity";
import { ElMessage } from "element-plus";

const router = useRouter();
const authStore = useAuthStore();
const statuses = ["待确认", "有效", "已完成", "已关闭"];
const searchForm = reactive({ keyword: "", status: "" });

const maxDemandItemCount = 8;

const getDemandItemLabel = (item: any) => `${item.productName || "未填写品类"}${item.quantity ? ` ${item.quantity}${item.quantityUnit || ""}` : ""}`;

const visibleDemandItems = (items: any[]) => {
  if (items.length <= maxDemandItemCount) return items;
  return items.slice(0, maxDemandItemCount - 1);
};

const hiddenDemandItemCount = (items: any[]) => items.length - visibleDemandItems(items).length;

const getDemandItemsTooltip = (items: any[]) =>
  `采购明细：${items.map(item => {
    const label = getDemandItemLabel(item);
    if (!item.matchedQuantity) return label;
    return `${label}（已采购 ${item.matchedQuantity}${item.quantityUnit || ""}，剩余 ${item.remainingQuantity ?? 0}${item.quantityUnit || ""}）`;
  }).join("、")}`;

const handleReset = () => {
  searchForm.keyword = "";
  searchForm.status = "";
};

const remainingOf = (item: any) => Number(item?.remainingQuantity ?? 0);
// 有明确需求数量的明细撮合满了才算完成；历史明细数量为空时仍可发起撮合。
const isItemCompleted = (item: any) => Number(item?.quantity || 0) > 0 && remainingOf(item) <= 0;
const hasRemainingDemand = (row: any) => (row.items || []).some((item: any) => !isItemCompleted(item));
const canStartMatch = (row: any) => row.status === "有效" && hasRemainingDemand(row) && authStore.hasPermissionGet("CRM_MATCH_OPPORTUNITY_MANAGE");
const startMatch = async (row: any) => {
  const demandItemId = row.items?.[0]?.id;
  if (!demandItemId) {
    ElMessage.warning("该采购列表没有明细，无法发起撮合");
    return;
  }
  const result = await crmMatchOpportunityApi.createEmpty({ source: "demand", demandItemId });
  ElMessage.success("已发起撮合，请在详情页选择候选基地");
  await router.replace({ name: "matchOpportunity", query: { detailId: result.data } });
};
const matchProgressLabel = (row: any) => {
  const progress = row.matchProgress;
  if (!progress?.totalItemCount) return "暂无采购明细";
  if (!hasRemainingDemand(row)) return `已采购完成 ${progress.totalItemCount}/${progress.totalItemCount}`;
  const segments = [`已发起 ${progress.startedItemCount}/${progress.totalItemCount}`];
  if (progress.completedItemCount) segments.push(`${progress.completedItemCount} 已采购完成`);
  if (progress.inProgressItemCount) segments.push(`${progress.inProgressItemCount} 撮合中`);
  if (progress.pausedItemCount) segments.push(`${progress.pausedItemCount} 已暂停`);
  if (progress.closedItemCount) segments.push(`${progress.closedItemCount} 已结束`);
  if (progress.pendingItemCount) segments.push(`${progress.pendingItemCount} 待撮合`);
  return segments.join(" · ");
};

const formatDate = (value?: string | null) => value ? new Date(value).toLocaleString("zh-CN") : "-";
</script>

<style scoped>
.demand-item-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
}

.demand-item-tags--compact {
  max-height: 52px;
  overflow: hidden;
}

.demand-item-tags--compact :deep(.el-tag) {
  display: flex;
  padding: 0 6px;
  justify-content: center;
  text-align: center;
  white-space: nowrap;
}

.match-progress { max-width: 100%; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
</style>
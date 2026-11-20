<template>
  <div class="purchase-demand-page">
    <QueryPage ref="queryPageRef" api="/admin/crm/purchase-demands/items" :searchParam="searchForm" @reset="handleReset">
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
          <el-form-item label="撮合状态">
            <el-select v-model="searchForm.matchStatus" clearable placeholder="全部状态">
              <el-option label="空闲" value="空闲" />
              <el-option label="撮合中" value="撮合中" />
              <el-option label="成交" value="成交" />
            </el-select>
          </el-form-item>
        </el-form>
      </template>

      <template #table="{ tableData }">
        <el-table
          :data="tableData"
          :row-key="(row: any) => row.item?.id || row.demand?.id"
          class="wide-list-table"
          style="--table-min-width: 1900px"
          border
          :span-method="(params: any) => mergeDemandCell(params, tableData)"
        >
          <!-- 单据级字段（同采购单合并）统一放左侧 -->
          <el-table-column prop="demand.demandNo" label="编号" min-width="180" show-overflow-tooltip />
          <el-table-column prop="demand.demandName" label="需求名称" min-width="200" show-overflow-tooltip />
          <el-table-column prop="demand.demandAt" label="提出日期" min-width="160">
            <template #default="{ row }">{{ formatDate(row.demand.demandAt) }}</template>
          </el-table-column>
          <el-table-column prop="demand.receivingAddress" label="收货地" min-width="160" show-overflow-tooltip>
            <template #default="{ row }">{{ row.demand.receivingAddress || "-" }}</template>
          </el-table-column>
          <el-table-column prop="demand.status" label="状态" min-width="100">
            <template #default="{ row }"><el-tag size="small">{{ row.demand.status }}</el-tag></template>
          </el-table-column>
          <el-table-column prop="demand.sourceType" label="来源" min-width="100">
            <template #default="{ row }">{{ row.demand.sourceType || "-" }}</template>
          </el-table-column>
          <el-table-column prop="demand.matchProgress" label="撮合进度" min-width="230" show-overflow-tooltip>
            <template #default="{ row }">{{ matchProgressLabel(row.demand) }}</template>
          </el-table-column>
          <!-- 明细级字段：一个品类一行 -->
          <el-table-column label="品类" min-width="120">
            <template #default="{ row }">
              <el-tag v-if="row.item?.productName" size="small" type="info" effect="plain">{{ row.item.productName }}</el-tag>
              <span v-else>-</span>
            </template>
          </el-table-column>
          <el-table-column label="需求数量" min-width="110" align="right">
            <template #default="{ row }">{{ quantityLabel(row.item) }}</template>
          </el-table-column>
          <el-table-column label="已中选 / 待撮合" min-width="150">
            <template #default="{ row }">
              <template v-if="row.item">
                <span class="matched">{{ row.item.matchedQuantity || 0 }}</span> / <span :class="{ 'no-remaining': remainingOf(row.item) <= 0 }">{{ remainingOf(row.item) }}</span>{{ row.item.quantityUnit || "" }}
              </template>
              <span v-else>-</span>
            </template>
          </el-table-column>
          <el-table-column label="期望到货" min-width="160">
            <template #default="{ row }">{{ formatDate(row.item?.expectedDeliveryAt || row.demand.expectedDeliveryAt) }}</template>
          </el-table-column>
          <el-table-column label="撮合状态" min-width="100" align="center">
            <template #default="{ row }">
              <el-tag v-if="row.item" size="small" :type="matchStatusType(row.item.matchStatus)">{{ row.item.matchStatus || "空闲" }}</el-tag>
              <span v-else>-</span>
            </template>
          </el-table-column>
          <el-table-column label="操作" width="200" fixed="right" class-name="actions-column" header-class-name="actions-column">
            <template #default="{ row }">
              <Permission code="CRM_MATCH_OPPORTUNITY_MANAGE">
                <el-button v-if="hasOpportunity(row)" link type="primary" @click="viewDetail(row)">撮合详情</el-button>
                <el-button v-if="canStartMatch(row)" link type="primary" @click="startMatch(row)">发起撮合</el-button>
              </Permission>
            </template>
          </el-table-column>
        </el-table>
      </template>
    </QueryPage>
    <MatchOpportunityDetailDialog ref="matchDetailDialogRef" @refresh="reloadList" />
  </div>
</template>

<script setup lang="ts">
import { reactive, ref } from "vue";
import { ElMessage } from "element-plus";
import Permission from "@/components/Permission/index.vue";
import QueryPage from "@/components/QueryPage/index.vue";
import MatchOpportunityDetailDialog from "@/views/crm/matchOpportunity/MatchOpportunityDetailDialog.vue";
import crmMatchOpportunityApi from "@/api/modules/crm/crmMatchOpportunity";

const queryPageRef = ref();
const reloadList = () => queryPageRef.value?.getTableList?.();
const statuses = ["待确认", "有效", "已完成", "已关闭"];
const searchForm = reactive({ keyword: "", status: "", matchStatus: "" });

// 采购明细是撮合的最小单位：接口直接按明细返回，一个品类一行；单据级字段在同一采购单内合并单元格
const mergedColumns = new Set(["demand.demandNo", "demand.demandName", "demand.demandAt", "demand.receivingAddress", "demand.status", "demand.sourceType", "demand.matchProgress"]);
const mergeDemandCell = ({ row, column, rowIndex }: any, rows: any[]) => {
  if (!mergedColumns.has(column.property)) return undefined;
  const key = row.demand.id;
  const prevKey = rowIndex > 0 ? rows[rowIndex - 1]?.demand?.id : undefined;
  if (prevKey === key) return { rowspan: 0, colspan: 0 };
  let rowspan = 1;
  for (let index = rowIndex + 1; index < rows.length; index += 1) {
    if (rows[index]?.demand?.id !== key) break;
    rowspan += 1;
  }
  return { rowspan, colspan: 1 };
};

const matchStatusType = (status?: string) => ({ "成交": "info", "撮合中": "warning", "空闲": "success" }[status || ""] || undefined);
// 待撮合 = 需求数量 - 已中选（中选量取 matchedQuantity，不再依赖后端 remainingQuantity）
const remainingOf = (item: any) => {
  const quantity = Number(item?.quantity ?? 0);
  const matched = Number(item?.matchedQuantity ?? 0);
  return quantity - matched;
};
const quantityLabel = (item: any) => item?.quantity === null || item?.quantity === undefined ? "-" : `${item.quantity}${item.quantityUnit || ""}`;
// 仅「撮合中 / 成交」可查看撮合详情；空闲状态只提供「发起撮合」
const hasOpportunity = (row: any) =>
  !!row.item && (row.item.matchStatus || "空闲") !== "空闲" && !!row.item.matchOpportunityId;
// 空闲且有剩余量才可发起（撮合失败关闭后仍可重新发起）
const canStartMatch = (row: any) => !!row.item
  && row.demand.status === "有效"
  && (row.item.matchStatus || "空闲") === "空闲"
  && (Number(row.item.quantity || 0) <= 0 || remainingOf(row.item) > 0);

const matchDetailDialogRef = ref();
const viewDetail = (row: any) => matchDetailDialogRef.value?.open(row.item.matchOpportunityId);
// 发起撮合与供应列表一致：不跳转，立即创建只含发起方（当前采购明细）的机会并弹详情，候选在详情内追加
const startMatch = async (row: any) => {
  const result = await crmMatchOpportunityApi.createEmpty({ source: "demand", demandItemId: row.item.id });
  ElMessage.success("撮合机会已创建");
  matchDetailDialogRef.value?.open(result.data);
  reloadList();
};

const handleReset = () => {
  searchForm.keyword = "";
  searchForm.status = "";
  searchForm.matchStatus = "";
};

// 单据撮合进度由接口按该单据全部明细统计，不依赖当前页返回了哪些明细。
const matchProgressLabel = (demand: any) => {
  const progress = demand.matchProgress;
  if (!progress?.totalItemCount) return "暂无采购明细";
  if (progress.completedItemCount >= progress.totalItemCount) return `已成交 ${progress.totalItemCount}/${progress.totalItemCount}`;
  const segments = [`已发起 ${progress.startedItemCount}/${progress.totalItemCount}`];
  if (progress.completedItemCount) segments.push(`${progress.completedItemCount} 已成交`);
  if (progress.inProgressItemCount) segments.push(`${progress.inProgressItemCount} 撮合中`);
  if (progress.pausedItemCount) segments.push(`${progress.pausedItemCount} 已暂停`);
  if (progress.closedItemCount) segments.push(`${progress.closedItemCount} 已结束`);
  if (progress.pendingItemCount) segments.push(`${progress.pendingItemCount} 空闲`);
  return segments.join(" · ");
};

const formatDate = (value?: string | null) => value ? new Date(value).toLocaleDateString("zh-CN") : "-";
</script>

<style scoped>
.matched { color: var(--el-color-success); }
.no-remaining { color: var(--el-text-color-placeholder); }
</style>

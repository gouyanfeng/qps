<template>
  <div class="supply-list-page">
    <QueryPage ref="queryPageRef" api="/admin/crm/herb-base-supplies" :search-param="searchForm" @reset="handleReset">
      <template #searchConditions>
        <el-form :model="searchForm" :inline="true">
          <el-form-item label="品类"><ProductSelect v-model="searchForm.productName" placeholder="请选择品类" /></el-form-item>
          <el-form-item label="基地"><el-input v-model="searchForm.herbBaseName" clearable placeholder="基地名称 / 主体" style="width: 180px" /></el-form-item>
          <el-form-item label="地区"><ChinaRegionCascader v-model="regionPath" placeholder="请选择省 / 市 / 区县" @change="handleRegionChange" /></el-form-item>
          <el-form-item label="供应状态">
            <el-select v-model="searchForm.status" clearable placeholder="全部状态">
              <el-option v-for="status in supplyStatuses" :key="status" :label="status" :value="status" />
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
        <el-table :data="tableData" border class="wide-list-table" style="--table-min-width: 1270px" :span-method="(params: any) => mergeBaseCell(params, tableData)">
          <el-table-column prop="herbBaseName" label="基地" min-width="180" show-overflow-tooltip>
            <template #default="{ row }">
              <el-button v-if="row.herbBaseSubjectId" link type="primary" @click="openHerbBase(row)">{{ row.herbBaseName || "-" }}</el-button>
              <span v-else>{{ row.herbBaseName || "-" }}</span>
            </template>
          </el-table-column>
          <el-table-column prop="region" label="地区" min-width="180" show-overflow-tooltip><template #default="{ row }">{{ regionLabel(row) }}</template></el-table-column>
          <el-table-column label="供应品类" min-width="120">
            <template #default="{ row }">
              <el-tag v-if="row.productName" size="small" type="info" effect="plain">{{ row.productName }}</el-tag>
              <span v-else>-</span>
            </template>
          </el-table-column>
          <el-table-column label="可供量" width="120" align="right"><template #default="{ row }">{{ quantityLabel(row) }}</template></el-table-column>
          <el-table-column prop="validUntil" label="有效截止日" width="130"><template #default="{ row }">{{ formatDate(row.validUntil) }}</template></el-table-column>
          <el-table-column label="供应状态" width="100" align="center"><template #default="{ row }"><el-tag size="small" :type="supplyStatusType(row.status)">{{ row.status || "-" }}</el-tag></template></el-table-column>
          <el-table-column label="撮合状态" width="100" align="center"><template #default="{ row }"><el-tag size="small" :type="matchStatusType(row.matchStatus)">{{ row.matchStatus || "空闲" }}</el-tag></template></el-table-column>
          <el-table-column label="操作" width="130" fixed="right" class-name="actions-column" header-class-name="actions-column">
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

<script setup lang="ts" name="herbBaseSupply">
import { reactive, ref } from "vue";
import { useRouter } from "vue-router";
import { ElMessage } from "element-plus";
import QueryPage from "@/components/QueryPage/index.vue";
import Permission from "@/components/Permission/index.vue";
import ChinaRegionCascader from "@/components/ChinaRegionCascader/index.vue";
import ProductSelect from "@/views/crm/components/ProductSelect/index.vue";
import MatchOpportunityDetailDialog from "@/views/crm/matchOpportunity/MatchOpportunityDetailDialog.vue";
import crmMatchOpportunityApi from "@/api/modules/crm/crmMatchOpportunity";

const router = useRouter();
const queryPageRef = ref();
const reloadList = () => queryPageRef.value?.getTableList?.();
const supplyStatuses = ["待确认", "有效", "暂停", "已售罄", "已失效"];
const searchForm = reactive({ productName: "", herbBaseName: "", region: "", status: "", matchStatus: "" });
const regionPath = ref<string[]>([]);

const quantityLabel = (row: any) => row.availableQuantity === null || row.availableQuantity === undefined ? "-" : `${row.availableQuantity}${row.quantityUnit || ""}`;
const regionLabel = (row: any) => [row.province, row.city, row.area].filter(Boolean).join(" / ") || "-";
const formatDate = (value?: string | null) => value ? new Date(value).toLocaleDateString("zh-CN") : "-";
const supplyStatusType = (status?: string) => ({ "有效": "success", "暂停": "warning", "已售罄": "info", "已失效": "danger" }[status || ""] || undefined);
const matchStatusType = (status?: string) => ({ "成交": "info", "撮合中": "warning", "空闲": "success" }[status || ""] || undefined);
// 同一基地的连续行合并「基地」与「地区」单元格
const mergeBaseCell = ({ row, column, rowIndex }: any, rows: any[]) => {
  if (column.property !== "herbBaseName" && column.property !== "region") return undefined;
  const key = row.herbBaseId || row.herbBaseName;
  const prevKey = rowIndex > 0 ? rows[rowIndex - 1]?.herbBaseId || rows[rowIndex - 1]?.herbBaseName : undefined;
  if (rowIndex > 0 && prevKey === key) return { rowspan: 0, colspan: 0 };
  let rowspan = 1;
  for (let index = rowIndex + 1; index < rows.length; index += 1) {
    const nextKey = rows[index]?.herbBaseId || rows[index]?.herbBaseName;
    if (nextKey !== key) break;
    rowspan += 1;
  }
  return { rowspan, colspan: 1 };
};
// 仅「撮合中 / 成交」可查看撮合详情；空闲状态只提供「发起撮合」
const hasOpportunity = (row: any) =>
  (row.matchStatus || "空闲") !== "空闲" && !!row.latestOpportunityId;
const canStartMatch = (row: any) => row.status === "有效" && (row.matchStatus || "空闲") === "空闲";
const matchDetailDialogRef = ref();
const viewDetail = (row: any) => matchDetailDialogRef.value?.open(row.latestOpportunityId);
// 发起撮合不跳转：立即创建只含发起方（当前供应）的机会，并弹出撮合详情，候选在详情内追加
const startMatch = async (row: any) => {
  const result = await crmMatchOpportunityApi.createEmpty({ source: "supply", supplyId: row.id });
  ElMessage.success("撮合机会已创建");
  matchDetailDialogRef.value?.open(result.data);
  reloadList();
};
const openHerbBase = (row: any) => router.push({ path: "/crm/herb-base", query: { detailId: row.herbBaseSubjectId } });
const handleRegionChange = (regions: string[]) => {
  searchForm.region = regions.at(-1) || "";
};
const handleReset = () => {
  Object.assign(searchForm, { productName: "", herbBaseName: "", region: "", status: "", matchStatus: "" });
  regionPath.value = [];
};
</script>

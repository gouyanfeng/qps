<template>
  <div class="supply-list-page">
    <QueryPage ref="queryPageRef" api="/admin/crm/herb-base-supplies" :search-param="searchForm" @reset="handleReset">
      <template #searchConditions>
        <el-form :model="searchForm" :inline="true">
          <el-form-item label="品类"><ProductSelect v-model="searchForm.productName" placeholder="请选择品类" /></el-form-item>
          <el-form-item label="地区"><ChinaRegionCascader v-model="regionPath" placeholder="请选择省 / 市 / 区县" @change="handleRegionChange" /></el-form-item>
          <el-form-item label="供应状态">
            <el-select v-model="searchForm.status" clearable placeholder="全部状态">
              <el-option v-for="status in supplyStatuses" :key="status" :label="status" :value="status" />
            </el-select>
          </el-form-item>
          <el-form-item label="占用状态">
            <el-select v-model="searchForm.occupancyStatus" clearable placeholder="全部状态">
              <el-option label="已中选" value="已中选" />
              <el-option label="未中选" value="未中选" />
            </el-select>
          </el-form-item>
        </el-form>
      </template>

      <template #table="{ tableData }">
        <el-table :data="tableData" border class="wide-list-table" style="--table-min-width: 1450px">
          <el-table-column prop="subjectName" label="基地主体" min-width="180" show-overflow-tooltip />
          <el-table-column prop="herbBaseName" label="基地" min-width="180" show-overflow-tooltip />
          <el-table-column prop="productName" label="供应品类" min-width="120" show-overflow-tooltip />
          <el-table-column label="可供量" width="120" align="right"><template #default="{ row }">{{ quantityLabel(row) }}</template></el-table-column>
          <el-table-column label="地区" min-width="180" show-overflow-tooltip><template #default="{ row }">{{ regionLabel(row) }}</template></el-table-column>
          <el-table-column prop="validUntil" label="有效截止日" width="130"><template #default="{ row }">{{ formatDate(row.validUntil) }}</template></el-table-column>
          <el-table-column label="供应状态" width="100" align="center"><template #default="{ row }"><el-tag size="small" :type="supplyStatusType(row.status)">{{ row.status || "-" }}</el-tag></template></el-table-column>
          <el-table-column prop="candidateCount" label="候选次数" width="96" align="center" />
          <el-table-column label="占用状态" width="100" align="center"><template #default="{ row }"><el-tag size="small" :type="row.selectedCount ? 'danger' : 'success'">{{ row.selectedCount ? "已中选" : "未中选" }}</el-tag></template></el-table-column>
          <el-table-column prop="occupiedDemandSummary" label="占用需求" min-width="180" show-overflow-tooltip><template #default="{ row }">{{ row.occupiedDemandSummary || "-" }}</template></el-table-column>
          <el-table-column label="操作" width="100" fixed="right"><template #default="{ row }"><el-button link type="primary" :disabled="!row.herbBaseSubjectId" @click="openHerbBase(row)">查看基地</el-button></template></el-table-column>
        </el-table>
      </template>
    </QueryPage>
  </div>
</template>

<script setup lang="ts" name="herbBaseSupply">
import { reactive, ref } from "vue";
import { useRouter } from "vue-router";
import QueryPage from "@/components/QueryPage/index.vue";
import ChinaRegionCascader from "@/components/ChinaRegionCascader/index.vue";
import ProductSelect from "@/components/ProductSelect/index.vue";

const router = useRouter();
const queryPageRef = ref();
const supplyStatuses = ["待确认", "有效", "暂停", "已售罄", "已失效"];
const searchForm = reactive({ productName: "", region: "", status: "", occupancyStatus: "" });
const regionPath = ref<string[]>([]);

const quantityLabel = (row: any) => row.availableQuantity === null || row.availableQuantity === undefined ? "-" : `${row.availableQuantity}${row.quantityUnit || ""}`;
const regionLabel = (row: any) => [row.province, row.city, row.area].filter(Boolean).join(" / ") || "-";
const formatDate = (value?: string | null) => value ? new Date(value).toLocaleDateString("zh-CN") : "-";
const supplyStatusType = (status?: string) => ({ "有效": "success", "暂停": "warning", "已售罄": "info", "已失效": "danger" }[status || ""] || undefined);
const openHerbBase = (row: any) => router.push({ path: "/crm/herb-base", query: { detailId: row.herbBaseSubjectId } });
const handleRegionChange = (regions: string[]) => {
  searchForm.region = regions.at(-1) || "";
};
const handleReset = () => {
  Object.assign(searchForm, { productName: "", region: "", status: "", occupancyStatus: "" });
  regionPath.value = [];
};
</script>

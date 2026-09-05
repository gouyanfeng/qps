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

      <template #headerButtons>
        <Permission code="CRM_PURCHASE_DEMAND_MANAGE">
          <el-button type="primary" :icon="Plus" @click="open()">新增采购需求</el-button>
        </Permission>
      </template>

      <template #table="{ tableData }">
        <el-table :data="tableData" :row-key="'id'" class="wide-list-table" style="--table-min-width: 1530px" border>
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
          <el-table-column label="操作" width="230" fixed="right">
            <template #default="{ row }">
              <Permission code="CRM_PURCHASE_DEMAND_MANAGE"><el-button link type="primary" @click="open(row)">编辑</el-button></Permission>
              <Permission code="CRM_PURCHASE_DEMAND_MANAGE"><el-button v-if="row.status === '待确认'" link type="success" @click="changeStatus(row, '有效')">确认有效</el-button></Permission>
              <Permission code="CRM_PURCHASE_DEMAND_MANAGE"><el-button v-if="row.status !== '已完成' && row.status !== '已关闭'" link type="danger" @click="close(row)">关闭</el-button></Permission>
            </template>
          </el-table-column>
        </el-table>
      </template>
    </QueryPage>

    <CrmVendorDemandEditor v-model="editorVisible" :demand="editingDemand" @saved="reloadList" />
  </div>
</template>

<script setup lang="ts">
import { reactive, ref } from "vue";
import { ElMessage, ElMessageBox } from "element-plus";
import { Plus } from "@element-plus/icons-vue";
import crmVendorDemandApi from "@/api/modules/crmVendorDemand";
import CrmVendorDemandEditor from "@/views/crm/components/vendorDemand/Editor.vue";
import Permission from "@/components/Permission/index.vue";
import QueryPage from "@/components/QueryPage/index.vue";

const queryPageRef = ref();
const editorVisible = ref(false);
const editingDemand = ref<any>(null);
const statuses = ["待确认", "有效", "匹配中", "已完成", "已关闭"];
const searchForm = reactive({ keyword: "", status: "" });

const maxDemandItemCount = 8;

const getDemandItemLabel = (item: any) => `${item.productName}${item.quantity ? ` ${item.quantity}${item.quantityUnit || ""}` : ""}`;

const visibleDemandItems = (items: any[]) => {
  if (items.length <= maxDemandItemCount) return items;
  return items.slice(0, maxDemandItemCount - 1);
};

const hiddenDemandItemCount = (items: any[]) => items.length - visibleDemandItems(items).length;

const getDemandItemsTooltip = (items: any[]) => `采购明细：${items.map(getDemandItemLabel).join("、")}`;

const reloadList = () => queryPageRef.value?.getTableList();

const handleReset = () => {
  searchForm.keyword = "";
  searchForm.status = "";
};

const open = (row?: any) => {
  editingDemand.value = row || null;
  editorVisible.value = true;
};

const changeStatus = async (row: any, status: string) => {
  await crmVendorDemandApi.changeStatus(row.id, { status });
  ElMessage.success("状态已更新");
  reloadList();
};

const close = async (row: any) => {
  const { value } = await ElMessageBox.prompt("关闭原因", "关闭采购需求", {
    inputPattern: /.+/,
    inputErrorMessage: "请填写关闭原因",
  });
  await crmVendorDemandApi.changeStatus(row.id, { status: "已关闭", closedReason: value });
  ElMessage.success("采购需求已关闭");
  reloadList();
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
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  grid-template-rows: repeat(2, 24px);
  overflow: hidden;
}

.demand-item-tags--compact :deep(.el-tag) {
  display: flex;
  min-width: 0;
  overflow: hidden;
  justify-content: center;
  text-align: center;
  text-overflow: ellipsis;
  white-space: nowrap;
}
</style>

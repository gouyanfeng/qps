<template>
  <div class="list-page">
    <QueryPage api="/admin/mall/skus" :searchParam="searchForm" @reset="handleReset" ref="queryPageRef">
      <!-- 搜索条件 -->
      <template #searchConditions>
        <el-form :model="searchForm" :inline="true">
          <el-form-item label="关键字">
            <el-input v-model="searchForm.keyword" placeholder="商品 / 规格编码 / 名称" clearable />
          </el-form-item>
          <el-form-item label="库存预警">
            <el-select v-model="searchForm.lowStock" placeholder="全部">
              <el-option label="全部" value="" />
              <el-option label="仅看低库存" :value="true" />
            </el-select>
          </el-form-item>
        </el-form>
      </template>

      <!-- 功能按钮 -->
      <template #headerButtons>
        <el-button type="primary" :icon="EditPen" :disabled="selectedRows.length === 0" @click="openBatchDialog">
          批量调整{{ selectedRows.length ? `（已选 ${selectedRows.length} 项）` : '' }}
        </el-button>
      </template>

      <!-- 表格 -->
      <template #table="{ tableData }">
        <el-table
          :data="tableData"
          :fit="true"
          style="width: 100%"
          border
          @selection-change="handleSelectionChange"
        >
          <el-table-column type="selection" width="50" align="center" />
          <el-table-column prop="productName" label="商品" min-width="160" show-overflow-tooltip />
          <el-table-column prop="productCode" label="商品编码" min-width="140" show-overflow-tooltip />
          <el-table-column label="规格组合" min-width="200" show-overflow-tooltip>
            <template #default="{ row }">
              <span v-if="formatCombination(row.combination)">{{ formatCombination(row.combination) }}</span>
              <span v-else>{{ row.specName || '-' }}</span>
            </template>
          </el-table-column>
          <el-table-column prop="skuCode" label="规格编码" min-width="140" show-overflow-tooltip />
          <el-table-column label="价格" width="110" align="right">
            <template #default="{ row }">¥{{ Number(row.price).toFixed(2) }}</template>
          </el-table-column>
          <el-table-column label="库存" width="110" align="center">
            <template #default="{ row }">
              <span :class="{ 'low-stock': row.isLowStock }">{{ row.stock }}</span>
            </template>
          </el-table-column>
          <el-table-column prop="warnStock" label="预警库存" width="100" align="center" />
          <el-table-column label="状态" width="100" align="center">
            <template #default="{ row }">
              <el-tag :type="row.isActive ? 'success' : 'info'">{{ row.isActive ? '启用' : '停用' }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column label="更新时间" width="180" align="center">
            <template #default="{ row }">{{ formatDate(row.updatedAt) }}</template>
          </el-table-column>
          <el-table-column label="操作" align="center" width="190" fixed="right" class-name="actions-column" header-class-name="actions-column">
            <template #default="{ row }">
              <el-button type="primary" link :icon="Sort" @click="openAdjustDialog(row)">调整库存</el-button>
              <el-button type="primary" link :icon="Tickets" @click="openLogDrawer(row)">流水</el-button>
            </template>
          </el-table-column>
        </el-table>
      </template>
    </QueryPage>

    <!-- 调整库存 -->
    <el-dialog v-model="adjustVisible" title="调整库存" width="440px">
      <el-form :model="adjustForm" label-width="90px">
        <el-form-item label="规格">
          <span>{{ currentSkuName }}</span>
        </el-form-item>
        <el-form-item label="当前库存">
          <span>{{ currentStock }}</span>
        </el-form-item>
        <el-form-item label="目标库存" required>
          <el-input-number v-model="adjustForm.targetStock" :min="0" :max="1000000" style="width: 100%" />
        </el-form-item>
        <el-form-item label="调整备注">
          <el-input
            v-model="adjustForm.remark"
            type="textarea"
            :rows="3"
            placeholder="请输入调整原因，便于追溯"
            maxlength="200"
            show-word-limit
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <span class="dialog-footer">
          <el-button @click="adjustVisible = false">取消</el-button>
          <el-button type="primary" :loading="adjustSubmitting" @click="submitAdjust">确定</el-button>
        </span>
      </template>
    </el-dialog>

    <!-- 批量调整 -->
    <el-dialog v-model="batchVisible" title="批量调整" width="480px">
      <el-form :model="batchForm" label-width="100px">
        <el-form-item label="已选规格">
          <span>{{ selectedRows.length }} 项</span>
        </el-form-item>
        <el-form-item label="修改价格">
          <el-checkbox v-model="batchForm.changePrice">同时设置价格</el-checkbox>
        </el-form-item>
        <el-form-item v-if="batchForm.changePrice" label="价格(元)">
          <el-input-number v-model="batchForm.price" :precision="2" :min="0.01" :max="9999999" style="width: 100%" />
        </el-form-item>
        <el-form-item label="修改库存">
          <el-checkbox v-model="batchForm.changeStock">同时设置库存</el-checkbox>
        </el-form-item>
        <el-form-item v-if="batchForm.changeStock" label="目标库存">
          <el-input-number v-model="batchForm.targetStock" :min="0" :max="1000000" style="width: 100%" />
        </el-form-item>
        <el-form-item label="调整备注">
          <el-input v-model="batchForm.remark" placeholder="请输入调整原因" maxlength="200" show-word-limit />
        </el-form-item>
      </el-form>
      <template #footer>
        <span class="dialog-footer">
          <el-button @click="batchVisible = false">取消</el-button>
          <el-button type="primary" :loading="batchSubmitting" @click="submitBatch">确定</el-button>
        </span>
      </template>
    </el-dialog>

    <!-- 库存流水 -->
    <el-drawer v-model="logDrawerVisible" :title="logDrawerTitle" size="760px">
      <el-table :data="logList" border style="width: 100%" v-loading="logLoading">
        <el-table-column label="变更类型" width="130" align="center">
          <template #default="{ row }">
            <el-tag :type="changeTypeTag(row.changeType)">{{ changeTypeLabel(row.changeType) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="变更数量" width="100" align="center">
          <template #default="{ row }">
            <span :class="row.changeQuantity >= 0 ? 'stock-in' : 'stock-out'">
              {{ row.changeQuantity > 0 ? `+${row.changeQuantity}` : row.changeQuantity }}
            </span>
          </template>
        </el-table-column>
        <el-table-column prop="beforeStock" label="变更前" width="90" align="center" />
        <el-table-column prop="afterStock" label="变更后" width="90" align="center" />
        <el-table-column prop="relatedOrderNo" label="关联订单号" min-width="170" show-overflow-tooltip />
        <el-table-column prop="remark" label="备注" min-width="160" show-overflow-tooltip />
        <el-table-column prop="operatorName" label="操作人" width="110" show-overflow-tooltip />
        <el-table-column label="时间" width="180" align="center">
          <template #default="{ row }">{{ formatDate(row.createdAt) }}</template>
        </el-table-column>
      </el-table>
      <div class="pagination mt20">
        <el-pagination
          v-model:current-page="logPage.page"
          v-model:page-size="logPage.pageSize"
          :page-sizes="[10, 20, 50]"
          layout="total, sizes, prev, pager, next"
          :total="logPage.total"
          @size-change="loadStockLogs"
          @current-change="loadStockLogs"
        />
      </div>
    </el-drawer>
  </div>
</template>

<script setup lang="ts" name="mallStock">
import { ref, reactive } from 'vue'
import { ElMessage } from 'element-plus'
import { EditPen, Sort, Tickets } from '@element-plus/icons-vue'
import { mallStockApi } from '@/api/modules/mallStock'
import QueryPage from '@/components/QueryPage/index.vue'
import { formatDate } from '@/utils'

const queryPageRef = ref()
const selectedRows = ref<any[]>([])

const searchForm = reactive({
  keyword: '',
  lowStock: '' as boolean | ''
})

const handleReset = () => {
  Object.assign(searchForm, {
    keyword: '',
    lowStock: ''
  })
}

const handleSelectionChange = (rows: any[]) => {
  selectedRows.value = rows
}

// 调整库存
const adjustVisible = ref(false)
const adjustSubmitting = ref(false)
const currentSkuId = ref('')
const currentSkuName = ref('')
const currentStock = ref(0)
const adjustForm = reactive({
  targetStock: 0,
  remark: ''
})

const openAdjustDialog = (row: any) => {
  currentSkuId.value = row.id
  currentSkuName.value = `${row.productName} / ${formatCombination(row.combination) || row.specName || row.skuCode}`
  currentStock.value = row.stock
  Object.assign(adjustForm, {
    targetStock: row.stock,
    remark: ''
  })
  adjustVisible.value = true
}

const submitAdjust = async () => {
  adjustSubmitting.value = true
  try {
    await mallStockApi.adjustStock(currentSkuId.value, {
      targetStock: adjustForm.targetStock,
      remark: adjustForm.remark.trim()
    })
    ElMessage.success('库存调整成功')
    adjustVisible.value = false
    queryPageRef.value?.getTableList()
  } catch {
    // 错误提示已由请求拦截器统一处理
  } finally {
    adjustSubmitting.value = false
  }
}

// 批量调整
const batchVisible = ref(false)
const batchSubmitting = ref(false)
const batchForm = reactive({
  changePrice: false,
  price: 1,
  changeStock: false,
  targetStock: 0,
  remark: ''
})

const openBatchDialog = () => {
  Object.assign(batchForm, {
    changePrice: false,
    price: 1,
    changeStock: false,
    targetStock: 0,
    remark: ''
  })
  batchVisible.value = true
}

const submitBatch = async () => {
  if (!batchForm.changePrice && !batchForm.changeStock) {
    ElMessage.warning('请至少选择价格或库存进行调整')
    return
  }

  batchSubmitting.value = true
  try {
    await mallStockApi.batchAdjust({
      ids: selectedRows.value.map(row => row.id),
      price: batchForm.changePrice ? batchForm.price : null,
      targetStock: batchForm.changeStock ? batchForm.targetStock : null,
      remark: batchForm.remark.trim()
    })
    ElMessage.success('批量调整成功')
    batchVisible.value = false
    queryPageRef.value?.getTableList()
  } catch {
    // 错误提示已由请求拦截器统一处理
  } finally {
    batchSubmitting.value = false
  }
}

// 库存流水
const logDrawerVisible = ref(false)
const logDrawerTitle = ref('库存流水')
const logLoading = ref(false)
const logList = ref<any[]>([])
const logPage = reactive({
  page: 1,
  pageSize: 10,
  total: 0
})
const currentLogSkuId = ref('')

const openLogDrawer = (row: any) => {
  logDrawerTitle.value = `库存流水 - ${row.productName} / ${row.specName}`
  currentLogSkuId.value = row.id
  logPage.page = 1
  logPage.total = 0
  logList.value = []
  logDrawerVisible.value = true
  loadStockLogs()
}

const loadStockLogs = async () => {
  logLoading.value = true
  try {
    const result = await mallStockApi.getStockLogs({
      skuId: currentLogSkuId.value,
      page: logPage.page,
      pageSize: logPage.pageSize
    })
    logList.value = result.data?.list || []
    logPage.total = result.data?.totalCount || 0
  } catch {
    logList.value = []
  } finally {
    logLoading.value = false
  }
}

const changeTypeLabel = (changeType: string) => {
  const labels: Record<string, string> = {
    OrderDeduct: '下单扣减',
    OrderCancel: '取消回补',
    RefundRestore: '退款回补',
    ManualAdjust: '手动调整'
  }
  return labels[changeType] || changeType
}

const changeTypeTag = (changeType: string) => {
  if (changeType === 'OrderDeduct') return 'danger'
  if (changeType === 'ManualAdjust') return 'warning'
  return 'success'
}

const formatCombination = (combo: Record<string, string>) => {
  if (!combo) return ''
  return Object.entries(combo).map(([k, v]) => `${k}:${v}`).join(' / ')
}
</script>

<style scoped lang="scss">
.low-stock {
  color: var(--el-color-danger);
  font-weight: 600;
}

.stock-in {
  color: var(--el-color-success);
}

.stock-out {
  color: var(--el-color-danger);
}

.pagination {
  display: flex;
  justify-content: flex-end;
}
</style>
<template>
  <div class="list-page">
    <QueryPage ref="queryRef" :key="filterKey" :api="currentApi" :search-param="queryParams" @reset="handleReset">
      <template #searchConditions>
        <el-form :model="searchForm" :inline="true">
          <el-form-item label="状态">
            <el-select v-model="statusFilter" placeholder="请选择状态">
              <el-option v-for="item in statusOptions" :key="item.value" :label="item.label" :value="item.value" />
            </el-select>
          </el-form-item>
          <el-form-item label="关键字">
            <el-input v-model="searchForm.keyword" placeholder="订单号 / 运单号" clearable />
          </el-form-item>
          <el-form-item :label="isPending ? '下单时间' : '发货时间'">
            <el-date-picker
              v-model="dateRange"
              type="daterange"
              value-format="YYYY-MM-DD"
              start-placeholder="开始日期"
              end-placeholder="结束日期"
              style="width: 260px"
              @change="handleDateChange"
            />
          </el-form-item>
        </el-form>
      </template>
      <template #table="{ tableData }">
        <el-table :data="tableData" style="width: 100%" border>
          <el-table-column label="订单号" min-width="200" show-overflow-tooltip>
            <template #default="{ row }">{{ row.orderNo || '-' }}</template>
          </el-table-column>
          <el-table-column label="发货单号" min-width="190" show-overflow-tooltip>
            <template #default="{ row }">{{ row.shipmentNo || '-' }}</template>
          </el-table-column>
          <el-table-column label="会员" min-width="130" show-overflow-tooltip>
            <template #default="{ row }">{{ row.memberName || '-' }}</template>
          </el-table-column>
          <el-table-column label="商品件数" width="100" align="center">
            <template #default="{ row }">{{ getItemCount(row) }}</template>
          </el-table-column>
          <el-table-column label="快递公司" min-width="120" show-overflow-tooltip>
            <template #default="{ row }">{{ row.expressCompany || '-' }}</template>
          </el-table-column>
          <el-table-column label="运单号" min-width="170" show-overflow-tooltip>
            <template #default="{ row }">{{ row.trackingNo || '-' }}</template>
          </el-table-column>
          <el-table-column label="付款时间" width="170" align="center">
            <template #default="{ row }">{{ row.paidAt ? formatDate(row.paidAt) : '-' }}</template>
          </el-table-column>
          <el-table-column label="发货时间" width="170" align="center">
            <template #default="{ row }">{{ row.shippedAt ? formatDate(row.shippedAt) : '-' }}</template>
          </el-table-column>
          <el-table-column label="状态" width="100" align="center">
            <template #default="{ row }">
              <el-tag :type="getRowStatusType(row)" size="small">{{ getRowStatusText(row) }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column label="操作" width="110" align="center" fixed="right">
            <template #default="{ row }">
              <el-button v-if="isOrderRow(row)" type="primary" link :icon="Van" @click="openShipmentDialog(row)">发货</el-button>
              <el-button v-else type="primary" link :icon="View" @click="openDetail(row)">详情</el-button>
            </template>
          </el-table-column>
        </el-table>
      </template>
    </QueryPage>

    <ShipmentDialog
      v-model="shipmentDialogVisible"
      :order-id="currentOrderId"
      :order-no="currentOrderNo"
      @shipped="handleShipped"
    />

    <el-drawer v-model="detailVisible" title="发货单详情" size="46%" class="shipment-detail-drawer">
      <div v-if="detail" class="drawer-body">
        <div class="detail-head">
          <div>
            <div class="detail-kicker">发货单号</div>
            <h2>{{ detail.shipmentNo }}</h2>
          </div>
          <el-tag :type="getShipmentStatusType(detail)" effect="dark">{{ getShipmentStatusText(detail) }}</el-tag>
        </div>

        <el-descriptions :column="2" border>
          <el-descriptions-item label="订单号">{{ detail.orderNo }}</el-descriptions-item>
          <el-descriptions-item label="订单状态">{{ getOrderStatusText(detail.orderStatus) }}</el-descriptions-item>
          <el-descriptions-item label="快递公司">{{ detail.expressCompany }}</el-descriptions-item>
          <el-descriptions-item label="运单号">{{ detail.trackingNo }}</el-descriptions-item>
          <el-descriptions-item label="会员">{{ detail.memberName || '-' }}</el-descriptions-item>
          <el-descriptions-item label="收货人">{{ detail.receiverName }} {{ detail.receiverPhone }}</el-descriptions-item>
          <el-descriptions-item label="商品件数">{{ detail.itemCount }}</el-descriptions-item>
          <el-descriptions-item label="实付金额">¥{{ Number(detail.paidAmount || 0).toFixed(2) }}</el-descriptions-item>
          <el-descriptions-item label="发货时间">{{ formatDate(detail.shippedAt) }}</el-descriptions-item>
          <el-descriptions-item label="签收时间">{{ formatDate(detail.deliveredAt) }}</el-descriptions-item>
          <el-descriptions-item label="备注" :span="2">{{ detail.remark || '-' }}</el-descriptions-item>
        </el-descriptions>

        <el-descriptions v-if="detail.isVoided" :column="2" border class="void-info">
          <el-descriptions-item label="作废时间">{{ formatDate(detail.voidedAt) }}</el-descriptions-item>
          <el-descriptions-item label="操作人">{{ detail.voidedBy || '-' }}</el-descriptions-item>
          <el-descriptions-item label="作废原因" :span="2">{{ detail.voidReason || '-' }}</el-descriptions-item>
        </el-descriptions>
      </div>

      <template #footer>
        <el-button @click="detailVisible = false">关闭</el-button>
        <el-button v-if="detail && !detail.isVoided" type="danger" :icon="CircleClose" @click="handleVoid">
          作废
        </el-button>
      </template>
    </el-drawer>
  </div>
</template>

<script setup lang="ts" name="mallShipment">
import { ref, reactive, computed } from 'vue'
import { Van, View, CircleClose } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { mallShipmentApi } from '@/api/modules/mallShipment'
import QueryPage from '@/components/QueryPage/index.vue'
import { formatDate } from '@/utils'
import ShipmentDialog from '../order/components/ShipmentDialog.vue'

type StatusFilter = 'pending' | 'shipped' | 'voided' | 'all'

const statusOptions: { label: string; value: StatusFilter }[] = [
  { label: '待发货', value: 'pending' },
  { label: '已发货', value: 'shipped' },
  { label: '已作废', value: 'voided' },
  { label: '全部', value: 'all' }
]

// 默认「待发货」：发货管理的日常主流程是处理待发货订单，与原首个页签一致
const statusFilter = ref<StatusFilter>('pending')
const queryRef = ref()
const filterKey = computed(() => statusFilter.value)
const isPending = computed(() => statusFilter.value === 'pending')

const searchForm = reactive({
  keyword: '',
  startTime: '',
  endTime: ''
})
const dateRange = ref<string[]>([])

// 待发货取订单列表，其余状态取运单列表
const currentApi = computed(() => (isPending.value ? '/admin/mall/orders' : '/admin/mall/shipments'))

const queryParams = computed(() => {
  const params: Record<string, any> = {
    keyword: searchForm.keyword,
    startTime: searchForm.startTime,
    endTime: searchForm.endTime
  }
  switch (statusFilter.value) {
    case 'pending':
      params.status = 'Paid'
      break
    case 'shipped':
      params.status = 'Shipped'
      params.isVoided = false
      break
    case 'voided':
      params.isVoided = true
      break
    default:
      // 全部：展示全部运单（含已作废）
      break
  }
  return params
})

const shipmentDialogVisible = ref(false)
const currentOrderId = ref('')
const currentOrderNo = ref('')

const detailVisible = ref(false)
const detail = ref<any>(null)

const handleDateChange = (value: string[] | null) => {
  if (value && value.length === 2) {
    searchForm.startTime = `${value[0]}T00:00:00`
    searchForm.endTime = `${value[1]}T23:59:59`
  } else {
    searchForm.startTime = ''
    searchForm.endTime = ''
  }
}

const handleReset = () => {
  searchForm.keyword = ''
  searchForm.startTime = ''
  searchForm.endTime = ''
  dateRange.value = []
}

const isOrderRow = (row: any) => !row.shipmentNo

const getItemCount = (row: any) => {
  const items = row.items || []
  if (!items.length) return '-'
  return items.reduce((total: number, item: any) => total + Number(item.quantity || 0), 0)
}

const getRowStatusText = (row: any) => {
  if (isOrderRow(row)) return '待发货'
  if (row.isVoided) return '已作废'
  return row.status === 'Delivered' ? '已签收' : '已发货'
}

const getRowStatusType = (row: any) => {
  if (isOrderRow(row)) return 'warning'
  if (row.isVoided) return 'danger'
  return row.status === 'Delivered' ? 'success' : 'info'
}

const openShipmentDialog = (row: any) => {
  currentOrderId.value = row.id
  currentOrderNo.value = row.orderNo || ''
  shipmentDialogVisible.value = true
}

const handleShipped = () => {
  queryRef.value?.getTableList()
}

const openDetail = async (row: any) => {
  detail.value = null
  detailVisible.value = true
  try {
    const result = await mallShipmentApi.getShipment(row.id)
    detail.value = result.data
  } catch {
    detailVisible.value = false
  }
}

const handleVoid = async () => {
  if (!detail.value || detail.value.isVoided) return
  try {
    const { value } = await ElMessageBox.prompt('请填写作废原因（必填）', '作废运单', {
      confirmButtonText: '确定作废',
      cancelButtonText: '取消',
      inputPlaceholder: '例如：运单号录入错误，需要重新发货',
      inputValidator: (input: string) => (input && input.trim() ? true : '请填写作废原因'),
      type: 'warning'
    })

    await mallShipmentApi.voidShipment(detail.value.id, { reason: value.trim() })
    ElMessage.success('运单已作废，订单已回到待发货')
    detailVisible.value = false
    queryRef.value?.getTableList()
  } catch {
    // 取消操作或请求失败，错误提示由拦截器统一处理
  }
}

const getShipmentStatusText = (row: any) => {
  if (row.isVoided) return '已作废'
  return row.status === 'Delivered' ? '已签收' : '已发货'
}

const getShipmentStatusType = (row: any) => {
  if (row.isVoided) return 'danger'
  return row.status === 'Delivered' ? 'success' : 'info'
}

const getOrderStatusText = (status: string) => {
  const map: Record<string, string> = {
    PendingPayment: '待支付',
    Paid: '待发货',
    Shipped: '已发货',
    Completed: '已完成',
    Cancelled: '已取消',
    Refunding: '退款中',
    Refunded: '已退款'
  }
  return map[status] || status || '-'
}
</script>

<style scoped lang="scss">
.drawer-body {
  padding: 4px 8px 24px;

  h2 {
    margin: 6px 0 16px;
    font-size: 20px;
  }
}

.detail-head {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 16px;
}

.detail-kicker {
  color: var(--el-text-color-secondary);
  font-size: 13px;
}

.void-info {
  margin-top: 16px;
}
</style>

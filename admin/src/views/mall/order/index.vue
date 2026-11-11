<template>
  <div class="list-page">
    <QueryPage api="/admin/mall/orders" :searchParam="searchForm" @reset="handleReset" ref="queryPageRef">
      <!-- 搜索条件 -->
      <template #searchConditions>
        <el-form :model="searchForm" :inline="true">
          <el-form-item label="关键字">
            <el-input v-model="searchForm.keyword" placeholder="订单号 / 收货人 / 电话" clearable />
          </el-form-item>
          <el-form-item label="状态">
            <el-select v-model="searchForm.status" placeholder="全部" clearable>
              <el-option v-for="item in statusOptions" :key="item.value" :label="item.label" :value="item.value" />
            </el-select>
          </el-form-item>
          <el-form-item label="门店">
            <el-select v-model="searchForm.storeId" placeholder="全部门店" clearable filterable>
              <el-option v-for="store in stores" :key="store.id" :label="store.name" :value="store.id" />
            </el-select>
          </el-form-item>
          <el-form-item label="下单时间">
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

      <!-- 表格 -->
      <template #table="{ tableData }">
        <el-table :data="tableData" :fit="true" style="width: 100%" border>
          <el-table-column prop="orderNo" label="订单号" min-width="200" show-overflow-tooltip />
          <el-table-column prop="storeName" label="门店" min-width="140" show-overflow-tooltip />
          <el-table-column prop="receiverName" label="收货人" min-width="110" show-overflow-tooltip />
          <el-table-column prop="receiverPhone" label="联系电话" min-width="130" show-overflow-tooltip />
          <el-table-column label="应付金额" width="120" align="right">
            <template #default="{ row }">¥{{ Number(row.payableAmount).toFixed(2) }}</template>
          </el-table-column>
          <el-table-column label="运费" width="100" align="right">
            <template #default="{ row }">¥{{ Number(row.freightAmount).toFixed(2) }}</template>
          </el-table-column>
          <el-table-column label="状态" width="110" align="center">
            <template #default="{ row }">
              <el-tag :type="getStatusType(row.status)" size="small">{{ getStatusText(row.status) }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column label="下单时间" width="180" align="center">
            <template #default="{ row }">{{ formatDate(row.createdAt) }}</template>
          </el-table-column>
          <el-table-column label="操作" align="center" width="280" fixed="right" class-name="actions-column" header-class-name="actions-column">
            <template #default="{ row }">
              <el-button type="primary" link :icon="View" @click="openDetail(row.id)">详情</el-button>
              <Permission code="MALL_ORDER_EDIT"><el-button v-if="row.status === 'PendingPayment'" type="danger" link :icon="CircleClose" @click="cancelOrder(row)">取消</el-button></Permission>
              <Permission code="MALL_ORDER_EDIT"><el-button v-if="row.status === 'Paid'" type="primary" link :icon="Van" @click="openShipmentDialog(row)">发货</el-button></Permission>
              <Permission code="MALL_ORDER_EDIT"><el-button v-if="row.status === 'Paid'" type="warning" link :icon="RefreshLeft" @click="createRefund(row)">退款</el-button></Permission>
              <Permission code="MALL_ORDER_EDIT"><el-button v-if="row.status === 'Shipped'" type="success" link :icon="CircleCheck" @click="confirmReceipt(row)">收货</el-button></Permission>
            </template>
          </el-table-column>
        </el-table>
      </template>
    </QueryPage>

    <OrderDetailDrawer
      v-model="drawerVisible"
      :order-id="currentOrderId"
      @refreshed="queryPageRef?.getTableList()"
    />

    <ShipmentDialog
      v-model="shipmentDialogVisible"
      :order-id="currentShipmentOrderId"
      @shipped="queryPageRef?.getTableList()"
    />
  </div>
</template>

<script setup lang="ts" name="mallOrder">
import { ref, reactive, onMounted } from 'vue'
import { View, CircleClose, Van, CircleCheck, RefreshLeft } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { mallOrderApi } from '@/api/modules/mallOrder'
import { mallStoreApi } from '@/api/modules/mallStore'
import QueryPage from '@/components/QueryPage/index.vue'
import { formatDate } from '@/utils'
import OrderDetailDrawer from './components/OrderDetailDrawer.vue'
import ShipmentDialog from './components/ShipmentDialog.vue'
import Permission from "@/components/Permission/index.vue"

const queryPageRef = ref()
const drawerVisible = ref(false)
const currentOrderId = ref('')
const stores = ref<any[]>([])
const dateRange = ref<string[]>([])

const statusOptions = [
  { label: '待支付', value: 'PendingPayment' },
  { label: '已支付', value: 'Paid' },
  { label: '已发货', value: 'Shipped' },
  { label: '已完成', value: 'Completed' },
  { label: '已取消', value: 'Cancelled' },
  { label: '退款中', value: 'Refunding' },
  { label: '已退款', value: 'Refunded' }
]

const searchForm = reactive({
  keyword: '',
  status: '',
  storeId: '',
  startTime: '',
  endTime: ''
})

const handleReset = () => {
  Object.assign(searchForm, {
    keyword: '',
    status: '',
    storeId: '',
    startTime: '',
    endTime: ''
  })
  dateRange.value = []
}

const handleDateChange = (value: string[] | null) => {
  if (value && value.length === 2) {
    searchForm.startTime = `${value[0]}T00:00:00`
    searchForm.endTime = `${value[1]}T23:59:59`
  } else {
    searchForm.startTime = ''
    searchForm.endTime = ''
  }
}

const loadStores = async () => {
  try {
    const result = await mallStoreApi.getStoreList({ page: 1, pageSize: 100 })
    stores.value = result.data?.list || []
  } catch {
    stores.value = []
  }
}

const openDetail = (id: string) => {
  currentOrderId.value = id
  drawerVisible.value = true
}

const shipmentDialogVisible = ref(false)
const currentShipmentOrderId = ref('')

const cancelOrder = async (row: any) => {
  try {
    await ElMessageBox.confirm('取消后订单库存将回补，确定取消该订单吗？', '取消订单', { type: 'warning' })
    await mallOrderApi.cancelOrder(row.id)
    ElMessage.success('取消订单成功')
    queryPageRef.value?.getTableList()
  } catch {}
}

const openShipmentDialog = (row: any) => {
  currentShipmentOrderId.value = row.id
  shipmentDialogVisible.value = true
}

const confirmReceipt = async (row: any) => {
  try {
    await ElMessageBox.confirm('确定该订单已签收吗？', '确认收货', { type: 'warning' })
    await mallOrderApi.confirmReceipt(row.id)
    ElMessage.success('确认收货成功')
    queryPageRef.value?.getTableList()
  } catch {}
}

const createRefund = async (row: any) => {
  try {
    const { value } = await ElMessageBox.prompt('请输入退款原因', '申请整单退款', {
      inputPlaceholder: '例如：客户取消购买'
    })
    await mallOrderApi.createRefund(row.id, { reason: value || '' })
    ElMessage.success('退款申请已提交')
    queryPageRef.value?.getTableList()
  } catch {}
}

const getStatusType = (status: string) => {
  const map: Record<string, any> = {
    PendingPayment: 'warning',
    Paid: 'primary',
    Shipped: 'info',
    Completed: 'success',
    Cancelled: 'info',
    Refunding: 'warning',
    Refunded: 'danger'
  }
  return map[status] || 'info'
}

const getStatusText = (status: string) => {
  const map: Record<string, string> = {
    PendingPayment: '待支付',
    Paid: '已支付',
    Shipped: '已发货',
    Completed: '已完成',
    Cancelled: '已取消',
    Refunding: '退款中',
    Refunded: '已退款'
  }
  return map[status] || status
}

onMounted(() => {
  loadStores()
})
</script>

<style scoped lang="scss"></style>
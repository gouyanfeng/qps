<template>
  <div class="list-page">
    <QueryPage api="/admin/mall/payments" :searchParam="searchForm" @reset="handleReset" ref="queryPageRef">
      <template #searchConditions>
        <el-form :model="searchForm" :inline="true">
          <el-form-item label="关键字">
            <el-input v-model="searchForm.keyword" placeholder="支付单号" clearable />
          </el-form-item>
          <el-form-item label="状态">
            <el-select v-model="searchForm.status" placeholder="全部" clearable>
              <el-option v-for="item in statusOptions" :key="item.value" :label="item.label" :value="item.value" />
            </el-select>
          </el-form-item>
          <el-form-item label="支付方式">
            <el-select v-model="searchForm.method" placeholder="全部" clearable>
              <el-option label="Mock" value="Mock" />
            </el-select>
          </el-form-item>
          <el-form-item label="创建时间">
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
        <el-table :data="tableData" :fit="true" style="width: 100%" border>
          <el-table-column prop="paymentNo" label="支付单号" min-width="180" show-overflow-tooltip />
          <el-table-column prop="orderNo" label="关联订单号" min-width="180" show-overflow-tooltip />
          <el-table-column label="金额" width="120" align="right">
            <template #default="{ row }">¥{{ Number(row.amount).toFixed(2) }}</template>
          </el-table-column>
          <el-table-column prop="method" label="支付方式" width="100" align="center" />
          <el-table-column label="状态" width="100" align="center">
            <template #default="{ row }">
              <el-tag :type="getStatusType(row.status)" size="small">{{ getStatusText(row.status) }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column prop="transactionNo" label="交易号" min-width="180" show-overflow-tooltip />
          <el-table-column prop="failureReason" label="失败原因" min-width="140" show-overflow-tooltip />
          <el-table-column label="支付时间" width="180" align="center">
            <template #default="{ row }">{{ formatDate(row.paidAt) }}</template>
          </el-table-column>
          <el-table-column label="创建时间" width="180" align="center">
            <template #default="{ row }">{{ formatDate(row.createdAt) }}</template>
          </el-table-column>
          <el-table-column label="操作" align="center" width="190" fixed="right" class-name="actions-column" header-class-name="actions-column">
            <template #default="{ row }">
              <template v-if="row.status === 'Pending'">
                <el-button type="success" link @click="simulatePayment(row, true)">支付成功</el-button>
                <el-button type="danger" link @click="simulatePayment(row, false)">支付失败</el-button>
              </template>
              <span v-else>-</span>
            </template>
          </el-table-column>
        </el-table>
      </template>
    </QueryPage>
  </div>
</template>

<script setup lang="ts" name="mallPayment">
import { ref, reactive } from 'vue'
import { ElMessage } from 'element-plus'
import { mallPaymentApi } from '@/api/modules/mallPayment'
import { mallOrderApi } from '@/api/modules/mallOrder'
import QueryPage from '@/components/QueryPage/index.vue'
import { formatDate } from '@/utils'

const queryPageRef = ref()
const dateRange = ref<string[]>([])

const statusOptions = [
  { label: '待支付', value: 'Pending' },
  { label: '成功', value: 'Success' },
  { label: '失败', value: 'Failed' },
  { label: '已关闭', value: 'Cancelled' }
]

const searchForm = reactive({
  keyword: '',
  status: '',
  method: '',
  startTime: '',
  endTime: ''
})

const handleReset = () => {
  Object.assign(searchForm, { keyword: '', status: '', method: '', startTime: '', endTime: '' })
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

const simulatePayment = async (row: any, success: boolean) => {
  try {
    await mallOrderApi.simulatePayment(row.id, { success, failureReason: success ? '' : '模拟支付失败' })
    ElMessage.success(success ? '支付成功' : '已模拟支付失败')
    queryPageRef.value?.getTableList()
  } catch {}
}

const getStatusType = (status: string) => {
  const map: Record<string, any> = { Pending: 'warning', Success: 'success', Failed: 'danger', Cancelled: 'info' }
  return map[status] || 'info'
}

const getStatusText = (status: string) => {
  const map: Record<string, string> = { Pending: '待支付', Success: '成功', Failed: '失败', Cancelled: '已关闭' }
  return map[status] || status
}
</script>

<style scoped lang="scss"></style>
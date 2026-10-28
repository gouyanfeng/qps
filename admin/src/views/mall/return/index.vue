<template>
  <div class="list-page">
    <QueryPage api="/admin/mall/returns" :searchParam="searchForm" @reset="handleReset" ref="queryPageRef">
      <template #searchConditions>
        <el-form :model="searchForm" :inline="true">
          <el-form-item label="门店">
            <el-select v-model="searchForm.storeId" placeholder="请选择门店" clearable>
              <el-option v-for="s in storeOptions" :key="s.id" :label="s.name" :value="s.id" />
            </el-select>
          </el-form-item>
          <el-form-item label="状态">
            <el-select v-model="searchForm.status" placeholder="全部" clearable>
              <el-option label="待审核" :value="0" />
              <el-option label="已通过" :value="1" />
              <el-option label="已拒绝" :value="2" />
              <el-option label="已收货" :value="3" />
              <el-option label="已退款" :value="4" />
              <el-option label="已取消" :value="5" />
            </el-select>
          </el-form-item>
          <el-form-item label="关键字">
            <el-input v-model="searchForm.keyword" placeholder="订单号/用户/商品" clearable />
          </el-form-item>
        </el-form>
      </template>

      <template #headerButtons>
        <span style="color: #999; font-size: 13px">退货申请统一由用户发起，管理端仅审核和处理</span>
      </template>

      <template #table="{ tableData }">
        <el-table :data="tableData" :fit="true" style="width: 100%" border>
          <el-table-column prop="returnNo" label="退货单号" width="160" show-overflow-tooltip />
          <el-table-column prop="orderNo" label="关联订单" width="160" show-overflow-tooltip />
          <el-table-column prop="userName" label="申请人" width="120" />
          <el-table-column prop="productName" label="商品" min-width="180" show-overflow-tooltip />
          <el-table-column label="退货数量" width="90" align="center">
            <template #default="{ row }">×{{ row.quantity }}</template>
          </el-table-column>
          <el-table-column label="申请金额" width="110" align="right">
            <template #default="{ row }">¥{{ Number(row.refundAmount).toFixed(2) }}</template>
          </el-table-column>
          <el-table-column label="原因" min-width="150" show-overflow-tooltip>
            <template #default="{ row }">{{ row.reason }}</template>
          </el-table-column>
          <el-table-column label="状态" width="100" align="center">
            <template #default="{ row }">
              <el-tag :type="statusTagMap[row.status].type">{{ statusTagMap[row.status].text }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column label="申请时间" width="170" align="center">
            <template #default="{ row }">{{ formatDate(row.createdAt) }}</template>
          </el-table-column>
          <el-table-column label="操作" align="center" width="280" fixed="right">
            <template #default="{ row }">
              <el-button type="primary" link @click="openDetail(row)">详情</el-button>
              <template v-if="row.status === 0">
                <el-button type="success" link @click="approve(row)">通过</el-button>
                <el-button type="danger" link @click="reject(row)">拒绝</el-button>
              </template>
              <template v-if="row.status === 1">
                <el-button type="warning" link @click="confirmReceive(row)">确认收货</el-button>
              </template>
              <template v-if="row.status === 3">
                <el-button type="success" link @click="doRefund(row)">退款</el-button>
              </template>
            </template>
          </el-table-column>
        </el-table>
      </template>
    </QueryPage>

    <el-drawer v-model="detailVisible" title="退货详情" size="560px">
      <template v-if="detail">
        <el-descriptions :column="1" border>
          <el-descriptions-item label="退货单号">{{ detail.returnNo }}</el-descriptions-item>
          <el-descriptions-item label="关联订单">{{ detail.orderNo }}</el-descriptions-item>
          <el-descriptions-item label="申请人">{{ detail.userName }} / {{ detail.userPhone }}</el-descriptions-item>
          <el-descriptions-item label="商品">{{ detail.productName }} ({{ detail.skuSpec }})</el-descriptions-item>
          <el-descriptions-item label="退货数量">×{{ detail.quantity }}</el-descriptions-item>
          <el-descriptions-item label="申请退款金额">¥{{ Number(detail.refundAmount).toFixed(2) }}</el-descriptions-item>
          <el-descriptions-item label="退货原因">{{ detail.reason }}</el-descriptions-item>
          <el-descriptions-item label="详细说明">{{ detail.description || '-' }}</el-descriptions-item>
          <el-descriptions-item label="状态">
            <el-tag :type="statusTagMap[detail.status].type">{{ statusTagMap[detail.status].text }}</el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="申请时间">{{ formatDate(detail.createdAt) }}</el-descriptions-item>
          <el-descriptions-item v-if="detail.rejectReason" label="拒绝原因">{{ detail.rejectReason }}</el-descriptions-item>
          <el-descriptions-item v-if="detail.trackingNo" label="退货物流">
            {{ detail.trackingCompany }} / {{ detail.trackingNo }}
          </el-descriptions-item>
        </el-descriptions>
      </template>
    </el-drawer>
  </div>
</template>

<script setup lang="ts" name="mallReturn">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { mallReturnApi } from '@/api/modules/mallReturn'
import { mallStoreApi } from '@/api/modules/mallStore'
import QueryPage from '@/components/QueryPage/index.vue'
import { formatDate } from '@/utils'

const statusTagMap: Record<number, { text: string; type: string }> = {
  0: { text: '待审核', type: 'warning' },
  1: { text: '已通过', type: 'primary' },
  2: { text: '已拒绝', type: 'info' },
  3: { text: '已收货', type: 'success' },
  4: { text: '已退款', type: 'success' },
  5: { text: '已取消', type: 'info' }
}

const queryPageRef = ref()
const storeOptions = ref<any[]>([])
const detailVisible = ref(false)
const detail = ref<any>(null)

const searchForm = reactive({ storeId: '', status: '' as number | '', keyword: '' })

onMounted(async () => {
  const res = await mallStoreApi.getStoreList({ pageNum: 1, pageSize: 200 })
  storeOptions.value = res?.data?.records || res?.data || []
  if (storeOptions.value.length) searchForm.storeId = storeOptions.value[0].id
})

const handleReset = () => { searchForm.status = ''; searchForm.keyword = '' }

const openDetail = async (row: any) => {
  const res = await mallReturnApi.get(row.id)
  detail.value = res?.data || res
  detailVisible.value = true
}

const approve = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定通过退货申请 "${row.returnNo}"？`, '审批通过', { type: 'warning' })
    await mallReturnApi.approve(row.id, {})
    ElMessage.success('已通过')
    queryPageRef.value?.getTableList()
  } catch {}
}

const reject = async (row: any) => {
  try {
    const { value } = await ElMessageBox.prompt('请输入拒绝原因', '拒绝退货', {
      confirmButtonText: '确定拒绝',
      cancelButtonText: '取消',
      inputPattern: /.+/,
      inputErrorMessage: '拒绝原因不能为空'
    })
    await mallReturnApi.reject(row.id, value)
    ElMessage.success('已拒绝')
    queryPageRef.value?.getTableList()
  } catch {}
}

const confirmReceive = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确认已收到退货商品 "${row.returnNo}"？`, '确认收货', { type: 'warning' })
    await mallReturnApi.confirmReceive(row.id)
    ElMessage.success('已确认收货')
    queryPageRef.value?.getTableList()
  } catch {}
}

const doRefund = async (row: any) => {
  try {
    await ElMessageBox.confirm(`对退货单 "${row.returnNo}" 执行退款？金额 ¥${Number(row.refundAmount).toFixed(2)}`, '执行退款', { type: 'warning' })
    await mallReturnApi.refund(row.id)
    ElMessage.success('退款成功')
    queryPageRef.value?.getTableList()
  } catch {}
}
</script>

<style scoped lang="scss"></style>
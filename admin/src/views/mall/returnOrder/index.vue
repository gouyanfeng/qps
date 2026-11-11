<template>
  <div class="list-page">
    <QueryPage api="/admin/mall/returns" :searchParam="searchForm" @reset="handleReset" ref="queryPageRef">
      <template #searchConditions>
        <el-form :model="searchForm" :inline="true">
          <el-form-item label="状态">
            <el-select v-model="searchForm.status" placeholder="全部" clearable>
              <el-option v-for="item in statusOptions" :key="item.value" :label="item.label" :value="item.value" />
            </el-select>
          </el-form-item>
          <el-form-item label="关键字">
            <el-input v-model="searchForm.keyword" placeholder="退货单号/订单号/收货人" clearable />
          </el-form-item>
        </el-form>
      </template>

      <template #headerButtons>
        <span style="color: #999; font-size: 13px">退货申请由用户发起，管理端审核后确认收货并自动退款</span>
      </template>

      <template #table="{ tableData }">
        <el-table :data="tableData" :fit="true" style="width: 100%" border>
          <el-table-column prop="returnNo" label="退货单号" width="160" show-overflow-tooltip />
          <el-table-column prop="orderNo" label="关联订单" width="160" show-overflow-tooltip />
          <el-table-column prop="userName" label="收货人" width="110" />
          <el-table-column prop="productName" label="商品" min-width="180" show-overflow-tooltip />
          <el-table-column label="退货数量" width="90" align="center">
            <template #default="{ row }">×{{ row.quantity }}</template>
          </el-table-column>
          <el-table-column label="退款金额" width="110" align="right">
            <template #default="{ row }">¥{{ Number(row.amount).toFixed(2) }}</template>
          </el-table-column>
          <el-table-column label="原因" min-width="140" show-overflow-tooltip>
            <template #default="{ row }">{{ row.reason }}</template>
          </el-table-column>
          <el-table-column label="状态" width="100" align="center">
            <template #default="{ row }">
              <el-tag :type="statusTagMap[row.status]?.type || 'info'">{{ statusTagMap[row.status]?.text || row.status }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column label="申请时间" width="170" align="center">
            <template #default="{ row }">{{ formatDate(row.createdAt) }}</template>
          </el-table-column>
          <el-table-column label="操作" align="center" width="240" fixed="right" class-name="actions-column" header-class-name="actions-column">
            <template #default="{ row }">
              <el-button type="primary" link @click="openDetail(row)">详情</el-button>
              <template v-if="row.status === 'Pending'">
                <Permission code="MALL_RETURN_ORDER_EDIT"><el-button type="success" link @click="approve(row)">通过</el-button></Permission>
                <Permission code="MALL_RETURN_ORDER_EDIT"><el-button type="danger" link @click="reject(row)">拒绝</el-button></Permission>
              </template>
              <template v-if="row.status === 'Approved'">
                <Permission code="MALL_RETURN_ORDER_EDIT"><el-button type="warning" link @click="confirmReceive(row)">确认收货并退款</el-button></Permission>
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
          <el-descriptions-item label="收货人">{{ detail.userName }} / {{ detail.userPhone }}</el-descriptions-item>
          <el-descriptions-item label="商品">{{ detail.productName }} <template v-if="detail.skuSpec">（{{ detail.skuSpec }}）</template></el-descriptions-item>
          <el-descriptions-item label="退货数量">×{{ detail.quantity }}</el-descriptions-item>
          <el-descriptions-item label="退款金额">¥{{ Number(detail.amount).toFixed(2) }}</el-descriptions-item>
          <el-descriptions-item label="退货原因">{{ detail.reason }}</el-descriptions-item>
          <el-descriptions-item label="补充说明">{{ detail.description || '-' }}</el-descriptions-item>
          <el-descriptions-item label="凭证图片">
            <div class="evidence-list" v-if="detail.evidenceImages?.length">
              <el-image
                v-for="(img, index) in detail.evidenceImages"
                :key="img + index"
                :src="resolveFileUrl(img)"
                :preview-src-list="resolveEvidence(detail.evidenceImages)"
                :initial-index="index"
                style="width: 72px; height: 72px; margin-right: 8px; border-radius: 4px"
                fit="cover"
                preview-teleported
              />
            </div>
            <span v-else>-</span>
          </el-descriptions-item>
          <el-descriptions-item label="状态">
            <el-tag :type="statusTagMap[detail.status]?.type || 'info'">{{ statusTagMap[detail.status]?.text || detail.status }}</el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="申请时间">{{ formatDate(detail.createdAt) }}</el-descriptions-item>
          <el-descriptions-item v-if="detail.approvedAt" label="审核时间">{{ formatDate(detail.approvedAt) }}</el-descriptions-item>
          <el-descriptions-item v-if="detail.rejectReason" label="拒绝原因">{{ detail.rejectReason }}</el-descriptions-item>
          <el-descriptions-item v-if="detail.trackingNo" label="退货物流">
            {{ detail.expressCompany }} / {{ detail.trackingNo }}
          </el-descriptions-item>
          <el-descriptions-item v-if="detail.receivedAt" label="收货时间">{{ formatDate(detail.receivedAt) }}</el-descriptions-item>
          <el-descriptions-item v-if="detail.completedAt" label="完成时间">{{ formatDate(detail.completedAt) }}</el-descriptions-item>
        </el-descriptions>
      </template>
    </el-drawer>
  </div>
</template>

<script setup lang="ts" name="mallReturnOrder">
import { ref, reactive } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { mallReturnApi } from '@/api/modules/mallReturn'
import QueryPage from '@/components/QueryPage/index.vue'
import { formatDate, resolveFileUrl } from '@/utils'
import Permission from "@/components/Permission/index.vue"

const statusTagMap: Record<string, { text: string; type: string }> = {
  Pending: { text: '待审核', type: 'warning' },
  Approved: { text: '待收货', type: 'primary' },
  Rejected: { text: '已拒绝', type: 'info' },
  Received: { text: '已收货', type: 'success' },
  Completed: { text: '已完成', type: 'success' }
}

const statusOptions = [
  { label: '待审核', value: 'Pending' },
  { label: '待收货', value: 'Approved' },
  { label: '已拒绝', value: 'Rejected' },
  { label: '已收货', value: 'Received' },
  { label: '已完成', value: 'Completed' }
]

const queryPageRef = ref()
const detailVisible = ref(false)
const detail = ref<any>(null)

const searchForm = reactive({ status: '', keyword: '' })

const handleReset = () => {
  searchForm.status = ''
  searchForm.keyword = ''
}

const openDetail = async (row: any) => {
  const res = await mallReturnApi.get(row.id)
  detail.value = res?.data || res
  detailVisible.value = true
}

const resolveEvidence = (images: string[] = []) => images.map((item) => resolveFileUrl(item))

const approve = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定通过退货申请 "${row.returnNo}"？`, '审批通过', { type: 'warning' })
    await mallReturnApi.approve(row.returnNo)
    ElMessage.success('已通过，等待用户寄回商品')
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
    await mallReturnApi.reject(row.returnNo, value)
    ElMessage.success('已拒绝，订单已恢复原状态')
    queryPageRef.value?.getTableList()
  } catch {}
}

const confirmReceive = async (row: any) => {
  try {
    await ElMessageBox.confirm(
      `确认已收到退货商品 "${row.returnNo}"？确认后将自动退款 ¥${Number(row.amount).toFixed(2)} 并回补库存。`,
      '确认收货',
      { type: 'warning' }
    )
    await mallReturnApi.confirmReceive(row.returnNo)
    ElMessage.success('已确认收货，退款完成')
    queryPageRef.value?.getTableList()
  } catch {}
}
</script>

<style scoped lang="scss"></style>

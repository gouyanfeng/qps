<template>
  <el-drawer
    :model-value="modelValue"
    size="70%"
    :with-header="false"
    class="order-detail-drawer"
    @update:model-value="handleVisibleChange"
  >
    <div v-if="detail" class="drawer-layout">
      <section class="drawer-head">
        <div>
          <div class="detail-kicker">商场订单详情</div>
          <div class="title-row">
            <h2>{{ detail.orderNo }}</h2>
            <el-tag :type="getOrderStatusType(detail.status)" effect="dark">{{ getOrderStatusText(detail.status) }}</el-tag>
          </div>
          <div class="head-meta">
            <span>{{ detail.storeName }}</span>
            <span>应付 ¥{{ Number(detail.payableAmount).toFixed(2) }}</span>
            <span>已付 ¥{{ Number(detail.paidAmount).toFixed(2) }}</span>
            <span>已退 ¥{{ Number(detail.refundedAmount).toFixed(2) }}</span>
          </div>
        </div>
        <div class="head-actions">
          <el-button v-if="detail.status === 'PendingPayment'" type="danger" plain :icon="CircleClose" @click="cancelOrder">
            取消订单
          </el-button>
          <el-button v-if="detail.status === 'Paid'" type="primary" :icon="Van" @click="openShipmentDialog">发货</el-button>
          <el-button v-if="detail.status === 'Shipped'" type="success" :icon="CircleCheck" @click="confirmReceipt">
            确认收货
          </el-button>
          <el-button v-if="detail.status === 'Paid'" type="warning" plain :icon="RefreshLeft" @click="createRefund">
            申请退款
          </el-button>
          <el-button :icon="Refresh" @click="loadDetail">刷新</el-button>
          <el-button class="drawer-close-button" :icon="Close" circle title="关闭详情" @click="emit('update:modelValue', false)" />
        </div>
      </section>

      <el-descriptions :column="3" border class="detail-descriptions">
        <el-descriptions-item label="门店">{{ detail.storeName }}</el-descriptions-item>
        <el-descriptions-item label="收货人">{{ detail.receiverName }}</el-descriptions-item>
        <el-descriptions-item label="联系电话">{{ detail.receiverPhone }}</el-descriptions-item>
        <el-descriptions-item label="收货地址" :span="2">{{ detail.receiverAddress }}</el-descriptions-item>
        <el-descriptions-item label="订单备注">{{ detail.remark || '-' }}</el-descriptions-item>
        <el-descriptions-item label="商品金额">¥{{ Number(detail.goodsAmount).toFixed(2) }}</el-descriptions-item>
        <el-descriptions-item label="运费">¥{{ Number(detail.freightAmount).toFixed(2) }}</el-descriptions-item>
        <el-descriptions-item label="订单总重">{{ detail.totalWeightGram }} 克</el-descriptions-item>
        <el-descriptions-item label="下单时间">{{ formatDate(detail.createdAt) }}</el-descriptions-item>
        <el-descriptions-item label="支付时间">{{ formatDate(detail.paidAt) }}</el-descriptions-item>
        <el-descriptions-item label="发货时间">{{ formatDate(detail.shippedAt) }}</el-descriptions-item>
        <el-descriptions-item label="完成时间">{{ formatDate(detail.completedAt) }}</el-descriptions-item>
        <el-descriptions-item label="取消时间">{{ formatDate(detail.cancelledAt) }}</el-descriptions-item>
        <el-descriptions-item label="退款时间">{{ formatDate(detail.refundedAt) }}</el-descriptions-item>
      </el-descriptions>

      <section class="detail-card">
        <div class="section-title"><h3>商品明细</h3></div>
        <el-table :data="detail.items || []" border style="width: 100%">
          <el-table-column prop="productName" label="商品" min-width="160" show-overflow-tooltip />
          <el-table-column label="规格" min-width="160" show-overflow-tooltip>
            <template #default="{ row }">
              <span v-if="formatCombination(row.combination)">{{ formatCombination(row.combination) }}</span>
              <span v-else>{{ row.specName || '-' }}</span>
            </template>
          </el-table-column>
          <el-table-column prop="skuCode" label="规格编码" min-width="140" show-overflow-tooltip />
          <el-table-column label="单价" width="100" align="right">
            <template #default="{ row }">¥{{ Number(row.unitPrice).toFixed(2) }}</template>
          </el-table-column>
          <el-table-column prop="quantity" label="数量" width="80" align="center" />
          <el-table-column prop="weightGram" label="单件重量(克)" width="130" align="center" />
          <el-table-column label="小计" width="110" align="right">
            <template #default="{ row }">¥{{ Number(row.itemAmount).toFixed(2) }}</template>
          </el-table-column>
        </el-table>
      </section>

      <section class="detail-card">
        <div class="section-title"><h3>支付单</h3></div>
        <el-table :data="detail.payments || []" border style="width: 100%">
          <el-table-column prop="paymentNo" label="支付单号" min-width="180" show-overflow-tooltip />
          <el-table-column label="金额" width="110" align="right">
            <template #default="{ row }">¥{{ Number(row.amount).toFixed(2) }}</template>
          </el-table-column>
          <el-table-column prop="method" label="方式" width="90" align="center" />
          <el-table-column label="状态" width="100" align="center">
            <template #default="{ row }">
              <el-tag :type="getPaymentStatusType(row.status)" size="small">{{ getPaymentStatusText(row.status) }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column prop="transactionNo" label="交易号" min-width="180" show-overflow-tooltip />
          <el-table-column prop="failureReason" label="失败原因" min-width="140" show-overflow-tooltip />
          <el-table-column label="创建时间" width="180" align="center">
            <template #default="{ row }">{{ formatDate(row.createdAt) }}</template>
          </el-table-column>
          <el-table-column label="操作" width="150" align="center" fixed="right">
            <template #default="{ row }">
              <template v-if="row.status === 'Pending'">
                <el-button type="success" link @click="simulatePayment(row, true)">支付成功</el-button>
                <el-button type="danger" link @click="simulatePayment(row, false)">支付失败</el-button>
              </template>
              <span v-else>-</span>
            </template>
          </el-table-column>
        </el-table>
      </section>

      <section class="detail-card">
        <div class="section-title"><h3>退款单</h3></div>
        <el-table :data="detail.refunds || []" border style="width: 100%">
          <el-table-column prop="refundNo" label="退款单号" min-width="180" show-overflow-tooltip />
          <el-table-column label="金额" width="110" align="right">
            <template #default="{ row }">¥{{ Number(row.amount).toFixed(2) }}</template>
          </el-table-column>
          <el-table-column prop="reason" label="退款原因" min-width="160" show-overflow-tooltip />
          <el-table-column label="状态" width="100" align="center">
            <template #default="{ row }">
              <el-tag :type="getRefundStatusType(row.status)" size="small">{{ getRefundStatusText(row.status) }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column label="退款时间" width="180" align="center">
            <template #default="{ row }">{{ formatDate(row.refundedAt) }}</template>
          </el-table-column>
          <el-table-column label="操作" width="150" align="center" fixed="right">
            <template #default="{ row }">
              <template v-if="row.status === 'Pending'">
                <el-button type="success" link @click="simulateRefund(row, true)">退款成功</el-button>
                <el-button type="danger" link @click="simulateRefund(row, false)">退款失败</el-button>
              </template>
              <span v-else>-</span>
            </template>
          </el-table-column>
        </el-table>
      </section>

      <section class="detail-card">
        <div class="section-title"><h3>物流信息</h3></div>
        <el-descriptions v-if="detail.shipment" :column="3" border>
          <el-descriptions-item label="运单号">{{ detail.shipment.shipmentNo }}</el-descriptions-item>
          <el-descriptions-item label="快递公司">{{ detail.shipment.expressCompany }}</el-descriptions-item>
          <el-descriptions-item label="快递单号">{{ detail.shipment.trackingNo }}</el-descriptions-item>
          <el-descriptions-item label="运单状态">
            {{ detail.shipment.status === 'Delivered' ? '已签收' : '已发货' }}
          </el-descriptions-item>
          <el-descriptions-item label="发货时间">{{ formatDate(detail.shipment.shippedAt) }}</el-descriptions-item>
          <el-descriptions-item label="签收时间">{{ formatDate(detail.shipment.deliveredAt) }}</el-descriptions-item>
          <el-descriptions-item label="备注" :span="3">{{ detail.shipment.remark || '-' }}</el-descriptions-item>
        </el-descriptions>
        <el-empty v-else description="暂无运单" :image-size="60" />
      </section>
    </div>

    <!-- 发货 -->
    <el-dialog v-model="shipmentDialogVisible" title="录入运单并发货" width="480px" append-to-body>
      <el-form :model="shipmentForm" label-width="90px">
        <el-form-item label="快递公司" required>
          <el-input v-model="shipmentForm.expressCompany" placeholder="请输入快递公司" maxlength="50" />
        </el-form-item>
        <el-form-item label="快递单号" required>
          <el-input v-model="shipmentForm.trackingNo" placeholder="请输入快递单号" maxlength="64" />
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="shipmentForm.remark" placeholder="请输入备注" maxlength="200" />
        </el-form-item>
      </el-form>
      <template #footer>
        <span class="dialog-footer">
          <el-button @click="shipmentDialogVisible = false">取消</el-button>
          <el-button type="primary" :loading="shipmentSubmitting" @click="submitShipment">确定发货</el-button>
        </span>
      </template>
    </el-dialog>
  </el-drawer>
</template>

<script setup lang="ts" name="MallOrderDetailDrawer">
import { ref, reactive, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Close, Refresh, Van, CircleCheck, CircleClose, RefreshLeft } from '@element-plus/icons-vue'
import { mallOrderApi } from '@/api/modules/mallOrder'
import { formatDate } from '@/utils'

const props = defineProps({
  modelValue: {
    type: Boolean,
    default: false
  },
  orderId: {
    type: String,
    default: ''
  }
})

const emit = defineEmits(['update:modelValue', 'refreshed'])

const detail = ref<any>(null)
const shipmentDialogVisible = ref(false)
const shipmentSubmitting = ref(false)
const shipmentForm = reactive({
  expressCompany: '',
  trackingNo: '',
  remark: ''
})

const handleVisibleChange = (visible: boolean) => {
  emit('update:modelValue', visible)
}

const loadDetail = async () => {
  if (!props.orderId) return
  try {
    const result = await mallOrderApi.getOrder(props.orderId)
    detail.value = result.data
  } catch {
    detail.value = null
  }
}

watch(
  () => [props.modelValue, props.orderId],
  ([visible]) => {
    if (visible) {
      loadDetail()
    }
  },
  { immediate: true }
)

const refreshAll = async () => {
  await loadDetail()
  emit('refreshed')
}

const cancelOrder = async () => {
  try {
    await ElMessageBox.confirm('取消后订单库存将回补，确定取消该订单吗？', '取消订单', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })
    await mallOrderApi.cancelOrder(detail.value.id)
    ElMessage.success('取消订单成功')
    await refreshAll()
  } catch {
    // 取消操作或请求失败
  }
}

const openShipmentDialog = () => {
  Object.assign(shipmentForm, {
    expressCompany: '',
    trackingNo: '',
    remark: ''
  })
  shipmentDialogVisible.value = true
}

const submitShipment = async () => {
  if (!shipmentForm.expressCompany.trim()) {
    ElMessage.warning('请输入快递公司')
    return
  }
  if (!shipmentForm.trackingNo.trim()) {
    ElMessage.warning('请输入快递单号')
    return
  }

  shipmentSubmitting.value = true
  try {
    await mallOrderApi.createShipment(detail.value.id, {
      expressCompany: shipmentForm.expressCompany.trim(),
      trackingNo: shipmentForm.trackingNo.trim(),
      remark: shipmentForm.remark.trim()
    })
    ElMessage.success('发货成功')
    shipmentDialogVisible.value = false
    await refreshAll()
  } catch {
    // 错误提示已由请求拦截器统一处理
  } finally {
    shipmentSubmitting.value = false
  }
}

const confirmReceipt = async () => {
  try {
    await ElMessageBox.confirm('确定该订单已签收吗？', '确认收货', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })
    await mallOrderApi.confirmReceipt(detail.value.id)
    ElMessage.success('确认收货成功')
    await refreshAll()
  } catch {
    // 取消操作或请求失败
  }
}

const createRefund = async () => {
  try {
    const { value } = await ElMessageBox.prompt('请输入退款原因', '申请整单退款', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      inputPlaceholder: '例如：客户取消购买'
    })
    await mallOrderApi.createRefund(detail.value.id, { reason: value || '' })
    ElMessage.success('退款申请已提交，请继续模拟退款结果')
    await refreshAll()
  } catch {
    // 取消操作或请求失败
  }
}

const simulatePayment = async (row: any, success: boolean) => {
  try {
    await mallOrderApi.simulatePayment(row.id, {
      success,
      failureReason: success ? '' : '模拟支付失败'
    })
    ElMessage.success(success ? '支付成功' : '已模拟支付失败')
    await refreshAll()
  } catch {
    // 请求失败提示已由拦截器处理
  }
}

const simulateRefund = async (row: any, success: boolean) => {
  try {
    await mallOrderApi.simulateRefund(row.id, { success })
    ElMessage.success(success ? '退款成功，库存已回补' : '已模拟退款失败')
    await refreshAll()
  } catch {
    // 请求失败提示已由拦截器处理
  }
}

const getOrderStatusType = (status: string) => {
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

const getOrderStatusText = (status: string) => {
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

const getPaymentStatusType = (status: string) => {
  const map: Record<string, any> = {
    Pending: 'warning',
    Success: 'success',
    Failed: 'danger',
    Cancelled: 'info'
  }
  return map[status] || 'info'
}

const getPaymentStatusText = (status: string) => {
  const map: Record<string, string> = {
    Pending: '待支付',
    Success: '成功',
    Failed: '失败',
    Cancelled: '已关闭'
  }
  return map[status] || status
}

const getRefundStatusType = (status: string) => {
  const map: Record<string, any> = {
    Pending: 'warning',
    Success: 'success',
    Failed: 'danger'
  }
  return map[status] || 'info'
}

const getRefundStatusText = (status: string) => {
  const map: Record<string, string> = {
    Pending: '处理中',
    Success: '成功',
    Failed: '失败'
  }
  return map[status] || status
}

const formatCombination = (combo: Record<string, string>) => {
  if (!combo) return ''
  return Object.entries(combo).map(([k, v]) => `${k}:${v}`).join(' / ')
}
</script>

<style scoped lang="scss">
.drawer-layout {
  padding: 4px 8px 24px;
}

.drawer-head {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 16px;
  margin-bottom: 16px;
}

.detail-kicker {
  color: var(--el-text-color-secondary);
  font-size: 13px;
}

.title-row {
  display: flex;
  align-items: center;
  gap: 12px;
  margin: 6px 0;

  h2 {
    margin: 0;
    font-size: 20px;
  }
}

.head-meta {
  display: flex;
  flex-wrap: wrap;
  gap: 16px;
  color: var(--el-text-color-secondary);
  font-size: 13px;
}

.head-actions {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.detail-descriptions {
  margin-bottom: 16px;
}

.detail-card {
  margin-bottom: 16px;

  .section-title {
    margin-bottom: 10px;

    h3 {
      margin: 0;
      font-size: 15px;
    }
  }
}
</style>
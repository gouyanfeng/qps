<template>
  <el-dialog
    :model-value="modelValue"
    :title="dialogTitle"
    width="480px"
    append-to-body
    @update:model-value="handleVisibleChange"
  >
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
        <el-button @click="handleVisibleChange(false)">取消</el-button>
        <el-button type="primary" :loading="submitting" @click="submit">确定发货</el-button>
      </span>
    </template>
  </el-dialog>
</template>

<script setup lang="ts" name="ShipmentDialog">
import { reactive, ref, watch } from 'vue'
import { ElMessage } from 'element-plus'
import { mallOrderApi } from '@/api/modules/mall/mallOrder'

const props = defineProps({
  modelValue: {
    type: Boolean,
    default: false
  },
  orderId: {
    type: String,
    default: ''
  },
  orderNo: {
    type: String,
    default: ''
  }
})

const emit = defineEmits(['update:modelValue', 'shipped'])

const dialogTitle = ref('录入运单并发货')
const submitting = ref(false)
const shipmentForm = reactive({
  expressCompany: '',
  trackingNo: '',
  remark: ''
})

const handleVisibleChange = (visible: boolean) => {
  emit('update:modelValue', visible)
}

watch(
  () => props.modelValue,
  (visible) => {
    if (visible) {
      Object.assign(shipmentForm, { expressCompany: '', trackingNo: '', remark: '' })
      dialogTitle.value = props.orderNo ? `录入运单并发货 · ${props.orderNo}` : '录入运单并发货'
    }
  }
)

const submit = async () => {
  if (!props.orderId) {
    ElMessage.warning('订单信息缺失，请刷新后重试')
    return
  }
  if (!shipmentForm.expressCompany.trim()) {
    ElMessage.warning('请输入快递公司')
    return
  }
  if (!shipmentForm.trackingNo.trim()) {
    ElMessage.warning('请输入快递单号')
    return
  }

  submitting.value = true
  try {
    await mallOrderApi.createShipment(props.orderId, {
      expressCompany: shipmentForm.expressCompany.trim(),
      trackingNo: shipmentForm.trackingNo.trim(),
      remark: shipmentForm.remark.trim()
    })
    ElMessage.success('发货成功')
    emit('update:modelValue', false)
    emit('shipped')
  } catch {
    // 错误提示已由请求拦截器统一处理
  } finally {
    submitting.value = false
  }
}
</script>

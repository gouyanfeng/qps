<template>
  <q-page>
    <q-page-header-back :title="pageTitle" />

    <view v-if="loading" class="page-body loading">加载中…</view>

    <view v-if="!loading">
      <!-- 申请售后：订单概要 -->
      <view v-if="mode !== 'tracking'" class="summary-card">
        <view class="summary-row">
          <text class="summary-label">订单编号</text>
          <text class="summary-value mono">{{ orderNo }}</text>
        </view>
        <view class="summary-row">
          <text class="summary-label">售后类型</text>
          <text class="summary-value">{{ mode === 'refund' ? '仅退款（整单）' : '退货退款（整单）' }}</text>
        </view>
        <view class="summary-row">
          <text class="summary-label">退款金额</text>
          <text class="summary-value price">¥{{ refundAmount.toFixed(2) }}</text>
        </view>
        <text class="summary-tip">仅支持整单退款，退款金额按订单实付金额（含运费）计算</text>
      </view>

      <!-- 填写寄回运单 -->
      <template v-if="mode === 'tracking'">
        <view class="form-card">
          <text class="form-label">退货单号</text>
          <text class="return-no mono">{{ returnNo }}</text>
        </view>

        <view class="form-card">
          <view class="label-row">
            <text class="form-label">快递公司</text>
            <text class="form-hint">必填</text>
          </view>
          <input
            v-model="expressCompany"
            class="text-input"
            placeholder="如：顺丰速运"
            placeholder-class="input-placeholder"
            :maxlength="50"
          />
        </view>

        <view class="form-card">
          <view class="label-row">
            <text class="form-label">快递单号</text>
            <text class="form-hint">必填</text>
          </view>
          <input
            v-model="trackingNo"
            class="text-input"
            placeholder="请输入寄回快递单号"
            placeholder-class="input-placeholder"
            :maxlength="64"
          />
        </view>

        <text class="tracking-tip">请如实填写寄回运单，便于商家核对收货。</text>
      </template>

      <!-- 申请原因 -->
      <template v-else>
        <view class="form-card">
          <view class="label-row">
            <text class="form-label">申请原因</text>
            <text class="form-hint">必选</text>
          </view>
          <view class="reason-list">
            <view
              v-for="reason in reasonOptions"
              :key="reason"
              class="reason-item"
              :class="{ active: selectedReason === reason }"
              @click="selectedReason = reason"
            >
              <view class="reason-dot" />
              <text>{{ reason }}</text>
            </view>
          </view>
        </view>

        <view v-if="selectedReason === '其他'" class="form-card">
          <view class="label-row">
            <text class="form-label">补充说明</text>
            <text class="form-hint">{{ description.length }}/500</text>
          </view>
          <textarea
            v-model="description"
            class="description-input"
            placeholder="请补充具体原因，便于商家快速处理"
            placeholder-class="input-placeholder"
            :maxlength="500"
          />
        </view>

        <view class="form-card">
          <view class="label-row">
            <text class="form-label">凭证图片</text>
            <text class="form-hint">最多 6 张（选填）</text>
          </view>
          <view class="image-grid">
            <view v-for="(img, idx) in images" :key="img" class="image-cell">
              <q-image :src="img" thumb :width="120" mode="aspectFill" @click="previewImage(idx)" />
              <view class="image-delete" @click.stop="removeImage(idx)">
                <q-icon name="close" size="12" color="#ffffff" />
              </view>
            </view>
            <view v-if="images.length < MAX_IMAGES" class="image-add" @click="chooseImages">
              <q-icon name="add" size="28" color="#8A8578" />
              <text>添加</text>
            </view>
          </view>
        </view>
      </template>

      <view class="bottom-space"></view>
    </view>

    <template #footer>
    <view v-if="!loading" class="page-footer">
      <view class="submit-btn" :class="{ disabled: uploading || submitting }" @click="submit">
        {{ submitting ? '提交中…' : mode === 'tracking' ? '提交运单' : '提交申请' }}
      </view>
    </view>
    </template>
  </q-page>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { onLoad } from '@dcloudio/uni-app'
import {
  createRefund,
  createReturn,
  getOrderDetail,
  submitReturnTracking,
  uploadAfterSaleImage
} from '@/api'
import { resolveFileUrl } from '@/utils/http'

const MAX_IMAGES = 6

const mode = ref<'refund' | 'return' | 'tracking'>('refund')
const orderNo = ref('')
const returnNo = ref('')
const paidAmount = ref(0)
const loading = ref(true)
const submitting = ref(false)
const uploading = ref(false)

const selectedReason = ref('')
const description = ref('')
const images = ref<string[]>([])
const expressCompany = ref('')
const trackingNo = ref('')

const reasonOptions = ['不想要了', '拍错/多拍', '商品破损/变质', '少件/漏发', '质量问题', '其他']

const pageTitle = computed(() => {
  if (mode.value === 'tracking') return '填写寄回运单'
  return mode.value === 'refund' ? '申请仅退款' : '申请退货退款'
})

const refundAmount = computed(() => paidAmount.value)

onLoad(async (options) => {
  orderNo.value = (options?.orderNo as string) || ''
  returnNo.value = (options?.returnNo as string) || ''
  const type = (options?.type as string) || ''

  if (type === 'tracking') {
    mode.value = 'tracking'
  } else if (type === 'refund' || type === 'return') {
    mode.value = type
  } else {
    mode.value = 'refund'
  }

  if (mode.value === 'tracking') {
    if (!returnNo.value) {
      uni.showToast({ title: '缺少退货单号', icon: 'none' })
    }
    loading.value = false
    return
  }

  if (!orderNo.value) {
    uni.showToast({ title: '缺少订单号', icon: 'none' })
    loading.value = false
    return
  }

  try {
    const detail = await getOrderDetail(orderNo.value)
    paidAmount.value = detail.paidAmount > 0 ? detail.paidAmount : detail.payableAmount
  } catch (error) {
    console.error('加载订单信息失败', error)
    uni.showToast({ title: '订单信息加载失败', icon: 'none' })
  } finally {
    loading.value = false
  }
})

function chooseImages() {
  if (uploading.value || images.value.length >= MAX_IMAGES) return
  const remain = MAX_IMAGES - images.value.length
  uni.chooseImage({
    count: remain,
    sizeType: ['compressed'],
    sourceType: ['album', 'camera'],
    success: async (res) => {
      const paths = res.tempFilePaths as string[]
      if (!paths.length) return
      uploading.value = true
      uni.showLoading({ title: '上传中…' })
      let message = ''
      try {
        for (const path of paths) {
          if (images.value.length >= MAX_IMAGES) break
          const fileUrl = await uploadAfterSaleImage(path)
          images.value.push(fileUrl)
        }
      } catch (error) {
        console.error('上传凭证图片失败', error)
        message = (error as Error).message || '图片上传失败'
      }
      uni.hideLoading()
      uploading.value = false
      if (message) uni.showToast({ title: message, icon: 'none' })
    }
  })
}

function removeImage(index: number) {
  images.value.splice(index, 1)
}

function previewImage(index: number) {
  if (!images.value.length) return
  uni.previewImage({
    urls: images.value.map(resolveFileUrl),
    current: resolveFileUrl(images.value[index])
  })
}

async function submit() {
  if (submitting.value || uploading.value) return

  if (mode.value === 'tracking') {
    const company = expressCompany.value.trim()
    const no = trackingNo.value.trim()
    if (!company || !no) {
      uni.showToast({ title: '请填写快递公司与快递单号', icon: 'none' })
      return
    }

    submitting.value = true
    try {
      await submitReturnTracking(returnNo.value, { expressCompany: company, trackingNo: no })
      uni.showToast({ title: '运单已提交', icon: 'success' })
      setTimeout(() => uni.navigateBack(), 400)
    } catch (error) {
      console.error('提交运单失败', error)
      uni.showToast({ title: (error as Error).message || '提交失败', icon: 'none' })
    } finally {
      submitting.value = false
    }
    return
  }

  if (!selectedReason.value) {
    uni.showToast({ title: '请选择申请原因', icon: 'none' })
    return
  }

  if (images.value.length > MAX_IMAGES) {
    uni.showToast({ title: '凭证图片最多 6 张', icon: 'none' })
    return
  }

  const payload = {
    reason: selectedReason.value,
    description: description.value.trim() || undefined,
    evidenceImages: [...images.value]
  }

  submitting.value = true
  try {
    if (mode.value === 'refund') {
      await createRefund(orderNo.value, payload)
      uni.showToast({ title: '退款申请已提交', icon: 'success' })
    } else {
      await createReturn(orderNo.value, payload)
      uni.showToast({ title: '退货申请已提交，等待审核', icon: 'success' })
    }
    setTimeout(() => uni.navigateBack(), 400)
  } catch (error) {
    console.error('提交售后申请失败', error)
    uni.showToast({ title: (error as Error).message || '提交失败', icon: 'none' })
  } finally {
    submitting.value = false
  }
}
</script>

<style lang="scss" scoped>
@import "./index.scss";
</style>

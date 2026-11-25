<template>
  <q-page>
    <q-page-header-back title="发表评价" />

    <view v-if="loading" class="page-body loading">加载中…</view>

    <view v-if="!loading" class="page-body">
      <!-- 商品信息 -->
      <view v-if="item" class="product-card">
        <view class="product-image">
          <q-image
            v-if="item.productImage"
            :src="item.productImage"
            thumb
            :width="120"
            mode="aspectFill"
          />
          <q-icon v-else name="tea" size="28" color="#BDBDBD" />
        </view>
        <view class="product-info">
          <text class="product-name">{{ item.productName }}</text>
          <text class="product-spec">{{ item.specName }}</text>
        </view>
      </view>

      <!-- 星级评分 -->
      <view class="form-card">
        <text class="form-label">商品评分</text>
        <view class="rating-row">
          <view
            v-for="n in 5"
            :key="n"
            class="star-btn"
            @click="rating = n"
          >
            <q-icon
              :name="n <= rating ? 'star-filled' : 'star'"
              size="34"
              :color="n <= rating ? '#FF9800' : '#D8D3CB'"
            />
          </view>
          <text class="rating-text">{{ ratingText }}</text>
        </view>
      </view>

      <!-- 标签 -->
      <view class="form-card">
        <view class="label-row">
          <text class="form-label">商品标签</text>
          <text class="form-hint">最多选 3 个</text>
        </view>
        <view class="tag-grid">
          <view
            v-for="tag in presetTags"
            :key="tag"
            class="tag-pill"
            :class="{ active: selectedTags.includes(tag) }"
            @click="toggleTag(tag)"
          >
            <text>{{ tag }}</text>
          </view>
        </view>
        <text v-if="!presetTags.length" class="tag-empty">暂无可选标签</text>
      </view>

      <!-- 评价内容 -->
      <view class="form-card">
        <view class="label-row">
          <text class="form-label">评价内容</text>
          <text class="form-hint">{{ content.length }}/500</text>
        </view>
        <textarea
          v-model="content"
          class="content-input"
          placeholder="分享您对商品的使用体验（5-500 字）"
          placeholder-class="content-placeholder"
          :maxlength="500"
        />
      </view>

      <!-- 图片 -->
      <view class="form-card">
        <view class="label-row">
          <text class="form-label">上传图片</text>
          <text class="form-hint">最多 6 张</text>
        </view>
        <view class="image-grid">
          <view v-for="(img, idx) in images" :key="img" class="image-cell">
            <q-image :src="img" thumb :width="120" mode="aspectFill" @click="previewImage(idx)" />
            <view class="image-delete" @click.stop="removeImage(idx)">
              <q-icon name="close" size="12" color="#ffffff" />
            </view>
          </view>
          <view v-if="images.length < 6" class="image-add" @click="chooseImages">
            <q-icon name="add" size="28" color="#8A8578" />
            <text>添加</text>
          </view>
        </view>
      </view>

      <view class="bottom-space"></view>
    </view>

    <template #footer>
    <view v-if="!loading" class="page-footer">
      <view
        class="submit-btn"
        :class="{ loading: submitting }"
        @click="submit"
      >{{ submitting ? '提交中…' : '提交评价' }}</view>
    </view>
    </template>
  </q-page>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { onLoad } from '@dcloudio/uni-app'
import { createProductReview, getOrderDetail, getReviewTags, uploadReviewImage } from '@/api'
import type { MallOrderItem } from '@/api'
import type { MallTagItem } from '@/api/types'
import { resolveFileUrl } from '@/utils/http'

const MAX_TAGS = 3
const MAX_IMAGES = 6

const orderNo = ref('')
const itemId = ref('')
const item = ref<MallOrderItem | null>(null)
const productId = ref('')
const rating = ref(5)
const content = ref('')
const selectedTags = ref<string[]>([])
const images = ref<string[]>([])
const loading = ref(true)
const uploading = ref(false)
const submitting = ref(false)

const tagOptions = ref<MallTagItem[]>([])
const presetTags = computed(() => tagOptions.value.map(tag => tag.name))

const ratingText = computed(() => ['', '很差', '一般', '满意', '很满意', '非常满意'][rating.value] || '')

onLoad(async (options) => {
  orderNo.value = (options?.orderNo as string) || ''
  itemId.value = (options?.itemId as string) || ''

  getReviewTags()
    .then(list => {
      tagOptions.value = list
    })
    .catch(() => {
      tagOptions.value = []
    })

  if (!orderNo.value || !itemId.value) {
    uni.showToast({ title: '缺少订单信息', icon: 'none' })
    loading.value = false
    return
  }

  try {
    const detail = await getOrderDetail(orderNo.value)
    const target = detail.items.find(orderItem => orderItem.id === itemId.value)
    if (!target) {
      uni.showToast({ title: '未找到商品明细', icon: 'none' })
      return
    }
    item.value = target
    productId.value = target.productId
  } catch (error) {
    console.error('加载订单明细失败', error)
    uni.showToast({ title: '订单信息加载失败', icon: 'none' })
  } finally {
    loading.value = false
  }
})

function toggleTag(tag: string) {
  const index = selectedTags.value.indexOf(tag)
  if (index >= 0) {
    selectedTags.value.splice(index, 1)
    return
  }
  if (selectedTags.value.length >= MAX_TAGS) {
    uni.showToast({ title: '最多选择 3 个标签', icon: 'none' })
    return
  }
  selectedTags.value.push(tag)
}

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
          const fileUrl = await uploadReviewImage(path)
          images.value.push(fileUrl)
        }
      } catch (error) {
        console.error('上传评价图片失败', error)
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
  if (!item.value || !productId.value) {
    uni.showToast({ title: '商品信息未加载完成', icon: 'none' })
    return
  }

  const text = content.value.trim()
  if (text.length < 5 || text.length > 500) {
    uni.showToast({ title: '评价内容需 5-500 字', icon: 'none' })
    return
  }

  submitting.value = true
  try {
    await createProductReview(productId.value, {
      orderNo: orderNo.value,
      orderItemId: itemId.value,
      rating: rating.value,
      content: text,
      tags: [...selectedTags.value],
      images: [...images.value]
    })
    uni.showToast({ title: '评价已提交，待审核', icon: 'success' })
    setTimeout(() => uni.navigateBack(), 400)
  } catch (error) {
    console.error('提交评价失败', error)
    uni.showToast({ title: (error as Error).message || '提交失败', icon: 'none' })
  } finally {
    submitting.value = false
  }
}
</script>

<style lang="scss" scoped>
@import "./index.scss";
</style>
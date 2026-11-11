<template>
  <div class="list-page">
    <QueryPage api="/admin/mall/reviews" :searchParam="searchForm" @reset="handleReset" ref="queryPageRef">
      <!-- 搜索条件 -->
      <template #searchConditions>
        <el-form :model="searchForm" :inline="true">
          <el-form-item label="关键词">
            <el-input v-model="searchForm.keyword" placeholder="会员昵称 / 评价内容" clearable />
          </el-form-item>
          <el-form-item label="商品ID">
            <el-input v-model="searchForm.productId" placeholder="请输入商品ID" clearable />
          </el-form-item>
          <el-form-item label="评分">
            <el-select v-model="searchForm.rating" placeholder="全部" clearable>
              <el-option label="全部" value="" />
              <el-option v-for="item in ratingOptions" :key="item" :label="`${item} 星`" :value="item" />
            </el-select>
          </el-form-item>
          <el-form-item label="审核状态">
            <el-select v-model="searchForm.auditStatus" placeholder="全部" clearable>
              <el-option label="全部" value="" />
              <el-option label="待审核" :value="0" />
              <el-option label="已通过" :value="1" />
              <el-option label="已隐藏" :value="2" />
            </el-select>
          </el-form-item>
        </el-form>
      </template>

      <!-- 表格 -->
      <template #table="{ tableData }">
        <el-table :data="tableData" :fit="true" style="width: 100%" border>
          <el-table-column prop="productName" label="商品名称" min-width="180" show-overflow-tooltip />
          <el-table-column label="会员" min-width="160">
            <template #default="{ row }">
              <span>{{ row.memberNickname || '-' }}</span>
              <el-tag v-if="row.memberLevelName" size="small" type="warning" class="ml5">{{ row.memberLevelName }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column label="评分" width="150" align="center">
            <template #default="{ row }">
              <el-rate :model-value="Number(row.rating) || 0" disabled />
            </template>
          </el-table-column>
          <el-table-column label="评价内容" min-width="240" show-overflow-tooltip>
            <template #default="{ row }">{{ row.content || '-' }}</template>
          </el-table-column>
          <el-table-column label="评价图片" width="120" align="center">
            <template #default="{ row }">
              <div v-if="(row.images || []).length" class="review-images">
                <el-image
                  v-for="(img, index) in row.images"
                  :key="index"
                  :src="resolveFileUrl(img)"
                  :preview-src-list="(row.images || []).map((item: string) => resolveFileUrl(item))"
                  :initial-index="index"
                  fit="cover"
                  preview-teleported
                  class="review-image"
                />
              </div>
              <span v-else>-</span>
            </template>
          </el-table-column>
          <el-table-column label="规格" width="140" show-overflow-tooltip>
            <template #default="{ row }">{{ row.specText || '-' }}</template>
          </el-table-column>
          <el-table-column label="标签" min-width="160">
            <template #default="{ row }">
              <el-tag v-for="tag in row.tags || []" :key="tag" size="small" class="product-tag">{{ tag }}</el-tag>
              <span v-if="!(row.tags || []).length">-</span>
            </template>
          </el-table-column>
          <el-table-column label="审核状态" width="110" align="center">
            <template #default="{ row }">
              <el-tag :type="auditStatusMap[row.auditStatus]?.type || 'info'">
                {{ auditStatusMap[row.auditStatus]?.label || '未知' }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column label="详情展示" width="180" align="center">
            <template #default="{ row }">
              <div class="featured-cell">
                <el-tooltip content="请先审核通过" placement="top" :disabled="isApproved(row)">
                  <span class="featured-switch">
                    <el-switch
                      v-model="row.isFeatured"
                      :disabled="!isApproved(row)"
                      @change="(value: any) => changeFeaturedSwitch(row, Boolean(value))"
                    />
                  </span>
                </el-tooltip>
                <el-input-number
                  v-model="row.sortOrder"
                  :min="0"
                  :max="99"
                  :disabled="!isApproved(row) || !row.isFeatured"
                  size="small"
                  controls-position="right"
                  class="featured-sort"
                  @change="(value: any, oldValue: any) => changeFeaturedSort(row, oldValue)"
                />
              </div>
            </template>
          </el-table-column>
          <el-table-column label="评价时间" width="170" align="center">
            <template #default="{ row }">{{ formatDateTime(row.reviewTime || row.createdAt) }}</template>
          </el-table-column>
          <el-table-column label="操作" align="center" width="220" fixed="right" class-name="actions-column" header-class-name="actions-column">
            <template #default="{ row }">
              <Permission code="MALL_REVIEW_EDIT"><el-button type="primary" link :icon="EditPen" @click="openAuditDialog(row)">审核</el-button></Permission>
              <Permission code="MALL_REVIEW_EDIT"><el-button type="primary" link :icon="ChatDotRound" @click="openReplyDialog(row)">回复</el-button></Permission>
              <Permission code="MALL_REVIEW_DELETE"><el-button type="danger" link :icon="Delete" @click="remove(row)">删除</el-button></Permission>
            </template>
          </el-table-column>
        </el-table>
      </template>
    </QueryPage>

    <!-- 审核评价 -->
    <el-dialog v-model="auditDialogVisible" title="审核评价" width="560px">
      <el-form :model="auditForm" label-width="90px">
        <el-form-item label="评价内容">
          <div class="review-content">{{ auditForm.content || '-' }}</div>
        </el-form-item>
        <el-form-item label="审核状态">
          <el-radio-group v-model="auditForm.auditStatus">
            <el-radio :value="0">待审核</el-radio>
            <el-radio :value="1">已通过</el-radio>
            <el-radio :value="2">已隐藏</el-radio>
          </el-radio-group>
        </el-form-item>
      </el-form>
      <template #footer>
        <span class="dialog-footer">
          <el-button @click="auditDialogVisible = false">取消</el-button>
          <el-button type="primary" :loading="auditSubmitting" @click="submitAudit">保存</el-button>
        </span>
      </template>
    </el-dialog>

    <!-- 回复评价 -->
    <el-dialog v-model="replyDialogVisible" title="回复评价" width="600px">
      <el-form :model="replyForm" label-width="80px">
        <el-form-item label="评价内容">
          <div class="review-content">{{ replyForm.content || '-' }}</div>
        </el-form-item>
        <el-form-item label="回复内容" required>
          <el-input
            v-model="replyForm.replyContent"
            type="textarea"
            :rows="5"
            placeholder="请输入回复内容"
            maxlength="500"
            show-word-limit
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <span class="dialog-footer">
          <el-button @click="replyDialogVisible = false">取消</el-button>
          <el-button type="primary" :loading="replySubmitting" @click="submitReply">保存</el-button>
        </span>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts" name="mallReview">
import { ref, reactive } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { EditPen, ChatDotRound, Delete } from '@element-plus/icons-vue'
import { mallReviewApi } from '@/api/modules/mallReview'
import { resolveFileUrl } from '@/utils'
import QueryPage from '@/components/QueryPage/index.vue'
import Permission from "@/components/Permission/index.vue"

const ratingOptions = [1, 2, 3, 4, 5]

const auditStatusMap: Record<number, { label: string; type: 'warning' | 'success' | 'info' }> = {
  0: { label: '待审核', type: 'warning' },
  1: { label: '已通过', type: 'success' },
  2: { label: '已隐藏', type: 'info' }
}

const queryPageRef = ref()
const auditDialogVisible = ref(false)
const auditSubmitting = ref(false)
const replyDialogVisible = ref(false)
const replySubmitting = ref(false)

const searchForm = reactive({
  keyword: '',
  productId: '',
  rating: '' as number | '',
  auditStatus: '' as number | ''
})

const auditForm = reactive({
  id: '',
  content: '',
  auditStatus: 0
})

const replyForm = reactive({
  id: '',
  content: '',
  replyContent: ''
})

const formatDateTime = (value?: string | null): string => {
  if (!value) return '-'
  const date = new Date(value)
  if (isNaN(date.getTime())) return '-'
  const pad = (num: number) => String(num).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())} ${pad(date.getHours())}:${pad(date.getMinutes())}`
}

const handleReset = () => {
  searchForm.keyword = ''
  searchForm.productId = ''
  searchForm.rating = ''
  searchForm.auditStatus = ''
}

const isApproved = (row: any) => Number(row.auditStatus) === 1

const openAuditDialog = (row: any) => {
  auditForm.id = row.id
  auditForm.content = row.content || ''
  auditForm.auditStatus = Number(row.auditStatus) || 0
  auditDialogVisible.value = true
}

const submitAudit = async () => {
  auditSubmitting.value = true
  try {
    await mallReviewApi.changeReviewStatus(auditForm.id, auditForm.auditStatus)
    ElMessage.success('审核状态更新成功')
    auditDialogVisible.value = false
    queryPageRef.value?.getTableList()
  } catch {} finally { auditSubmitting.value = false }
}

const submitFeaturedChange = async (row: any, previous: { isFeatured: boolean; sortOrder: number }) => {
  if (!isApproved(row)) {
    row.isFeatured = previous.isFeatured
    row.sortOrder = previous.sortOrder
    ElMessage.warning('请先审核通过')
    return
  }
  try {
    await mallReviewApi.changeReviewFeatured(row.id, {
      isFeatured: Boolean(row.isFeatured),
      sortOrder: Number(row.sortOrder) || 0
    })
    if (!row.isFeatured) row.sortOrder = 0
    ElMessage.success('详情展示设置成功')
  } catch {
    row.isFeatured = previous.isFeatured
    row.sortOrder = previous.sortOrder
  }
}

const changeFeaturedSwitch = (row: any, value: boolean) => {
  submitFeaturedChange(row, { isFeatured: !value, sortOrder: Number(row.sortOrder) || 0 })
}

const changeFeaturedSort = (row: any, oldValue?: number) => {
  const currentSortOrder = Number(row.sortOrder) || 0
  const previousSortOrder = typeof oldValue === 'number' ? oldValue : currentSortOrder
  submitFeaturedChange(row, {
    isFeatured: Boolean(row.isFeatured),
    sortOrder: previousSortOrder
  })
}

const openReplyDialog = (row: any) => {
  replyForm.id = row.id
  replyForm.content = row.content || ''
  replyForm.replyContent = row.replyContent || ''
  replyDialogVisible.value = true
}

const submitReply = async () => {
  if (!replyForm.replyContent.trim()) {
    ElMessage.warning('请输入回复内容')
    return
  }
  replySubmitting.value = true
  try {
    await mallReviewApi.replyReview(replyForm.id, replyForm.replyContent.trim())
    ElMessage.success('回复成功')
    replyDialogVisible.value = false
    queryPageRef.value?.getTableList()
  } catch {} finally { replySubmitting.value = false }
}

const remove = async (row: any) => {
  try {
    await ElMessageBox.confirm('确定要删除该评价吗？', '删除', { type: 'warning' })
    await mallReviewApi.deleteReview(row.id)
    ElMessage.success('删除成功')
    queryPageRef.value?.getTableList()
  } catch {}
}
</script>

<style scoped lang="scss">
.review-content {
  width: 100%;
  padding: 8px 12px;
  background: #f7f8fa;
  border-radius: 4px;
  color: #606266;
  font-size: 13px;
  line-height: 1.6;
  word-break: break-all;
}
.product-tag { margin-right: 5px; }
.review-images { display: flex; flex-wrap: wrap; gap: 4px; justify-content: center; }
.review-image { width: 40px; height: 40px; border-radius: 4px; }
.featured-cell { display: flex; align-items: center; justify-content: center; gap: 8px; }
.featured-sort { width: 80px; }
</style>

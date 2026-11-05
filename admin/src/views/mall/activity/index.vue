<template>
  <div class="list-page">
    <QueryPage api="/admin/mall/activities" :searchParam="searchForm" @reset="handleReset" ref="queryPageRef">
      <template #searchConditions>
        <el-form :model="searchForm" :inline="true">
          <el-form-item label="关键字">
            <el-input v-model="searchForm.keyword" placeholder="活动名称 / 副标题" clearable />
          </el-form-item>
          <el-form-item label="状态">
            <el-select v-model="searchForm.isActive" placeholder="全部">
              <el-option label="全部" value="" />
              <el-option label="启用" :value="true" />
              <el-option label="停用" :value="false" />
            </el-select>
          </el-form-item>
        </el-form>
      </template>

      <template #headerButtons>
        <el-button type="primary" :icon="CirclePlus" @click="openDialog('新增')">新增活动</el-button>
      </template>

      <template #table="{ tableData }">
        <el-table :data="tableData" :fit="true" style="width: 100%" border>
          <el-table-column label="封面" width="100" align="center">
            <template #default="{ row }">
              <el-image
                v-if="row.coverImage"
                :src="resolveFileUrl(row.coverImage)"
                :preview-src-list="[resolveFileUrl(row.coverImage)]"
                preview-teleported
                fit="cover"
                class="activity-cover"
              />
              <span v-else class="empty-image">-</span>
            </template>
          </el-table-column>
          <el-table-column label="活动名称" min-width="200">
            <template #default="{ row }">
              <div class="activity-name">{{ row.name }}</div>
              <div v-if="row.subtitle" class="activity-subtitle">{{ row.subtitle }}</div>
            </template>
          </el-table-column>
          <el-table-column label="活动时间" width="240">
            <template #default="{ row }">
              <span style="font-size: 12px; color: #666">
                {{ formatDate(row.startDate) }} ~ {{ formatDate(row.endDate) }}
              </span>
            </template>
          </el-table-column>
          <el-table-column prop="sortOrder" label="排序" width="80" align="center" />
          <el-table-column label="状态" width="100" align="center">
            <template #default="{ row }">
              <el-tag :type="row.isActive ? 'success' : 'info'">{{ row.isActive ? '启用' : '停用' }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column label="操作" align="center" width="280" fixed="right" class-name="actions-column" header-class-name="actions-column">
            <template #default="{ row }">
              <el-button type="primary" link :icon="View" @click="openDrawer(row)">详情</el-button>
              <el-button type="primary" link :icon="EditPen" @click="openDialog('编辑', row)">编辑</el-button>
              <el-button :type="row.isActive ? 'warning' : 'success'" link @click="changeStatus(row)">
                {{ row.isActive ? '停用' : '启用' }}
              </el-button>
              <el-button type="danger" link :icon="Delete" @click="remove(row)">删除</el-button>
            </template>
          </el-table-column>
        </el-table>
      </template>
    </QueryPage>

    <!-- 活动详情抽屉 -->
    <el-drawer v-model="drawerVisible" :title="detail?.name || '活动详情'" size="640px" destroy-on-close>
      <div v-if="detail" class="activity-detail">
        <div class="detail-band">
          <el-image
            v-if="detail.coverImage"
            :src="resolveFileUrl(detail.coverImage)"
            :preview-src-list="[resolveFileUrl(detail.coverImage)]"
            preview-teleported
            fit="cover"
            class="detail-cover"
          />
          <div class="detail-head">
            <div class="detail-name">{{ detail.name }}</div>
            <div v-if="detail.subtitle" class="detail-subtitle">{{ detail.subtitle }}</div>
            <div class="detail-meta">
              <el-tag size="small" :type="detail.isActive ? 'success' : 'info'">
                {{ detail.isActive ? '启用' : '停用' }}
              </el-tag>
              <span class="detail-time">{{ formatDate(detail.startDate) }} ~ {{ formatDate(detail.endDate) }}</span>
            </div>
          </div>
        </div>

        <el-descriptions :column="2" border class="detail-desc">
          <el-descriptions-item label="排序">{{ detail.sortOrder }}</el-descriptions-item>
          <el-descriptions-item label="创建时间">{{ formatDate(detail.createdAt) }}</el-descriptions-item>
          <el-descriptions-item label="更新时间">{{ formatDate(detail.updatedAt) }}</el-descriptions-item>
          <el-descriptions-item label="活动说明" :span="2">{{ detail.description || '-' }}</el-descriptions-item>
        </el-descriptions>

        <div class="detail-section">
          <div class="section-title">活动详情</div>
          <div v-if="detail.detailHtml" class="detail-html" v-html="detail.detailHtml"></div>
          <div v-else class="detail-empty">暂无活动详情内容</div>
        </div>
      </div>
    </el-drawer>

    <!-- 新增 / 编辑活动 -->
    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="900px" top="5vh">
      <el-form :model="form" label-width="100px">
        <el-tabs v-model="editTab" type="border-card">
          <el-tab-pane label="基本信息" name="basic">
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="活动名称" required>
              <el-input v-model="form.name" maxlength="100" show-word-limit placeholder="请输入活动名称" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="副标题">
              <el-input v-model="form.subtitle" maxlength="100" show-word-limit placeholder="用于列表与详情展示" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="活动时间">
          <el-date-picker
            v-model="form.dateRange"
            type="datetimerange"
            range-separator="至"
            start-placeholder="开始时间"
            end-placeholder="结束时间"
            value-format="YYYY-MM-DDTHH:mm:ss"
            style="width: 100%"
          />
        </el-form-item>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="排序">
              <el-input-number v-model="form.sortOrder" :min="0" :max="9999" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item v-if="dialogType === '新增'" label="启用状态">
              <el-switch v-model="form.isActive" active-text="启用" inactive-text="停用" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="活动封面">
          <UploadImg v-model:imageUrl="coverImageModel" scene="activity" :width="'120px'" :height="'120px'">
            <template #tip>建议上传 16:9 图片，大小不超过 5MB</template>
          </UploadImg>
        </el-form-item>
        <el-form-item label="活动说明">
          <el-input v-model="form.description" type="textarea" :rows="2" maxlength="500" show-word-limit />
        </el-form-item>
          </el-tab-pane>
          <el-tab-pane label="活动详情" name="detail">
        <el-form-item label="活动详情" class="full-editor">
          <div class="activity-detail-editor">
            <ElementTiptap
              v-model:content="form.detailHtml"
              :extensions="activityDetailExtensions"
              :locale="editorLocale"
              height="460px"
            />
            <div class="detail-editor-hint" :class="{ 'is-over-limit': form.detailHtml.length > 50000 }">
              HTML 长度 {{ form.detailHtml.length.toLocaleString() }} / 50,000 字符；图片请使用工具栏上传。
            </div>
          </div>
        </el-form-item>
          </el-tab-pane>
        </el-tabs>
      </el-form>
      <template #footer>
        <span class="dialog-footer">
          <el-button @click="dialogVisible = false">取消</el-button>
          <el-button type="primary" :loading="submitting" @click="submitForm">确定</el-button>
        </span>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { CirclePlus, Delete, EditPen, View } from '@element-plus/icons-vue'
import { mallActivityApi } from '@/api/modules/mallActivity'
import QueryPage from '@/components/QueryPage/index.vue'
import UploadImg from '@/components/Upload/Img.vue'
import { formatDate, resolveFileUrl } from '@/utils'
import { uploadImg } from '@/api/modules/upload'
import {
  ElementTiptap,
  Document,
  Text,
  Paragraph,
  Heading,
  Bold,
  Italic,
  Underline,
  Strike,
  BulletList,
  OrderedList,
  Blockquote,
  Link,
  Image as TiptapImage,
  Table,
  TextAlign,
  Color,
  Highlight,
  History,
  HorizontalRule,
  FormatClear,
  Fullscreen
} from 'element-tiptap'
import editorLocale from 'element-tiptap/lib/locales/zh'
import 'element-tiptap/lib/style.css'

const uploadActivityDetailImage = async (file: File): Promise<string> => {
  if (!['image/jpeg', 'image/png', 'image/gif'].includes(file.type)) {
    ElMessage.warning('图片仅支持 JPG、PNG 或 GIF 格式')
    throw new Error('活动详情仅支持上传图片')
  }
  if (file.size > 5 * 1024 * 1024) {
    ElMessage.warning('活动详情图片大小不能超过 5MB')
    throw new Error('活动详情图片超过 5MB')
  }

  const formData = new FormData()
  formData.append('file', file)
  const { data } = await uploadImg(formData, 'activity')
  if (!data.fileUrl) {
    ElMessage.error('图片上传成功，但未返回图片地址')
    throw new Error('活动详情图片上传未返回地址')
  }
  return resolveFileUrl(data.fileUrl)
}

const imageOptions: Parameters<typeof TiptapImage.configure>[0] & {
  uploadRequest: (file: File) => Promise<string>
} = {
  uploadRequest: uploadActivityDetailImage
}

const queryPageRef = ref()
const dialogVisible = ref(false)
const dialogTitle = ref('')
const dialogType = ref('')
const currentId = ref('')
const submitting = ref(false)
const drawerVisible = ref(false)
const detail = ref<any>(null)
const editTab = ref('basic')

const activityDetailExtensions = [
  Document,
  Text,
  Paragraph,
  Heading,
  Bold,
  Italic,
  Underline,
  Strike,
  BulletList,
  OrderedList,
  Blockquote,
  Link,
  TiptapImage.configure(imageOptions),
  Table,
  TextAlign.configure({ types: ['heading', 'paragraph'] }),
  Color,
  Highlight,
  History,
  HorizontalRule,
  FormatClear,
  Fullscreen
]

const searchForm = reactive({
  keyword: '',
  isActive: '' as boolean | ''
})

const form = reactive({
  name: '',
  subtitle: '',
  coverImage: '',
  description: '',
  detailHtml: '',
  dateRange: [] as string[],
  sortOrder: 0,
  isActive: true
})

const apiOrigin = (import.meta.env.VITE_API_URL || 'http://localhost:5000/api').replace(/\/api\/?$/, '')

const coverImageModel = computed({
  get: () => resolveFileUrl(form.coverImage),
  set: (value: string) => {
    form.coverImage = value.startsWith(apiOrigin) ? value.slice(apiOrigin.length) : value
  }
})

const handleReset = () => {
  searchForm.keyword = ''
  searchForm.isActive = ''
}

const resetForm = () => {
  form.name = ''
  form.subtitle = ''
  form.coverImage = ''
  form.description = ''
  form.detailHtml = ''
  form.dateRange = []
  form.sortOrder = 0
  form.isActive = true
}

const openDialog = async (type: string, row?: any) => {
  dialogType.value = type
  dialogTitle.value = type
  if (type === '新增') {
    resetForm()
    currentId.value = ''
    dialogVisible.value = true
    return
  }

  if (!row) {
    return
  }

  try {
    const result = await mallActivityApi.getActivity(row.id)
    const data = result.data
    Object.assign(form, {
      name: data.name,
      subtitle: data.subtitle || '',
      coverImage: data.coverImage || '',
      description: data.description || '',
      detailHtml: data.detailHtml || '',
      dateRange: data.startDate || data.endDate ? [data.startDate, data.endDate] : [],
      sortOrder: data.sortOrder || 0,
      isActive: data.isActive
    })
    currentId.value = data.id
    dialogVisible.value = true
  } catch {
    ElMessage.error('加载活动详情失败')
  }
}

const openDrawer = async (row: any) => {
  try {
    const result = await mallActivityApi.getActivity(row.id)
    detail.value = result.data
    drawerVisible.value = true
  } catch {
    ElMessage.error('加载活动详情失败')
  }
}

const submitForm = async () => {
  if (!form.name.trim()) {
    ElMessage.warning('请输入活动名称')
    return
  }
  if (form.dateRange.length === 2 && form.dateRange[0] && form.dateRange[1] && form.dateRange[1] < form.dateRange[0]) {
    ElMessage.warning('活动结束时间不能早于开始时间')
    return
  }

  const payload = {
    name: form.name.trim(),
    subtitle: form.subtitle.trim(),
    coverImage: form.coverImage,
    description: form.description.trim(),
    detailHtml: form.detailHtml,
    startDate: form.dateRange[0] || null,
    endDate: form.dateRange[1] || null,
    sortOrder: form.sortOrder,
    ...(dialogType.value === '新增' ? { isActive: form.isActive } : {})
  }

  submitting.value = true
  try {
    if (dialogType.value === '新增') {
      await mallActivityApi.addActivity(payload)
      ElMessage.success('新增成功')
    } else {
      await mallActivityApi.updateActivity(currentId.value, payload)
      ElMessage.success('更新成功')
    }
    dialogVisible.value = false
    queryPageRef.value?.getTableList()
  } catch {} finally {
    submitting.value = false
  }
}

const changeStatus = async (row: any) => {
  const next = !row.isActive
  try {
    await ElMessageBox.confirm(`确定要${next ? '启用' : '停用'}活动“${row.name}”吗？`, '状态变更', {
      type: 'warning'
    })
    await mallActivityApi.changeActivityStatus(row.id, next)
    ElMessage.success(`已${next ? '启用' : '停用'}`)
    queryPageRef.value?.getTableList()
  } catch {}
}

const remove = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定要删除活动“${row.name}”吗？`, '删除', { type: 'warning' })
    await mallActivityApi.deleteActivity(row.id)
    ElMessage.success('删除成功')
    queryPageRef.value?.getTableList()
  } catch {}
}
</script>

<style scoped lang="scss">
.activity-cover {
  width: 72px;
  height: 48px;
  border-radius: 4px;
}

.activity-name {
  font-weight: 600;
}

.activity-subtitle {
  margin-top: 2px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.activity-detail {
  padding: 0 4px 12px;
}

.detail-band {
  display: flex;
  gap: 16px;
  align-items: flex-start;
  padding-bottom: 16px;
  border-bottom: 1px solid var(--el-border-color-lighter);
}

.detail-cover {
  width: 200px;
  height: 112px;
  border-radius: 8px;
  flex-shrink: 0;
}

.detail-head {
  flex: 1;
}

.detail-name {
  font-size: 18px;
  font-weight: 700;
}

.detail-subtitle {
  margin-top: 4px;
  color: var(--el-text-color-secondary);
}

.detail-meta {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-top: 10px;
}

.detail-time {
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.detail-desc {
  margin-top: 16px;
}

.detail-section {
  margin-top: 20px;

  .section-title {
    margin-bottom: 10px;
    font-size: 14px;
    font-weight: 600;
  }
}

.detail-html {
  padding: 12px;
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 6px;
  line-height: 1.7;
  word-break: break-word;
}

.detail-empty {
  padding: 24px 0;
  text-align: center;
  color: var(--el-text-color-secondary);
}

.activity-detail-editor {
  width: 100%;
}

.detail-editor-hint {
  margin-top: 6px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.detail-editor-hint.is-over-limit {
  color: var(--el-color-danger);
  font-weight: 600;
}
</style>
<template>
  <div class="list-page">
    <QueryPage v-if="searchForm.storeId" api="/admin/mall/banners" :searchParam="searchForm" @reset="handleReset" ref="queryPageRef">
      <template #searchConditions>
        <el-form :model="searchForm" :inline="true">
          <el-form-item label="门店">
            <el-select v-model="searchForm.storeId" placeholder="请选择门店" clearable>
              <el-option v-for="s in storeOptions" :key="s.id" :label="s.name" :value="s.id" />
            </el-select>
          </el-form-item>
          <el-form-item label="位置">
            <el-input v-model="searchForm.position" placeholder="如 HomeTop" clearable />
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
        <el-button type="primary" :icon="CirclePlus" @click="openDialog('新增')">新增 Banner</el-button>
        <el-button type="warning" :icon="Sort" @click="openSortDialog" :disabled="!searchForm.storeId">批量排序</el-button>
      </template>

      <template #table="{ tableData }">
        <el-table :data="tableData" :fit="true" style="width: 100%" border>
          <el-table-column label="图片" width="100" align="center">
            <template #default="{ row }">
              <el-image :src="resolveFileUrl(row.imageUrl)" :preview-src-list="[resolveFileUrl(row.imageUrl)]" style="width: 60px; height: 36px; object-fit: cover" fit="cover" preview-teleported />
            </template>
          </el-table-column>
          <el-table-column prop="position" label="位置" width="120" />
          <el-table-column prop="title" label="标题" min-width="150" show-overflow-tooltip />
          <el-table-column label="跳转" width="180">
            <template #default="{ row }">
              <el-tag size="small">{{ linkTypeMap[row.linkType] }}</el-tag>
              <span class="ml5" style="font-size: 12px; color: #999">{{ row.linkValue }}</span>
            </template>
          </el-table-column>
          <el-table-column prop="sortOrder" label="排序" width="80" align="center" />
          <el-table-column label="时间窗口" width="220">
            <template #default="{ row }">
              <span style="font-size: 12px; color: #666">
                {{ formatDate(row.startDate) || '不限' }} ~ {{ formatDate(row.endDate) || '永久' }}
              </span>
            </template>
          </el-table-column>
          <el-table-column label="状态" width="100" align="center">
            <template #default="{ row }">
              <el-tag :type="row.isActive ? 'success' : 'info'">{{ row.isActive ? '启用' : '停用' }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column label="操作" align="center" width="240" fixed="right" class-name="actions-column" header-class-name="actions-column">
            <template #default="{ row }">
              <el-button type="primary" link :icon="EditPen" @click="openDialog('编辑', row)">编辑</el-button>
              <el-button
                :type="row.isActive ? 'warning' : 'success'"
                link
                @click="changeStatus(row)"
              >
                {{ row.isActive ? '停用' : '启用' }}
              </el-button>
              <el-button type="danger" link :icon="Delete" @click="remove(row)">删除</el-button>
            </template>
          </el-table-column>
        </el-table>
      </template>
    </QueryPage>

    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="600px">
      <el-form :model="form" label-width="100px">
        <el-form-item label="门店" required>
          <el-select v-model="form.storeId" placeholder="请选择门店" style="width: 100%">
            <el-option v-for="s in storeOptions" :key="s.id" :label="s.name" :value="s.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="展示位置" required>
          <el-input v-model="form.position" placeholder="如 HomeTop / HomeMiddle / Category_xxx" maxlength="50" />
        </el-form-item>
        <el-form-item label="Banner图片" required>
          <UploadImg v-model:imageUrl="imageUrlModel" scene="banner" :width="'240px'" :height="'100px'">
            <template #tip>建议上传 16:9 图片，大小不超过 5MB</template>
          </UploadImg>
        </el-form-item>
        <el-form-item label="Banner标题">
          <el-input v-model="form.title" placeholder="可选，图片alt" maxlength="100" />
        </el-form-item>
        <el-form-item label="跳转类型">
          <el-select v-model="form.linkType" style="width: 100%" @change="form.linkValue = ''">
            <el-option label="商品详情" :value="0" />
            <el-option label="分类页" :value="1" />
            <el-option label="静态页" :value="2" />
            <el-option label="外链" :value="3" />
            <el-option label="专题活动" :value="6" />
          </el-select>
        </el-form-item>
        <el-form-item label="跳转值" required>
          <el-select
            v-if="form.linkType === 6"
            v-model="form.linkValue"
            filterable
            clearable
            style="width: 100%"
            placeholder="请选择专题活动"
          >
            <el-option v-for="item in campaignOptions" :key="item.id" :label="item.name" :value="item.id" />
          </el-select>
          <el-input
            v-else
            v-model="form.linkValue"
            :placeholder="form.linkType === 3 ? '请输入完整外链URL' : '商品ID / 分类ID / 页面编码'"
            maxlength="500"
          />
        </el-form-item>
        <el-form-item label="排序">
          <el-input-number v-model="form.sortOrder" :min="0" :max="9999" style="width: 100%" />
        </el-form-item>
        <el-form-item label="启用状态">
          <el-switch v-model="form.isActive" active-text="启用" inactive-text="停用" />
        </el-form-item>
      </el-form>
      <template #footer>
        <span class="dialog-footer">
          <el-button @click="dialogVisible = false">取消</el-button>
          <el-button type="primary" :loading="submitting" @click="submitForm">确定</el-button>
        </span>
      </template>
    </el-dialog>

    <el-dialog v-model="sortDialogVisible" title="批量排序" width="500px">
      <el-alert v-if="!sortList.length" title="当前门店没有 Banner" type="info" show-icon :closable="false" class="mb16" />
      <div v-else>
        <div v-for="item in sortList" :key="item.id" class="sort-item">
          <el-image :src="resolveFileUrl(item.imageUrl)" style="width: 40px; height: 24px; object-fit: cover; margin-right: 8px" fit="cover" />
          <span class="sort-title">{{ item.title || item.position }}</span>
          <el-input-number v-model="item.sortOrder" :min="0" :max="9999" size="small" />
        </div>
      </div>
      <template #footer>
        <el-button @click="sortDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="sortSubmitting" @click="submitSort">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts" name="mallBanner">
import { ref, reactive, computed, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { CirclePlus, EditPen, Delete, Sort } from '@element-plus/icons-vue'
import { mallBannerApi } from '@/api/modules/mallBanner'
import { mallStoreApi } from '@/api/modules/mallStore'
import { mallCampaignApi } from '@/api/modules/mallCampaign'
import QueryPage from '@/components/QueryPage/index.vue'
import UploadImg from '@/components/Upload/Img.vue'
import { formatDate, resolveFileUrl } from '@/utils'

const linkTypeMap: Record<number, string> = { 0: '商品', 1: '分类', 2: '页面', 3: '外链', 6: '专题活动' }

const queryPageRef = ref()
const dialogVisible = ref(false)
const submitting = ref(false)
const dialogTitle = ref('')
const dialogType = ref('')
const currentId = ref('')
const storeOptions = ref<any[]>([])
const campaignOptions = ref<any[]>([])

const sortDialogVisible = ref(false)
const sortSubmitting = ref(false)
const sortList = ref<any[]>([])

const searchForm = reactive({
  storeId: '',
  position: '',
  isActive: '' as boolean | ''
})

const form = reactive({
  storeId: '',
  position: '',
  imageUrl: '',
  title: '',
  linkType: 0,
  linkValue: '',
  sortOrder: 0,
  isActive: true
})

const apiOrigin = (import.meta.env.VITE_API_URL || 'http://localhost:5000/api').replace(/\/api\/?$/, '')
const imageUrlModel = computed({
  get: () => resolveFileUrl(form.imageUrl),
  set: (value: string) => {
    form.imageUrl = value.startsWith(apiOrigin) ? value.slice(apiOrigin.length) : value
  }
})

onMounted(async () => {
  try {
    const [storesResponse, campaignsResponse] = await Promise.all([
      mallStoreApi.getStoreList({ page: 1, pageSize: 200, isActive: true }),
      mallCampaignApi.getCampaignList({ page: 1, pageSize: 200 })
    ])
    storeOptions.value = storesResponse?.data?.list || storesResponse?.data?.records || []
    campaignOptions.value = campaignsResponse?.data?.list || []
    if (storeOptions.value.length) {
      searchForm.storeId = storeOptions.value[0].id
      form.storeId = storeOptions.value[0].id
    }
  } catch (error) {
    console.error('加载 Banner 配置选项失败', error)
    ElMessage.error('加载门店、专题活动或促销活动失败')
  }
})

const handleReset = () => {
  searchForm.position = ''
  searchForm.isActive = ''
}

const resetForm = () => {
  form.position = ''
  form.imageUrl = ''
  form.title = ''
  form.linkType = 0
  form.linkValue = ''
  form.sortOrder = 0
  form.isActive = true
}

const openDialog = (type: string, row?: any) => {
  dialogTitle.value = type
  dialogType.value = type

  if (type === '新增') {
    resetForm()
    currentId.value = ''
    if (storeOptions.value.length) form.storeId = storeOptions.value[0].id
  } else if (row) {
    Object.assign(form, {
      storeId: searchForm.storeId || (storeOptions.value[0]?.id ?? ''),
      position: row.position,
      imageUrl: row.imageUrl,
      title: row.title,
      linkType: row.linkType,
      linkValue: row.linkValue,
      sortOrder: row.sortOrder,
      isActive: row.isActive
    })
    currentId.value = row.id
  }

  dialogVisible.value = true
}

const submitForm = async () => {
  if (!form.storeId) { ElMessage.warning('请选择门店'); return }
  if (!form.position.trim()) { ElMessage.warning('请输入展示位置'); return }
  if (!form.imageUrl.trim()) { ElMessage.warning('请上传 Banner 图片'); return }
  if (form.linkType === 4 || form.linkType === 5) {
    if (!form.linkValue.trim()) { ElMessage.warning(form.linkType === 4 ? '请选择促销活动' : '请选择报名活动'); return }
  }

  submitting.value = true
  try {
    if (dialogType.value === '新增') {
      await mallBannerApi.add({ ...form, position: form.position.trim(), imageUrl: form.imageUrl.trim() })
      ElMessage.success('新增成功')
    } else {
      await mallBannerApi.update(currentId.value, {
        position: form.position.trim(),
        imageUrl: form.imageUrl.trim(),
        linkType: form.linkType,
        linkValue: form.linkValue,
        title: form.title
      })
      ElMessage.success('更新成功')
    }
    dialogVisible.value = false
    queryPageRef.value?.getTableList()
  } catch {} finally { submitting.value = false }
}

const changeStatus = async (row: any) => {
  const next = !row.isActive
  try {
    await ElMessageBox.confirm(`确定要${next ? '启用' : '停用'}该 Banner 吗？`, '状态变更', { type: 'warning' })
    await mallBannerApi.changeStatus(row.id, next)
    queryPageRef.value?.getTableList()
  } catch {}
}

const remove = async (row: any) => {
  try {
    await ElMessageBox.confirm('确定要删除该 Banner 吗？', '删除', { type: 'warning' })
    await mallBannerApi.remove(row.id)
    ElMessage.success('删除成功')
    queryPageRef.value?.getTableList()
  } catch {}
}

const openSortDialog = async () => {
  if (!searchForm.storeId) { ElMessage.warning('请先选择门店'); return }
  const res = await mallBannerApi.getList({ storeId: searchForm.storeId, pageNum: 1, pageSize: 500 })
  const list = res?.data?.records || res?.data || []
  sortList.value = list.map((b: any) => ({ id: b.id, title: b.title, imageUrl: b.imageUrl, position: b.position, sortOrder: b.sortOrder }))
  sortDialogVisible.value = true
}

const submitSort = async () => {
  sortSubmitting.value = true
  try {
    await mallBannerApi.batchSort(sortList.value.map(i => ({ id: i.id, sortOrder: i.sortOrder })))
    ElMessage.success('排序保存成功')
    sortDialogVisible.value = false
    queryPageRef.value?.getTableList()
  } catch {} finally { sortSubmitting.value = false }
}
</script>

<style scoped lang="scss">
.sort-item { display: flex; align-items: center; padding: 8px 0; border-bottom: 1px solid #f0f0f0; }
.sort-title { flex: 1; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; font-size: 13px; }
</style>
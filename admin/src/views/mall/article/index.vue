<template>
  <div class="list-page">
    <QueryPage api="/admin/mall/articles" :searchParam="searchForm" @reset="handleReset" ref="queryPageRef">
      <template #searchConditions>
        <el-form :model="searchForm" :inline="true">
          <el-form-item label="门店">
            <el-select v-model="searchForm.storeId" placeholder="请选择门店" clearable>
              <el-option v-for="s in storeOptions" :key="s.id" :label="s.name" :value="s.id" />
            </el-select>
          </el-form-item>
          <el-form-item label="类型">
            <el-select v-model="searchForm.articleType" placeholder="全部" clearable>
              <el-option label="帮助中心" :value="0" />
              <el-option label="商城资讯" :value="1" />
              <el-option label="活动规则" :value="2" />
              <el-option label="用户指南" :value="3" />
            </el-select>
          </el-form-item>
          <el-form-item label="关键字">
            <el-input v-model="searchForm.keyword" placeholder="标题/摘要" clearable />
          </el-form-item>
          <el-form-item label="发布状态">
            <el-select v-model="searchForm.isActive" placeholder="全部">
              <el-option label="全部" value="" />
              <el-option label="已发布" :value="true" />
              <el-option label="草稿" :value="false" />
            </el-select>
          </el-form-item>
        </el-form>
      </template>

      <template #headerButtons>
        <el-button type="primary" :icon="CirclePlus" @click="openDialog('新增')">新增文章</el-button>
      </template>

      <template #table="{ tableData }">
        <el-table :data="tableData" :fit="true" style="width: 100%" border>
          <el-table-column prop="title" label="标题" min-width="220" show-overflow-tooltip />
          <el-table-column label="类型" width="100" align="center">
            <template #default="{ row }">{{ articleTypeMap[row.articleType] }}</template>
          </el-table-column>
          <el-table-column prop="author" label="作者" width="80" />
          <el-table-column label="置顶" width="70" align="center">
            <template #default="{ row }">
              <el-tag v-if="row.isTop" type="danger">顶</el-tag>
              <span v-else>-</span>
            </template>
          </el-table-column>
          <el-table-column prop="viewCount" label="浏览量" width="80" align="center" />
          <el-table-column label="状态" width="100" align="center">
            <template #default="{ row }">
              <el-tag v-if="row.isActive" type="success">已发布</el-tag>
              <el-tag v-else type="info">草稿</el-tag>
            </template>
          </el-table-column>
          <el-table-column label="发布时间" width="170" align="center">
            <template #default="{ row }">{{ formatDate(row.publishedAt) || '-' }}</template>
          </el-table-column>
          <el-table-column label="操作" align="center" width="340" fixed="right">
            <template #default="{ row }">
              <el-button type="primary" link :icon="EditPen" @click="openDialog('编辑', row)">编辑</el-button>
              <el-button :type="row.isActive ? 'warning' : 'success'" link @click="togglePublish(row)">
                {{ row.isActive ? '下线' : '发布' }}
              </el-button>
              <el-button :type="row.isTop ? 'warning' : 'success'" link @click="toggleTop(row)">
                {{ row.isTop ? '取消置顶' : '置顶' }}
              </el-button>
              <el-button type="danger" link :icon="Delete" @click="remove(row)">删除</el-button>
            </template>
          </el-table-column>
        </el-table>
      </template>
    </QueryPage>

    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="800px" top="3vh">
      <el-form :model="form" label-width="100px">
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="门店" required>
              <el-select v-model="form.storeId" style="width: 100%">
                <el-option v-for="s in storeOptions" :key="s.id" :label="s.name" :value="s.id" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="文章类型">
              <el-select v-model="form.articleType" style="width: 100%">
                <el-option label="帮助中心" :value="0" />
                <el-option label="商城资讯" :value="1" />
                <el-option label="活动规则" :value="2" />
                <el-option label="用户指南" :value="3" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="标题" required>
          <el-input v-model="form.title" maxlength="200" show-word-limit />
        </el-form-item>
        <el-form-item label="摘要">
          <el-input v-model="form.summary" type="textarea" :rows="2" maxlength="500" show-word-limit placeholder="列表页展示 / SEO描述" />
        </el-form-item>
        <el-form-item label="正文">
          <el-input v-model="form.contentHtml" type="textarea" :rows="12" placeholder="HTML 富文本正文" />
        </el-form-item>
        <el-row :gutter="16">
          <el-col :span="8">
            <el-form-item label="作者">
              <el-input v-model="form.author" maxlength="50" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="排序">
              <el-input-number v-model="form.sortOrder" :min="0" :max="9999" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="置顶">
              <el-switch v-model="form.isTop" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-divider content-position="left">SEO 设置</el-divider>
        <el-row :gutter="16">
          <el-col :span="8">
            <el-form-item label="SEO标题">
              <el-input v-model="form.seoTitle" maxlength="100" show-word-limit />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="SEO关键词">
              <el-input v-model="form.seoKeywords" maxlength="200" show-word-limit />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="SEO描述">
              <el-input v-model="form.seoDescription" maxlength="300" show-word-limit />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="立即发布">
          <el-switch v-model="form.isActive" active-text="发布" inactive-text="草稿" />
        </el-form-item>
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

<script setup lang="ts" name="mallArticle">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { CirclePlus, EditPen, Delete } from '@element-plus/icons-vue'
import { mallArticleApi } from '@/api/modules/mallArticle'
import { mallStoreApi } from '@/api/modules/mallStore'
import QueryPage from '@/components/QueryPage/index.vue'
import { formatDate } from '@/utils'

const articleTypeMap: Record<number, string> = { 0: '帮助中心', 1: '商城资讯', 2: '活动规则', 3: '用户指南' }

const queryPageRef = ref()
const dialogVisible = ref(false)
const submitting = ref(false)
const dialogTitle = ref('')
const dialogType = ref('')
const currentId = ref('')
const storeOptions = ref<any[]>([])

const searchForm = reactive({
  storeId: '',
  articleType: '' as number | '',
  keyword: '',
  isActive: '' as boolean | ''
})

const form = reactive({
  storeId: '',
  title: '',
  summary: '',
  contentHtml: '',
  articleType: 0,
  author: '',
  seoTitle: '',
  seoKeywords: '',
  seoDescription: '',
  isTop: false,
  sortOrder: 0,
  isActive: false
})

onMounted(async () => {
  const res = await mallStoreApi.getStoreList({ pageNum: 1, pageSize: 200 })
  storeOptions.value = res?.data?.records || res?.data || []
  if (storeOptions.value.length) {
    searchForm.storeId = storeOptions.value[0].id
    form.storeId = storeOptions.value[0].id
  }
})

const handleReset = () => {
  searchForm.articleType = ''; searchForm.keyword = ''; searchForm.isActive = ''
}

const resetForm = () => {
  form.title = ''; form.summary = ''; form.contentHtml = ''
  form.articleType = 0; form.author = ''
  form.seoTitle = ''; form.seoKeywords = ''; form.seoDescription = ''
  form.isTop = false; form.sortOrder = 0; form.isActive = false
}

const openDialog = (type: string, row?: any) => {
  dialogTitle.value = type; dialogType.value = type
  if (type === '新增') {
    resetForm(); currentId.value = ''
    if (storeOptions.value.length) form.storeId = storeOptions.value[0].id
  } else if (row) {
    Object.assign(form, row); currentId.value = row.id
  }
  dialogVisible.value = true
}

const submitForm = async () => {
  if (!form.storeId) { ElMessage.warning('请选择门店'); return }
  if (!form.title.trim()) { ElMessage.warning('请输入标题'); return }

  submitting.value = true
  try {
    if (dialogType.value === '新增') {
      await mallArticleApi.add({ ...form, title: form.title.trim() })
      ElMessage.success('新增成功')
    } else {
      await mallArticleApi.update(currentId.value, {
        title: form.title.trim(),
        summary: form.summary,
        contentHtml: form.contentHtml,
        articleType: form.articleType,
        author: form.author,
        seoTitle: form.seoTitle,
        seoKeywords: form.seoKeywords,
        seoDescription: form.seoDescription,
        sortOrder: form.sortOrder
      })
      ElMessage.success('更新成功')
    }
    dialogVisible.value = false
    queryPageRef.value?.getTableList()
  } catch {} finally { submitting.value = false }
}

const togglePublish = async (row: any) => {
  try {
    if (row.isActive) {
      await ElMessageBox.confirm(`确定要下线文章“${row.title}”吗？`, '下线', { type: 'warning' })
      await mallArticleApi.unpublish(row.id)
    } else {
      await mallArticleApi.publish(row.id)
    }
    ElMessage.success(row.isActive ? '下线成功' : '发布成功')
    queryPageRef.value?.getTableList()
  } catch {}
}

const toggleTop = async (row: any) => {
  try {
    await mallArticleApi.setTop(row.id, !row.isTop)
    queryPageRef.value?.getTableList()
  } catch {}
}

const remove = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定要删除文章“${row.title}”吗？`, '删除', { type: 'warning' })
    await mallArticleApi.remove(row.id)
    ElMessage.success('删除成功')
    queryPageRef.value?.getTableList()
  } catch {}
}
</script>

<style scoped lang="scss"></style>
<template>
  <div class="list-page">
    <QueryPage api="/admin/mall/pages" :searchParam="searchForm" @reset="handleReset" ref="queryPageRef">
      <template #searchConditions>
        <el-form :model="searchForm" :inline="true">
          <el-form-item label="门店">
            <el-select v-model="searchForm.storeId" placeholder="请选择门店" clearable>
              <el-option v-for="s in storeOptions" :key="s.id" :label="s.name" :value="s.id" />
            </el-select>
          </el-form-item>
        </el-form>
      </template>

      <template #headerButtons>
        <el-button type="primary" :icon="CirclePlus" @click="openDialog('新增')">新增页面</el-button>
      </template>

      <template #table="{ tableData }">
        <el-table :data="tableData" :fit="true" style="width: 100%" border>
          <el-table-column prop="pageCode" label="页面编码" width="150" />
          <el-table-column prop="pageName" label="页面名称" width="150" />
          <el-table-column prop="title" label="浏览器标题" min-width="180" show-overflow-tooltip />
          <el-table-column label="状态" width="100" align="center">
            <template #default="{ row }">
              <el-tag :type="row.isActive ? 'success' : 'info'">{{ row.isActive ? '启用' : '停用' }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column label="创建时间" width="170" align="center">
            <template #default="{ row }">{{ formatDate(row.createdAt) }}</template>
          </el-table-column>
          <el-table-column label="操作" align="center" width="220" fixed="right" class-name="actions-column" header-class-name="actions-column">
            <template #default="{ row }">
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

    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="800px" top="5vh">
      <el-form :model="form" label-width="100px">
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="门店" required>
              <el-select v-model="form.storeId" placeholder="请选择门店" style="width: 100%">
                <el-option v-for="s in storeOptions" :key="s.id" :label="s.name" :value="s.id" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="页面编码" required>
              <el-input v-model="form.pageCode" placeholder="如 AboutUs / ServiceAgreement" maxlength="50" :disabled="dialogType === '编辑'" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="页面名称" required>
              <el-input v-model="form.pageName" placeholder="如 关于我们" maxlength="100" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="浏览器标题">
              <el-input v-model="form.title" placeholder="浏览器Tab展示的标题" maxlength="100" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="页面内容">
          <el-input v-model="form.contentHtml" type="textarea" :rows="10" placeholder="HTML 富文本内容" />
        </el-form-item>
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
  </div>
</template>

<script setup lang="ts" name="mallPage">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { CirclePlus, EditPen, Delete } from '@element-plus/icons-vue'
import { mallPageApi } from '@/api/modules/mall/mallPage'
import { mallStoreApi } from '@/api/modules/mall/mallStore'
import QueryPage from '@/components/QueryPage/index.vue'
import { formatDate } from '@/utils'

const queryPageRef = ref()
const dialogVisible = ref(false)
const submitting = ref(false)
const dialogTitle = ref('')
const dialogType = ref('')
const currentId = ref('')
const storeOptions = ref<any[]>([])

const searchForm = reactive({ storeId: '' })

const form = reactive({
  storeId: '',
  pageCode: '',
  pageName: '',
  title: '',
  contentHtml: '',
  seoTitle: '',
  seoKeywords: '',
  seoDescription: '',
  isActive: true
})

onMounted(async () => {
  const res = await mallStoreApi.getStoreList({ pageNum: 1, pageSize: 200 })
  storeOptions.value = res?.data?.records || res?.data || []
  if (storeOptions.value.length) {
    searchForm.storeId = storeOptions.value[0].id
    form.storeId = storeOptions.value[0].id
  }
})

const handleReset = () => { searchForm.storeId = '' }

const resetForm = () => {
  form.pageCode = ''; form.pageName = ''; form.title = ''; form.contentHtml = ''
  form.seoTitle = ''; form.seoKeywords = ''; form.seoDescription = ''; form.isActive = true
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
  if (!form.pageCode.trim()) { ElMessage.warning('请输入页面编码'); return }
  if (!form.pageName.trim()) { ElMessage.warning('请输入页面名称'); return }

  submitting.value = true
  try {
    if (dialogType.value === '新增') {
      await mallPageApi.add({ ...form, pageCode: form.pageCode.trim(), pageName: form.pageName.trim() })
      ElMessage.success('新增成功')
    } else {
      await mallPageApi.update(currentId.value, {
        pageName: form.pageName.trim(),
        title: form.title,
        contentHtml: form.contentHtml,
        seoTitle: form.seoTitle,
        seoKeywords: form.seoKeywords,
        seoDescription: form.seoDescription
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
    await ElMessageBox.confirm(`确定要${next ? '启用' : '停用'}该页面吗？`, '状态变更', { type: 'warning' })
    await mallPageApi.changeStatus(row.id, next)
    queryPageRef.value?.getTableList()
  } catch {}
}

const remove = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定要删除页面“${row.pageName}”吗？`, '删除', { type: 'warning' })
    await mallPageApi.remove(row.id)
    ElMessage.success('删除成功')
    queryPageRef.value?.getTableList()
  } catch {}
}
</script>

<style scoped lang="scss"></style>
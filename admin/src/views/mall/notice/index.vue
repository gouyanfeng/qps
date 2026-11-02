<template>
  <div class="list-page">
    <QueryPage api="/admin/mall/notices" :searchParam="searchForm" @reset="handleReset" ref="queryPageRef">
      <template #searchConditions>
        <el-form :model="searchForm" :inline="true">
          <el-form-item label="门店">
            <el-select v-model="searchForm.storeId" placeholder="请选择门店" clearable>
              <el-option v-for="s in storeOptions" :key="s.id" :label="s.name" :value="s.id" />
            </el-select>
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
        <el-button type="primary" :icon="CirclePlus" @click="openDialog('新增')">新增公告</el-button>
      </template>

      <template #table="{ tableData }">
        <el-table :data="tableData" :fit="true" style="width: 100%" border>
          <el-table-column prop="title" label="标题" min-width="200" show-overflow-tooltip />
          <el-table-column prop="content" label="内容" min-width="250" show-overflow-tooltip />
          <el-table-column label="类型" width="100" align="center">
            <template #default="{ row }">{{ noticeTypeMap[row.noticeType] }}</template>
          </el-table-column>
          <el-table-column label="置顶" width="80" align="center">
            <template #default="{ row }">
              <el-tag v-if="row.isTop" type="danger">置顶</el-tag>
              <span v-else>-</span>
            </template>
          </el-table-column>
          <el-table-column prop="sortOrder" label="排序" width="80" align="center" />
          <el-table-column label="状态" width="80" align="center">
            <template #default="{ row }">
              <el-tag :type="row.isActive ? 'success' : 'info'">{{ row.isActive ? '启用' : '停用' }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column label="创建时间" width="170" align="center">
            <template #default="{ row }">{{ formatDate(row.createdAt) }}</template>
          </el-table-column>
          <el-table-column label="操作" align="center" width="300" fixed="right">
            <template #default="{ row }">
              <el-button type="primary" link :icon="EditPen" @click="openDialog('编辑', row)">编辑</el-button>
              <el-button
                :type="row.isTop ? 'warning' : 'success'"
                link
                @click="toggleTop(row)"
              >
                {{ row.isTop ? '取消置顶' : '置顶' }}
              </el-button>
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

    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="560px">
      <el-form :model="form" label-width="100px">
        <el-form-item label="门店" required>
          <el-select v-model="form.storeId" placeholder="请选择门店" style="width: 100%">
            <el-option v-for="s in storeOptions" :key="s.id" :label="s.name" :value="s.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="标题" required>
          <el-input v-model="form.title" placeholder="请输入标题" maxlength="100" show-word-limit />
        </el-form-item>
        <el-form-item label="内容" required>
          <el-input v-model="form.content" type="textarea" :rows="3" placeholder="请输入公告内容" maxlength="500" show-word-limit />
        </el-form-item>
        <el-form-item label="类型">
          <el-select v-model="form.noticeType" style="width: 100%">
            <el-option label="系统公告" :value="0" />
            <el-option label="活动通知" :value="1" />
            <el-option label="维护通知" :value="2" />
          </el-select>
        </el-form-item>
        <el-form-item label="排序">
          <el-input-number v-model="form.sortOrder" :min="0" :max="9999" style="width: 100%" />
        </el-form-item>
        <el-form-item label="置顶">
          <el-switch v-model="form.isTop" />
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
  </div>
</template>

<script setup lang="ts" name="mallNotice">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { CirclePlus, EditPen, Delete } from '@element-plus/icons-vue'
import { mallNoticeApi } from '@/api/modules/mallNotice'
import { mallStoreApi } from '@/api/modules/mallStore'
import QueryPage from '@/components/QueryPage/index.vue'
import { formatDate } from '@/utils'

const noticeTypeMap: Record<number, string> = { 0: '系统公告', 1: '活动通知', 2: '维护通知' }

const queryPageRef = ref()
const dialogVisible = ref(false)
const submitting = ref(false)
const dialogTitle = ref('')
const dialogType = ref('')
const currentId = ref('')
const storeOptions = ref<any[]>([])

const searchForm = reactive({
  storeId: '',
  isActive: '' as boolean | ''
})

const form = reactive({
  storeId: '',
  title: '',
  content: '',
  noticeType: 0,
  sortOrder: 0,
  isTop: false,
  isActive: true
})

onMounted(async () => {
  const res = await mallStoreApi.getStoreList({ pageNum: 1, pageSize: 200 })
  storeOptions.value = res?.data?.records || res?.data || []
  if (storeOptions.value.length && !searchForm.storeId) {
    searchForm.storeId = storeOptions.value[0].id
    form.storeId = storeOptions.value[0].id
  }
})

const handleReset = () => {
  searchForm.storeId = ''
  searchForm.isActive = ''
}

const resetForm = () => {
  form.title = ''
  form.content = ''
  form.noticeType = 0
  form.sortOrder = 0
  form.isTop = false
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
      storeId: row.storeId,
      title: row.title,
      content: row.content,
      noticeType: row.noticeType,
      sortOrder: row.sortOrder,
      isTop: row.isTop,
      isActive: row.isActive
    })
    currentId.value = row.id
  }

  dialogVisible.value = true
}

const submitForm = async () => {
  if (!form.storeId) { ElMessage.warning('请选择门店'); return }
  if (!form.title.trim()) { ElMessage.warning('请输入标题'); return }
  if (!form.content.trim()) { ElMessage.warning('请输入内容'); return }

  submitting.value = true
  try {
    if (dialogType.value === '新增') {
      await mallNoticeApi.add({ ...form, title: form.title.trim(), content: form.content.trim() })
      ElMessage.success('新增公告成功')
    } else {
      await mallNoticeApi.update(currentId.value, {
        title: form.title.trim(),
        content: form.content.trim(),
        noticeType: form.noticeType,
        sortOrder: form.sortOrder
      })
      ElMessage.success('更新公告成功')
    }
    dialogVisible.value = false
    queryPageRef.value?.getTableList()
  } catch {
  } finally {
    submitting.value = false
  }
}

const changeStatus = async (row: any) => {
  const next = !row.isActive
  const text = next ? '启用' : '停用'
  try {
    await ElMessageBox.confirm(`确定要${text}公告“${row.title}”吗？`, `${text}公告`, { type: 'warning' })
    await mallNoticeApi.changeStatus(row.id, next)
    ElMessage.success(`${text}成功`)
    queryPageRef.value?.getTableList()
  } catch {}
}

const toggleTop = async (row: any) => {
  try {
    await mallNoticeApi.setTop(row.id, !row.isTop)
    ElMessage.success(row.isTop ? '取消置顶成功' : '置顶成功')
    queryPageRef.value?.getTableList()
  } catch {}
}

const remove = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定要删除公告“${row.title}”吗？`, '删除公告', { type: 'warning' })
    await mallNoticeApi.remove(row.id)
    ElMessage.success('删除公告成功')
    queryPageRef.value?.getTableList()
  } catch {}
}
</script>

<style scoped lang="scss"></style>
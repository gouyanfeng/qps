<template>
  <div class="list-page">
    <QueryPage api="/admin/mall/categories" :searchParam="searchForm" @reset="handleReset" ref="queryPageRef">
      <!-- 搜索条件 -->
      <template #searchConditions>
        <el-form :model="searchForm" :inline="true">
          <el-form-item label="关键字">
            <el-input v-model="searchForm.keyword" placeholder="分类编码 / 名称" clearable />
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

      <!-- 功能按钮 -->
      <template #headerButtons>
        <el-button type="primary" :icon="CirclePlus" @click="openDialog('新增')">新增分类</el-button>
      </template>

      <!-- 表格 -->
      <template #table="{ tableData }">
        <el-table :data="tableData" :fit="true" style="width: 100%" border>
          <el-table-column prop="code" label="分类编码" min-width="160" show-overflow-tooltip />
          <el-table-column prop="name" label="分类名称" min-width="180" show-overflow-tooltip />
          <el-table-column prop="sortOrder" label="排序" width="100" align="center" />
          <el-table-column label="状态" width="100" align="center">
            <template #default="{ row }">
              <el-tag :type="row.isActive ? 'success' : 'info'">{{ row.isActive ? '启用' : '停用' }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column label="创建时间" width="180" align="center">
            <template #default="{ row }">{{ formatDate(row.createdAt) }}</template>
          </el-table-column>
          <el-table-column label="操作" align="center" width="280" fixed="right">
            <template #default="{ row }">
              <el-button type="primary" link :icon="EditPen" @click="openDialog('编辑', row)">编辑</el-button>
              <el-button
                :type="row.isActive ? 'warning' : 'success'"
                link
                :icon="SwitchButton"
                @click="changeStatus(row)"
              >
                {{ row.isActive ? '停用' : '启用' }}
              </el-button>
              <el-button type="danger" link :icon="Delete" @click="deleteCategory(row)">删除</el-button>
            </template>
          </el-table-column>
        </el-table>
      </template>
    </QueryPage>

    <!-- 新增 / 编辑对话框 -->
    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="520px">
      <el-form :model="form" label-width="100px">
        <el-form-item label="分类编码" required>
          <el-input v-model="form.code" placeholder="请输入分类编码" maxlength="50" show-word-limit />
        </el-form-item>
        <el-form-item label="分类名称" required>
          <el-input v-model="form.name" placeholder="请输入分类名称" maxlength="100" show-word-limit />
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
  </div>
</template>

<script setup lang="ts" name="mallCategory">
import { ref, reactive } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { CirclePlus, EditPen, Delete, SwitchButton } from '@element-plus/icons-vue'
import { mallCategoryApi } from '@/api/modules/mallCategory'
import QueryPage from '@/components/QueryPage/index.vue'
import { formatDate } from '@/utils'

const queryPageRef = ref()

const dialogVisible = ref(false)
const submitting = ref(false)
const dialogTitle = ref('')
const dialogType = ref('')
const currentId = ref('')
const currentStatus = ref(true)

const searchForm = reactive({
  keyword: '',
  isActive: '' as boolean | ''
})

const form = reactive({
  code: '',
  name: '',
  sortOrder: 0,
  isActive: true
})

const handleReset = () => {
  Object.assign(searchForm, {
    keyword: '',
    isActive: ''
  })
}

const resetForm = () => {
  Object.assign(form, {
    code: '',
    name: '',
    sortOrder: 0,
    isActive: true
  })
}

const openDialog = (type: string, row?: any) => {
  dialogTitle.value = type
  dialogType.value = type

  if (type === '新增') {
    resetForm()
    currentId.value = ''
    currentStatus.value = true
  } else if (row) {
    Object.assign(form, {
      code: row.code,
      name: row.name,
      sortOrder: row.sortOrder,
      isActive: row.isActive
    })
    currentId.value = row.id
    currentStatus.value = row.isActive
  }

  dialogVisible.value = true
}

const validateForm = () => {
  if (!form.code.trim()) {
    ElMessage.warning('请输入分类编码')
    return false
  }
  if (!form.name.trim()) {
    ElMessage.warning('请输入分类名称')
    return false
  }
  return true
}

const submitForm = async () => {
  if (!validateForm()) return

  submitting.value = true
  try {
    const payload = {
      code: form.code.trim(),
      name: form.name.trim(),
      sortOrder: form.sortOrder
    }

    if (dialogType.value === '新增') {
      await mallCategoryApi.addCategory({ ...payload, isActive: form.isActive })
      ElMessage.success('新增分类成功')
    } else {
      await mallCategoryApi.updateCategory(currentId.value, payload)
      if (form.isActive !== currentStatus.value) {
        await mallCategoryApi.changeCategoryStatus(currentId.value, form.isActive)
      }
      ElMessage.success('更新分类成功')
    }

    dialogVisible.value = false
    queryPageRef.value?.getTableList()
  } catch {
    // 错误提示已由请求拦截器统一处理
  } finally {
    submitting.value = false
  }
}

const changeStatus = async (row: any) => {
  const nextStatus = !row.isActive
  const actionText = nextStatus ? '启用' : '停用'
  try {
    await ElMessageBox.confirm(`确定要${actionText}分类“${row.name}”吗？`, `${actionText}分类`, {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })
    await mallCategoryApi.changeCategoryStatus(row.id, nextStatus)
    ElMessage.success(`${actionText}成功`)
    queryPageRef.value?.getTableList()
  } catch {
    // 取消操作或请求失败
  }
}

const deleteCategory = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定要删除分类“${row.name}”吗？`, '删除分类', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })
    await mallCategoryApi.deleteCategory(row.id)
    ElMessage.success('删除分类成功')
    queryPageRef.value?.getTableList()
  } catch {
    // 取消删除或请求失败
  }
}
</script>

<style scoped lang="scss"></style>

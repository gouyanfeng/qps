<template>
  <div class="list-page">
    <QueryPage api="/admin/mall/stores" :searchParam="searchForm" @reset="handleReset" ref="queryPageRef">
      <!-- 搜索条件 -->
      <template #searchConditions>
        <el-form :model="searchForm" :inline="true">
          <el-form-item label="关键字">
            <el-input v-model="searchForm.keyword" placeholder="门店编码 / 名称 / 地址" clearable />
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
        <el-button type="primary" :icon="CirclePlus" @click="openDialog('新增')">新增门店</el-button>
      </template>

      <!-- 表格 -->
      <template #table="{ tableData }">
        <el-table :data="tableData" :fit="true" style="width: 100%" border>
          <el-table-column prop="code" label="门店编码" min-width="140" show-overflow-tooltip />
          <el-table-column prop="name" label="门店名称" min-width="180" show-overflow-tooltip />
          <el-table-column prop="address" label="门店地址" min-width="240" show-overflow-tooltip />
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
              <el-button type="primary" link :icon="View" @click="openDetail(row.id)">详情</el-button>
              <el-button type="primary" link :icon="EditPen" @click="openDialog('编辑', row)">编辑</el-button>
              <el-button
                :type="row.isActive ? 'warning' : 'success'"
                link
                :icon="SwitchButton"
                @click="changeStatus(row)"
              >
                {{ row.isActive ? '停用' : '启用' }}
              </el-button>
              <el-button type="danger" link :icon="Delete" @click="deleteStore(row)">删除</el-button>
            </template>
          </el-table-column>
        </el-table>
      </template>
    </QueryPage>

    <!-- 新增 / 编辑对话框 -->
    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="520px">
      <el-form :model="form" label-width="100px">
        <el-form-item label="门店编码" required>
          <el-input v-model="form.code" placeholder="请输入门店编码" maxlength="50" show-word-limit />
        </el-form-item>
        <el-form-item label="门店名称" required>
          <el-input v-model="form.name" placeholder="请输入门店名称" maxlength="100" show-word-limit />
        </el-form-item>
        <el-form-item label="门店地址" required>
          <el-input v-model="form.address" placeholder="请输入门店地址" maxlength="200" show-word-limit />
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

    <!-- 详情对话框 -->
    <el-dialog v-model="detailVisible" title="门店详情" width="520px">
      <el-descriptions :column="1" border v-if="detail">
        <el-descriptions-item label="门店编码">{{ detail.code }}</el-descriptions-item>
        <el-descriptions-item label="门店名称">{{ detail.name }}</el-descriptions-item>
        <el-descriptions-item label="门店地址">{{ detail.address }}</el-descriptions-item>
        <el-descriptions-item label="状态">
          <el-tag :type="detail.isActive ? 'success' : 'info'">{{ detail.isActive ? '启用' : '停用' }}</el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="创建时间">{{ formatDate(detail.createdAt) }}</el-descriptions-item>
        <el-descriptions-item label="更新时间">{{ formatDate(detail.updatedAt) }}</el-descriptions-item>
      </el-descriptions>
    </el-dialog>
  </div>
</template>

<script setup lang="ts" name="mallStore">
import { ref, reactive } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { CirclePlus, EditPen, Delete, View, SwitchButton } from '@element-plus/icons-vue'
import { mallStoreApi } from '@/api/modules/mallStore'
import QueryPage from '@/components/QueryPage/index.vue'
import { formatDate } from '@/utils'

// 引用
const queryPageRef = ref()

// 状态管理
const dialogVisible = ref(false)
const detailVisible = ref(false)
const submitting = ref(false)
const dialogTitle = ref('')
const dialogType = ref('')
const currentId = ref('')
const currentStatus = ref(true)
const detail = ref<any>(null)

// 搜索表单
const searchForm = reactive({
  keyword: '',
  isActive: '' as boolean | ''
})

// 表单数据
const form = reactive({
  code: '',
  name: '',
  address: '',
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
    address: '',
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
      address: row.address,
      isActive: row.isActive
    })
    currentId.value = row.id
    currentStatus.value = row.isActive
  }

  dialogVisible.value = true
}

const validateForm = () => {
  if (!form.code.trim()) {
    ElMessage.warning('请输入门店编码')
    return false
  }
  if (!form.name.trim()) {
    ElMessage.warning('请输入门店名称')
    return false
  }
  if (!form.address.trim()) {
    ElMessage.warning('请输入门店地址')
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
      address: form.address.trim()
    }

    if (dialogType.value === '新增') {
      await mallStoreApi.addStore({ ...payload, isActive: form.isActive })
      ElMessage.success('新增门店成功')
    } else {
      await mallStoreApi.updateStore(currentId.value, payload)
      if (form.isActive !== currentStatus.value) {
        await mallStoreApi.changeStoreStatus(currentId.value, form.isActive)
      }
      ElMessage.success('更新门店成功')
    }

    dialogVisible.value = false
    queryPageRef.value?.getTableList()
  } catch {
    // 错误提示已由请求拦截器统一处理
  } finally {
    submitting.value = false
  }
}

const openDetail = async (id: string) => {
  const result = await mallStoreApi.getStore(id)
  detail.value = result.data
  detailVisible.value = true
}

const changeStatus = async (row: any) => {
  const nextStatus = !row.isActive
  const actionText = nextStatus ? '启用' : '停用'
  try {
    await ElMessageBox.confirm(`确定要${actionText}门店“${row.name}”吗？`, `${actionText}门店`, {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })
    await mallStoreApi.changeStoreStatus(row.id, nextStatus)
    ElMessage.success(`${actionText}成功`)
    queryPageRef.value?.getTableList()
  } catch {
    // 取消操作或请求失败
  }
}

const deleteStore = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定要删除门店“${row.name}”吗？`, '删除门店', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })
    await mallStoreApi.deleteStore(row.id)
    ElMessage.success('删除门店成功')
    queryPageRef.value?.getTableList()
  } catch {
    // 取消删除或请求失败
  }
}
</script>

<style scoped lang="scss"></style>

<template>
  <div class="list-page">
    <QueryPage
      api="/admin/mall/tags/page"
      :searchParam="searchForm"
      @reset="handleReset"
      ref="queryPageRef"
    >
      <template #searchConditions>
        <el-form :model="searchForm" :inline="true">
          <el-form-item label="类型">
            <el-select v-model="searchForm.type" placeholder="全部" clearable>
              <el-option label="全部" value="" />
              <el-option v-for="type in typeOptions" :key="type" :label="typeLabel(type)" :value="type" />
            </el-select>
          </el-form-item>
          <el-form-item label="关键字">
            <el-input v-model="searchForm.keyword" placeholder="编码 / 名称" clearable />
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
        <el-button type="primary" :icon="CirclePlus" @click="openDialog()">新增标签</el-button>
      </template>

      <template #table="{ tableData }">
        <el-table :data="tableData" :fit="true" style="width: 100%" border>
          <el-table-column prop="type" label="类型" width="120">
            <template #default="{ row }">
              <el-tag :type="typeTagTone(row.type)">{{ typeLabel(row.type) }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column prop="code" label="编码" min-width="160" show-overflow-tooltip />
          <el-table-column prop="name" label="名称" min-width="160" show-overflow-tooltip />
          <el-table-column label="颜色" width="150">
            <template #default="{ row }">
              <div class="color-cell">
                <span v-if="isHexColor(row.color)" class="color-dot" :style="{ background: row.color }" />
                <span class="color-text">{{ row.color || '-' }}</span>
              </div>
            </template>
          </el-table-column>
          <el-table-column prop="sortOrder" label="排序" width="80" align="center" />
          <el-table-column label="状态" width="100" align="center">
            <template #default="{ row }">
              <el-switch
                :model-value="row.isActive"
                :loading="row.statusSaving"
                @change="(value: boolean) => toggleStatus(row, value)"
              />
            </template>
          </el-table-column>
          <el-table-column
            label="操作"
            width="160"
            align="center"
            fixed="right"
            class-name="actions-column"
            header-class-name="actions-column"
          >
            <template #default="{ row }">
              <el-button type="primary" link :icon="EditPen" @click="openDialog(row)">编辑</el-button>
              <el-button type="danger" link :icon="Delete" @click="deleteTag(row)">删除</el-button>
            </template>
          </el-table-column>
        </el-table>
      </template>
    </QueryPage>

    <el-dialog v-model="dialogVisible" :title="editingId ? '编辑标签' : '新增标签'" width="480px">
      <el-form :model="form" label-width="90px">
        <el-form-item label="类型" required>
          <el-select
            v-model="form.type"
            :disabled="Boolean(editingId)"
            filterable
            allow-create
            default-first-option
            placeholder="选择或输入类型"
            style="width: 100%"
          >
            <el-option label="Product（商品）" value="Product" />
            <el-option label="Review（评价）" value="Review" />
          </el-select>
        </el-form-item>
        <el-form-item label="编码" required>
          <el-input v-model="form.code" maxlength="50" show-word-limit placeholder="自由输入，同类型内唯一" />
        </el-form-item>
        <el-form-item label="名称" required>
          <el-input v-model="form.name" maxlength="50" show-word-limit placeholder="自由输入，同类型内唯一" />
        </el-form-item>
        <el-form-item label="颜色">
          <el-color-picker
            v-model="form.color"
            color-format="hex"
            :predefine="predefineColors"
            @active-change="(value: string) => value && (form.color = value)"
          />
        </el-form-item>
        <el-form-item label="排序">
          <el-input-number v-model="form.sortOrder" :min="0" :max="9999" />
        </el-form-item>
        <el-form-item label="启用">
          <el-switch v-model="form.isActive" />
        </el-form-item>
      </el-form>
      <template #footer>
        <span class="dialog-footer">
          <el-button @click="dialogVisible = false">取消</el-button>
          <el-button type="primary" :loading="submitting" @click="submitForm">保存</el-button>
        </span>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts" name="mallTag">
import { reactive, ref } from 'vue'
import { CirclePlus, Delete, EditPen } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { mallTagApi } from '@/api/modules/mallTag'
import QueryPage from '@/components/QueryPage/index.vue'

const queryPageRef = ref()
const submitting = ref(false)
const dialogVisible = ref(false)
const editingId = ref('')
const typeOptions = ref<string[]>(['Product', 'Review'])

const searchForm = reactive({
  type: '',
  keyword: '',
  isActive: '' as boolean | ''
})

const predefineColors = ['#2E7D32', '#C2410C', '#409EFF', '#909399', '#F56C6C', '#E6A23C']

const LEGACY_COLORS: Record<string, string> = {
  success: '#67C23A',
  warning: '#E6A23C',
  primary: '#409EFF',
  info: '#909399',
  danger: '#F56C6C'
}

const form = reactive({
  type: 'Product',
  code: '',
  name: '',
  color: '#2E7D32',
  sortOrder: 0,
  isActive: true
})

const typeLabel = (type: string) => {
  if (type === 'Product') return '商品'
  if (type === 'Review') return '评价'
  return type || '-'
}

const typeTagTone = (type: string) => {
  if (type === 'Product') return 'primary'
  if (type === 'Review') return 'warning'
  return 'info'
}

const isHexColor = (color: string) => /^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$/.test(String(color || ''))

const normalizePickerColor = (color: string) => {
  if (isHexColor(color)) return color
  return LEGACY_COLORS[String(color || '').toLowerCase()] || ''
}

const loadTypeOptions = async () => {
  try {
    const result = await mallTagApi.getTags()
    const values = new Set<string>(['Product', 'Review'])
    ;(result.data || []).forEach((tag: any) => tag.type && values.add(tag.type))
    typeOptions.value = Array.from(values)
  } catch {
    typeOptions.value = ['Product', 'Review']
  }
}

const handleReset = () => {
  Object.assign(searchForm, {
    type: '',
    keyword: '',
    isActive: ''
  })
}

const openDialog = (row?: any) => {
  editingId.value = row?.id || ''
  if (row) {
    Object.assign(form, {
      type: row.type,
      code: row.code,
      name: row.name,
      color: normalizePickerColor(row.color),
      sortOrder: row.sortOrder,
      isActive: row.isActive
    })
  } else {
    Object.assign(form, {
      type: searchForm.type || 'Product',
      code: '',
      name: '',
      color: '#2E7D32',
      sortOrder: 0,
      isActive: true
    })
  }
  dialogVisible.value = true
}

const validateForm = () => {
  if (!form.type.trim()) {
    ElMessage.warning('请选择或输入标签类型')
    return false
  }
  if (!form.code.trim()) {
    ElMessage.warning('请输入标签编码')
    return false
  }
  if (!form.name.trim()) {
    ElMessage.warning('请输入标签名称')
    return false
  }
  return true
}

const submitForm = async () => {
  if (!validateForm()) return

  submitting.value = true
  try {
    const payload = {
      type: form.type.trim(),
      code: form.code.trim(),
      name: form.name.trim(),
      color: form.color || '',
      sortOrder: form.sortOrder,
      isActive: form.isActive
    }
    if (editingId.value) {
      await mallTagApi.updateTag(editingId.value, payload)
      ElMessage.success('标签已更新')
    } else {
      await mallTagApi.addTag(payload)
      ElMessage.success('标签已新增')
    }
    dialogVisible.value = false
    await loadTypeOptions()
    queryPageRef.value?.getTableList()
  } catch (error: any) {
    ElMessage.error(error?.message || '保存标签失败')
  } finally {
    submitting.value = false
  }
}

const toggleStatus = async (row: any, value: boolean) => {
  row.statusSaving = true
  try {
    await mallTagApi.changeTagStatus(row.id, value)
    row.isActive = value
    ElMessage.success(value ? '标签已启用' : '标签已停用')
  } catch (error: any) {
    ElMessage.error(error?.message || '更新标签状态失败')
  } finally {
    row.statusSaving = false
  }
}

const deleteTag = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定删除标签「${row.name}」吗？`, '提示', { type: 'warning' })
  } catch {
    return
  }
  try {
    await mallTagApi.deleteTag(row.id)
    ElMessage.success('标签已删除')
    await loadTypeOptions()
    queryPageRef.value?.getTableList()
  } catch (error: any) {
    ElMessage.error(error?.message || '删除标签失败')
  }
}

loadTypeOptions()
</script>

<style scoped lang="scss">
.color-cell {
  display: flex;
  align-items: center;
  gap: 8px;
}

.color-dot {
  width: 16px;
  height: 16px;
  border-radius: 3px;
  border: 1px solid #dcdfe6;
  flex-shrink: 0;
}

.color-text {
  color: #606266;
  font-size: 12px;
}
</style>

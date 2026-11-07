<template>
  <div class="list-page">
    <QueryPage api="/admin/mall/categories" :searchParam="searchForm" @reset="handleReset" ref="queryPageRef">
      <!-- 搜索条件 -->
      <template #searchConditions>
        <el-form :model="searchForm" :inline="true">
          <el-form-item label="关键字">
            <el-input v-model="searchForm.keyword" placeholder="分类编码 / 名称" clearable />
          </el-form-item>
          <el-form-item label="门店">
            <el-select v-model="searchForm.storeId" placeholder="全部门店" clearable>
              <el-option v-for="store in storeOptions" :key="store.id" :label="store.name" :value="store.id" />
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

      <!-- 功能按钮 -->
      <template #headerButtons>
        <el-button type="primary" :icon="CirclePlus" @click="openDialog('新增')">新增分类</el-button>
      </template>

      <!-- 表格 -->
      <template #table="{ tableData }">
        <el-table :data="tableData" :fit="true" style="width: 100%" border>
          <el-table-column label="图标" width="80" align="center">
            <template #default="{ row }">
              <el-image
                v-if="row.icon"
                :src="resolveFileUrl(row.icon)"
                fit="cover"
                class="category-icon"
                :preview-src-list="[resolveFileUrl(row.icon)]"
                preview-teleported
              />
              <span v-else class="icon-empty">—</span>
            </template>
          </el-table-column>
          <el-table-column prop="code" label="分类编码" min-width="160" show-overflow-tooltip />
          <el-table-column prop="name" label="分类名称" min-width="180" show-overflow-tooltip />
          <el-table-column label="父级分类" min-width="150">
            <template #default="{ row }">{{ row.parentName || '顶级分类' }}</template>
          </el-table-column>
          <el-table-column prop="sortOrder" label="排序" width="100" align="center" />
          <el-table-column label="状态" width="100" align="center">
            <template #default="{ row }">
              <el-tag :type="row.isActive ? 'success' : 'info'">{{ row.isActive ? '启用' : '停用' }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column label="创建时间" width="180" align="center">
            <template #default="{ row }">{{ formatDate(row.createdAt) }}</template>
          </el-table-column>
          <el-table-column label="操作" align="center" width="240" fixed="right" class-name="actions-column" header-class-name="actions-column">
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
    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="560px">
      <el-form :model="form" label-width="100px">
        <el-form-item label="所属门店" required>
          <el-select
            v-model="form.storeId"
            placeholder="请选择门店"
            :disabled="dialogType === '编辑'"
            style="width: 100%"
            @change="form.parentId = ''"
          >
            <el-option v-for="store in storeOptions" :key="store.id" :label="store.name" :value="store.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="父级分类">
          <el-select v-model="form.parentId" placeholder="顶级分类" clearable style="width: 100%">
            <el-option
              v-for="item in parentOptions"
              :key="item.id"
              :label="item.name"
              :value="item.id"
            />
          </el-select>
          <div class="form-tip">最多两级；有商品的分类应作为末级分类。</div>
        </el-form-item>
        <el-form-item label="分类编码" required>
          <el-input v-model="form.code" placeholder="请输入分类编码" maxlength="50" show-word-limit />
        </el-form-item>
        <el-form-item label="分类名称" required>
          <el-input v-model="form.name" placeholder="请输入分类名称" maxlength="100" show-word-limit />
        </el-form-item>
        <el-form-item label="分类图标">
          <UploadImg
            v-model:imageUrl="iconUrlModel"
            scene="category"
            :width="'80px'"
            :height="'80px'"
            :border-radius="'8px'"
          >
            <template #tip>建议上传 1:1 图标，大小不超过 5MB</template>
          </UploadImg>
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
import { ref, reactive, computed, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { CirclePlus, EditPen, Delete, SwitchButton } from '@element-plus/icons-vue'
import { mallCategoryApi } from '@/api/modules/mallCategory'
import { mallStoreApi } from '@/api/modules/mallStore'
import QueryPage from '@/components/QueryPage/index.vue'
import UploadImg from '@/components/Upload/Img.vue'
import { formatDate, resolveFileUrl } from '@/utils'

const queryPageRef = ref()

const dialogVisible = ref(false)
const submitting = ref(false)
const dialogTitle = ref('')
const dialogType = ref('')
const currentId = ref('')
const currentStatus = ref(true)
const storeOptions = ref<any[]>([])
const categoryOptions = ref<any[]>([])

const searchForm = reactive({
  keyword: '',
  storeId: '',
  isActive: '' as boolean | ''
})

const form = reactive({
  storeId: '',
  parentId: '',
  code: '',
  name: '',
  icon: '',
  sortOrder: 0,
  isActive: true
})

const parentOptions = computed(() => categoryOptions.value.filter(category =>
  category.storeId === form.storeId &&
  !category.parentId &&
  !categoryOptions.value.some(child => child.parentId === category.id) &&
  category.id !== currentId.value &&
  category.isActive
))

// 上传组件展示绝对地址，保存时仍写回后端相对路径
const apiOrigin = (import.meta.env.VITE_API_URL || 'http://localhost:5000/api').replace(/\/api\/?$/, '')
const iconUrlModel = computed({
  get: () => resolveFileUrl(form.icon),
  set: (value: string) => {
    form.icon = value.startsWith(apiOrigin) ? value.slice(apiOrigin.length) : value
  }
})

const loadOptions = async () => {
  const [storesResponse, categoriesResponse] = await Promise.all([
    mallStoreApi.getStoreList({ page: 1, pageSize: 200, isActive: true }),
    mallCategoryApi.getCategoryList({ page: 1, pageSize: 1000 })
  ])
  storeOptions.value = storesResponse.data?.list || []
  categoryOptions.value = categoriesResponse.data?.list || []
}

onMounted(async () => {
  try {
    await loadOptions()
  } catch {
    ElMessage.error('加载门店或分类选项失败')
  }
})

const handleReset = () => {
  Object.assign(searchForm, {
    keyword: '',
    storeId: '',
    isActive: ''
  })
}

const resetForm = () => {
  Object.assign(form, {
    code: '',
    storeId: storeOptions.value[0]?.id || '',
    parentId: '',
    name: '',
    icon: '',
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
      storeId: row.storeId,
      parentId: row.parentId || '',
      name: row.name,
      icon: row.icon || '',
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
  if (!form.storeId) {
    ElMessage.warning('请选择所属门店')
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
      icon: form.icon.trim() || null,
      parentId: form.parentId || null,
      sortOrder: form.sortOrder
    }

    if (dialogType.value === '新增') {
      await mallCategoryApi.addCategory({ ...payload, storeId: form.storeId, isActive: form.isActive })
      ElMessage.success('新增分类成功')
    } else {
      await mallCategoryApi.updateCategory(currentId.value, payload)
      if (form.isActive !== currentStatus.value) {
        await mallCategoryApi.changeCategoryStatus(currentId.value, form.isActive)
      }
      ElMessage.success('更新分类成功')
    }

    dialogVisible.value = false
    await loadOptions()
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
    await loadOptions()
    queryPageRef.value?.getTableList()
  } catch {
    // 取消删除或请求失败
  }
}
</script>

<style scoped lang="scss">
.form-tip {
  color: var(--el-text-color-secondary);
  font-size: 12px;
}
.category-icon {
  width: 40px;
  height: 40px;
  border-radius: 6px;
  vertical-align: middle;
}
.icon-empty {
  color: var(--el-text-color-placeholder);
}
</style>
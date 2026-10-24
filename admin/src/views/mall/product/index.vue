<template>
  <div class="list-page">
    <QueryPage api="/admin/mall/products" :searchParam="searchForm" @reset="handleReset" ref="queryPageRef">
      <!-- 搜索条件 -->
      <template #searchConditions>
        <el-form :model="searchForm" :inline="true">
          <el-form-item label="关键字">
            <el-input v-model="searchForm.keyword" placeholder="商品编码 / 名称" clearable />
          </el-form-item>
          <el-form-item label="分类">
            <el-select v-model="searchForm.categoryId" placeholder="全部" clearable>
              <el-option v-for="item in categoryOptions" :key="item.id" :label="item.name" :value="item.id" />
            </el-select>
          </el-form-item>
          <el-form-item label="状态">
            <el-select v-model="searchForm.isActive" placeholder="全部">
              <el-option label="全部" value="" />
              <el-option label="上架" :value="true" />
              <el-option label="下架" :value="false" />
            </el-select>
          </el-form-item>
        </el-form>
      </template>

      <!-- 功能按钮 -->
      <template #headerButtons>
        <el-button type="primary" :icon="CirclePlus" @click="openProductDialog('新增')">新增商品</el-button>
      </template>

      <!-- 表格 -->
      <template #table="{ tableData }">
        <el-table :data="tableData" :fit="true" style="width: 100%" border>
          <el-table-column label="主图" width="90" align="center">
            <template #default="{ row }">
              <el-image
                v-if="row.mainImageUrl"
                :src="resolveFileUrl(row.mainImageUrl)"
                :preview-src-list="[resolveFileUrl(row.mainImageUrl)]"
                preview-teleported
                fit="cover"
                class="product-thumb"
              />
              <span v-else class="empty-image">-</span>
            </template>
          </el-table-column>
          <el-table-column prop="code" label="商品编码" min-width="140" show-overflow-tooltip />
          <el-table-column prop="name" label="商品名称" min-width="180" show-overflow-tooltip />
          <el-table-column label="分类" width="130" show-overflow-tooltip>
            <template #default="{ row }">{{ row.categoryName || '-' }}</template>
          </el-table-column>
          <el-table-column prop="description" label="商品描述" min-width="220" show-overflow-tooltip />
          <el-table-column prop="sortOrder" label="排序" width="80" align="center" />
          <el-table-column label="状态" width="100" align="center">
            <template #default="{ row }">
              <el-tag :type="row.isActive ? 'success' : 'info'">{{ row.isActive ? '上架' : '下架' }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column label="创建时间" width="180" align="center">
            <template #default="{ row }">{{ formatDate(row.createdAt) }}</template>
          </el-table-column>
          <el-table-column label="操作" align="center" width="300" fixed="right">
            <template #default="{ row }">
              <el-button type="primary" link :icon="Goods" @click="openSkuDialog(row)">规格</el-button>
              <el-button type="primary" link :icon="EditPen" @click="openProductDialog('编辑', row)">编辑</el-button>
              <el-button
                :type="row.isActive ? 'warning' : 'success'"
                link
                :icon="SwitchButton"
                @click="changeProductStatus(row)"
              >
                {{ row.isActive ? '下架' : '上架' }}
              </el-button>
              <el-button type="danger" link :icon="Delete" @click="deleteProduct(row)">删除</el-button>
            </template>
          </el-table-column>
        </el-table>
      </template>
    </QueryPage>

    <!-- 新增 / 编辑商品 -->
    <el-dialog v-model="productDialogVisible" :title="productDialogTitle" width="760px">
      <el-form :model="productForm" label-width="100px">
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="商品编码" required>
              <el-input v-model="productForm.code" placeholder="请输入商品编码" maxlength="50" show-word-limit />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="商品名称" required>
              <el-input v-model="productForm.name" placeholder="请输入商品名称" maxlength="100" show-word-limit />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="商品分类">
              <el-select v-model="productForm.categoryId" placeholder="请选择分类" clearable style="width: 100%">
                <el-option v-for="item in categoryOptions" :key="item.id" :label="item.name" :value="item.id" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="排序">
              <el-input-number v-model="productForm.sortOrder" :min="0" :max="9999" style="width: 100%" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="商品描述">
          <el-input
            v-model="productForm.description"
            type="textarea"
            :rows="3"
            placeholder="请输入商品描述"
            maxlength="500"
            show-word-limit
          />
        </el-form-item>
        <el-form-item label="商品主图">
          <UploadImg v-model:imageUrl="mainImageUrlModel" :width="'120px'" :height="'120px'">
            <template #tip>建议上传 1:1 图片，大小不超过 5MB</template>
          </UploadImg>
        </el-form-item>
        <el-form-item label="商品相册">
          <UploadImgs
            :file-list="albumFileList"
            :limit="9"
            :width="'100px'"
            :height="'100px'"
            @update:file-list="handleAlbumChange"
          >
            <template #tip>最多上传 9 张图片</template>
          </UploadImgs>
        </el-form-item>
        <el-form-item label="商品详情">
          <el-input
            v-model="productForm.detailHtml"
            type="textarea"
            :rows="6"
            placeholder="请输入商品详情，支持 HTML"
            maxlength="50000"
            show-word-limit
          />
        </el-form-item>
        <el-form-item v-if="productDialogType === '新增'" label="上架状态">
          <el-switch v-model="productForm.isActive" active-text="上架" inactive-text="下架" />
        </el-form-item>
      </el-form>
      <template #footer>
        <span class="dialog-footer">
          <el-button @click="productDialogVisible = false">取消</el-button>
          <el-button type="primary" :loading="submitting" @click="submitProduct">确定</el-button>
        </span>
      </template>
    </el-dialog>

    <!-- 规格维护 -->
    <el-dialog v-model="skuDialogVisible" :title="skuDialogTitle" width="960px">
      <div class="sku-header">
        <span class="sku-tip">库存与价格记录在规格上，全门店共享库存。</span>
        <el-button type="primary" :icon="CirclePlus" size="small" @click="openSkuForm('新增')">新增规格</el-button>
      </div>
      <el-table :data="skuList" border style="width: 100%" v-loading="skuLoading">
        <el-table-column prop="skuCode" label="规格编码" min-width="140" show-overflow-tooltip />
        <el-table-column prop="specName" label="规格名称" min-width="140" show-overflow-tooltip />
        <el-table-column label="价格" width="100" align="right">
          <template #default="{ row }">¥{{ Number(row.price).toFixed(2) }}</template>
        </el-table-column>
        <el-table-column prop="weightGram" label="重量(克)" width="100" align="center" />
        <el-table-column label="库存" width="90" align="center">
          <template #default="{ row }">
            <span :class="{ 'low-stock': isLowStock(row) }">{{ row.stock }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="warnStock" label="预警库存" width="100" align="center" />
        <el-table-column label="状态" width="90" align="center">
          <template #default="{ row }">
            <el-tag :type="row.isActive ? 'success' : 'info'">{{ row.isActive ? '启用' : '停用' }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="220" align="center" fixed="right">
          <template #default="{ row }">
            <el-button type="primary" link :icon="EditPen" @click="openSkuForm('编辑', row)">编辑</el-button>
            <el-button
              :type="row.isActive ? 'warning' : 'success'"
              link
              :icon="SwitchButton"
              @click="changeSkuStatus(row)"
            >
              {{ row.isActive ? '停用' : '启用' }}
            </el-button>
            <el-button type="danger" link :icon="Delete" @click="deleteSku(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-dialog>

    <!-- 新增 / 编辑规格 -->
    <el-dialog v-model="skuFormVisible" :title="skuFormTitle" width="520px" append-to-body>
      <el-form :model="skuForm" label-width="110px">
        <el-form-item label="规格编码" required>
          <el-input v-model="skuForm.skuCode" placeholder="请输入规格编码" maxlength="50" show-word-limit />
        </el-form-item>
        <el-form-item label="规格名称" required>
          <el-input v-model="skuForm.specName" placeholder="例如：500g 装" maxlength="100" show-word-limit />
        </el-form-item>
        <el-form-item label="价格(元)" required>
          <el-input-number v-model="skuForm.price" :precision="2" :min="0.01" :max="9999999" style="width: 100%" />
        </el-form-item>
        <el-form-item label="重量(克)" required>
          <el-input-number v-model="skuForm.weightGram" :min="1" :max="1000000" :step="100" style="width: 100%" />
        </el-form-item>
        <el-form-item label="库存" required>
          <el-input-number v-model="skuForm.stock" :min="0" :max="1000000" style="width: 100%" />
        </el-form-item>
        <el-form-item label="预警库存">
          <el-input-number v-model="skuForm.warnStock" :min="0" :max="1000000" style="width: 100%" />
        </el-form-item>
        <el-form-item label="启用状态">
          <el-switch v-model="skuForm.isActive" active-text="启用" inactive-text="停用" />
        </el-form-item>
      </el-form>
      <template #footer>
        <span class="dialog-footer">
          <el-button @click="skuFormVisible = false">取消</el-button>
          <el-button type="primary" :loading="skuSubmitting" @click="submitSku">确定</el-button>
        </span>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts" name="mallProduct">
import { ref, reactive, computed, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { CirclePlus, EditPen, Delete, SwitchButton, Goods } from '@element-plus/icons-vue'
import type { UploadUserFile } from 'element-plus'
import { mallProductApi } from '@/api/modules/mallProduct'
import { mallCategoryApi } from '@/api/modules/mallCategory'
import UploadImg from '@/components/Upload/Img.vue'
import UploadImgs from '@/components/Upload/Imgs.vue'
import QueryPage from '@/components/QueryPage/index.vue'
import { formatDate, resolveFileUrl } from '@/utils'

const queryPageRef = ref()

// 分类选项
const categoryOptions = ref<any[]>([])

const loadCategories = async () => {
  try {
    const result = await mallCategoryApi.getCategoryList({ page: 1, pageSize: 200 })
    categoryOptions.value = result.data?.list || []
  } catch {
    categoryOptions.value = []
  }
}

onMounted(loadCategories)

// 商品
const productDialogVisible = ref(false)
const productDialogTitle = ref('')
const productDialogType = ref('')
const currentProductId = ref('')
const submitting = ref(false)
const albumFileList = ref<UploadUserFile[]>([])
const apiOrigin = (import.meta.env.VITE_API_URL || 'http://localhost:5000/api').replace(/\/api\/?$/, '')

const searchForm = reactive({
  keyword: '',
  categoryId: '',
  isActive: '' as boolean | ''
})

const productForm = reactive({
  code: '',
  name: '',
  description: '',
  categoryId: '',
  mainImageUrl: '',
  albumImages: [] as string[],
  detailHtml: '',
  sortOrder: 0,
  isActive: true
})

const mainImageUrlModel = computed({
  get: () => resolveFileUrl(productForm.mainImageUrl),
  set: (value: string) => {
    productForm.mainImageUrl = value.startsWith(apiOrigin) ? value.slice(apiOrigin.length) : value
  }
})

const toRelativeUrl = (url: string) => (url.startsWith(apiOrigin) ? url.slice(apiOrigin.length) : url)

const handleAlbumChange = (files: UploadUserFile[]) => {
  albumFileList.value = files
  productForm.albumImages = files
    .map(file => file.url || '')
    .filter(url => !!url)
    .map(toRelativeUrl)
}

// 规格
const skuDialogVisible = ref(false)
const skuDialogTitle = ref('')
const currentSkuProductId = ref('')
const skuList = ref<any[]>([])
const skuLoading = ref(false)
const skuFormVisible = ref(false)
const skuFormTitle = ref('')
const skuFormType = ref('')
const currentSkuId = ref('')
const currentSkuStatus = ref(true)
const skuSubmitting = ref(false)

const skuForm = reactive({
  skuCode: '',
  specName: '',
  price: 1,
  weightGram: 100,
  stock: 0,
  warnStock: 0,
  isActive: true
})

const isLowStock = (row: any) => row.warnStock > 0 && row.stock <= row.warnStock

const handleReset = () => {
  Object.assign(searchForm, {
    keyword: '',
    categoryId: '',
    isActive: ''
  })
}

const resetProductForm = () => {
  Object.assign(productForm, {
    code: '',
    name: '',
    description: '',
    categoryId: '',
    mainImageUrl: '',
    albumImages: [],
    detailHtml: '',
    sortOrder: 0,
    isActive: true
  })
  albumFileList.value = []
}

const openProductDialog = (type: string, row?: any) => {
  productDialogTitle.value = `${type}商品`
  productDialogType.value = type

  if (type === '新增') {
    resetProductForm()
    currentProductId.value = ''
  } else if (row) {
    Object.assign(productForm, {
      code: row.code,
      name: row.name,
      description: row.description,
      categoryId: row.categoryId || '',
      mainImageUrl: row.mainImageUrl || '',
      albumImages: row.albumImages || [],
      detailHtml: row.detailHtml || '',
      sortOrder: row.sortOrder || 0,
      isActive: row.isActive
    })
    currentProductId.value = row.id
    albumFileList.value = (row.albumImages || []).map((url: string, index: number) => ({
      name: `图片${index + 1}`,
      url: resolveFileUrl(url)
    }))
  }

  productDialogVisible.value = true
}

const validateProductForm = () => {
  if (!productForm.code.trim()) {
    ElMessage.warning('请输入商品编码')
    return false
  }
  if (!productForm.name.trim()) {
    ElMessage.warning('请输入商品名称')
    return false
  }
  return true
}

const submitProduct = async () => {
  if (!validateProductForm()) return

  submitting.value = true
  try {
    const payload: any = {
      code: productForm.code.trim(),
      name: productForm.name.trim(),
      description: productForm.description.trim(),
      categoryId: productForm.categoryId || null,
      mainImageUrl: productForm.mainImageUrl || '',
      albumImages: productForm.albumImages,
      detailHtml: productForm.detailHtml,
      sortOrder: productForm.sortOrder
    }

    if (productDialogType.value === '新增') {
      await mallProductApi.addProduct({ ...payload, isActive: productForm.isActive })
      ElMessage.success('新增商品成功')
    } else {
      await mallProductApi.updateProduct(currentProductId.value, payload)
      ElMessage.success('更新商品成功')
    }

    productDialogVisible.value = false
    queryPageRef.value?.getTableList()
  } catch {
    // 错误提示已由请求拦截器统一处理
  } finally {
    submitting.value = false
  }
}

const changeProductStatus = async (row: any) => {
  const nextStatus = !row.isActive
  const actionText = nextStatus ? '上架' : '下架'
  try {
    await ElMessageBox.confirm(`确定要${actionText}商品“${row.name}”吗？`, `${actionText}商品`, {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })
    await mallProductApi.changeProductStatus(row.id, nextStatus)
    ElMessage.success(`${actionText}成功`)
    queryPageRef.value?.getTableList()
  } catch {
    // 取消操作或请求失败
  }
}

const deleteProduct = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定要删除商品“${row.name}”吗？`, '删除商品', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })
    await mallProductApi.deleteProduct(row.id)
    ElMessage.success('删除商品成功')
    queryPageRef.value?.getTableList()
  } catch {
    // 取消删除或请求失败
  }
}

// 规格操作
const openSkuDialog = async (row: any) => {
  skuDialogTitle.value = `规格维护 - ${row.name}`
  currentSkuProductId.value = row.id
  skuDialogVisible.value = true
  await loadSkuList()
}

const loadSkuList = async () => {
  skuLoading.value = true
  try {
    const { data } = await mallProductApi.getProduct(currentSkuProductId.value)
    skuList.value = data?.skus || []
  } catch {
    skuList.value = []
  } finally {
    skuLoading.value = false
  }
}

const resetSkuForm = () => {
  Object.assign(skuForm, {
    skuCode: '',
    specName: '',
    price: 1,
    weightGram: 100,
    stock: 0,
    warnStock: 0,
    isActive: true
  })
}

const openSkuForm = (type: string, row?: any) => {
  skuFormTitle.value = `${type}规格`
  skuFormType.value = type

  if (type === '新增') {
    resetSkuForm()
    currentSkuId.value = ''
  } else if (row) {
    Object.assign(skuForm, {
      skuCode: row.skuCode,
      specName: row.specName,
      price: Number(row.price),
      weightGram: row.weightGram,
      stock: row.stock,
      warnStock: row.warnStock,
      isActive: row.isActive
    })
    currentSkuId.value = row.id
    currentSkuStatus.value = row.isActive
  }

  skuFormVisible.value = true
}

const submitSku = async () => {
  if (!skuForm.skuCode.trim()) {
    ElMessage.warning('请输入规格编码')
    return
  }
  if (!skuForm.specName.trim()) {
    ElMessage.warning('请输入规格名称')
    return
  }

  skuSubmitting.value = true
  try {
    const payload = {
      skuCode: skuForm.skuCode.trim(),
      specName: skuForm.specName.trim(),
      price: skuForm.price,
      weightGram: skuForm.weightGram,
      stock: skuForm.stock,
      warnStock: skuForm.warnStock
    }

    if (skuFormType.value === '新增') {
      await mallProductApi.addSku(currentSkuProductId.value, { ...payload, isActive: skuForm.isActive })
      ElMessage.success('新增规格成功')
    } else {
      await mallProductApi.updateSku(currentSkuId.value, payload)
      if (skuForm.isActive !== currentSkuStatus.value) {
        await mallProductApi.changeSkuStatus(currentSkuId.value, skuForm.isActive)
      }
      ElMessage.success('更新规格成功')
    }

    skuFormVisible.value = false
    await loadSkuList()
  } catch {
    // 错误提示已由请求拦截器统一处理
  } finally {
    skuSubmitting.value = false
  }
}

const changeSkuStatus = async (row: any) => {
  const nextStatus = !row.isActive
  const actionText = nextStatus ? '启用' : '停用'
  try {
    await mallProductApi.changeSkuStatus(row.id, nextStatus)
    ElMessage.success(`${actionText}成功`)
    await loadSkuList()
  } catch {
    // 请求失败已提示
  }
}

const deleteSku = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定要删除规格“${row.specName}”吗？`, '删除规格', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })
    await mallProductApi.deleteSku(row.id)
    ElMessage.success('删除规格成功')
    await loadSkuList()
  } catch {
    // 取消删除或请求失败
  }
}
</script>

<style scoped lang="scss">
.sku-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
}

.sku-tip {
  color: var(--el-text-color-secondary);
  font-size: 13px;
}

.product-thumb {
  width: 52px;
  height: 52px;
  border-radius: 6px;
}

.empty-image {
  color: var(--el-text-color-placeholder);
}

.low-stock {
  color: var(--el-color-danger);
  font-weight: 600;
}
</style>

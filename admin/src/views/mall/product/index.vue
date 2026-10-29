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
              <el-option v-for="item in leafCategoryOptions" :key="item.id" :label="item.name" :value="item.id" />
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
        <el-button @click="openTagManager">管理标签</el-button>
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
          <el-table-column label="标签" min-width="180">
            <template #default="{ row }">
              <el-tag v-for="tag in row.tags || []" :key="tag.id" size="small" class="product-tag">
                {{ tag.name }}
              </el-tag>
              <span v-if="!row.tags?.length">-</span>
            </template>
          </el-table-column>
          <el-table-column prop="description" label="商品描述" min-width="220" show-overflow-tooltip />
          <el-table-column label="规格数" width="90" align="center">
            <template #default="{ row }">
              <el-tag size="small" type="info">{{ (row.skus || []).length }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column label="价格" width="120" align="right">
            <template #default="{ row }">
              <span v-if="minPrice(row) !== null">¥{{ minPrice(row)!.toFixed(2) }}</span>
              <span v-else>-</span>
            </template>
          </el-table-column>
          <el-table-column prop="sortOrder" label="排序" width="80" align="center" />
          <el-table-column label="首页精选" width="100" align="center">
            <template #default="{ row }">
              <el-switch v-model="row.isHot" @change="changeProductHot(row)" />
            </template>
          </el-table-column>
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
            <el-form-item label="商品分类" required>
              <el-select v-model="productForm.categoryId" placeholder="请选择末级分类" style="width: 100%">
                <el-option v-for="item in leafCategoryOptions" :key="item.id" :label="`${item.parentName || '未分组'} / ${item.name}`" :value="item.id" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="排序">
              <el-input-number v-model="productForm.sortOrder" :min="0" :max="9999" style="width: 100%" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="商品标签">
          <el-select v-model="productForm.tagIds" multiple clearable filterable placeholder="请选择商品标签" style="width: 100%">
            <el-option v-for="tag in activeTagOptions" :key="tag.id" :label="tag.name" :value="tag.id" />
          </el-select>
        </el-form-item>
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
          <div class="product-detail-editor">
            <ElementTiptap
              v-model:content="productForm.detailHtml"
              :extensions="productDetailExtensions"
              :locale="editorLocale"
              height="280px"
            />
            <div class="detail-editor-hint" :class="{ 'is-over-limit': productForm.detailHtml.length > 50000 }">
              HTML 长度 {{ productForm.detailHtml.length.toLocaleString() }} / 50,000 字符；图片请使用工具栏上传。
            </div>
          </div>
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

    <el-dialog v-model="tagDialogVisible" title="商品标签管理" width="680px">
      <div class="tag-toolbar">
        <el-button type="primary" :icon="CirclePlus" @click="openTagForm()">新增标签</el-button>
      </div>
      <el-table :data="tagOptions" border>
        <el-table-column prop="code" label="标签编码" min-width="140" />
        <el-table-column prop="name" label="标签名称" min-width="140" />
        <el-table-column label="颜色" width="100">
          <template #default="{ row }">
            <el-tag :type="row.color || 'success'" size="small">{{ row.name }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="状态" width="90">
          <template #default="{ row }">
            <el-tag :type="row.isActive ? 'success' : 'info'">{{ row.isActive ? '启用' : '停用' }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="190" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" @click="openTagForm(row)">编辑</el-button>
            <el-button link :type="row.isActive ? 'warning' : 'success'" @click="toggleTagStatus(row)">
              {{ row.isActive ? '停用' : '启用' }}
            </el-button>
            <el-button link type="danger" @click="deleteTag(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
      <template #footer>
        <el-button @click="tagDialogVisible = false">关闭</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="tagFormVisible" :title="editingTagId ? '编辑标签' : '新增标签'" width="480px">
      <el-form :model="tagForm" label-width="100px">
        <el-form-item label="标签编码" required>
          <el-input v-model="tagForm.code" maxlength="50" />
        </el-form-item>
        <el-form-item label="标签名称" required>
          <el-input v-model="tagForm.name" maxlength="50" />
        </el-form-item>
        <el-form-item label="颜色">
          <el-select v-model="tagForm.color" style="width: 100%">
            <el-option label="绿色" value="success" />
            <el-option label="橙色" value="warning" />
            <el-option label="蓝色" value="primary" />
            <el-option label="灰色" value="info" />
            <el-option label="红色" value="danger" />
          </el-select>
        </el-form-item>
        <el-form-item label="排序">
          <el-input-number v-model="tagForm.sortOrder" :min="0" :max="9999" />
        </el-form-item>
        <el-form-item label="启用">
          <el-switch v-model="tagForm.isActive" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="tagFormVisible = false">取消</el-button>
        <el-button type="primary" :loading="tagSubmitting" @click="submitTag">保存</el-button>
      </template>
    </el-dialog>

    <!-- 商品规格（SKU）维护 -->
    <el-dialog v-model="skuDialogVisible" :title="skuDialogTitle" width="1000px" top="6vh" @closed="refreshProductList">
      <div class="sku-main" v-loading="skuLoading">
        <!-- 规格维度：组合生成 SKU -->
        <div class="spec-section">
          <div class="panel-header">
            <span class="panel-title">规格维度</span>
            <div>
              <el-button size="small" :icon="Plus" @click="addDimension">添加维度</el-button>
              <el-button type="primary" size="small" @click="generateCombinations">生成组合</el-button>
            </div>
          </div>
          <div v-if="!specDimensions.length" class="spec-empty">
            不维护维度时，可直接用「添加默认规格」兜底，按单品单规格售卖。
          </div>
          <div v-for="(dimension, index) in specDimensions" :key="index" class="spec-row">
            <el-input
              v-model="dimension.name"
              class="spec-name-input"
              maxlength="20"
              placeholder="维度名，如 规格 / 产地"
            />
            <div class="spec-values">
              <el-tag
                v-for="(value, valueIndex) in dimension.values"
                :key="value"
                closable
                class="spec-value-tag"
                @close="removeSpecValue(index, valueIndex)"
              >
                {{ value }}
              </el-tag>
              <el-input
                v-model="dimension.input"
                class="spec-value-input"
                maxlength="30"
                placeholder="规格值，回车添加"
                @keyup.enter="addSpecValue(index)"
              />
              <el-button link type="primary" @click="addSpecValue(index)">添加</el-button>
            </div>
            <el-button link type="danger" :icon="Delete" @click="removeDimension(index)">删除维度</el-button>
          </div>
        </div>

        <!-- 待保存的组合 -->
        <div v-if="batchRows.length" class="combo-section">
          <div class="panel-header">
            <span class="panel-title">待保存组合（{{ batchRows.length }}）</span>
            <div>
              <el-button size="small" @click="batchRows = []">清空</el-button>
              <el-button type="primary" size="small" :loading="batchSaving" @click="saveBatchRows">保存全部</el-button>
            </div>
          </div>
          <div class="batch-bar">
            <span class="batch-label">批量设置</span>
            <el-input-number v-model="batchFill.price" :precision="2" :min="0" :max="9999999" controls-position="right" placeholder="销售价" />
            <el-input-number v-model="batchFill.marketPrice" :precision="2" :min="0" :max="9999999" controls-position="right" placeholder="市场价" />
            <el-input-number v-model="batchFill.weightGram" :min="0" :max="1000000" :step="100" controls-position="right" placeholder="重量(克)" />
            <el-input-number v-model="batchFill.stock" :min="0" :max="1000000" controls-position="right" placeholder="库存" />
            <el-button size="small" @click="applyBatchFill">应用到全部</el-button>
            <span class="form-tip">填 0 表示该项不修改</span>
          </div>
          <el-table :data="batchRows" border max-height="220" style="width: 100%">
            <el-table-column label="规格编码" width="150">
              <template #default="{ row }">
                <el-input v-model="row.skuCode" size="small" maxlength="50" />
              </template>
            </el-table-column>
            <el-table-column prop="specName" label="规格名称" min-width="180" show-overflow-tooltip />
            <el-table-column label="销售价" width="140">
              <template #default="{ row }">
                <el-input-number v-model="row.price" size="small" :precision="2" :min="0.01" :max="9999999" controls-position="right" />
              </template>
            </el-table-column>
            <el-table-column label="市场价" width="140">
              <template #default="{ row }">
                <el-input-number v-model="row.marketPrice" size="small" :precision="2" :min="0" :max="9999999" controls-position="right" />
              </template>
            </el-table-column>
            <el-table-column label="重量(克)" width="120">
              <template #default="{ row }">
                <el-input-number v-model="row.weightGram" size="small" :min="1" :max="1000000" :step="100" controls-position="right" />
              </template>
            </el-table-column>
            <el-table-column label="库存" width="120">
              <template #default="{ row }">
                <el-input-number v-model="row.stock" size="small" :min="0" :max="1000000" controls-position="right" />
              </template>
            </el-table-column>
            <el-table-column label="预警" width="110">
              <template #default="{ row }">
                <el-input-number v-model="row.warnStock" size="small" :min="0" :max="1000000" controls-position="right" />
              </template>
            </el-table-column>
            <el-table-column label="操作" width="70" align="center">
              <template #default="{ $index }">
                <el-button link type="danger" :icon="Delete" @click="batchRows.splice($index, 1)" />
              </template>
            </el-table-column>
          </el-table>
        </div>

        <div class="panel-header">
          <span class="panel-title">已保存规格（{{ skuList.length }}）</span>
          <div>
            <el-button size="small" @click="addDefaultSpec">添加默认规格</el-button>
            <el-button type="primary" size="small" :icon="Plus" @click="openSkuForm()">新增单个规格</el-button>
          </div>
        </div>
        <el-table :data="skuList" border style="width: 100%">
          <el-table-column prop="skuCode" label="规格编码" min-width="130" show-overflow-tooltip />
          <el-table-column prop="specName" label="规格名称" min-width="150" show-overflow-tooltip />
          <el-table-column label="销售价" width="110" align="right">
            <template #default="{ row }">¥{{ Number(row.price).toFixed(2) }}</template>
          </el-table-column>
          <el-table-column label="市场价" width="110" align="right">
            <template #default="{ row }">
              <span v-if="Number(row.marketPrice) > 0">¥{{ Number(row.marketPrice).toFixed(2) }}</span>
              <span v-else class="text-hint">-</span>
            </template>
          </el-table-column>
          <el-table-column prop="weightGram" label="重量(克)" width="90" align="center" />
          <el-table-column label="库存" width="80" align="center">
            <template #default="{ row }">
              <span :class="{ 'low-stock': isLowStock(row) }">{{ row.stock }}</span>
            </template>
          </el-table-column>
          <el-table-column prop="warnStock" label="预警" width="70" align="center" />
          <el-table-column label="状态" width="80" align="center">
            <template #default="{ row }">
              <el-tag :type="row.isActive ? 'success' : 'info'">{{ row.isActive ? '启用' : '停用' }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column label="操作" width="200" align="center" fixed="right">
            <template #default="{ row }">
              <el-button link type="primary" :icon="EditPen" @click="openSkuForm(row)">编辑</el-button>
              <el-button
                link
                :type="row.isActive ? 'warning' : 'success'"
                :icon="SwitchButton"
                @click="changeSkuStatus(row)"
              >
                {{ row.isActive ? '停用' : '启用' }}
              </el-button>
              <el-button link type="danger" :icon="Delete" @click="deleteSku(row)">删除</el-button>
            </template>
          </el-table-column>
          <template #empty>
            <el-empty description="该商品还没有规格：可定义维度后生成组合，或用「添加默认规格」兜底" :image-size="80" />
          </template>
        </el-table>
      </div>

      <template #footer>
        <span class="dialog-footer">
          <el-button @click="skuDialogVisible = false">关闭</el-button>
        </span>
      </template>
    </el-dialog>

    <!-- 新增 / 编辑规格 -->
    <el-dialog v-model="skuFormVisible" :title="skuFormTitle" width="520px">
      <el-form :model="skuForm" label-width="100px">
        <el-form-item label="规格名称" required>
          <el-input v-model="skuForm.specName" maxlength="100" placeholder="如 500g 装；单品单规格可填「默认规格」" />
        </el-form-item>
        <el-form-item label="规格编码" required>
          <el-input v-model="skuForm.skuCode" maxlength="50" placeholder="如 500G-01，全局唯一" />
        </el-form-item>
        <el-form-item label="销售价" required>
          <el-input-number v-model="skuForm.price" :precision="2" :min="0.01" :max="9999999" />
        </el-form-item>
        <el-form-item label="市场价">
          <el-input-number v-model="skuForm.marketPrice" :precision="2" :min="0" :max="9999999" />
          <span class="form-tip">0 表示不显示划线价</span>
        </el-form-item>
        <el-form-item label="重量(克)" required>
          <el-input-number v-model="skuForm.weightGram" :min="1" :max="1000000" :step="100" />
        </el-form-item>
        <el-form-item label="库存" required>
          <el-input-number v-model="skuForm.stock" :min="0" :max="1000000" />
        </el-form-item>
        <el-form-item label="预警库存">
          <el-input-number v-model="skuForm.warnStock" :min="0" :max="1000000" />
        </el-form-item>
        <el-form-item label="启用">
          <el-switch v-model="skuForm.isActive" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="skuFormVisible = false">取消</el-button>
        <el-button type="primary" :loading="skuFormSubmitting" @click="submitSkuForm">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts" name="mallProduct">
import { ref, reactive, computed, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { CirclePlus, EditPen, Delete, SwitchButton, Goods, Plus } from '@element-plus/icons-vue'
import type { UploadUserFile } from 'element-plus'
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
import { mallProductApi } from '@/api/modules/mallProduct'
import { mallTagApi } from '@/api/modules/mallTag'
import { mallCategoryApi } from '@/api/modules/mallCategory'
import { uploadImg } from '@/api/modules/upload'
import UploadImg from '@/components/Upload/Img.vue'
import UploadImgs from '@/components/Upload/Imgs.vue'
import QueryPage from '@/components/QueryPage/index.vue'
import { formatDate, resolveFileUrl } from '@/utils'

const uploadProductDetailImage = async (file: File): Promise<string> => {
  if (!['image/jpeg', 'image/png', 'image/gif'].includes(file.type)) {
    ElMessage.warning('图片仅支持 JPG、PNG 或 GIF 格式')
    throw new Error('商品详情仅支持上传图片')
  }
  if (file.size > 5 * 1024 * 1024) {
    ElMessage.warning('商品详情图片大小不能超过 5MB')
    throw new Error('商品详情图片超过 5MB')
  }

  const formData = new FormData()
  formData.append('file', file)
  const { data } = await uploadImg(formData)
  if (!data.fileUrl) {
    ElMessage.error('图片上传成功，但未返回图片地址')
    throw new Error('商品详情图片上传未返回地址')
  }
  return resolveFileUrl(data.fileUrl)
}

const imageOptions: Parameters<typeof TiptapImage.configure>[0] & {
  uploadRequest: (file: File) => Promise<string>
} = {
  uploadRequest: uploadProductDetailImage
}

const productDetailExtensions = [
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

const queryPageRef = ref()

// 分类选项
const categoryOptions = ref<any[]>([])
const leafCategoryOptions = computed(() => categoryOptions.value.filter(item => !item.hasChildren && item.isActive))
const tagOptions = ref<any[]>([])
const activeTagOptions = computed(() => tagOptions.value.filter(tag => tag.isActive))

const loadCategories = async () => {
  try {
    const result = await mallCategoryApi.getCategoryList({ page: 1, pageSize: 200 })
    categoryOptions.value = result.data?.list || []
    categoryOptions.value = categoryOptions.value.map((category: any) => ({
      ...category,
      hasChildren: categoryOptions.value.some((child: any) => child.parentId === category.id)
    }))
    const tagResult = await mallTagApi.getTags()
    tagOptions.value = tagResult.data || []
  } catch {
    ElMessage.error('加载分类或标签失败')
  }
}

onMounted(loadCategories)

// ---------------- 商品 ----------------
const productDialogVisible = ref(false)
const productDialogTitle = ref('')
const productDialogType = ref('')
const currentProductId = ref('')
const submitting = ref(false)
const albumFileList = ref<UploadUserFile[]>([])
const apiOrigin = (import.meta.env.VITE_API_URL || 'http://localhost:5000/api').replace(/\/api\/?$/, '')
const resolveDetailImageUrls = (html: string) => html.replace(
  /(<img\b[^>]*\bsrc=["'])([^"']+)(["'])/gi,
  (_match, prefix: string, url: string, suffix: string) => `${prefix}${resolveFileUrl(url)}${suffix}`
)
const normalizeDetailImageUrls = (html: string) => html.replace(
  /(<img\b[^>]*\bsrc=["'])([^"']+)(["'])/gi,
  (_match, prefix: string, url: string, suffix: string) => `${prefix}${toRelativeUrl(url)}${suffix}`
)

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
  isActive: true,
  tagIds: [] as string[]
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

// ---------------- 商品规格（SKU） ----------------
const skuDialogVisible = ref(false)
const skuDialogTitle = ref('')
const currentSkuProductId = ref('')
const currentProductCode = ref('')
const skuLoading = ref(false)

// 服务端返回的 SKU 列表
const skuList = ref<any[]>([])

// 新增 / 编辑规格表单
const skuFormVisible = ref(false)
const skuFormTitle = ref('')
const skuFormSubmitting = ref(false)
const editingSkuId = ref('')
const skuForm = reactive({
  skuCode: '',
  specName: '',
  price: 1,
  marketPrice: 0,
  weightGram: 100,
  stock: 0,
  warnStock: 0,
  isActive: true
})

// ---------- 打开规格对话框，加载商品详情 ----------
const refreshProductList = () => {
  queryPageRef.value?.getTableList?.()
}

const openSkuDialog = async (row: any) => {
  skuDialogTitle.value = `规格维护 - ${row.name}`
  currentSkuProductId.value = row.id
  currentProductCode.value = row.code || ''
  specDimensions.value = []
  batchRows.value = []
  batchFill.price = 0
  batchFill.marketPrice = 0
  batchFill.weightGram = 0
  batchFill.stock = 0
  skuDialogVisible.value = true
  await loadSkuDetail()
}

const loadSkuDetail = async () => {
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

// ---------- 规格维度与组合 ----------
interface SpecDimension {
  name: string
  values: string[]
  input: string
}

const specDimensions = ref<SpecDimension[]>([])
const batchRows = ref<any[]>([])
const batchSaving = ref(false)
const batchFill = reactive({ price: 0, marketPrice: 0, weightGram: 0, stock: 0 })

// 按商品编码生成不重复的规格编码
const nextSkuCode = (offset = 0) => {
  const prefix = (currentProductCode.value || 'SKU').slice(0, 40)
  const used = new Set([
    ...skuList.value.map((sku: any) => sku.skuCode),
    ...batchRows.value.map((row: any) => row.skuCode)
  ])
  let seq = offset + 1
  while (used.has(`${prefix}-${seq}`)) {
    seq += 1
  }
  return `${prefix}-${seq}`
}

const addDimension = () => {
  specDimensions.value.push({ name: '', values: [], input: '' })
}

const removeDimension = (index: number) => {
  specDimensions.value.splice(index, 1)
}

const addSpecValue = (index: number) => {
  const dimension = specDimensions.value[index]
  const value = dimension.input.trim()
  if (!value) {
    return
  }
  if (dimension.values.includes(value)) {
    ElMessage.warning('该规格值已存在')
    return
  }
  dimension.values.push(value)
  dimension.input = ''
}

const removeSpecValue = (dimensionIndex: number, valueIndex: number) => {
  specDimensions.value[dimensionIndex].values.splice(valueIndex, 1)
}

// 维度值做笛卡尔积，生成待保存组合
const generateCombinations = () => {
  const dimensions = specDimensions.value.filter(item => item.values.length)
  if (!dimensions.length) {
    ElMessage.warning('请先添加维度及至少一个规格值')
    return
  }
  let names = ['']
  dimensions.forEach((dimension) => {
    const next: string[] = []
    names.forEach((name) => {
      dimension.values.forEach((value) => {
        next.push(name ? `${name} / ${value}` : value)
      })
    })
    names = next
  })
  const existed = new Set([...skuList.value, ...batchRows.value].map((item: any) => item.specName))
  const added = names
    .filter(name => !existed.has(name))
    .map((name, index) => ({
      skuCode: nextSkuCode(index),
      specName: name,
      price: 1,
      marketPrice: 0,
      weightGram: 100,
      stock: 0,
      warnStock: 0
    }))
  if (!added.length) {
    ElMessage.warning('生成的组合已全部存在')
    return
  }
  batchRows.value = [...batchRows.value, ...added]
  ElMessage.success(`已生成 ${added.length} 条组合`)
}

const applyBatchFill = () => {
  const { price, marketPrice, weightGram, stock } = batchFill
  if (!price && !marketPrice && !weightGram && !stock) {
    ElMessage.warning('请先填写要批量设置的数值')
    return
  }
  batchRows.value.forEach((row) => {
    if (price > 0) row.price = price
    if (marketPrice > 0) row.marketPrice = marketPrice
    if (weightGram > 0) row.weightGram = weightGram
    if (stock > 0) row.stock = stock
  })
}

const saveBatchRows = async () => {
  const invalid = batchRows.value.find(
    (row: any) => !row.skuCode || !row.price || row.price <= 0 || !row.weightGram || row.weightGram <= 0
  )
  if (invalid) {
    ElMessage.warning(`请检查「${invalid.specName}」的规格编码、销售价和重量`)
    return
  }
  batchSaving.value = true
  let success = 0
  try {
    for (const row of batchRows.value) {
      await mallProductApi.addSku(currentSkuProductId.value, {
        skuCode: row.skuCode.trim(),
        specName: row.specName,
        price: row.price,
        marketPrice: row.marketPrice || 0,
        weightGram: row.weightGram,
        volumeCm3: 0,
        stock: row.stock,
        warnStock: row.warnStock || 0,
        isActive: true
      })
      success += 1
    }
    ElMessage.success(`已保存 ${success} 条规格`)
    batchRows.value = []
  } catch {
    ElMessage.error(`已保存 ${success} 条，其余保存失败`)
    batchRows.value = batchRows.value.slice(success)
  } finally {
    batchSaving.value = false
    await loadSkuDetail()
  }
}

// ---------- 新增 / 编辑规格 ----------
const openSkuForm = (row?: any) => {
  editingSkuId.value = row?.id || ''
  skuFormTitle.value = row ? '编辑规格' : '新增规格'
  Object.assign(skuForm, row
    ? {
        skuCode: row.skuCode || '',
        specName: row.specName || '',
        price: Number(row.price) || 0,
        marketPrice: Number(row.marketPrice) || 0,
        weightGram: row.weightGram || 100,
        stock: row.stock ?? 0,
        warnStock: row.warnStock ?? 0,
        isActive: row.isActive ?? true
      }
    : {
        skuCode: nextSkuCode(),
        specName: '',
        price: 1,
        marketPrice: 0,
        weightGram: 100,
        stock: 0,
        warnStock: 0,
        isActive: true
      })
  skuFormVisible.value = true
}

// 添加默认规格：没有维度、或单品单规格时兜底
const addDefaultSpec = () => {
  if (skuList.value.some((sku: any) => sku.specName === '默认规格')) {
    ElMessage.warning('该商品已有默认规格')
    return
  }
  openSkuForm()
  skuFormTitle.value = '添加默认规格'
  skuForm.specName = '默认规格'
}

const submitSkuForm = async () => {
  if (!skuForm.specName.trim()) {
    ElMessage.warning('请填写规格名称')
    return
  }
  if (!skuForm.price || skuForm.price <= 0) {
    ElMessage.warning('销售价必须大于 0')
    return
  }
  if (!skuForm.weightGram || skuForm.weightGram <= 0) {
    ElMessage.warning('重量必须大于 0')
    return
  }

  const payload = {
    skuCode: skuForm.skuCode.trim(),
    specName: skuForm.specName.trim(),
    price: skuForm.price,
    marketPrice: skuForm.marketPrice || 0,
    weightGram: skuForm.weightGram,
    volumeCm3: 0,
    stock: skuForm.stock,
    warnStock: skuForm.warnStock,
    isActive: skuForm.isActive
  }

  skuFormSubmitting.value = true
  try {
    if (editingSkuId.value) {
      await mallProductApi.updateSku(editingSkuId.value, payload)
      ElMessage.success('规格已更新')
    } else {
      await mallProductApi.addSku(currentSkuProductId.value, payload)
      ElMessage.success('规格已新增')
    }
    skuFormVisible.value = false
    await loadSkuDetail()
  } catch {
    // 请求拦截器会提示
  } finally {
    skuFormSubmitting.value = false
  }
}

// ---------- 规格状态切换 / 删除 ----------
const changeSkuStatus = async (row: any) => {
  const nextStatus = !row.isActive
  const actionText = nextStatus ? '启用' : '停用'
  try {
    await mallProductApi.changeSkuStatus(row.id, nextStatus)
    ElMessage.success(`${actionText}成功`)
    await loadSkuDetail()
  } catch {
    // 已提示
  }
}

const deleteSku = async (row: any) => {
  try {
    const display = row.specName || row.skuCode
    await ElMessageBox.confirm(`确定要删除规格"${display}"吗？`, '删除规格', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })
    await mallProductApi.deleteSku(row.id)
    ElMessage.success('删除规格成功')
    await loadSkuDetail()
  } catch {
    // 已取消或失败
  }
}

// ---------------- 商品 CRUD ----------------
const handleReset = () => {
  Object.assign(searchForm, {
    keyword: '',
    categoryId: '',
    isActive: ''
  })
}

const tagDialogVisible = ref(false)
const tagFormVisible = ref(false)
const tagSubmitting = ref(false)
const editingTagId = ref('')
const tagForm = reactive({
  code: '',
  name: '',
  color: 'success',
  sortOrder: 0,
  isActive: true
})

const openTagManager = async () => {
  await loadCategories()
  tagDialogVisible.value = true
}

const openTagForm = (tag?: any) => {
  editingTagId.value = tag?.id || ''
  Object.assign(tagForm, tag
    ? { code: tag.code, name: tag.name, color: tag.color || 'success', sortOrder: tag.sortOrder, isActive: tag.isActive }
    : { code: '', name: '', color: 'success', sortOrder: tagOptions.value.length, isActive: true })
  tagFormVisible.value = true
}

const submitTag = async () => {
  if (!tagForm.code.trim() || !tagForm.name.trim()) {
    ElMessage.warning('请填写标签编码和名称')
    return
  }
  tagSubmitting.value = true
  try {
    const payload = { ...tagForm, code: tagForm.code.trim(), name: tagForm.name.trim() }
    if (editingTagId.value) await mallTagApi.updateTag(editingTagId.value, payload)
    else await mallTagApi.addTag(payload)
    ElMessage.success('标签已保存')
    tagFormVisible.value = false
    await loadCategories()
  } catch {
    // 请求拦截器会提示
  } finally {
    tagSubmitting.value = false
  }
}

const toggleTagStatus = async (tag: any) => {
  try {
    await mallTagApi.changeTagStatus(tag.id, !tag.isActive)
    await loadCategories()
  } catch {
    // 请求拦截器会提示
  }
}

const deleteTag = async (tag: any) => {
  try {
    await ElMessageBox.confirm(`确定删除标签“${tag.name}”吗？`, '删除标签')
    await mallTagApi.deleteTag(tag.id)
    ElMessage.success('标签已删除')
    await loadCategories()
  } catch {
    // 用户取消或请求拦截器已提示
  }
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
    isActive: true,
    tagIds: []
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
      detailHtml: resolveDetailImageUrls(row.detailHtml || ''),
      sortOrder: row.sortOrder || 0,
      isActive: row.isActive,
      tagIds: (row.tags || []).map((tag: any) => tag.id)
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
  if (!productForm.categoryId) {
    ElMessage.warning('请选择商品末级分类')
    return false
  }
  return true
}

const submitProduct = async () => {
  if (!validateProductForm()) return
  if (productForm.detailHtml.length > 50000) {
    ElMessage.warning('商品详情 HTML 内容不能超过 50,000 字符')
    return
  }

  submitting.value = true
  try {
    const payload: any = {
      code: productForm.code.trim(),
      name: productForm.name.trim(),
      description: productForm.description.trim(),
      categoryId: productForm.categoryId || null,
      mainImageUrl: productForm.mainImageUrl || '',
      albumImages: productForm.albumImages,
      detailHtml: normalizeDetailImageUrls(productForm.detailHtml),
      sortOrder: productForm.sortOrder,
      tagIds: productForm.tagIds
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
    // 请求拦截器会提示
  } finally {
    submitting.value = false
  }
}

// 列表价格取启用规格中的最低价
const minPrice = (row: any): number | null => {
  const prices = (row.skus || [])
    .map((sku: any) => Number(sku.price))
    .filter((price: number) => price > 0)
  return prices.length ? Math.min(...prices) : null
}

const changeProductStatus = async (row: any) => {
  const nextStatus = !row.isActive
  const actionText = nextStatus ? '上架' : '下架'
  try {
    await ElMessageBox.confirm(`确定要${actionText}商品"${row.name}"吗？`, `${actionText}商品`, {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })
    await mallProductApi.changeProductStatus(row.id, nextStatus)
    ElMessage.success(`${actionText}成功`)
    queryPageRef.value?.getTableList()
  } catch {
    // 已取消或失败
  }
}

const changeProductHot = async (row: any) => {
  const next = row.isHot
  try {
    await mallProductApi.changeProductHot(row.id, next)
    ElMessage.success(next ? '已加入首页精选' : '已移出首页精选')
  } catch {
    row.isHot = !next
    ElMessage.error('更新首页精选状态失败')
  }
}

const deleteProduct = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定要删除商品"${row.name}"吗？`, '删除商品', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })
    await mallProductApi.deleteProduct(row.id)
    ElMessage.success('删除商品成功')
    queryPageRef.value?.getTableList()
  } catch {
    // 已取消或失败
  }
}

const isLowStock = (row: any) => row.warnStock > 0 && row.stock <= row.warnStock
</script>

<style scoped lang="scss">
.product-thumb {
  width: 52px;
  height: 52px;
  border-radius: 6px;
}

.product-tag {
  margin: 2px 4px 2px 0;
}

.tag-toolbar {
  margin-bottom: 12px;
}

.empty-image {
  color: var(--el-text-color-placeholder);
}

.low-stock {
  color: var(--el-color-danger);
  font-weight: 600;
}

.sku-main {
  min-height: 300px;
}

.spec-section {
  margin-bottom: 16px;
  padding: 12px;
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 6px;
}

.spec-empty {
  padding: 4px 0 8px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.spec-row {
  display: flex;
  align-items: flex-start;
  gap: 12px;
  padding: 10px 0;
  border-top: 1px dashed var(--el-border-color-lighter);
}

.spec-name-input {
  width: 150px;
  flex-shrink: 0;
}

.spec-values {
  flex: 1;
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
}

.spec-value-tag {
  margin: 0;
}

.spec-value-input {
  width: 160px;
}

.combo-section {
  margin-bottom: 16px;
  padding: 12px;
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 6px;
}

.batch-bar {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
  margin-bottom: 10px;
  padding: 8px 10px;
  background: var(--el-fill-color-light);
  border-radius: 4px;
}

.batch-label {
  font-size: 13px;
  color: var(--el-text-color-primary);
  font-weight: 600;
}

.batch-bar .el-input-number {
  width: 130px;
}

.panel-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
  padding-bottom: 8px;
  border-bottom: 1px solid var(--el-border-color-lighter);
}

.panel-title {
  font-weight: 600;
  font-size: 14px;
}

.form-tip {
  margin-left: 10px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.text-hint {
  color: var(--el-text-color-secondary);
}

.product-detail-editor {
  width: 100%;
}

.product-detail-editor :deep(.el-tiptap-editor) {
  width: 100%;
}

.detail-editor-hint {
  margin-top: 6px;
  color: var(--el-text-color-secondary);
  font-size: 12px;
}

.detail-editor-hint.is-over-limit {
  color: var(--el-color-danger);
}
</style>
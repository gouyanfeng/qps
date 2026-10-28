<template>
  <div class="list-page">
    <QueryPage api="/admin/mall/promotions" :searchParam="searchForm" @reset="handleReset" ref="queryPageRef">
      <template #searchConditions>
        <el-form :model="searchForm" :inline="true">
          <el-form-item label="状态">
            <el-select v-model="searchForm.isActive" placeholder="全部">
              <el-option label="全部" value="" />
              <el-option label="启用" :value="true" />
              <el-option label="停用" :value="false" />
            </el-select>
          </el-form-item>
          <el-form-item label="关键字">
            <el-input v-model="searchForm.keyword" placeholder="促销名称" clearable />
          </el-form-item>
        </el-form>
      </template>

      <template #headerButtons>
        <el-button type="primary" :icon="CirclePlus" @click="openDialog('新增')">新增促销</el-button>
      </template>

      <template #table="{ tableData }">
        <el-table :data="tableData" :fit="true" style="width: 100%" border>
          <el-table-column prop="name" label="促销名称" min-width="180" show-overflow-tooltip />
          <el-table-column label="类型" width="100" align="center">
            <template #default="{ row }">{{ getPromotionTypeName(row) }}</template>
          </el-table-column>
          <el-table-column label="规则" min-width="220" show-overflow-tooltip>
            <template #default="{ row }">
              {{ getPromotionRule(row) }}
            </template>
          </el-table-column>
          <el-table-column label="时间窗口" width="240">
            <template #default="{ row }">
              <span style="font-size: 12px; color: #666">
                {{ formatDate(row.startDate) }} ~ {{ formatDate(row.endDate) }}
              </span>
            </template>
          </el-table-column>
          <el-table-column label="范围" width="120" align="center">
            <template #default="{ row }">{{ scopeMap[row.scopeType] || row.scopeType }}</template>
          </el-table-column>
          <el-table-column label="状态" width="80" align="center">
            <template #default="{ row }">
              <el-tag :type="row.isActive ? 'success' : 'info'">{{ row.isActive ? '启用' : '停用' }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column label="操作" align="center" width="200" fixed="right">
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

    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="860px" top="3vh" class="promotion-dialog">
      <el-form :model="form" label-width="110px" class="promotion-form">
        <section class="form-section">
          <div class="section-title">活动信息与适用范围</div>
        <el-row :gutter="16">
          <el-col :span="24">
            <el-form-item label="促销名称" required>
              <el-input v-model="form.name" maxlength="50" show-word-limit />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="活动说明">
          <el-input v-model="form.description" maxlength="200" show-word-limit />
        </el-form-item>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="促销类型">
              <el-select v-model="form.promotionType" style="width: 100%" @change="handleTypeChange">
                <el-option label="满减" :value="0" />
                <el-option label="折扣" :value="1" />
                <el-option label="买赠" :value="2" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="适用范围">
              <el-select v-model="form.scopeType" style="width: 100%" @change="handleScopeTypeChange">
                <el-option label="全场商品" value="Global" />
                <el-option label="指定分类" value="Category" />
                <el-option label="指定商品" value="Product" />
                <el-option label="指定规格" value="Sku" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item v-if="form.scopeType !== 'Global'" label="适用对象" required>
          <el-select v-model="form.scopeTargetIds" multiple filterable clearable style="width: 100%" placeholder="请选择适用对象">
            <el-option v-for="item in scopeTargetOptions" :key="item.id" :label="item.name" :value="item.id" />
          </el-select>
        </el-form-item>
        </section>

        <section class="form-section">
        <div class="section-title">促销规则</div>

        <template v-if="form.promotionType === 0">
          <el-row :gutter="16">
            <el-col :span="12">
              <el-form-item label="满额门槛" required>
                <el-input-number v-model="form.thresholdAmount" :min="0.01" :precision="2" style="width: 100%" />
              </el-form-item>
            </el-col>
            <el-col :span="12">
              <el-form-item label="减免金额" required>
                <el-input-number v-model="form.discountAmount" :min="0.01" :precision="2" style="width: 100%" />
              </el-form-item>
            </el-col>
          </el-row>
        </template>
        <template v-if="form.promotionType === 1">
          <el-form-item label="折扣比例" required>
            <el-slider v-model="form.discountPercent" :min="0.1" :max="1" :step="0.01" show-stops :marks="{ 0.5: '5折', 0.7: '7折', 0.9: '9折' }" />
            <span style="font-size: 12px; color: #999">当前 {{ (form.discountPercent * 10).toFixed(1) }} 折</span>
          </el-form-item>
        </template>
        <template v-if="form.promotionType === 2">
          <el-row :gutter="16">
            <el-col :span="12">
              <el-form-item label="购买商品" required>
                <el-select v-model="form.buyProductId" filterable style="width: 100%">
                  <el-option v-for="product in productOptions" :key="product.id" :label="product.name" :value="product.id" />
                </el-select>
              </el-form-item>
            </el-col>
            <el-col :span="12">
              <el-form-item label="购买数量" required>
                <el-input-number v-model="form.buyQuantity" :min="1" :max="99" style="width: 100%" />
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="16">
            <el-col :span="12">
              <el-form-item label="赠送规格" required>
                <el-select v-model="form.giftSkuId" filterable style="width: 100%">
                  <el-option v-for="sku in skuOptions" :key="sku.id" :label="`${sku.productName} / ${sku.specName}`" :value="sku.id" />
                </el-select>
              </el-form-item>
            </el-col>
            <el-col :span="12">
              <el-form-item label="赠送数量" required>
                <el-input-number v-model="form.giftQuantity" :min="1" :max="99" style="width: 100%" />
              </el-form-item>
            </el-col>
          </el-row>
        </template>
        </section>

        <section class="form-section">
        <div class="section-title">活动时间与其他设置</div>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="开始时间">
              <el-date-picker v-model="form.startDate" type="datetime" placeholder="开始时间" style="width: 100%" value-format="YYYY-MM-DDTHH:mm:ssZ" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="结束时间">
              <el-date-picker v-model="form.endDate" type="datetime" placeholder="结束时间" style="width: 100%" value-format="YYYY-MM-DDTHH:mm:ssZ" />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="优先级">
              <el-input-number v-model="form.priority" :min="0" :max="999" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="每人限用">
              <el-input-number v-model="form.maxUsagePerUser" :min="0" :max="99999" style="width: 100%" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="允许叠加">
              <el-switch v-model="form.combinable" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="启用状态">
              <el-switch v-model="form.isActive" active-text="启用" inactive-text="停用" />
            </el-form-item>
          </el-col>
        </el-row>
        </section>
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

<script setup lang="ts" name="mallPromotion">
import { ref, reactive, computed, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { CirclePlus, EditPen, Delete } from '@element-plus/icons-vue'
import { mallPromotionApi } from '@/api/modules/mallPromotion'
import { mallProductApi } from '@/api/modules/mallProduct'
import { mallCategoryApi } from '@/api/modules/mallCategory'
import QueryPage from '@/components/QueryPage/index.vue'
import { formatDate } from '@/utils'

const scopeMap: Record<string, string> = {
  Global: '全场',
  Category: '指定分类',
  Product: '指定商品',
  Sku: '指定规格'
}

const queryPageRef = ref()
const dialogVisible = ref(false)
const submitting = ref(false)
const dialogTitle = ref('')
const dialogType = ref('')
const currentId = ref('')
const productOptions = ref<any[]>([])
const categoryOptions = ref<any[]>([])
const skuOptions = ref<any[]>([])

const searchForm = reactive({
  isActive: '' as boolean | '',
  keyword: ''
})

const form = reactive({
  name: '',
  description: '',
  promotionType: 0,
  scopeType: 'Global',
  scopeTargetIds: [] as string[],
  thresholdAmount: 100,
  discountAmount: 20,
  discountPercent: 0.8,
  buyQuantity: 2,
  giftQuantity: 1,
  buyProductId: '',
  giftSkuId: '',
  startDate: '',
  endDate: '',
  priority: 0,
  combinable: false,
  maxUsagePerUser: 0,
  maxUsagePerOrder: 0,
  isActive: true
})

const scopeTargetOptions = computed(() => {
  if (form.scopeType === 'Category') return categoryOptions.value.map(item => ({
    id: item.id,
    name: item.parentName ? `${item.parentName} / ${item.name}` : item.name
  }))
  if (form.scopeType === 'Product') return productOptions.value.map(item => ({ id: item.id, name: item.name }))
  if (form.scopeType === 'Sku') return skuOptions.value.map(item => ({
    id: item.id,
    name: `${item.productName} / ${item.specName}`
  }))
  return []
})

const parseJson = (value: string | null | undefined): Record<string, any> | null => {
  try {
    return JSON.parse(value || '{}')
  } catch (error) {
    console.error('解析促销配置失败', error)
    return null
  }
}

const getPromotionTypeName = (promotion: any) => {
  if (promotion.actionType === 'DiscountAmount') return '满减'
  if (promotion.actionType === 'DiscountRate') return '折扣'
  if (promotion.actionType === 'Gift') return '买赠'
  return promotion.actionType || '其他'
}

const getPromotionRule = (promotion: any) => {
  const trigger = parseJson(promotion.triggerConfigJson)
  const action = parseJson(promotion.actionConfigJson)
  if (!trigger || !action) return '促销规则配置异常'
  if (promotion.actionType === 'DiscountAmount') {
    return `满 ¥${Number(trigger.minAmount || 0).toFixed(2)} 减 ¥${Number(action.amount || 0).toFixed(2)}`
  }
  if (promotion.actionType === 'DiscountRate') {
    return `满 ¥${Number(trigger.minAmount || 0).toFixed(2)} 打 ${(Number(action.rate || 1) * 10).toFixed(1)} 折`
  }
  if (promotion.actionType === 'Gift') {
    return `买指定商品送 ${Number(action.quantity || 1)} 件赠品`
  }
  return promotion.description || '-'
}

const loadOptions = async () => {
  try {
    const [products, categories] = await Promise.all([
      mallProductApi.getProductList({ page: 1, pageSize: 1000, isActive: true }),
      mallCategoryApi.getCategoryList({ page: 1, pageSize: 1000, isActive: true })
    ])
    productOptions.value = products.data?.list || products.data?.items || []
    categoryOptions.value = categories.data?.list || categories.data?.items || []
    skuOptions.value = productOptions.value.flatMap((product: any) =>
      (product.skus || []).map((sku: any) => ({ ...sku, productName: product.name }))
    )
  } catch (error) {
    console.error('加载促销适用商品失败', error)
    ElMessage.error('加载促销适用商品失败')
  }
}

onMounted(loadOptions)

const handleReset = () => {
  searchForm.isActive = ''; searchForm.keyword = ''
}

const resetForm = () => {
  form.name = ''; form.description = ''; form.promotionType = 0; form.scopeType = 'Global'
  form.scopeTargetIds = []
  form.thresholdAmount = 100; form.discountAmount = 20
  form.discountPercent = 0.8; form.buyQuantity = 2; form.giftQuantity = 1
  form.buyProductId = ''; form.giftSkuId = ''
  form.startDate = ''; form.endDate = ''; form.priority = 0
  form.combinable = false; form.maxUsagePerUser = 0; form.maxUsagePerOrder = 0; form.isActive = true
}

const handleTypeChange = () => {
  if (form.promotionType === 0) { form.thresholdAmount = 100; form.discountAmount = 20 }
  if (form.promotionType === 1) { form.discountPercent = 0.8 }
  if (form.promotionType === 2) { form.buyQuantity = 2; form.giftQuantity = 1 }
}

const handleScopeTypeChange = () => {
  form.scopeTargetIds = []
}

const openDialog = (type: string, row?: any) => {
  dialogTitle.value = type; dialogType.value = type
  if (type === '新增') {
    resetForm(); currentId.value = ''
  } else if (row) {
    const trigger = parseJson(row.triggerConfigJson)
    const action = parseJson(row.actionConfigJson)
    if (!trigger || !action) {
      ElMessage.error('该促销规则配置异常，无法编辑')
      return
    }
    const promotionType = row.actionType === 'DiscountAmount' ? 0
      : row.actionType === 'DiscountRate' ? 1 : row.actionType === 'Gift' ? 2 : 0
    Object.assign(form, {
      name: row.name,
      description: row.description || '',
      promotionType,
      scopeType: row.scopeType || 'Global',
      scopeTargetIds: (row.scopes || []).map((scope: any) => scope.scopeTargetId),
      thresholdAmount: trigger.minAmount || 100,
      discountAmount: action.amount || 20,
      discountPercent: action.rate || 0.8,
      buyQuantity: trigger.minQuantity || 1,
      giftQuantity: action.quantity || 1,
      buyProductId: trigger.productIds?.[0] || '',
      giftSkuId: action.skuId || '',
      startDate: row.startDate, endDate: row.endDate,
      priority: row.priority || 0,
      combinable: row.combinable || false,
      maxUsagePerUser: row.maxUsagePerUser || 0,
      maxUsagePerOrder: row.maxUsagePerOrder || 0,
      isActive: row.isActive
    })
    currentId.value = row.id
  }
  dialogVisible.value = true
}

const submitForm = async () => {
  if (!form.name.trim()) { ElMessage.warning('请输入促销名称'); return }
  if (form.scopeType !== 'Global' && !form.scopeTargetIds.length) {
    ElMessage.warning('请选择促销适用对象')
    return
  }
  if (form.promotionType === 0 && form.discountAmount > form.thresholdAmount) {
    ElMessage.warning('减免金额不能大于满额门槛')
    return
  }
  if (form.promotionType === 2 && (!form.buyProductId || !form.giftSkuId)) {
    ElMessage.warning('请选择购买商品和赠送规格')
    return
  }

  submitting.value = true
  try {
    const promotionConfig = {
      triggerType: form.promotionType === 2 ? 'BuyProduct' : 'FullAmount',
      triggerConfigJson: JSON.stringify(form.promotionType === 2
        ? { productIds: [form.buyProductId], minQuantity: form.buyQuantity }
        : { minAmount: form.promotionType === 0 ? form.thresholdAmount : 0.01 }),
      actionType: form.promotionType === 0 ? 'DiscountAmount' : form.promotionType === 1 ? 'DiscountRate' : 'Gift',
      actionConfigJson: JSON.stringify(form.promotionType === 0
        ? { amount: form.discountAmount }
        : form.promotionType === 1
          ? { rate: form.discountPercent }
          : { skuId: form.giftSkuId, quantity: form.giftQuantity })
    }
    const scopeType = form.scopeType
    const scopeTargetType = scopeType
    const payload = {
      name: form.name.trim(),
      description: form.description.trim(),
      ...promotionConfig,
      scopeType,
      scopes: scopeType === 'Global'
        ? []
        : form.scopeTargetIds.map(scopeTargetId => ({ scopeTargetType, scopeTargetId })),
      priority: form.priority,
      combinable: form.combinable,
      maxUsagePerUser: form.maxUsagePerUser,
      maxUsagePerOrder: form.maxUsagePerOrder,
      startDate: form.startDate || null,
      endDate: form.endDate || null,
      isActive: form.isActive
    }
    if (dialogType.value === '新增') {
      await mallPromotionApi.add(payload)
      ElMessage.success('新增成功')
    } else {
      await mallPromotionApi.update(currentId.value, payload)
      ElMessage.success('更新成功')
    }
    dialogVisible.value = false
    queryPageRef.value?.getTableList()
  } catch {} finally { submitting.value = false }
}

const changeStatus = async (row: any) => {
  const next = !row.isActive
  try {
    await ElMessageBox.confirm(`确定要${next ? '启用' : '停用'}该促销吗？`, '状态变更', { type: 'warning' })
    await mallPromotionApi.changeStatus(row.id, next)
    ElMessage.success(`已${next ? '启用' : '停用'}`)
    queryPageRef.value?.getTableList()
  } catch {}
}

const remove = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定要删除促销“${row.name}”吗？`, '删除', { type: 'warning' })
    await mallPromotionApi.remove(row.id)
    ElMessage.success('删除成功')
    queryPageRef.value?.getTableList()
  } catch {}
}
</script>

<style scoped lang="scss">
.promotion-form {
  .form-section {
    padding: 18px 20px 4px;
    border: 1px solid var(--el-border-color-lighter);
    border-radius: 8px;
    background: var(--el-fill-color-blank);

    & + .form-section {
      margin-top: 18px;
    }
  }

  .section-title {
    margin-bottom: 18px;
    padding-left: 10px;
    border-left: 4px solid var(--el-color-primary);
    color: var(--el-text-color-primary);
    font-size: 15px;
    font-weight: 600;
    line-height: 20px;
  }
}

@media (max-width: 900px) {
  .promotion-form {
    .form-section {
      padding-right: 12px;
      padding-left: 12px;
    }
  }
}
</style>
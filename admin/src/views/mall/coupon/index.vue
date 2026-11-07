<template>
  <div class="list-page">
    <QueryPage api="/admin/mall/coupons" :searchParam="searchForm" @reset="handleReset" ref="queryPageRef">
      <template #searchConditions>
        <el-form :model="searchForm" :inline="true">
          <el-form-item label="券类型">
            <el-select v-model="searchForm.couponType" placeholder="全部" clearable>
              <el-option label="满减券" value="fixed" />
              <el-option label="折扣券" value="percent" />
            </el-select>
          </el-form-item>
          <el-form-item label="状态">
            <el-select v-model="searchForm.isActive" placeholder="全部">
              <el-option label="全部" value="" />
              <el-option label="启用" :value="true" />
              <el-option label="停用" :value="false" />
            </el-select>
          </el-form-item>
          <el-form-item label="关键字">
            <el-input v-model="searchForm.keyword" placeholder="券名称 / 券码" clearable />
          </el-form-item>
        </el-form>
      </template>

      <template #headerButtons>
        <el-button type="primary" :icon="CirclePlus" @click="openDialog('新增')">新增优惠券</el-button>
      </template>

      <template #table="{ tableData }">
        <el-table :data="tableData" :fit="true" style="width: 100%" border>
          <el-table-column label="优惠券" min-width="190">
            <template #default="{ row }">
              <div class="coupon-name">{{ row.name }}</div>
              <div class="coupon-code">{{ row.code }}</div>
            </template>
          </el-table-column>
          <el-table-column label="类型" width="100" align="center">
            <template #default="{ row }">
              <el-tag :type="isPercentCoupon(row.couponType) ? 'warning' : 'primary'" effect="plain">
                {{ couponTypeLabel(row.couponType) }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column label="优惠内容" width="150">
            <template #default="{ row }">
              <span class="coupon-value">{{ couponValueText(row) }}</span>
            </template>
          </el-table-column>
          <el-table-column label="使用门槛" width="120" align="center">
            <template #default="{ row }">
              <span v-if="row.thresholdAmount > 0">满 ¥{{ formatMoney(row.thresholdAmount) }}</span>
              <span v-else>无门槛</span>
            </template>
          </el-table-column>
          <el-table-column label="库存" width="110" align="center">
            <template #default="{ row }">
              <span :class="{ 'stock-empty': row.remainCount === 0 }">{{ row.remainCount }} / {{ row.totalCount }}</span>
            </template>
          </el-table-column>
          <el-table-column label="每人限领" width="90" align="center">
            <template #default="{ row }">{{ row.perLimit }} 张</template>
          </el-table-column>
          <el-table-column label="有效期" width="250">
            <template #default="{ row }">
              <span class="coupon-time">
                {{ row.startAt ? formatDate(row.startAt) : '不限' }} ~ {{ row.endAt ? formatDate(row.endAt) : '不限' }}
              </span>
              <div class="coupon-valid-days">领取后 {{ row.validDays }} 天有效</div>
            </template>
          </el-table-column>
          <el-table-column label="适用范围" width="120">
            <template #default="{ row }">{{ scopeText(row) }}</template>
          </el-table-column>
          <el-table-column label="状态" width="90" align="center">
            <template #default="{ row }">
              <el-tag :type="row.isActive ? 'success' : 'info'">{{ row.isActive ? '启用' : '停用' }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column
            label="操作"
            align="center"
            width="250"
            fixed="right"
            class-name="actions-column"
            header-class-name="actions-column"
          >
            <template #default="{ row }">
              <el-button type="primary" link :icon="EditPen" @click="openDialog('编辑', row)">编辑</el-button>
              <el-button :type="row.isActive ? 'warning' : 'success'" link @click="changeStatus(row)">
                {{ row.isActive ? '停用' : '启用' }}
              </el-button>
              <el-button type="primary" link @click="replenish(row)">补库存</el-button>
              <el-button type="danger" link :icon="Delete" @click="remove(row)">删除</el-button>
            </template>
          </el-table-column>
        </el-table>
      </template>
    </QueryPage>

    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="900px" top="4vh" destroy-on-close class="coupon-dialog">
      <el-form :model="form" label-width="110px">
        <section class="form-section">
          <div class="section-title">券基础信息</div>
          <el-row :gutter="16">
            <el-col :span="12">
              <el-form-item label="券名称" required>
                <el-input v-model="form.name" maxlength="50" show-word-limit placeholder="请输入券名称" />
              </el-form-item>
            </el-col>
            <el-col :span="12">
              <el-form-item label="券码" :required="dialogType === '新增'">
                <el-input
                  v-model="form.code"
                  maxlength="50"
                  :disabled="dialogType === '编辑'"
                  placeholder="唯一编码，如 AUTUMN10"
                />
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="16">
            <el-col :span="12">
              <el-form-item label="券类型" required>
                <el-radio-group v-model="form.couponType">
                  <el-radio value="fixed">满减券</el-radio>
                  <el-radio value="percent">折扣券</el-radio>
                </el-radio-group>
              </el-form-item>
            </el-col>
            <el-col :span="12">
              <el-form-item label="使用门槛">
                <el-input-number v-model="form.thresholdAmount" :min="0" :precision="2" :step="10" style="width: 100%" />
                <span class="field-tip">0 表示无门槛</span>
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="16">
            <el-col :span="12">
              <el-form-item :label="form.couponType === 'percent' ? '减免比例' : '抵扣金额'" required>
                <el-input-number
                  v-if="form.couponType === 'percent'"
                  v-model="form.discountAmount"
                  :min="0.01"
                  :max="0.99"
                  :precision="2"
                  :step="0.05"
                  style="width: 100%"
                />
                <el-input-number
                  v-else
                  v-model="form.discountAmount"
                  :min="0.01"
                  :precision="2"
                  :step="5"
                  style="width: 100%"
                />
                <span class="field-tip">{{ form.couponType === 'percent' ? `相当于 ${((1 - form.discountAmount) * 10).toFixed(1)} 折` : '元' }}</span>
              </el-form-item>
            </el-col>
            <el-col :span="12">
              <el-form-item label="最高抵扣">
                <el-input-number
                  v-model="form.maxDiscount"
                  :min="0"
                  :precision="2"
                  :step="10"
                  :disabled="form.couponType !== 'percent'"
                  style="width: 100%"
                />
                <span class="field-tip">{{ form.couponType === 'percent' ? '留空表示不封顶' : '仅折扣券可用' }}</span>
              </el-form-item>
            </el-col>
          </el-row>
        </section>

        <section class="form-section">
          <div class="section-title">发放与有效期</div>
          <el-row :gutter="16">
            <el-col :span="8">
              <el-form-item label="发放总量" required>
                <el-input-number
                  v-model="form.totalCount"
                  :min="1"
                  :step="10"
                  :disabled="dialogType === '编辑'"
                  style="width: 100%"
                />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="每人限领" required>
                <el-input-number v-model="form.perLimit" :min="1" :max="99" style="width: 100%" />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="有效天数" required>
                <el-input-number v-model="form.validDays" :min="1" :max="365" style="width: 100%" />
                <span class="field-tip">领取后 N 天内有效</span>
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="16">
            <el-col :span="12">
              <el-form-item label="可领开始">
                <el-date-picker
                  v-model="form.startAt"
                  type="datetime"
                  placeholder="留空表示不限"
                  value-format="YYYY-MM-DDTHH:mm:ss"
                  style="width: 100%"
                />
              </el-form-item>
            </el-col>
            <el-col :span="12">
              <el-form-item label="可领结束">
                <el-date-picker
                  v-model="form.endAt"
                  type="datetime"
                  placeholder="留空表示不限"
                  value-format="YYYY-MM-DDTHH:mm:ss"
                  style="width: 100%"
                />
              </el-form-item>
            </el-col>
          </el-row>
        </section>

        <section class="form-section">
          <div class="section-title">适用范围</div>
          <el-row :gutter="16">
            <el-col :span="12">
              <el-form-item label="范围类型" required>
                <el-select v-model="form.scopeType" style="width: 100%" @change="handleScopeTypeChange">
                  <el-option label="全场商品" value="all" />
                  <el-option label="指定商品" value="product" />
                  <el-option label="指定分类" value="category" />
                </el-select>
              </el-form-item>
            </el-col>
            <el-col :span="12" v-if="form.scopeType !== 'all'">
              <el-form-item :label="form.scopeType === 'product' ? '适用商品' : '适用分类'" required>
                <el-select v-model="scopeIds" multiple filterable clearable style="width: 100%" placeholder="请选择">
                  <el-option
                    v-for="item in scopeOptions"
                    :key="item.id"
                    :label="item.name"
                    :value="item.id"
                  />
                </el-select>
              </el-form-item>
            </el-col>
          </el-row>
          <div class="scope-tip">
            门槛与抵扣按所选范围内商品金额计算；选全场时按全订单商品金额计算。
          </div>
        </section>

        <section class="form-section">
          <div class="section-title">其他设置</div>
          <el-form-item label="启用状态">
            <el-switch v-model="form.isActive" active-text="启用" inactive-text="停用" />
          </el-form-item>
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

<script setup lang="ts" name="mallCoupon">
import { reactive, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { CirclePlus, Delete, EditPen } from '@element-plus/icons-vue'
import { mallCouponApi } from '@/api/modules/mallCoupon'
import { mallProductApi } from '@/api/modules/mallProduct'
import { mallCategoryApi } from '@/api/modules/mallCategory'
import { couponFaceValueText, couponScopeText, couponTypeLabel, isPercentCoupon } from '@/utils/coupon'
import QueryPage from '@/components/QueryPage/index.vue'
import { formatDate } from '@/utils'

const queryPageRef = ref()
const dialogVisible = ref(false)
const dialogTitle = ref('')
const dialogType = ref('')
const currentId = ref('')
const submitting = ref(false)

const searchForm = reactive({
  couponType: '',
  isActive: '' as boolean | '',
  keyword: ''
})

const form = reactive({
  code: '',
  name: '',
  couponType: 'fixed',
  thresholdAmount: 0,
  discountAmount: 10,
  maxDiscount: null as number | null,
  totalCount: 100,
  perLimit: 1,
  validDays: 30,
  startAt: '' as string,
  endAt: '' as string,
  scopeType: 'all',
  isActive: true
})

const scopeIds = ref<string[]>([])
const scopeOptions = ref<Array<{ id: string; name: string }>>([])

const formatMoney = (value: number) => Number(value).toFixed(2)

const couponValueText = (row: any) => {
  const base = couponFaceValueText(row.couponType, row.discountAmount)
  return isPercentCoupon(row.couponType) && row.maxDiscount ? `${base}（最高 ¥${formatMoney(row.maxDiscount)}）` : base
}

const scopeText = (row: any) => couponScopeText(row.scopeType)

const handleReset = () => {
  searchForm.couponType = ''
  searchForm.isActive = ''
  searchForm.keyword = ''
}

const resetForm = () => {
  form.code = ''
  form.name = ''
  form.couponType = 'fixed'
  form.thresholdAmount = 0
  form.discountAmount = 10
  form.maxDiscount = null
  form.totalCount = 100
  form.perLimit = 1
  form.validDays = 30
  form.startAt = ''
  form.endAt = ''
  form.scopeType = 'all'
  form.isActive = true
  scopeIds.value = []
}

const loadScopeOptions = async (type: string) => {
  scopeOptions.value = []
  try {
    if (type === 'product') {
      const { data } = await mallProductApi.getProductList({ page: 1, pageSize: 200 })
      scopeOptions.value = (data.list || []).map((item: any) => ({ id: item.id, name: `${item.name}（${item.code}）` }))
    } else if (type === 'category') {
      const { data } = await mallCategoryApi.getCategoryList({ page: 1, pageSize: 200 })
      scopeOptions.value = (data.list || []).map((item: any) => ({ id: item.id, name: item.name }))
    }
  } catch {
    scopeOptions.value = []
  }
}

const handleScopeTypeChange = async (type: string) => {
  scopeIds.value = []
  scopeOptions.value = []
  if (type === 'product' || type === 'category') {
    await loadScopeOptions(type)
  }
}

const openDialog = async (type: string, row?: any) => {
  resetForm()
  dialogType.value = type
  dialogTitle.value = type === '新增' ? '新增优惠券' : '编辑优惠券'
  currentId.value = ''
  if (type === '新增') {
    dialogVisible.value = true
    return
  }
  if (!row) return
  try {
    const { data } = await mallCouponApi.getCoupon(row.id)
    form.code = data.code || ''
    form.name = data.name || ''
    form.couponType = data.couponType || 'fixed'
    form.thresholdAmount = Number(data.thresholdAmount) || 0
    form.discountAmount = Number(data.discountAmount) || 0
    form.maxDiscount = data.maxDiscount ?? null
    form.totalCount = data.totalCount || 0
    form.perLimit = data.perLimit || 1
    form.validDays = data.validDays || 30
    form.startAt = data.startAt ? formatPicker(data.startAt) : ''
    form.endAt = data.endAt ? formatPicker(data.endAt) : ''
    form.scopeType = data.scopeType || 'all'
    form.isActive = Boolean(data.isActive)
    if (form.scopeType === 'product' || form.scopeType === 'category') {
      scopeIds.value = (data.scopeValue || '').split(',').filter((id: string) => id)
      await loadScopeOptions(form.scopeType)
    }
    currentId.value = row.id
    dialogVisible.value = true
  } catch {
    ElMessage.error('加载优惠券失败')
  }
}

const formatPicker = (value: string) => {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return ''
  const pad = (number: number) => String(number).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(
    date.getMinutes()
  )}:${pad(date.getSeconds())}`
}

const validateForm = () => {
  if (!form.name.trim()) {
    ElMessage.warning('请输入券名称')
    return false
  }
  if (dialogType.value === '新增' && !form.code.trim()) {
    ElMessage.warning('请输入券码')
    return false
  }
  if (form.couponType === 'percent' && (form.discountAmount <= 0 || form.discountAmount >= 1)) {
    ElMessage.warning('减免比例需在 0.01 ~ 0.99 之间')
    return false
  }
  if (form.couponType === 'fixed' && form.discountAmount <= 0) {
    ElMessage.warning('抵扣金额必须大于 0')
    return false
  }
  if (form.scopeType !== 'all' && scopeIds.value.length === 0) {
    ElMessage.warning(form.scopeType === 'product' ? '请选择适用商品' : '请选择适用分类')
    return false
  }
  if (form.startAt && form.endAt && form.endAt <= form.startAt) {
    ElMessage.warning('可领结束时间必须晚于开始时间')
    return false
  }
  return true
}

const submitForm = async () => {
  if (!validateForm()) return
  const payload = {
    name: form.name.trim(),
    couponType: form.couponType,
    thresholdAmount: form.thresholdAmount,
    discountAmount: form.discountAmount,
    maxDiscount: form.couponType === 'percent' ? form.maxDiscount || null : null,
    perLimit: form.perLimit,
    validDays: form.validDays,
    startAt: form.startAt || null,
    endAt: form.endAt || null,
    scopeType: form.scopeType,
    scopeValue: form.scopeType === 'all' ? '' : scopeIds.value.join(',')
  }
  submitting.value = true
  try {
    if (dialogType.value === '新增') {
      await mallCouponApi.addCoupon({
        ...payload,
        code: form.code.trim(),
        totalCount: form.totalCount
      })
      ElMessage.success('新增成功')
    } else {
      await mallCouponApi.updateCoupon(currentId.value, payload)
      await mallCouponApi.changeCouponStatus(currentId.value, form.isActive)
      ElMessage.success('更新成功')
    }
    dialogVisible.value = false
    queryPageRef.value?.getTableList()
  } catch {} finally {
    submitting.value = false
  }
}

const changeStatus = async (row: any) => {
  const next = !row.isActive
  try {
    await ElMessageBox.confirm(`确定要${next ? '启用' : '停用'}优惠券“${row.name}”吗？`, '状态变更', { type: 'warning' })
    await mallCouponApi.changeCouponStatus(row.id, next)
    ElMessage.success(`已${next ? '启用' : '停用'}`)
    queryPageRef.value?.getTableList()
  } catch {}
}

const replenish = async (row: any) => {
  try {
    const { value } = await ElMessageBox.prompt('请输入补库存数量', `补库存 - ${row.name}`, {
      inputPattern: /^\d+$/,
      inputErrorMessage: '请输入正整数',
      inputValue: '50'
    })
    await mallCouponApi.replenishCoupon(row.id, Number(value))
    ElMessage.success('补库存成功')
    queryPageRef.value?.getTableList()
  } catch {}
}

const remove = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定要删除优惠券“${row.name}”吗？`, '删除', { type: 'warning' })
    await mallCouponApi.deleteCoupon(row.id)
    ElMessage.success('删除成功')
    queryPageRef.value?.getTableList()
  } catch {}
}
</script>

<style scoped lang="scss">
.coupon-name {
  font-weight: 500;
}

.coupon-code {
  margin-top: 2px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.coupon-value {
  font-weight: 600;
  color: var(--el-color-danger);
}

.coupon-time {
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.coupon-valid-days {
  margin-top: 2px;
  font-size: 12px;
  color: var(--el-text-color-placeholder);
}

.stock-empty {
  color: var(--el-color-danger);
}

.coupon-dialog :deep(.el-dialog__body) {
  max-height: 72vh;
  overflow-y: auto;
}

.form-section {
  margin-bottom: 8px;
  padding: 12px 16px 0;
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 4px;

  .section-title {
    margin-bottom: 12px;
    font-size: 14px;
    font-weight: 600;
  }
}

.field-tip {
  margin-left: 8px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.scope-tip {
  margin-bottom: 14px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}
</style>

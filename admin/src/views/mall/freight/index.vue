<template>
  <div class="list-page">
    <div class="card table-main">
      <div class="table-header">
        <div class="header-button-lf">
          <el-input
            v-model="keyword"
            placeholder="门店编码 / 名称"
            clearable
            style="width: 240px"
            :prefix-icon="Search"
          />
        </div>
        <div class="header-button-ri">
          <el-button :icon="Refresh" @click="loadData">刷新</el-button>
        </div>
      </div>

      <div class="table-container">
        <el-table :data="filteredRows" :fit="true" style="width: 100%" border v-loading="loading">
          <el-table-column prop="code" label="门店编码" min-width="140" show-overflow-tooltip />
          <el-table-column prop="name" label="门店名称" min-width="180" show-overflow-tooltip />
          <el-table-column label="首重" min-width="150" align="center">
            <template #default="{ row }">
              <span v-if="row.rule">{{ row.rule.firstWeightGram }} 克 / ¥{{ Number(row.rule.firstWeightFee).toFixed(2) }}</span>
              <el-tag v-else type="info" size="small">未配置</el-tag>
            </template>
          </el-table-column>
          <el-table-column label="续重" min-width="170" align="center">
            <template #default="{ row }">
              <span v-if="row.rule">
                每 {{ row.rule.additionalWeightGram }} 克 +¥{{ Number(row.rule.additionalWeightFee).toFixed(2) }}
              </span>
              <span v-else>-</span>
            </template>
          </el-table-column>
          <el-table-column label="满额包邮" width="130" align="center">
            <template #default="{ row }">
              <span v-if="row.rule?.freeShippingAmount">满 ¥{{ Number(row.rule.freeShippingAmount).toFixed(2) }}</span>
              <span v-else>不包邮</span>
            </template>
          </el-table-column>
          <el-table-column label="状态" width="100" align="center">
            <template #default="{ row }">
              <el-tag v-if="row.rule" :type="row.rule.isActive ? 'success' : 'info'" size="small">
                {{ row.rule.isActive ? '启用' : '停用' }}
              </el-tag>
              <span v-else>-</span>
            </template>
          </el-table-column>
          <el-table-column label="更新时间" width="180" align="center">
            <template #default="{ row }">{{ row.rule ? formatDate(row.rule.updatedAt) : '-' }}</template>
          </el-table-column>
          <el-table-column label="操作" width="110" align="center" fixed="right">
            <template #default="{ row }">
              <el-button type="primary" link :icon="EditPen" @click="openDialog(row)">
                {{ row.rule ? '编辑' : '新增' }}
              </el-button>
            </template>
          </el-table-column>
        </el-table>
      </div>
    </div>

    <!-- 运费规则编辑 -->
    <el-dialog v-model="dialogVisible" :title="`运费规则 - ${currentStoreName}`" width="540px">
      <el-form :model="form" label-width="130px">
        <el-form-item label="首重重量(克)" required>
          <el-input-number v-model="form.firstWeightGram" :min="1" :max="1000000" :step="500" style="width: 100%" />
        </el-form-item>
        <el-form-item label="首重价格(元)" required>
          <el-input-number v-model="form.firstWeightFee" :precision="2" :min="0" :max="999999" style="width: 100%" />
        </el-form-item>
        <el-form-item label="续重步长(克)" required>
          <el-input-number v-model="form.additionalWeightGram" :min="1" :max="1000000" :step="500" style="width: 100%" />
        </el-form-item>
        <el-form-item label="续重价格(元)" required>
          <el-input-number v-model="form.additionalWeightFee" :precision="2" :min="0" :max="999999" style="width: 100%" />
        </el-form-item>
        <el-form-item label="满额包邮">
          <el-switch v-model="freeShippingEnabled" active-text="启用" inactive-text="关闭" />
          <el-input-number
            v-if="freeShippingEnabled"
            v-model="form.freeShippingAmount"
            :precision="2"
            :min="0.01"
            :max="9999999"
            style="width: 180px; margin-left: 12px"
          />
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

<script setup lang="ts" name="mallFreight">
import { ref, reactive, computed, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { EditPen, Refresh, Search } from '@element-plus/icons-vue'
import { mallStoreApi } from '@/api/modules/mallStore'
import { mallFreightApi } from '@/api/modules/mallFreight'
import { formatDate } from '@/utils'

const loading = ref(false)
const keyword = ref('')
const rows = ref<any[]>([])
const dialogVisible = ref(false)
const submitting = ref(false)
const currentStoreId = ref('')
const currentStoreName = ref('')
const freeShippingEnabled = ref(false)

const form = reactive({
  firstWeightGram: 1000,
  firstWeightFee: 10,
  additionalWeightGram: 1000,
  additionalWeightFee: 5,
  freeShippingAmount: 99,
  isActive: true
})

const filteredRows = computed(() => {
  const value = keyword.value.trim()
  if (!value) return rows.value
  return rows.value.filter(
    (row) => (row.code || '').includes(value) || (row.name || '').includes(value)
  )
})

const loadData = async () => {
  loading.value = true
  try {
    const [storeResult, ruleResult] = await Promise.all([
      mallStoreApi.getStoreList({ page: 1, pageSize: 100 }),
      mallFreightApi.getFreightRules()
    ])
    const stores = storeResult.data?.list || []
    const rules = ruleResult.data || []
    const ruleMap = new Map<string, any>(rules.map((rule: any) => [rule.storeId, rule]))

    rows.value = stores.map((store: any) => ({
      storeId: store.id,
      code: store.code,
      name: store.name,
      rule: ruleMap.get(store.id) || null
    }))
  } catch {
    rows.value = []
  } finally {
    loading.value = false
  }
}

const openDialog = (row: any) => {
  currentStoreId.value = row.storeId
  currentStoreName.value = row.name

  if (row.rule) {
    Object.assign(form, {
      firstWeightGram: row.rule.firstWeightGram,
      firstWeightFee: Number(row.rule.firstWeightFee),
      additionalWeightGram: row.rule.additionalWeightGram,
      additionalWeightFee: Number(row.rule.additionalWeightFee),
      freeShippingAmount: row.rule.freeShippingAmount ? Number(row.rule.freeShippingAmount) : 99,
      isActive: row.rule.isActive
    })
    freeShippingEnabled.value = !!row.rule.freeShippingAmount
  } else {
    Object.assign(form, {
      firstWeightGram: 1000,
      firstWeightFee: 10,
      additionalWeightGram: 1000,
      additionalWeightFee: 5,
      freeShippingAmount: 99,
      isActive: true
    })
    freeShippingEnabled.value = true
  }

  dialogVisible.value = true
}

const submitForm = async () => {
  if (form.firstWeightGram <= 0) {
    ElMessage.warning('首重重量必须大于 0')
    return
  }
  if (form.additionalWeightGram <= 0) {
    ElMessage.warning('续重步长必须大于 0')
    return
  }
  if (freeShippingEnabled.value && (!form.freeShippingAmount || form.freeShippingAmount <= 0)) {
    ElMessage.warning('满额包邮金额必须大于 0')
    return
  }

  submitting.value = true
  try {
    await mallFreightApi.upsertFreightRule(currentStoreId.value, {
      firstWeightGram: form.firstWeightGram,
      firstWeightFee: form.firstWeightFee,
      additionalWeightGram: form.additionalWeightGram,
      additionalWeightFee: form.additionalWeightFee,
      freeShippingAmount: freeShippingEnabled.value ? form.freeShippingAmount : null,
      isActive: form.isActive
    })
    ElMessage.success('保存运费规则成功')
    dialogVisible.value = false
    await loadData()
  } catch {
    // 错误提示已由请求拦截器统一处理
  } finally {
    submitting.value = false
  }
}

onMounted(() => {
  loadData()
})
</script>

<style scoped lang="scss">
.table-header {
  height: 40px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
}

.table-container {
  min-width: 0;
  overflow-x: auto;
}
</style>

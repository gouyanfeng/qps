<template>
  <div class="list-page">
    <QueryPage api="/admin/mall/freight-templates" :searchParam="searchForm" @reset="handleReset" ref="queryPageRef">
      <template #searchConditions>
        <el-form :model="searchForm" :inline="true">
          <el-form-item label="计费方式">
            <el-select v-model="searchForm.billingType" placeholder="全部" clearable>
              <el-option label="按重量" :value="0" />
              <el-option label="按件数" :value="1" />
              <el-option label="按体积" :value="2" />
            </el-select>
          </el-form-item>
          <el-form-item label="关键字">
            <el-input v-model="searchForm.keyword" placeholder="模板名称" clearable />
          </el-form-item>
        </el-form>
      </template>

      <template #headerButtons>
        <el-button type="primary" :icon="CirclePlus" @click="openDialog('新增')">新增模板</el-button>
      </template>

      <template #table="{ tableData }">
        <el-table :data="tableData" :fit="true" style="width: 100%" border>
          <el-table-column prop="name" label="模板名称" min-width="180" show-overflow-tooltip />
          <el-table-column label="计费方式" width="100" align="center">
            <template #default="{ row }">{{ billingTypeMap[row.billingType] }}</template>
          </el-table-column>
          <el-table-column prop="regionCount" label="区域数" width="90" align="center" />
          <el-table-column label="默认" width="80" align="center">
            <template #default="{ row }">
              <el-tag v-if="row.isDefault" type="warning">默认</el-tag>
              <span v-else>-</span>
            </template>
          </el-table-column>
          <el-table-column label="状态" width="80" align="center">
            <template #default="{ row }">
              <el-tag :type="row.isActive ? 'success' : 'info'">{{ row.isActive ? '启用' : '停用' }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column label="操作" align="center" width="200" fixed="right">
            <template #default="{ row }">
              <el-button type="primary" link :icon="EditPen" @click="openDialog('编辑', row)">编辑</el-button>
              <el-button type="danger" link :icon="Delete" @click="remove(row)">删除</el-button>
            </template>
          </el-table-column>
        </el-table>
      </template>
    </QueryPage>

    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="900px" top="3vh">
      <el-form :model="form" label-width="100px">
        <el-form-item label="模板名称" required>
          <el-input v-model="form.name" maxlength="100" />
        </el-form-item>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="计费方式">
              <el-select v-model="form.billingType" style="width: 100%">
                <el-option label="按重量（克）" :value="0" />
                <el-option label="按件数" :value="1" />
                <el-option label="按体积（cm³）" :value="2" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="设为默认">
              <el-switch v-model="form.isDefault" />
            </el-form-item>
          </el-col>
        </el-row>

        <el-divider content-position="left">区域规则（至少 1 条）</el-divider>

        <div v-for="(region, idx) in form.regions" :key="idx" class="region-block">
          <div class="region-header">
            <span class="region-title">区域 #{{ idx + 1 }}</span>
            <el-button v-if="form.regions.length > 1" type="danger" link @click="removeRegion(idx)">删除</el-button>
          </div>
          <el-row :gutter="12">
            <el-col :span="16">
              <el-form-item label="覆盖地区" label-width="80px">
                <el-tree-select
                  v-model="region.selectedRegionCodes"
                  :data="regionTree"
                  :props="regionTreeProps"
                  multiple
                  filterable
                  clearable
                  show-checkbox
                  :check-strictly="false"
                  collapse-tags
                  value-key="code"
                  node-key="code"
                  placeholder="请选择省 / 市 / 区县"
                  style="width: 100%"
                />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="兜底区域" label-width="80px">
                <el-switch v-model="region.isDefaultRegion" />
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="12">
            <el-col :span="6">
              <el-form-item label="首值" label-width="80px">
                <el-input-number v-model="region.firstValue" :min="0.01" :precision="2" style="width: 100%" />
              </el-form-item>
            </el-col>
            <el-col :span="6">
              <el-form-item label="首费" label-width="80px">
                <el-input-number v-model="region.firstFee" :min="0" :precision="2" style="width: 100%" />
              </el-form-item>
            </el-col>
            <el-col :span="6">
              <el-form-item label="续步长" label-width="80px">
                <el-input-number v-model="region.additionalStep" :min="0.01" :precision="2" style="width: 100%" />
              </el-form-item>
            </el-col>
            <el-col :span="6">
              <el-form-item label="续费" label-width="80px">
                <el-input-number v-model="region.additionalFee" :min="0" :precision="2" style="width: 100%" />
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="12">
            <el-col :span="12">
              <el-form-item label="满额包邮" label-width="80px">
                <el-input-number v-model="region.freeShippingAmount" :min="0" :precision="2" placeholder="0 表示不包邮" style="width: 100%" />
              </el-form-item>
            </el-col>
          </el-row>
        </div>

        <el-button type="primary" link :icon="Plus" @click="addRegion">+ 添加区域规则</el-button>
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

<script setup lang="ts" name="mallFreightTemplate">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { CirclePlus, EditPen, Delete, Plus } from '@element-plus/icons-vue'
import { mallFreightTemplateApi } from '@/api/modules/mallFreightTemplate'
import { regionApi } from '@/api/modules/region'
import QueryPage from '@/components/QueryPage/index.vue'

const billingTypeMap: Record<number, string> = { 0: '按重量', 1: '按件数', 2: '按体积' }

const queryPageRef = ref()
const dialogVisible = ref(false)
const submitting = ref(false)
const dialogTitle = ref('')
const dialogType = ref('')
const currentId = ref('')

const searchForm = reactive({ billingType: '' as number | '', keyword: '' })
const regionTree = ref<any[]>([])
const regionTreeProps = { children: 'children', value: 'code', label: 'name' }
const regionMap = ref<Map<string, any>>(new Map())

const normalizeCodes = (codes: string | null | undefined) => {
  if (!codes) return []
  return Array.from(new Set((codes || '').split(',').map(item => item.trim()).filter(Boolean)))
}

const namesToCodes = (names: string | null | undefined): string[] => {
  if (!names) return []
  const nameList = names.split(',').map(n => n.trim()).filter(Boolean)
  const codes: string[] = []
  for (const [code, node] of regionMap.value.entries()) {
    if (nameList.includes(node?.name)) codes.push(code)
  }
  return codes
}

const buildRegionTree = (regions: any[]) => {
  const map = new Map<string, any>()
  const tree: any[] = []

  regions.filter(item => item && item.code && item.name).forEach((item) => {
    const node = {
      code: item.code,
      name: item.name,
      level: item.level,
      parentCode: item.parentCode,
      children: []
    }
    map.set(item.code, node)
  })

  regions.filter(item => item && item.code && item.name).forEach((item) => {
    const node = map.get(item.code)
    if (!node) return
    if (item.parentCode && map.has(item.parentCode)) {
      map.get(item.parentCode).children.push(node)
    } else {
      tree.push(node)
    }
  })

  const cleanChildren = (nodes: any[]): any[] => nodes.map(node => ({
    ...node,
    children: cleanChildren(node.children || [])
  })).filter(node => node.children.length || node.level >= 1)

  regionMap.value = map
  regionTree.value = cleanChildren(tree)
}

const syncRegionSelection = (region: any) => {
  const selected = Array.from(new Set((region.selectedRegionCodes as Array<string | number> || [])
    .filter((code): code is string | number => Boolean(code))
    .map(code => String(code))))

  const selectedSet = new Set(selected)
  const topLevelSelection = selected.filter(code => {
    let parentCode = regionMap.value.get(code)?.parentCode
    while (parentCode) {
      if (selectedSet.has(parentCode)) return false
      parentCode = regionMap.value.get(parentCode)?.parentCode
    }
    return true
  })
  const provinceCodes = topLevelSelection.filter(code => Number(regionMap.value.get(code)?.level) === 1)
  const cityCodes = topLevelSelection
    .map(code => {
      const node = regionMap.value.get(code)
      if (Number(node?.level) === 2) return code
      if (Number(node?.level) === 3) return node.parentCode
      return null
    })
    .filter((code): code is string => Boolean(code))

  const provinceNames = provinceCodes.map(code => regionMap.value.get(code)?.name).filter(Boolean)
  const cityNameSet = new Set<string>()
  cityCodes.forEach(code => {
    const name = regionMap.value.get(code)?.name
    if (name) cityNameSet.add(name)
  })

  region.provinceNames = provinceNames.join(',')
  region.cityNames = Array.from(cityNameSet).join(',')
}

const createRegion = () => ({
  id: '',
  provinceNames: '',
  cityNames: '',
  selectedRegionCodes: [] as string[],
  isDefaultRegion: false,
  firstValue: 1000,
  firstFee: 10,
  additionalStep: 1000,
  additionalFee: 5,
  freeShippingAmount: 0
})

const form = reactive({
  name: '',
  billingType: 0,
  isDefault: false,
  isActive: true,
  regions: [createRegion()] as any[]
})

onMounted(async () => {
  const regionRes = await regionApi.getChinaRegionList({ isActive: true, pageSize: 0 })
  buildRegionTree(regionRes?.data?.list || [])
})

const handleReset = () => { searchForm.billingType = ''; searchForm.keyword = '' }

const resetForm = () => {
  form.name = ''; form.billingType = 0; form.isDefault = false; form.isActive = true
  form.regions = [createRegion()]
}

const addRegion = () => { form.regions.push(createRegion()) }
const removeRegion = (idx: number) => { form.regions.splice(idx, 1) }

const openDialog = async (type: string, row?: any) => {
  dialogTitle.value = type; dialogType.value = type

  if (type === '新增') {
    resetForm(); currentId.value = ''
  } else if (row) {
    const detail = await mallFreightTemplateApi.get(row.id)
    const data = detail?.data || detail
    Object.assign(form, {
      name: data.name,
      billingType: data.billingType,
      isDefault: data.isDefault,
      isActive: data.isActive,
      regions: (data.regions || []).map((r: any) => {
        const selected = [
          ...namesToCodes(r.provinceNames),
          ...namesToCodes(r.cityNames)
        ]
        return {
          id: r.id || '',
          provinceNames: r.provinceNames || '',
          cityNames: r.cityNames || '',
          selectedRegionCodes: selected,
          isDefaultRegion: r.isDefaultRegion,
          firstValue: r.firstValue,
          firstFee: r.firstFee,
          additionalStep: r.additionalStep,
          additionalFee: r.additionalFee,
          freeShippingAmount: r.freeShippingAmount || 0
        }
      })
    })
    if (!form.regions.length) form.regions = [createRegion()]
    currentId.value = row.id
  }
  dialogVisible.value = true
}

const validateRegions = () => {
  if (!form.regions.length) { ElMessage.warning('至少添加 1 条区域规则'); return false }
  let defaultCount = 0
  for (const r of form.regions) {
    if (r.isDefaultRegion) defaultCount++
    r.selectedRegionCodes = Array.from(new Set((r.selectedRegionCodes || []).filter(Boolean)))
    if (!r.isDefaultRegion && (!r.selectedRegionCodes || r.selectedRegionCodes.length === 0)) {
      ElMessage.warning('请选择该区域规则的覆盖地区')
      return false
    }
    syncRegionSelection(r)
    if (r.firstValue <= 0) { ElMessage.warning('首值必须大于 0'); return false }
    if (r.additionalStep <= 0) { ElMessage.warning('续步长必须大于 0'); return false }
  }
  if (defaultCount > 1) { ElMessage.warning('只能有 1 个兜底区域'); return false }
  return true
}

const submitForm = async () => {
  if (!form.name.trim()) { ElMessage.warning('请输入模板名称'); return }
  if (!validateRegions()) return

  submitting.value = true
  try {
    const payload = {
      name: form.name.trim(),
      billingType: form.billingType,
      isDefault: form.isDefault,
      isActive: form.isActive,
      regions: form.regions.map(r => {
        syncRegionSelection(r)
        return {
          id: r.id || undefined,
          provinceNames: r.provinceNames,
          cityNames: r.cityNames,
          isDefaultRegion: r.isDefaultRegion,
          firstValue: r.firstValue,
          firstFee: r.firstFee,
          additionalStep: r.additionalStep,
          additionalFee: r.additionalFee,
          freeShippingAmount: r.freeShippingAmount
        }
      })
    }

    if (dialogType.value === '新增') {
      await mallFreightTemplateApi.add(payload)
      ElMessage.success('新增成功')
    } else {
      await mallFreightTemplateApi.update(currentId.value, payload)
      ElMessage.success('更新成功')
    }
    dialogVisible.value = false
    queryPageRef.value?.getTableList()
  } catch {} finally { submitting.value = false }
}

const remove = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定要删除模板“${row.name}”吗？`, '删除', { type: 'warning' })
    await mallFreightTemplateApi.remove(row.id)
    ElMessage.success('删除成功')
    queryPageRef.value?.getTableList()
  } catch {}
}
</script>

<style scoped lang="scss">
.region-block { border: 1px solid #e4e7ed; border-radius: 4px; padding: 12px; margin-bottom: 12px; background: #fafafa; }
.region-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 8px; }
.region-title { font-weight: 600; color: #303133; font-size: 14px; }
</style>
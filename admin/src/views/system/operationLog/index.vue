<template>
  <div class="list-page operation-log-page">
    <QueryPage api="/admin/operation-logs" :searchParam="searchForm" @reset="handleReset">
      <template #searchConditions>
        <el-form :model="searchForm" :inline="true">
          <el-form-item label="实体类型">
            <el-input v-model="searchForm.entityType" placeholder="请输入实体类型" clearable />
          </el-form-item>
          <el-form-item label="实体ID">
            <el-input v-model="searchForm.entityId" placeholder="请输入实体ID" clearable />
          </el-form-item>
          <el-form-item label="操作类型">
            <el-select v-model="searchForm.actionType" placeholder="请选择操作类型" clearable>
              <el-option label="新增" value="Create" />
              <el-option label="修改" value="Update" />
              <el-option label="删除" value="Delete" />
              <el-option label="状态变更" value="StatusChange" />
              <el-option label="分配负责人" value="AssignOwner" />
            </el-select>
          </el-form-item>
          <el-form-item label="操作人">
            <el-input v-model="searchForm.operatorName" placeholder="请输入操作人" clearable />
          </el-form-item>
          <el-form-item label="请求路径">
            <el-input v-model="searchForm.requestPath" placeholder="请输入请求路径" clearable />
          </el-form-item>
          <el-form-item label="开始时间">
            <el-date-picker v-model="searchForm.startAt" type="datetime" value-format="YYYY-MM-DDTHH:mm:ss"
              placeholder="请选择开始时间" />
          </el-form-item>
          <el-form-item label="结束时间">
            <el-date-picker v-model="searchForm.endAt" type="datetime" value-format="YYYY-MM-DDTHH:mm:ss"
              placeholder="请选择结束时间" />
          </el-form-item>
        </el-form>
      </template>

      <template #table="{ tableData }">
        <el-table :data="tableData" :fit="true" style="width: 100%" border>
          <el-table-column prop="createdAt" label="操作时间" width="180">
            <template #default="{ row }">{{ formatDateTime(row.createdAt) }}</template>
          </el-table-column>
          <el-table-column prop="actionType" label="操作类型" width="120">
            <template #default="{ row }">
              <el-tag :type="getActionTagType(row.actionType)">{{ getActionText(row.actionType) }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column prop="entityType" label="实体类型" width="160" />
          <el-table-column prop="entityId" label="实体ID" min-width="220" show-overflow-tooltip />
          <el-table-column prop="operatorName" label="操作人" width="140" />
          <el-table-column prop="requestPath" label="请求路径" min-width="220" show-overflow-tooltip />
          <el-table-column prop="ipAddress" label="IP地址" width="140" show-overflow-tooltip />
          <el-table-column label="变更内容" width="120" align="center">
            <template #default="{ row }">
              <el-button type="primary" link :icon="View" @click="openDetail(row)">查看</el-button>
            </template>
          </el-table-column>
        </el-table>
      </template>
    </QueryPage>

    <el-dialog v-model="detailVisible" title="变更内容" width="820px">
      <el-table v-if="detailRows.length" :data="detailRows" border max-height="460">
        <el-table-column prop="field" label="字段" width="180" show-overflow-tooltip />
        <el-table-column prop="oldValue" label="原值" min-width="260" show-overflow-tooltip />
        <el-table-column prop="newValue" label="新值" min-width="260" show-overflow-tooltip />
      </el-table>
      <pre v-else class="operation-detail">{{ detailContent || '-' }}</pre>
    </el-dialog>
  </div>
</template>

<script setup lang="ts" name="operationLog">
import { reactive, ref } from 'vue'
import { View } from '@element-plus/icons-vue'
import QueryPage from '@/components/QueryPage/index.vue'

const searchForm = reactive({
  entityType: '',
  entityId: '',
  actionType: '',
  operatorName: '',
  requestPath: '',
  startAt: '',
  endAt: ''
})

const detailVisible = ref(false)
const detailContent = ref('')
const detailRows = ref<any[]>([])

const handleReset = () => {
  searchForm.entityType = ''
  searchForm.entityId = ''
  searchForm.actionType = ''
  searchForm.operatorName = ''
  searchForm.requestPath = ''
  searchForm.startAt = ''
  searchForm.endAt = ''
}

const getActionText = (actionType: string) => {
  const map: Record<string, string> = {
    Create: '新增',
    Update: '修改',
    Delete: '删除',
    StatusChange: '状态变更',
    AssignOwner: '分配负责人'
  }
  return map[actionType] || actionType
}

const getActionTagType = (actionType: string) => {
  const map: Record<string, string> = {
    Create: 'success',
    Update: 'primary',
    Delete: 'danger',
    StatusChange: 'warning',
    AssignOwner: 'info'
  }
  return map[actionType] || 'info'
}

const formatDateTime = (value: string) => {
  if (!value) return ''
  return new Date(value).toLocaleString()
}

const formatValue = (value: any) => {
  if (value === null || value === undefined) return ''
  if (typeof value === 'object') return JSON.stringify(value)
  return String(value)
}

const openDetail = (row: any) => {
  detailRows.value = []
  detailContent.value = ''

  try {
    const changes = JSON.parse(row.changeJson || '{}')
    detailRows.value = Object.keys(changes).map(field => ({
      field,
      oldValue: formatValue(changes[field]?.old),
      newValue: formatValue(changes[field]?.new)
    }))

    if (!detailRows.value.length) {
      detailContent.value = '-'
    }
  } catch (error) {
    detailContent.value = row.changeJson || ''
  }

  detailVisible.value = true
}
</script>

<style scoped lang="scss">
.operation-detail {
  max-height: 420px;
  margin: 0;
  padding: 10px;
  overflow: auto;
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 6px;
  background: #f8fafc;
  color: var(--el-text-color-regular);
  font-family: Consolas, monospace;
  font-size: 12px;
  line-height: 1.5;
  white-space: pre-wrap;
  word-break: break-word;
}
</style>



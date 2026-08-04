<template>
  <div class="list-page error-log-page">
    <QueryPage api="/admin/error-logs" :searchParam="searchForm" @reset="handleReset">
      <template #searchConditions>
        <el-form :model="searchForm" :inline="true">
          <el-form-item label="错误类型">
            <el-input v-model="searchForm.errorType" placeholder="请输入错误类型" clearable />
          </el-form-item>
          <el-form-item label="错误信息">
            <el-input v-model="searchForm.errorMessage" placeholder="请输入错误信息" clearable />
          </el-form-item>
          <el-form-item label="请求地址">
            <el-input v-model="searchForm.requestUrl" placeholder="请输入请求地址" clearable />
          </el-form-item>
          <el-form-item label="用户">
            <el-input v-model="searchForm.username" placeholder="请输入用户" clearable />
          </el-form-item>
          <el-form-item label="状态码">
            <el-input-number v-model="searchForm.httpStatusCode" :min="100" :max="599" controls-position="right" />
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
          <el-table-column prop="createdAt" label="发生时间" width="180">
            <template #default="{ row }">{{ formatDateTime(row.createdAt) }}</template>
          </el-table-column>
          <el-table-column prop="httpStatusCode" label="状态码" width="100">
            <template #default="{ row }">
              <el-tag :type="row.httpStatusCode >= 500 ? 'danger' : 'warning'">{{ row.httpStatusCode }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column prop="errorType" label="错误类型" width="180" show-overflow-tooltip />
          <el-table-column prop="errorMessage" label="错误信息" min-width="260" show-overflow-tooltip />
          <el-table-column prop="requestMethod" label="方法" width="90" />
          <el-table-column prop="requestUrl" label="请求地址" min-width="260" show-overflow-tooltip />
          <el-table-column prop="username" label="用户" width="140" show-overflow-tooltip />
          <el-table-column prop="ipAddress" label="IP地址" width="140" show-overflow-tooltip />
          <el-table-column label="详情" width="100" align="center" fixed="right">
            <template #default="{ row }">
              <el-button type="primary" link :icon="View" @click="openDetail(row)">查看</el-button>
            </template>
          </el-table-column>
        </el-table>
      </template>
    </QueryPage>

    <el-dialog v-model="detailVisible" title="错误详情" width="820px">
      <el-descriptions :column="2" border>
        <el-descriptions-item label="错误类型">{{ currentLog.errorType || '-' }}</el-descriptions-item>
        <el-descriptions-item label="状态码">{{ currentLog.httpStatusCode || '-' }}</el-descriptions-item>
        <el-descriptions-item label="用户">{{ currentLog.username || '-' }}</el-descriptions-item>
        <el-descriptions-item label="IP地址">{{ currentLog.ipAddress || '-' }}</el-descriptions-item>
        <el-descriptions-item label="请求方法">{{ currentLog.requestMethod || '-' }}</el-descriptions-item>
        <el-descriptions-item label="请求地址">{{ currentLog.requestUrl || '-' }}</el-descriptions-item>
      </el-descriptions>
      <div class="error-detail-block">
        <h4>错误信息</h4>
        <pre>{{ currentLog.errorMessage || '-' }}</pre>
      </div>
      <div class="error-detail-block">
        <h4>请求体</h4>
        <pre>{{ currentLog.requestBody || '-' }}</pre>
      </div>
      <div class="error-detail-block">
        <h4>堆栈</h4>
        <pre>{{ currentLog.stackTrace || '-' }}</pre>
      </div>
    </el-dialog>
  </div>
</template>

<script setup lang="ts" name="errorLog">
import { reactive, ref } from 'vue'
import { View } from '@element-plus/icons-vue'
import QueryPage from '@/components/QueryPage/index.vue'

const searchForm = reactive({
  errorType: '',
  errorMessage: '',
  requestUrl: '',
  username: '',
  httpStatusCode: undefined as number | undefined,
  startAt: '',
  endAt: ''
})

const detailVisible = ref(false)
const currentLog = ref<any>({})

const handleReset = () => {
  searchForm.errorType = ''
  searchForm.errorMessage = ''
  searchForm.requestUrl = ''
  searchForm.username = ''
  searchForm.httpStatusCode = undefined
  searchForm.startAt = ''
  searchForm.endAt = ''
}

const formatDateTime = (value: string) => {
  if (!value) return ''
  return new Date(value).toLocaleString()
}

const openDetail = (row: any) => {
  currentLog.value = row || {}
  detailVisible.value = true
}
</script>

<style scoped lang="scss">
.error-detail-block {
  margin-top: 14px;

  h4 {
    margin: 0 0 8px;
    color: var(--el-text-color-primary);
    font-size: 14px;
  }

  pre {
    max-height: 220px;
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
}
</style>

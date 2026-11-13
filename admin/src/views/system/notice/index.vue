<template>
  <div class="list-page">
    <QueryPage api="/admin/system-messages" :searchParam="searchForm" @reset="handleReset" ref="queryPageRef">
      <template #searchConditions>
        <el-form :model="searchForm" :inline="true">
          <el-form-item label="关键字">
            <el-input v-model="searchForm.keyword" placeholder="标题关键字" clearable />
          </el-form-item>
          <el-form-item label="类型">
            <el-select v-model="searchForm.messageType" placeholder="全部" clearable style="width: 130px">
              <el-option label="通知（全员）" :value="0" />
              <el-option label="私信（指定人）" :value="1" />
            </el-select>
          </el-form-item>
          <el-form-item label="状态">
            <el-select v-model="searchForm.isPublished" placeholder="全部" clearable style="width: 110px">
              <el-option label="已发布" :value="true" />
              <el-option label="未发布" :value="false" />
            </el-select>
          </el-form-item>
        </el-form>
      </template>

      <template #headerButtons>
        <el-button type="primary" :icon="CirclePlus" @click="openDialog('新增')">新增消息</el-button>
      </template>

      <template #table="{ tableData }">
        <el-table :data="tableData" :fit="true" style="width: 100%" border>
          <el-table-column prop="title" label="标题" min-width="180" show-overflow-tooltip />
          <el-table-column label="类型" width="110" align="center">
            <template #default="{ row }">
              <el-tag :type="row.messageType === 1 ? 'success' : 'primary'">
                {{ typeMap[row.messageType] || "-" }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column prop="content" label="内容" min-width="220" show-overflow-tooltip />
          <el-table-column label="置顶" width="80" align="center">
            <template #default="{ row }">
              <el-tag v-if="row.isTop" type="danger">置顶</el-tag>
              <span v-else>-</span>
            </template>
          </el-table-column>
          <el-table-column prop="sortOrder" label="排序" width="70" align="center" />
          <el-table-column label="展示窗口" width="220" align="center">
            <template #default="{ row }">
              <span v-if="row.messageType === 1">-</span>
              <span v-else-if="row.startDate || row.endDate">
                {{ formatDate(row.startDate) || "不限" }} ~ {{ formatDate(row.endDate) || "不限" }}
              </span>
              <span v-else>长期</span>
            </template>
          </el-table-column>
          <el-table-column label="状态" width="90" align="center">
            <template #default="{ row }">
              <el-tag :type="row.isPublished ? 'success' : 'info'">{{ row.isPublished ? "已发布" : "未发布" }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column label="发布人" width="100" align="center">
            <template #default="{ row }">{{ row.senderName || "-" }}</template>
          </el-table-column>
          <el-table-column label="创建时间" width="170" align="center">
            <template #default="{ row }">{{ formatDate(row.createdAt) }}</template>
          </el-table-column>
          <el-table-column label="操作" align="center" width="250" fixed="right" class-name="actions-column" header-class-name="actions-column">
            <template #default="{ row }">
              <el-button type="primary" link :icon="EditPen" @click="openDialog('编辑', row)">编辑</el-button>
              <el-button
                :type="row.isPublished ? 'warning' : 'success'"
                link
                @click="changePublishStatus(row)"
              >
                {{ row.isPublished ? "下架" : "发布" }}
              </el-button>
              <el-button type="danger" link :icon="Delete" @click="remove(row)">删除</el-button>
            </template>
          </el-table-column>
        </el-table>
      </template>
    </QueryPage>

    <el-dialog v-model="dialogVisible" :title="dialogTitle + '消息'" width="600px">
      <el-form :model="form" label-width="100px">
        <el-form-item label="类型" required>
          <!-- 类型决定持久化形态（是否写收件人），发送后不可变更，故编辑态只读展示 -->
          <el-radio-group v-if="dialogType === '新增'" v-model="form.messageType">
            <el-radio :value="0">通知（全员可见）</el-radio>
            <el-radio :value="1">私信（指定收件人）</el-radio>
          </el-radio-group>
          <el-tag v-else :type="form.messageType === 1 ? 'success' : 'primary'">
            {{ typeMap[form.messageType] }}
          </el-tag>
        </el-form-item>
        <el-form-item v-if="form.messageType === 1 && dialogType === '新增'" label="收件人" required>
          <el-select
            v-model="form.recipientIds"
            multiple
            filterable
            remote
            reserve-keyword
            :remote-method="loadUsers"
            :loading="userLoading"
            placeholder="输入用户名或姓名搜索，可多选"
            style="width: 100%"
          >
            <el-option v-for="user in userOptions" :key="user.value" :label="user.label" :value="user.value" />
          </el-select>
        </el-form-item>
        <el-form-item label="标题" required>
          <el-input v-model="form.title" placeholder="请输入标题" maxlength="100" show-word-limit />
        </el-form-item>
        <el-form-item label="内容" required>
          <el-input v-model="form.content" type="textarea" :rows="4" placeholder="请输入内容" maxlength="2000" show-word-limit />
        </el-form-item>
        <template v-if="form.messageType === 0">
          <el-form-item label="展示窗口">
            <el-date-picker
              v-model="form.window"
              type="datetimerange"
              range-separator="至"
              start-placeholder="开始时间"
              end-placeholder="结束时间"
              style="width: 100%"
            />
          </el-form-item>
          <el-form-item label="置顶">
            <el-switch v-model="form.isTop" />
          </el-form-item>
          <el-form-item label="排序">
            <el-input-number v-model="form.sortOrder" :min="0" :max="9999" style="width: 100%" />
          </el-form-item>
          <el-form-item v-if="dialogType === '新增'" label="立即发布">
            <el-switch v-model="form.isPublished" active-text="发布" inactive-text="存草稿" />
          </el-form-item>
        </template>
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

<script setup lang="ts" name="systemNotice">
import { ref, reactive } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { CirclePlus, EditPen, Delete } from '@element-plus/icons-vue'
import { systemMessageApi } from '@/api/modules/system/systemMessage'
import { userApi } from '@/api/modules/system/user'
import QueryPage from '@/components/QueryPage/index.vue'
import { formatDate } from '@/utils'

const typeMap: Record<number, string> = { 0: '通知', 1: '私信' }

const queryPageRef = ref()
const dialogVisible = ref(false)
const submitting = ref(false)
const dialogTitle = ref('')
const dialogType = ref('')
const currentId = ref('')

const userLoading = ref(false)
const userOptions = ref<Array<{ label: string; value: string }>>([])
let userSearchTimer: ReturnType<typeof setTimeout> | null = null

const searchForm = reactive({
  keyword: '',
  messageType: '' as number | '',
  isPublished: '' as boolean | ''
})

const defaultForm = () => ({
  messageType: 0,
  recipientIds: [] as string[],
  title: '',
  content: '',
  window: null as [Date | null, Date | null] | null,
  isTop: false,
  sortOrder: 0,
  isPublished: true
})

const form = reactive(defaultForm())

const handleReset = () => {
  searchForm.keyword = ''
  searchForm.messageType = ''
  searchForm.isPublished = ''
}

/** 收件人远程搜索（250ms 防抖，避免连续输入狂发请求） */
const loadUsers = (keyword = '') => {
  if (userSearchTimer) clearTimeout(userSearchTimer)
  userSearchTimer = setTimeout(async () => {
    userLoading.value = true
    try {
      const res: any = await userApi.getUserList({ page: 1, pageSize: 50, keyword: keyword.trim() })
      const list = res?.data?.list ?? res?.data ?? []
      userOptions.value = list.map((user: any) => ({
        label: user.realName ? `${user.realName}（${user.username}）` : user.username,
        value: user.id
      }))
    } catch {
      userOptions.value = []
    } finally {
      userLoading.value = false
    }
  }, 250)
}

const openDialog = (type: string, row?: any) => {
  dialogTitle.value = type
  dialogType.value = type

  Object.assign(form, defaultForm())
  if (type === '编辑' && row) {
    form.messageType = row.messageType ?? 0
    form.title = row.title
    form.content = row.content
    form.window = row.startDate || row.endDate ? [row.startDate, row.endDate] : null
    form.isTop = row.isTop
    form.sortOrder = row.sortOrder
    currentId.value = row.id
  } else {
    currentId.value = ''
    // 打开即预载一批用户，方便私信直接选人
    loadUsers()
  }

  dialogVisible.value = true
}

const submitForm = async () => {
  if (!form.title.trim()) { ElMessage.warning('请输入标题'); return }
  if (!form.content.trim()) { ElMessage.warning('请输入内容'); return }

  if (dialogType.value === '新增' && form.messageType === 1) {
    if (!form.recipientIds.length) { ElMessage.warning('请选择收件人'); return }

    submitting.value = true
    try {
      // 私信：后端按“有收件人”判定类型，发出即发布，无需状态/窗口字段
      await systemMessageApi.add({
        title: form.title.trim(),
        content: form.content.trim(),
        recipientIds: form.recipientIds
      })
      ElMessage.success('发送私信成功')
      dialogVisible.value = false
      queryPageRef.value?.getTableList()
    } catch {
    } finally {
      submitting.value = false
    }
    return
  }

  const window = Array.isArray(form.window) ? form.window : null
  const payload = {
    title: form.title.trim(),
    content: form.content.trim(),
    isTop: form.isTop,
    sortOrder: form.sortOrder,
    startDate: window?.[0] || null,
    endDate: window?.[1] || null
  }

  submitting.value = true
  try {
    if (dialogType.value === '新增') {
      await systemMessageApi.add({ ...payload, isPublished: form.isPublished })
      ElMessage.success(form.isPublished ? '发布通知成功' : '草稿已保存')
    } else {
      await systemMessageApi.update(currentId.value, payload)
      ElMessage.success('更新消息成功')
    }
    dialogVisible.value = false
    queryPageRef.value?.getTableList()
  } catch {
  } finally {
    submitting.value = false
  }
}

const changePublishStatus = async (row: any) => {
  const next = !row.isPublished
  const text = next ? '发布' : '下架'
  try {
    await ElMessageBox.confirm(`确定要${text}“${row.title}”吗？`, `${text}消息`, { type: 'warning' })
    await systemMessageApi.changePublishStatus(row.id, next)
    ElMessage.success(`${text}成功`)
    queryPageRef.value?.getTableList()
  } catch {}
}

const remove = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确定要删除“${row.title}”吗？`, '删除消息', { type: 'warning' })
    await systemMessageApi.remove(row.id)
    ElMessage.success('删除消息成功')
    queryPageRef.value?.getTableList()
  } catch {}
}
</script>

<style scoped lang="scss"></style>

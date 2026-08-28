<template>
    <div class="list-page">
        <QueryPage api="/admin/users" :searchParam="searchForm" @reset="handleReset" ref="queryPageRef">
            <!-- 搜索条件 -->
            <template #searchConditions>
                <el-form :model="searchForm" :inline="true">
                    <el-form-item label="用户名">
                        <el-input v-model="searchForm.username" placeholder="请输入用户名" />
                    </el-form-item>
                    <el-form-item label="真实姓名">
                        <el-input v-model="searchForm.realName" placeholder="请输入真实姓名" />
                    </el-form-item>
                    <el-form-item label="状态">
                        <el-select v-model="searchForm.isActive" placeholder="请选择状态" style="width: 200px">
                            <el-option label="激活" :value="true" />
                            <el-option label="禁用" :value="false" />
                        </el-select>
                    </el-form-item>
                </el-form>
            </template>

            <!-- 功能按钮 -->
            <template #headerButtons>
                <Permission code="SYSTEM_USER_ADD"><el-button type="primary" :icon="CirclePlus" @click="openDialog('新增')">新增用户</el-button></Permission>
            </template>

            <!-- 表格 -->
            <template #table="{ tableData }">
                <el-table :data="tableData" :fit="true" style="width: 100%" border>
                    <el-table-column prop="username" label="用户名" min-width="180" show-overflow-tooltip />
                    <el-table-column prop="realName" label="真实姓名" min-width="150" show-overflow-tooltip />
                    <el-table-column prop="roleName" label="角色" width="140">
                        <template #default="{ row }">
                            <el-tag :type="row.isActive ? 'success' : 'danger'">
                                {{ row.roleName }}
                            </el-tag>
                        </template>
                    </el-table-column>
                    <el-table-column prop="isActive" label="状态" width="100">
                        <template #default="{ row }">
                            <el-tag :type="row.isActive ? 'success' : 'danger'">
                                {{ row.isActive ? '激活' : '禁用' }}
                            </el-tag>
                        </template>
                    </el-table-column>
                    <el-table-column prop="createdAt" label="创建时间" width="180">
                        <template #default="{ row }">
                            {{ formatDate(row.createdAt) }}
                        </template>
                    </el-table-column>
                    <el-table-column label="操作" align="center" width="120" fixed="right">
                        <template #default="{ row }">
                            <Permission code="SYSTEM_USER_EDIT"><el-button type="primary" link :icon="EditPen" @click="openDialog('编辑', row)">编辑</el-button></Permission>
                        </template>
                    </el-table-column>
                </el-table>
            </template>
        </QueryPage>

        <!-- 用户对话框 -->
        <el-dialog v-model="dialogVisible" :title="dialogTitle" width="500px">
            <el-form :model="form" label-width="120px">
                <el-form-item label="用户名">
                    <el-input v-model="form.username" placeholder="请输入用户名" />
                </el-form-item>
                <el-form-item label="密码" v-if="dialogType === '新增'">
                    <el-input v-model="form.password" type="password" placeholder="请输入密码" />
                </el-form-item>
                <el-form-item label="真实姓名">
                    <el-input v-model="form.realName" placeholder="请输入真实姓名" />
                </el-form-item>
                <el-form-item label="角色">
                    <el-select v-model="form.roleId" placeholder="请选择角色" style="width: 200px">
                        <el-option v-for="r in roles" :key="r.id" :label="r.name" :value="r.id" />
                    </el-select>
                </el-form-item>

                <el-form-item label="状态">
                    <el-switch v-model="form.isActive" active-text="激活" inactive-text="禁用" />
                </el-form-item>
            </el-form>
            <template #footer>
                <span class="dialog-footer">
                    <el-button @click="dialogVisible = false">取消</el-button>
                    <el-button type="primary" @click="submitForm">确定</el-button>
                </span>
            </template>
        </el-dialog>
    </div>
</template>

<script setup lang="ts" name="users">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { User } from '@/api/interface'
import { CirclePlus, EditPen, View } from '@element-plus/icons-vue'
import { userApi } from '@/api/modules/user'
import { roleApi } from '@/api/modules/role'
import { formatDate } from '@/utils'
import QueryPage from '@/components/QueryPage/index.vue'
import Permission from '@/components/Permission/index.vue'

// 引用
const queryPageRef = ref()


// 状态管理
const dialogVisible = ref(false)
const dialogTitle = ref('')
const dialogType = ref('')
const currentUserId = ref('')

// 角色列表（从 API 获取）
const roles = ref<any[]>([])

onMounted(async () => {
  try {
    const res = await roleApi.getRoleList({ page: 1, pageSize: 100 }) as any
    roles.value = res.data?.list || res.data || []
  } catch {
    // 静默失败
  }
})

// 表单数据
const searchForm = reactive({
    username: '',
    realName: '',
    roleId: '',
    isActive: undefined as boolean | undefined
})

const form = reactive<User.ReqUserForm>({
    username: '',
    password: '',
    realName: '',
    roleId: '',
    isActive: true
})

// 处理重置事件
const handleReset = () => {
    // 重置搜索表单
    Object.assign(searchForm, {
        username: '',
        realName: '',
        roleId: '',
        isActive: undefined
    })
}

// 事件处理
const openDialog = (type: string, row?: any) => {
    dialogTitle.value = type
    dialogType.value = type

    if (type === '新增') {
        // 重置表单
        Object.assign(form, {
            username: '',
            password: '',
            realName: '',
            roleId: '',
            isActive: true
        })
        currentUserId.value = ''
    } else if (row) {
        // 填充表单数据
        Object.assign(form, {
            username: row.username,
            password: '',
            realName: row.realName,
            roleId: row.roleId,
            isActive: row.isActive
        })
        currentUserId.value = row.id
    }

    dialogVisible.value = true
}

const submitForm = async () => {
    try {
        if (dialogType.value === '新增') {
            await userApi.addUser(form)
            ElMessage.success('新增用户成功')
        } else if (dialogType.value === '编辑' && currentUserId.value) {
            await userApi.updateUser(currentUserId.value, form)
            ElMessage.success('更新用户成功')
        }
        dialogVisible.value = false
        // 重新获取数据
        if (queryPageRef.value) {
            queryPageRef.value.getTableList()
        }
    } catch (error) {
        ElMessage.error('操作失败')
    }
}


</script>

<style scoped lang="scss"></style>










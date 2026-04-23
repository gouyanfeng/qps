<template>
    <div class="users">
        <QueryPage :pagination="pagination" v-model:collapsed="collapsed" @search="handleSearch" @reset="resetSearch"
            @sizeChange="handleSizeChange" @currentChange="handleCurrentChange">
            <!-- 搜索条件 -->
            <template #searchConditions>
                <el-form :model="searchForm" :inline="true">
                    <el-form-item label="用户名">
                        <el-input v-model="searchForm.username" placeholder="请输入用户名" />
                    </el-form-item>
                    <el-form-item label="真实姓名">
                        <el-input v-model="searchForm.realName" placeholder="请输入真实姓名" />
                    </el-form-item>
                    <el-form-item label="角色">
                        <el-select v-model="searchForm.role" placeholder="请选择角色">
                            <el-option label="管理员" value="admin" />
                            <el-option label="商户" value="merchant" />
                            <el-option label="用户" value="user" />
                        </el-select>
                    </el-form-item>
                    <el-form-item label="状态">
                        <el-select v-model="searchForm.isActive" placeholder="请选择状态">
                            <el-option label="激活" :value="true" />
                            <el-option label="禁用" :value="false" />
                        </el-select>
                    </el-form-item>
                </el-form>
            </template>

            <!-- 功能按钮 -->
            <template #headerButtons>
                <el-button type="primary" :icon="CirclePlus" @click="openDialog('新增')">新增用户</el-button>
            </template>

            <!-- 表格 -->
            <template #table>
                <el-table v-loading="loading" :data="userList" style="width: 100%" border>
                    <el-table-column prop="username" label="用户名" width="180" />
                    <el-table-column prop="realName" label="真实姓名" width="150" />
                    <el-table-column prop="role" label="角色" width="120">
                        <template #default="{ row }">
                            <el-tag :type="getRoleType(row.role)">
                                {{ getRoleLabel(row.role) }}
                            </el-tag>
                        </template>
                    </el-table-column>
                    <el-table-column prop="merchantId" label="商户ID" width="150" />
                    <el-table-column prop="isActive" label="状态" width="100">
                        <template #default="{ row }">
                            <el-tag :type="row.isActive ? 'success' : 'danger'">
                                {{ row.isActive ? '激活' : '禁用' }}
                            </el-tag>
                        </template>
                    </el-table-column>
                    <el-table-column prop="createdAt" label="创建时间" width="200">
                        <template #default="{ row }">
                            {{ formatDate(row.createdAt) }}
                        </template>
                    </el-table-column>
                    <el-table-column label="操作" fixed="right" align="center">
                        <template #default="{ row }">
                            <el-button type="primary" link :icon="View" @click="openDialog('查看', row)">查看</el-button>
                            <el-button type="primary" link :icon="EditPen" @click="openDialog('编辑', row)">编辑</el-button>
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
                    <el-select v-model="form.role" placeholder="请选择角色">
                        <el-option label="管理员" value="admin" />
                        <el-option label="商户" value="merchant" />
                        <el-option label="用户" value="user" />
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
import QueryPage from '@/components/QueryPage/index.vue'

// 状态管理
const loading = ref(false)
const collapsed = ref(true)
const userList = ref<User.ResUserList[]>([])
const dialogVisible = ref(false)
const dialogTitle = ref('')
const dialogType = ref('')
const currentUserId = ref('')

// 表单数据
const searchForm = reactive({
    username: '',
    realName: '',
    role: '',
    isActive: undefined as boolean | undefined
})

const pagination = reactive({
    currentPage: 1,
    pageSize: 10,
    total: 0
})

const form = reactive<User.ReqUserForm>({
    username: '',
    password: '',
    realName: '',
    role: 'user',
    merchantId: '',
    isActive: true
})

// 工具函数
const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString()
}

const getRoleLabel = (role: string) => {
    const roleMap = {
        admin: '管理员',
        merchant: '商户',
        user: '用户'
    }
    return roleMap[role as keyof typeof roleMap] || role
}

const getRoleType = (role: string) => {
    const typeMap = {
        admin: 'primary',
        merchant: 'success',
        user: 'info'
    }
    return typeMap[role as keyof typeof typeMap] || 'default'
}

// API 调用
const getUserList = async () => {

    try {
        const params = {
            page: pagination.currentPage,
            pageSize: pagination.pageSize,
            ...searchForm
        }
        const response = await userApi.getUserList(params)
        userList.value = response.data.list || []
        pagination.total = response.data.totalCount || 0
    } catch (error) {
        ElMessage.error('获取用户列表失败')
    }
}

// 事件处理
const handleSearch = () => {
    getUserList()
}

const resetSearch = () => {
    searchForm.username = ''
    searchForm.realName = ''
    searchForm.role = ''
    searchForm.isActive = undefined
    getUserList()
}

const handleSizeChange = (size: number) => {
    getUserList()
}

const handleCurrentChange = (current: number) => {
    getUserList()
}

const openDialog = (type: string, row?: User.ResUserList) => {
    dialogTitle.value = type
    dialogType.value = type

    if (type === '新增') {
        // 重置表单
        Object.assign(form, {
            username: '',
            password: '',
            realName: '',
            role: 'user',
            merchantId: '',
            isActive: true
        })
        currentUserId.value = ''
    } else if (row) {
        // 填充表单数据
        Object.assign(form, {
            username: row.username,
            password: '',
            realName: row.realName,
            role: row.role,
            merchantId: row.merchantId,
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
        getUserList()
    } catch (error) {
        ElMessage.error('操作失败')
    }
}

// 初始化
onMounted(() => {
    getUserList()
})
</script>

<style scoped lang="scss">
@import './index.scss';
</style>
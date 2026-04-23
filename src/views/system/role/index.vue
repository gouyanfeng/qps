<template>
    <div class="list-page">
        <QueryPage :pagination="pagination" v-model:collapsed="collapsed" @search="handleSearch" @reset="resetSearch"
            @sizeChange="handleSizeChange" @currentChange="handleCurrentChange">
            <!-- 搜索条件 -->
            <template #searchConditions>
                <el-form :model="searchForm" :inline="true">
                    <el-form-item label="角色名称">
                        <el-input v-model="searchForm.label" placeholder="请输入角色名称" />
                    </el-form-item>
                    <el-form-item label="角色值">
                        <el-input v-model="searchForm.value" placeholder="请输入角色值" />
                    </el-form-item>
                </el-form>
            </template>

            <!-- 功能按钮 -->
            <template #headerButtons>
                <el-button type="primary" :icon="CirclePlus" @click="openDialog('新增')">新增角色</el-button>
            </template>

            <!-- 表格 -->
            <template #table>
                <el-table v-loading="loading" :data="roleList" style="width: 100%" border>
                    <el-table-column prop="label" label="角色名称" width="180" />
                    <el-table-column prop="value" label="角色值" width="150" />
                    <el-table-column label="操作" fixed="right" align="center">
                        <template #default="{ row }">
                            <el-button type="primary" link :icon="View" @click="openDialog('查看', row)">查看</el-button>
                            <el-button type="primary" link :icon="EditPen" @click="openDialog('编辑', row)">编辑</el-button>
                            <el-button type="danger" link :icon="Delete" @click="deleteRole(row)">删除</el-button>
                        </template>
                    </el-table-column>
                </el-table>
            </template>
        </QueryPage>

        <!-- 角色对话框 -->
        <el-dialog v-model="dialogVisible" :title="dialogTitle" width="500px">
            <el-form :model="form" label-width="120px">
                <el-form-item label="角色名称">
                    <el-input v-model="form.label" placeholder="请输入角色名称" />
                </el-form-item>
                <el-form-item label="角色值">
                    <el-input v-model="form.value" placeholder="请输入角色值" />
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

<script setup lang="ts" name="role">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { CirclePlus, EditPen, View, Delete } from '@element-plus/icons-vue'
import { useRoleStore } from '@/stores/modules/role'
import QueryPage from '@/components/QueryPage/index.vue'

// 状态管理
const loading = ref(false)
const collapsed = ref(true)
const roleStore = useRoleStore()
const roleList = ref(roleStore.rolesGet)
const dialogVisible = ref(false)
const dialogTitle = ref('')
const dialogType = ref('')
const currentRoleValue = ref('')

// 表单数据
const searchForm = reactive({
    label: '',
    value: ''
})

const pagination = reactive({
    currentPage: 1,
    pageSize: 10,
    total: 0
})

const form = reactive({
    label: '',
    value: ''
})

// 加载角色列表
const loadRoles = () => {
    roleList.value = roleStore.rolesGet
    pagination.total = roleList.value.length
}

// API 调用
const getRoleList = async () => {
    try {
        await roleStore.getRoles()
        loadRoles()
    } catch (error) {
        ElMessage.error('获取角色列表失败')
    }
}

// 事件处理
const handleSearch = () => {
    // 过滤角色列表
    const filteredRoles = roleStore.rolesGet.filter(role => {
        return (
            role.label.toLowerCase().includes(searchForm.label.toLowerCase()) &&
            role.value.toLowerCase().includes(searchForm.value.toLowerCase())
        )
    })
    roleList.value = filteredRoles
    pagination.total = filteredRoles.length
}

const resetSearch = () => {
    searchForm.label = ''
    searchForm.value = ''
    loadRoles()
}

const handleSizeChange = (size: number) => {
    pagination.pageSize = size
    loadRoles()
}

const handleCurrentChange = (current: number) => {
    pagination.currentPage = current
    loadRoles()
}

const openDialog = (type: string, row?: any) => {
    dialogTitle.value = type
    dialogType.value = type

    if (type === '新增') {
        // 重置表单
        Object.assign(form, {
            label: '',
            value: ''
        })
        currentRoleValue.value = ''
    } else if (row) {
        // 填充表单数据
        Object.assign(form, {
            label: row.label,
            value: row.value
        })
        currentRoleValue.value = row.value
    }

    dialogVisible.value = true
}

const submitForm = async () => {
    try {
        if (dialogType.value === '新增') {
            roleStore.addRole(form)
            ElMessage.success('新增角色成功')
        } else if (dialogType.value === '编辑' && currentRoleValue.value) {
            roleStore.updateRole(form)
            ElMessage.success('更新角色成功')
        }
        dialogVisible.value = false
        loadRoles()
    } catch (error) {
        ElMessage.error('操作失败')
    }
}

const deleteRole = (row: any) => {
    ElMessageBox.confirm(
        '确定要删除这个角色吗？',
        '删除角色',
        {
            confirmButtonText: '确定',
            cancelButtonText: '取消',
            type: 'warning'
        }
    ).then(async () => {
        try {
            roleStore.removeRole(row.value)
            ElMessage.success('删除角色成功')
            loadRoles()
        } catch (error) {
            ElMessage.error('删除失败')
        }
    }).catch(() => {
        // 取消删除
    })
}

// 初始化
onMounted(() => {
    getRoleList()
})
</script>

<style scoped lang="scss"></style>
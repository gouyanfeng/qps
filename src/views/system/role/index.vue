<template>
    <div class="list-page">
        <QueryPage api="/admin/roles" :searchParam="searchForm" @reset="handleReset" ref="queryPageRef">
            <!-- 搜索条件 -->
            <template #searchConditions>
                <el-form :model="searchForm" :inline="true">
                    <el-form-item label="角色名称">
                        <el-input v-model="searchForm.name" placeholder="请输入角色名称" />
                    </el-form-item>
                    <el-form-item label="角色值">
                        <el-input v-model="searchForm.code" placeholder="请输入角色值" />
                    </el-form-item>
                </el-form>
            </template>

            <!-- 功能按钮 -->
            <template #headerButtons>
                <el-button v-if="BUTTONS.add" type="primary" :icon="CirclePlus" @click="openDialog('新增')">新增角色</el-button>
            </template>

            <!-- 表格 -->
            <template #table="{ tableData }">
                <el-table :data="tableData" :fit="true" style="width: 100% " border>
                    <el-table-column prop="name" label="角色名称" min-width="180" show-overflow-tooltip />
                    <el-table-column prop="code" label="角色值" min-width="160" show-overflow-tooltip />
                    <el-table-column label="操作" align="center" width="180" fixed="right">
                        <template #default="{ row }">
                            <el-button v-if="BUTTONS.edit" type="primary" link :icon="EditPen" @click="openDialog('编辑', row)">编辑</el-button>
                            <el-button v-if="BUTTONS.delete" type="danger" link :icon="Delete" @click="deleteRole(row)">删除</el-button>
                        </template>
                    </el-table-column>
                </el-table>
            </template>
        </QueryPage>

        <!-- 角色对话框 -->
        <el-dialog v-model="dialogVisible" :title="dialogTitle" width="500px">
            <el-form :model="form" label-width="120px">
                <el-form-item label="角色名称">
                    <el-input v-model="form.name" placeholder="请输入角色名称" />
                </el-form-item>
                <el-form-item label="角色值">
                    <el-input v-model="form.code" placeholder="请输入角色值" />
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
import { ref, reactive } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { CirclePlus, EditPen, Delete } from '@element-plus/icons-vue'
import { roleApi } from '@/api/modules/role'
import QueryPage from '@/components/QueryPage/index.vue'
import { useAuthButtons } from '@/hooks/useAuthButtons'

const { BUTTONS } = useAuthButtons()

// 引用
const queryPageRef = ref()

// 状态管理
const dialogVisible = ref(false)
const dialogTitle = ref('')
const dialogType = ref('')
const currentRoleValue = ref('')

// 表单数据
const searchForm = reactive({
    name: '',
    code: ''
})

const form = reactive({
    name: '',
    code: ''
})

// 处理重置事件
const handleReset = () => {
    // 重置搜索表单
    Object.assign(searchForm, {
        name: '',
        code: ''
    })
}

// 事件处理
const openDialog = (type: string, row?: any) => {
    dialogTitle.value = type
    dialogType.value = type

    if (type === '新增') {
        // 重置表单
        Object.assign(form, {
            name: '',
            code: ''
        })
        currentRoleValue.value = ''
    } else if (row) {
        // 填充表单数据
        Object.assign(form, {
            name: row.name,
            code: row.code
        })
        currentRoleValue.value = row.id
    }

    dialogVisible.value = true
}

const submitForm = async () => {
    try {
        if (dialogType.value === '新增') {
            await roleApi.addRole(form)
            ElMessage.success('新增角色成功')
        } else if (dialogType.value === '编辑' && currentRoleValue.value) {
            await roleApi.updateRole(currentRoleValue.value, form)
            ElMessage.success('更新角色成功')
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

const deleteRole = async (row: any) => {
    try {
        await ElMessageBox.confirm('确定要删除这个角色吗？', '删除角色', {
            confirmButtonText: '确定',
            cancelButtonText: '取消',
            type: 'warning'
        })
        await roleApi.deleteRole(row.id)
        ElMessage.success('删除角色成功')
        // 重新获取数据
        if (queryPageRef.value) {
            queryPageRef.value.getTableList()
        }
    } catch (error) {
        // 取消删除
    }
}




</script>

<style scoped lang="scss"></style>



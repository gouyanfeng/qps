<template>
    <div class="list-page">
        <QueryPage api="/admin/rooms" :searchParam="searchForm" @reset="handleReset" ref="queryPageRef">
            <!-- 搜索条件 -->
            <template #searchConditions>
                <el-form :model="searchForm" :inline="true">
                    <el-form-item label="房间名称">
                        <el-input v-model="searchForm.name" placeholder="请输入房间名称" />
                    </el-form-item>
                    <el-form-item label="房间类型">
                        <el-input v-model="searchForm.type" placeholder="请输入房间类型" />
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
                <el-button type="primary" :icon="CirclePlus" @click="openDialog('新增')">新增房间</el-button>
            </template>

            <!-- 表格 -->
            <template #table="{ tableData }">
                <el-table :data="tableData" style="width: 100%" border>
                    <el-table-column prop="name" label="房间名称" width="180" />
                    <el-table-column prop="type" label="房间类型" width="150" />
                    <el-table-column prop="capacity" label="容量" width="100" />
                    <el-table-column prop="price" label="价格" width="100" />
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
                            <el-button type="danger" link :icon="Delete" @click="deleteRoom(row)">删除</el-button>
                        </template>
                    </el-table-column>
                </el-table>
            </template>
        </QueryPage>

        <!-- 房间对话框 -->
        <el-dialog v-model="dialogVisible" :title="dialogTitle" width="500px">
            <el-form :model="form" label-width="120px">
                <el-form-item label="房间名称">
                    <el-input v-model="form.name" placeholder="请输入房间名称" />
                </el-form-item>
                <el-form-item label="房间类型">
                    <el-input v-model="form.type" placeholder="请输入房间类型" />
                </el-form-item>
                <el-form-item label="容量">
                    <el-input v-model.number="form.capacity" type="number" placeholder="请输入容量" />
                </el-form-item>
                <el-form-item label="价格">
                    <el-input v-model.number="form.price" type="number" placeholder="请输入价格" />
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

<script setup lang="ts" name="rooms">
import { ref, reactive } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { CirclePlus, EditPen, View, Delete } from '@element-plus/icons-vue'
import { roomApi } from '@/api/modules/room'
import QueryPage from '@/components/QueryPage/index.vue'

// 引用
const queryPageRef = ref()

// 状态管理
const dialogVisible = ref(false)
const dialogTitle = ref('')
const dialogType = ref('')
const currentRoomId = ref('')

// 表单数据
const searchForm = reactive({
    name: '',
    type: '',
    isActive: undefined as boolean | undefined
})

const form = reactive({
    name: '',
    type: '',
    capacity: 0,
    price: 0,
    isActive: true
})

// 工具函数
const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString()
}

// 处理重置事件
const handleReset = () => {
    // 重置搜索表单
    Object.assign(searchForm, {
        name: '',
        type: '',
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
            name: '',
            type: '',
            capacity: 0,
            price: 0,
            isActive: true
        })
        currentRoomId.value = ''
    } else if (row) {
        // 填充表单数据
        Object.assign(form, {
            name: row.name,
            type: row.type,
            capacity: row.capacity,
            price: row.price,
            isActive: row.isActive
        })
        currentRoomId.value = row.id
    }

    dialogVisible.value = true
}

const submitForm = async () => {
    try {
        if (dialogType.value === '新增') {
            await roomApi.addRoom(form)
            ElMessage.success('新增房间成功')
        } else if (dialogType.value === '编辑' && currentRoomId.value) {
            await roomApi.updateRoom(currentRoomId.value, form)
            ElMessage.success('更新房间成功')
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

const deleteRoom = async (row: any) => {
    try {
        await ElMessageBox.confirm('确定要删除这个房间吗？', '删除房间', {
            confirmButtonText: '确定',
            cancelButtonText: '取消',
            type: 'warning'
        })
        await roomApi.deleteRoom(row.id)
        ElMessage.success('删除房间成功')
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
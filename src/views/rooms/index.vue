<template>
    <div class="list-page">
        <QueryPage api="/admin/rooms" :searchParam="searchForm" @reset="handleReset" ref="queryPageRef">
            <!-- 搜索条件 -->
            <template #searchConditions>
                <el-form :model="searchForm" :inline="true">
                    <el-form-item label="房间号">
                        <el-input v-model="searchForm.roomNumber" placeholder="请输入房间号" />
                    </el-form-item>
                    <el-form-item label="状态">
                        <el-select v-model="searchForm.status" placeholder="请选择状态">
                            <el-option label="空闲" value="idle" />
                            <el-option label="使用中" value="occupied" />
                            <el-option label="维护中" value="maintenance" />
                        </el-select>
                    </el-form-item>
                    <el-form-item label="是否启用">
                        <el-select v-model="searchForm.isEnabled" placeholder="请选择">
                            <el-option label="启用" :value="true" />
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
                    <el-table-column prop="roomNumber" label="房间号" width="150" />
                    <el-table-column prop="shopId" label="所属门店" width="180" />
                    <el-table-column prop="unitPrice" label="单价" width="120">
                        <template #default="{ row }">
                            ¥{{ row.unitPrice }}
                        </template>
                    </el-table-column>
                    <el-table-column prop="status" label="状态" width="120">
                        <template #default="{ row }">
                            <el-tag :type="getStatusType(row.status)">
                                {{ getStatusText(row.status) }}
                            </el-tag>
                        </template>
                    </el-table-column>
                    <el-table-column prop="isEnabled" label="是否启用" width="120">
                        <template #default="{ row }">
                            <el-switch :model-value="row.isEnabled" disabled />
                        </template>
                    </el-table-column>
                    <el-table-column prop="createdAt" label="创建时间" width="200">
                        <template #default="{ row }">
                            {{ formatDate(row.createdAt) }}
                        </template>
                    </el-table-column>
                    <el-table-column label="操作" align="center">
                        <template #default="{ row }">
                            <el-button type="primary" link :icon="View" @click="openDialog('查看', row)">查看</el-button>
                            <el-button type="primary" link :icon="EditPen" @click="openDialog('编辑', row)">编辑</el-button>
                            <el-button :type="row.status === 'occupied' ? 'success' : 'warning'" link
                                :icon="row.status === 'occupied' ? Sunrise : Sunny" @click="handlePower(row)">
                                {{ row.status === 'occupied' ? '断电' : '通电' }}
                            </el-button>
                            <el-button type="danger" link :icon="Delete" @click="deleteRoom(row)">删除</el-button>
                        </template>
                    </el-table-column>
                </el-table>
            </template>
        </QueryPage>

        <!-- 房间对话框 -->
        <el-dialog v-model="dialogVisible" :title="dialogTitle" width="500px">
            <el-form :model="form" label-width="120px">
                <el-form-item label="房间号">
                    <el-input v-model="form.roomNumber" placeholder="请输入房间号" />
                </el-form-item>
                <el-form-item label="所属门店">
                    <el-input v-model="form.shopId" placeholder="请输入门店ID" />
                </el-form-item>
                <el-form-item label="单价">
                    <el-input v-model.number="form.unitPrice" type="number" placeholder="请输入单价" />
                </el-form-item>
                <el-form-item label="是否启用">
                    <el-switch v-model="form.isEnabled" active-text="启用" inactive-text="禁用" />
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
import { CirclePlus, EditPen, View, Delete, Sunrise, Sunny } from '@element-plus/icons-vue'
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
    roomNumber: '',
    status: '',
    isEnabled: undefined as boolean | undefined
})

const form = reactive({
    roomNumber: '',
    shopId: '',
    unitPrice: 0,
    isEnabled: true
})

// 工具函数
const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString()
}

const getStatusType = (status: string) => {
    switch (status) {
        case 'idle':
            return 'success'
        case 'occupied':
            return 'warning'
        case 'maintenance':
            return 'danger'
        default:
            return 'info'
    }
}

const getStatusText = (status: string) => {
    switch (status) {
        case 'idle':
            return '空闲'
        case 'occupied':
            return '使用中'
        case 'maintenance':
            return '维护中'
        default:
            return status
    }
}

// 处理重置事件
const handleReset = () => {
    // 重置搜索表单
    Object.assign(searchForm, {
        roomNumber: '',
        status: '',
        isEnabled: undefined
    })
}

// 事件处理
const openDialog = (type: string, row?: any) => {
    dialogTitle.value = type
    dialogType.value = type

    if (type === '新增') {
        // 重置表单
        Object.assign(form, {
            roomNumber: '',
            shopId: '',
            unitPrice: 0,
            isEnabled: true
        })
        currentRoomId.value = ''
    } else if (row) {
        // 填充表单数据
        Object.assign(form, {
            roomNumber: row.roomNumber,
            shopId: row.shopId,
            unitPrice: row.unitPrice,
            isEnabled: row.isEnabled
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

const handlePower = async (row: any) => {
    try {
        const powerOn = row.status !== 'occupied'
        await roomApi.togglePower(row.id, powerOn)
        ElMessage.success(powerOn ? '通电成功' : '断电成功')
        // 重新获取数据
        if (queryPageRef.value) {
            queryPageRef.value.getTableList()
        }
    } catch (error) {
        ElMessage.error('电源控制失败')
    }
}

</script>

<style scoped lang="scss"></style>
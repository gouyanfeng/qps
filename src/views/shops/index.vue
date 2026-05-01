<template>
    <div class="list-page">
        <QueryPage api="/admin/shops" :searchParam="searchForm" @reset="handleReset" ref="queryPageRef">
            <!-- 搜索条件 -->
            <template #searchConditions>
                <el-form :model="searchForm" :inline="true">
                    <el-form-item label="门店名称">
                        <el-input v-model="searchForm.name" placeholder="请输入门店名称" />
                    </el-form-item>
                </el-form>
            </template>

            <!-- 功能按钮 -->
            <template #headerButtons>
                <el-button type="primary" :icon="CirclePlus" @click="openDialog('新增')">新增门店</el-button>
            </template>

            <!-- 表格 -->
            <template #table="{ tableData }">
                <el-table :data="tableData" style="width: 100%" border>
                    <el-table-column prop="name" label="门店名称" width="180" />
                    <el-table-column prop="address" label="地址" width="250" />
                    <el-table-column prop="phone" label="联系电话" width="150" />
                    <el-table-column prop="createdAt" label="创建时间" width="200">
                        <template #default="{ row }">
                            {{ formatDate(row.createdAt) }}
                        </template>
                    </el-table-column>
                    <el-table-column label="操作" align="center">
                        <template #default="{ row }">
                            <el-button type="primary" link :icon="View" @click="openDialog('查看', row)">查看</el-button>
                            <el-button type="primary" link :icon="EditPen" @click="openDialog('编辑', row)">编辑</el-button>
                            <el-button type="danger" link :icon="Delete" @click="deleteShop(row)">删除</el-button>
                        </template>
                    </el-table-column>
                </el-table>
            </template>
        </QueryPage>

        <!-- 门店对话框 -->
        <el-dialog v-model="dialogVisible" :title="dialogTitle" width="500px">
            <el-form :model="form" label-width="120px">
                <el-form-item label="门店名称">
                    <el-input v-model="form.name" placeholder="请输入门店名称" />
                </el-form-item>
                <el-form-item label="地址">
                    <el-input v-model="form.address" placeholder="请输入门店地址" />
                </el-form-item>
                <el-form-item label="联系电话">
                    <el-input v-model="form.phone" placeholder="请输入联系电话" />
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

<script setup lang="ts" name="shops">
import { ref, reactive } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { CirclePlus, EditPen, View, Delete } from '@element-plus/icons-vue'
import { shopApi } from '@/api/modules/shop'
import QueryPage from '@/components/QueryPage/index.vue'

// 引用
const queryPageRef = ref()

// 状态管理
const dialogVisible = ref(false)
const dialogTitle = ref('')
const dialogType = ref('')
const currentShopId = ref('')

// 表单数据
const searchForm = reactive({
    name: ''
})

const form = reactive({
    name: '',
    address: '',
    phone: ''
})

// 工具函数
const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString()
}

// 处理重置事件
const handleReset = () => {
    Object.assign(searchForm, {
        name: ''
    })
}

// 事件处理
const openDialog = (type: string, row?: any) => {
    dialogTitle.value = type
    dialogType.value = type

    if (type === '新增') {
        Object.assign(form, {
            name: '',
            address: '',
            phone: ''
        })
        currentShopId.value = ''
    } else if (row) {
        Object.assign(form, {
            name: row.name,
            address: row.address,
            phone: row.phone
        })
        currentShopId.value = row.id
    }

    dialogVisible.value = true
}

const submitForm = async () => {
    try {
        if (dialogType.value === '新增') {
            await shopApi.addShop(form)
            ElMessage.success('新增门店成功')
        } else if (dialogType.value === '编辑' && currentShopId.value) {
            await shopApi.updateShop(currentShopId.value, form)
            ElMessage.success('更新门店成功')
        }
        dialogVisible.value = false
        if (queryPageRef.value) {
            queryPageRef.value.getTableList()
        }
    } catch (error) {
        ElMessage.error('操作失败')
    }
}

const deleteShop = async (row: any) => {
    try {
        await ElMessageBox.confirm('确定要删除这个门店吗？', '删除门店', {
            confirmButtonText: '确定',
            cancelButtonText: '取消',
            type: 'warning'
        })
        await shopApi.deleteShop(row.id)
        ElMessage.success('删除门店成功')
        if (queryPageRef.value) {
            queryPageRef.value.getTableList()
        }
    } catch (error) {
        // 取消删除
    }
}

</script>

<style scoped lang="scss"></style>
<template>
    <div class="list-page">
        <QueryPage api="/admin/plans" :searchParam="searchForm" @reset="handleReset" ref="queryPageRef">
            <!-- 搜索条件 -->
            <template #searchConditions>
                <el-form :model="searchForm" :inline="true">
                    <el-form-item label="套餐名称">
                        <el-input v-model="searchForm.name" placeholder="请输入套餐名称" />
                    </el-form-item>
                    <el-form-item label="是否启用">
                        <el-select v-model="searchForm.isActive" placeholder="请选择" style="width: 200px">
                            <el-option label="启用" :value="true" />
                            <el-option label="禁用" :value="false" />
                        </el-select>
                    </el-form-item>
                </el-form>
            </template>

            <!-- 功能按钮 -->
            <template #headerButtons>
                <el-button type="primary" :icon="CirclePlus" @click="openDialog('新增')">新增套餐</el-button>
            </template>

            <!-- 表格 -->
            <template #table="{ tableData }">
                <el-table :data="tableData" style="width: 100%" border>
                    <el-table-column prop="name" label="套餐名称" width="180" />
                    <el-table-column prop="description" label="描述" width="250" />
                    <el-table-column prop="price" label="价格" width="120">
                        <template #default="{ row }">
                            ¥{{ row.price }}
                        </template>
                    </el-table-column>
                    <el-table-column prop="durationMinutes" label="时长(分钟)" width="150" />
                    <el-table-column prop="isActive" label="状态" width="100">
                        <template #default="{ row }">
                            <el-tag :type="row.isActive ? 'success' : 'danger'">
                                {{ row.isActive ? '启用' : '禁用' }}
                            </el-tag>
                        </template>
                    </el-table-column>
                    <el-table-column prop="createdAt" label="创建时间" width="200">
                        <template #default="{ row }">
                            {{ formatDate(row.createdAt) }}
                        </template>
                    </el-table-column>
                    <el-table-column label="操作" align="center">
                        <template #default="{ row }">
                            <el-button type="primary" link :icon="EditPen" @click="openDialog('编辑', row)">编辑</el-button>
                            <el-button :type="row.isActive ? 'warning' : 'success'" link
                                :icon="row.isActive ? Sunrise : Sunny" @click="toggleStatus(row)">
                                {{ row.isActive ? '禁用' : '启用' }}
                            </el-button>
                            <el-button type="danger" link :icon="Delete" @click="deletePlan(row)">删除</el-button>
                        </template>
                    </el-table-column>
                </el-table>
            </template>
        </QueryPage>

        <!-- 套餐对话框 -->
        <el-dialog v-model="dialogVisible" :title="dialogTitle" width="500px">
            <el-form :model="form" label-width="120px">
                <el-form-item label="套餐名称">
                    <el-input v-model="form.name" placeholder="请输入套餐名称" />
                </el-form-item>
                <el-form-item label="描述">
                    <el-input v-model="form.description" type="textarea" placeholder="请输入套餐描述" />
                </el-form-item>
                <el-form-item label="价格">
                    <el-input v-model.number="form.price" type="number" placeholder="请输入价格" />
                </el-form-item>
                <el-form-item label="时长(分钟)">
                    <el-input v-model.number="form.durationMinutes" type="number" placeholder="请输入时长" />
                </el-form-item>
                <el-form-item label="状态">
                    <el-switch v-model="form.isActive" active-text="启用" inactive-text="禁用" />
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

<script setup lang="ts" name="plans">
import { ref, reactive } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { CirclePlus, EditPen, View, Delete, Sunrise, Sunny } from '@element-plus/icons-vue'
import { planApi } from '@/api/modules/plan'
import QueryPage from '@/components/QueryPage/index.vue'
import { formatDate } from '@/utils'

// 引用
const queryPageRef = ref()

// 状态管理
const dialogVisible = ref(false)
const dialogTitle = ref('')
const dialogType = ref('')
const currentPlanId = ref('')

// 表单数据
const searchForm = reactive({
    name: '',
    isActive: undefined as boolean | undefined
})

const form = reactive({
    name: '',
    description: '',
    price: 0,
    durationMinutes: 0,
    isActive: true
})

// 处理重置事件
const handleReset = () => {
    Object.assign(searchForm, {
        name: '',
        isActive: undefined
    })
}

// 事件处理
const openDialog = (type: string, row?: any) => {
    dialogTitle.value = type
    dialogType.value = type

    if (type === '新增') {
        Object.assign(form, {
            name: '',
            description: '',
            price: 0,
            durationMinutes: 0,
            isActive: true
        })
        currentPlanId.value = ''
    } else if (row) {
        Object.assign(form, {
            name: row.name,
            description: row.description,
            price: row.price,
            durationMinutes: row.durationMinutes,
            isActive: row.isActive
        })
        currentPlanId.value = row.id
    }

    dialogVisible.value = true
}

const submitForm = async () => {
    try {
        if (dialogType.value === '新增') {
            await planApi.addPlan(form)
            ElMessage.success('新增套餐成功')
        } else if (dialogType.value === '编辑' && currentPlanId.value) {
            await planApi.updatePlan(currentPlanId.value, form)
            ElMessage.success('更新套餐成功')
        }
        dialogVisible.value = false
        if (queryPageRef.value) {
            queryPageRef.value.getTableList()
        }
    } catch (error) {
        ElMessage.error('操作失败')
    }
}

const deletePlan = async (row: any) => {
    try {
        await ElMessageBox.confirm('确定要删除这个套餐吗？', '删除套餐', {
            confirmButtonText: '确定',
            cancelButtonText: '取消',
            type: 'warning'
        })
        await planApi.deletePlan(row.id)
        ElMessage.success('删除套餐成功')
        if (queryPageRef.value) {
            queryPageRef.value.getTableList()
        }
    } catch (error) {
        // 取消删除
    }
}

const toggleStatus = async (row: any) => {
    try {
        await planApi.togglePlanStatus(row.id, !row.isActive)
        ElMessage.success(row.isActive ? '套餐已禁用' : '套餐已启用')
        if (queryPageRef.value) {
            queryPageRef.value.getTableList()
        }
    } catch (error) {
        ElMessage.error('操作失败')
    }
}

</script>

<style scoped lang="scss"></style>
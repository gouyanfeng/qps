<template>
    <div class="list-page">
        <QueryPage api="/admin/tags" :searchParam="searchForm" @reset="handleReset" ref="queryPageRef">
            <!-- 搜索条件 -->
            <template #searchConditions>
                <el-form :model="searchForm" :inline="true">
                    <el-form-item label="标签名称">
                        <el-input v-model="searchForm.Name" placeholder="请输入标签名称" />
                    </el-form-item>
                </el-form>
            </template>

            <!-- 功能按钮 -->
            <template #headerButtons>
                <el-button type="primary" :icon="CirclePlus" @click="openDialog('新增')">新增标签</el-button>
            </template>

            <!-- 表格 -->
            <template #table="{ tableData }">
                <el-table :data="tableData" style="width: 100%" border>
                    <el-table-column prop="tagName" label="标签名称" width="180" />
                    <el-table-column prop="createdAt" label="创建时间" width="200">
                        <template #default="{ row }">
                            {{ formatDate(row.createdAt) }}
                        </template>
                    </el-table-column>
                    <el-table-column label="操作" align="center">
                        <template #default="{ row }">
                            <el-button type="primary" link :icon="EditPen" @click="openDialog('编辑', row)">编辑</el-button>
                            <el-button type="danger" link :icon="Delete" @click="deleteTag(row)">删除</el-button>
                        </template>
                    </el-table-column>
                </el-table>
            </template>
        </QueryPage>

        <!-- 标签对话框 -->
        <el-dialog v-model="dialogVisible" :title="dialogTitle" width="400px">
            <el-form :model="form" label-width="80px">
                <el-form-item label="标签名称">
                    <el-input v-model="form.tagName" placeholder="请输入标签名称" />
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

<script setup lang="ts" name="tags">
import { ref, reactive } from 'vue'
import { ElMessage } from 'element-plus'
import { CirclePlus, EditPen, View, Delete } from '@element-plus/icons-vue'
import { tagApi } from '@/api/modules/tag'
import QueryPage from '@/components/QueryPage/index.vue'

const queryPageRef = ref()
const dialogVisible = ref(false)
const dialogTitle = ref('')
const dialogType = ref('')
const currentTagId = ref('')

const searchForm = reactive({
    Name: ''
})

const form = reactive({
    tagName: ''
})

const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString()
}

const handleReset = () => {
    searchForm.Name = ''
}

const openDialog = (type: string, row?: any) => {
    dialogTitle.value = type
    dialogType.value = type

    if (type === '新增') {
        form.tagName = ''
        currentTagId.value = ''
    } else if (row) {
        form.tagName = row.tagName || ''
        currentTagId.value = row.id
    }

    dialogVisible.value = true
}

const submitForm = async () => {
    try {
        if (dialogType.value === '新增') {
            await tagApi.createTag(form)
            ElMessage.success('新增标签成功')
        } else if (dialogType.value === '编辑' && currentTagId.value) {
            await tagApi.updateTag(currentTagId.value, form)
            ElMessage.success('更新标签成功')
        }
        dialogVisible.value = false
        if (queryPageRef.value) {
            queryPageRef.value.getTableList()
        }
    } catch (error) {
        ElMessage.error('操作失败')
    }
}

const deleteTag = async (row: any) => {
    try {
        await tagApi.deleteTag(row.id)
        ElMessage.success('删除标签成功')
        if (queryPageRef.value) {
            queryPageRef.value.getTableList()
        }
    } catch (error) {
        ElMessage.error('删除失败')
    }
}
</script>

<style scoped lang="scss"></style>
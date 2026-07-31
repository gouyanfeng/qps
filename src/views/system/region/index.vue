<template>
    <div class="list-page">
        <QueryPage api="/admin/regions" :searchParam="searchForm" @reset="handleReset" ref="queryPageRef">
            <template #searchConditions>
                <el-form :model="searchForm" :inline="true">
                    <el-form-item label="区域编码">
                        <el-input v-model="searchForm.code" placeholder="请输入区域编码" />
                    </el-form-item>
                    <el-form-item label="区域名称">
                        <el-input v-model="searchForm.name" placeholder="请输入区域名称" />
                    </el-form-item>
                    <el-form-item label="区域层级">
                        <el-select v-model="searchForm.level" placeholder="请选择层级" clearable>
                            <el-option label="省" :value="1" />
                            <el-option label="市" :value="2" />
                            <el-option label="区县" :value="3" />
                        </el-select>
                    </el-form-item>
                    <el-form-item label="状态">
                        <el-select v-model="searchForm.isActive" placeholder="请选择状态" clearable>
                            <el-option label="启用" :value="true" />
                            <el-option label="禁用" :value="false" />
                        </el-select>
                    </el-form-item>
                </el-form>
            </template>

            <template #headerButtons>
                <el-button v-if="BUTTONS.add" type="primary" :icon="CirclePlus" @click="openDialog('新增')">
                    新增区域
                </el-button>
            </template>

            <template #table="{ tableData }">
                <el-table :data="tableData" :fit="true" style="width: 100%" border>
                    <el-table-column prop="code" label="区域编码" min-width="160" show-overflow-tooltip />
                    <el-table-column prop="name" label="区域名称" min-width="180" show-overflow-tooltip />
                    <el-table-column prop="level" label="层级" width="100">
                        <template #default="{ row }">
                            {{ levelText(row.level) }}
                        </template>
                    </el-table-column>
                    <el-table-column prop="parentName" label="上级区域" min-width="160" show-overflow-tooltip>
                        <template #default="{ row }">
                            <span v-if="row.parentName">{{ row.parentName }}</span>
                            <span v-else class="text-gray">无</span>
                        </template>
                    </el-table-column>
                    <el-table-column prop="sortOrder" label="排序" width="90" />
                    <el-table-column prop="isActive" label="状态" width="100">
                        <template #default="{ row }">
                            <el-tag :type="row.isActive ? 'success' : 'warning'">
                                {{ row.isActive ? '启用' : '禁用' }}
                            </el-tag>
                        </template>
                    </el-table-column>
                    <el-table-column label="操作" align="center" width="180" fixed="right">
                        <template #default="{ row }">
                            <el-button v-if="BUTTONS.edit" type="primary" link :icon="EditPen"
                                @click="openDialog('编辑', row)">编辑</el-button>
                            <el-button v-if="BUTTONS.delete" type="danger" link :icon="Delete"
                                @click="deleteRegion(row)">删除</el-button>
                        </template>
                    </el-table-column>
                </el-table>
            </template>
        </QueryPage>

        <el-dialog v-model="dialogVisible" :title="dialogTitle" width="520px">
            <el-form :model="form" label-width="110px">
                <el-form-item label="区域编码">
                    <el-input v-model="form.code" placeholder="请输入区域编码" />
                </el-form-item>
                <el-form-item label="区域名称">
                    <el-input v-model="form.name" placeholder="请输入区域名称" />
                </el-form-item>
                <el-form-item label="区域层级">
                    <el-select v-model="form.level" placeholder="请选择层级">
                        <el-option label="省" :value="1" />
                        <el-option label="市" :value="2" />
                        <el-option label="区县" :value="3" />
                    </el-select>
                </el-form-item>
                <el-form-item label="上级区域">
                    <el-select v-model="form.parentId" placeholder="请选择上级区域" clearable filterable>
                        <el-option v-for="item in parentOptions" :key="item.id" :label="formatRegionOption(item)"
                            :value="item.id" />
                    </el-select>
                </el-form-item>
                <el-form-item label="排序序号">
                    <el-input-number v-model="form.sortOrder" :min="0" />
                </el-form-item>
                <el-form-item label="状态">
                    <el-switch v-model="form.isActive" />
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

<script setup lang="ts" name="region">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { CirclePlus, EditPen, Delete } from '@element-plus/icons-vue'
import QueryPage from '@/components/QueryPage/index.vue'
import { regionApi } from '@/api/modules/region'
import { useAuthButtons } from '@/hooks/useAuthButtons'

const { BUTTONS } = useAuthButtons()
const queryPageRef = ref()
const parentOptions = ref<any[]>([])

const dialogVisible = ref(false)
const dialogTitle = ref('')
const dialogType = ref('')

const searchForm = reactive({
    code: '',
    name: '',
    level: '',
    isActive: ''
})

const form = reactive({
    id: '',
    parentId: '',
    code: '',
    name: '',
    level: 1,
    sortOrder: 0,
    isActive: true
})

const levelText = (level: number) => {
    const map: Record<number, string> = {
        1: '省',
        2: '市',
        3: '区县'
    }
    return map[level] || '未知'
}

const formatRegionOption = (item: any) => {
    return `${item.name}（${item.code}）`
}

const handleReset = () => {
    searchForm.code = ''
    searchForm.name = ''
    searchForm.level = ''
    searchForm.isActive = ''
}

const loadParentOptions = async () => {
    try {
        const res = await regionApi.getRegionList({
            page: 1,
            pageSize: 1000,
            isActive: true
        })
        parentOptions.value = res.data?.list || []
    } catch (error) {
        console.error('获取上级区域失败:', error)
    }
}

const openDialog = async (type: string, row?: any) => {
    dialogType.value = type
    dialogTitle.value = type + '地址区域'
    dialogVisible.value = true
    await loadParentOptions()

    if (type === '新增') {
        form.id = ''
        form.parentId = ''
        form.code = ''
        form.name = ''
        form.level = 1
        form.sortOrder = 0
        form.isActive = true
    } else if (type === '编辑' && row) {
        form.id = row.id
        form.parentId = row.parentId || ''
        form.code = row.code
        form.name = row.name
        form.level = row.level
        form.sortOrder = row.sortOrder || 0
        form.isActive = row.isActive
    }
}

const submitForm = async () => {
    try {
        const params = {
            parentId: form.parentId || null,
            code: form.code,
            name: form.name,
            level: form.level,
            sortOrder: form.sortOrder,
            isActive: form.isActive
        }

        if (dialogType.value === '新增') {
            await regionApi.addRegion(params)
        } else {
            await regionApi.updateRegion(form.id, params)
        }

        dialogVisible.value = false
        await refreshTable()
        ElMessage.success(dialogType.value === '新增' ? '新增成功' : '更新成功')
    } catch (error) {
        console.error('保存区域失败:', error)
        ElMessage.error('操作失败')
    }
}

const deleteRegion = async (row: any) => {
    try {
        await ElMessageBox.confirm(
            '此操作将永久删除该地址区域, 是否继续?',
            '提示',
            {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning'
            }
        )
        await regionApi.deleteRegion(row.id)
        await refreshTable()
        ElMessage.success('删除成功')
    } catch (error) {
        ElMessage.info('已取消删除')
    }
}

const refreshTable = async () => {
    await loadParentOptions()
    if (queryPageRef.value && typeof queryPageRef.value.getTableList === 'function') {
        await queryPageRef.value.getTableList()
    }
}

onMounted(() => {
    loadParentOptions()
})
</script>



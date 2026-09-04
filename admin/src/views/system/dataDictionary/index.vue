<template>
    <div class="list-page">
        <QueryPage api="/admin/data-dictionaries" :searchParam="searchForm" @reset="handleReset" ref="queryPageRef">
            <!-- 搜索条件 -->
            <template #searchConditions>
                <el-form :model="searchForm" :inline="true">
                    <el-form-item label="字典编码">
                        <el-input v-model="searchForm.code" placeholder="请输入字典编码" />
                    </el-form-item>
                    <el-form-item label="字典名称">
                        <el-input v-model="searchForm.name" placeholder="请输入字典名称" />
                    </el-form-item>
                    <el-form-item label="父级字典">
                        <el-tree-select v-model="searchForm.parentId" :data="treeData" :props="treeProps" value-key="id"
                            node-key="id" placeholder="请选择父级字典" clearable filterable check-strictly />
                    </el-form-item>
                    <el-form-item label="状态">
                        <el-select v-model="searchForm.isActive" placeholder="请选择状态">
                            <el-option label="全部" value="" />
                            <el-option label="启用" :value="true" />
                            <el-option label="禁用" :value="false" />
                        </el-select>
                    </el-form-item>
                </el-form>
            </template>

            <!-- 功能按钮 -->
            <template #headerButtons>
                <Permission code="SYSTEM_DATA_DICTIONARY_ADD"><el-button type="primary" :icon="CirclePlus"
                    @click="openDialog('新增')">新增字典</el-button></Permission>
            </template>

            <!-- 表格 -->
            <template #table="{ tableData }">
                <el-table :data="tableData" :fit="true" style="width: 100% " border>
                    <el-table-column prop="code" label="字典编码" min-width="180" show-overflow-tooltip />
                    <el-table-column prop="name" label="字典名称" min-width="160" show-overflow-tooltip />
                    <el-table-column prop="value" label="字典值" min-width="160" show-overflow-tooltip />
                    <el-table-column prop="description" label="描述" min-width="220" show-overflow-tooltip />
                    <el-table-column prop="parentName" label="父级" min-width="150" show-overflow-tooltip>
                        <template #default="{ row }">
                            <span v-if="row.parentName">{{ row.parentName }}</span>
                            <span v-else class="text-gray">无</span>
                        </template>
                    </el-table-column>
                    <el-table-column prop="isActive" label="状态" width="100">
                        <template #default="{ row }">
                            <el-tag :type="row.isActive ? 'success' : 'warning'">
                                {{ row.isActive ? '启用' : '禁用' }}
                            </el-tag>
                        </template>
                    </el-table-column>
                    <el-table-column label="操作" align="center" width="180" fixed="right">
                        <template #default="{ row }">
                            <Permission code="SYSTEM_DATA_DICTIONARY_EDIT"><el-button type="primary" link :icon="EditPen"
                                @click="openDialog('编辑', row)">编辑</el-button></Permission>
                            <Permission v-if="!isHerbProductNode(row)" code="SYSTEM_DATA_DICTIONARY_DELETE"><el-button type="danger" link :icon="Delete"
                                @click="deleteDataDictionary(row)">删除</el-button></Permission>
                        </template>
                    </el-table-column>
                </el-table>
            </template>
        </QueryPage>

        <!-- 对话框 -->
        <el-dialog v-model="dialogVisible" :title="dialogTitle" width="500px">
            <el-form :model="form" label-width="120px">
                <el-form-item label="字典编码">
                    <el-input v-model="form.code" placeholder="请输入字典编码" :disabled="dialogType === '编辑' || isHerbProductItem" />
                </el-form-item>
                <el-form-item label="字典名称">
                    <el-input v-model="form.name" placeholder="请输入字典名称" :disabled="isHerbProductItem && dialogType === '编辑'" @input="syncHerbProductFields" />
                </el-form-item>
                <el-form-item label="字典值">
                    <el-input v-model="form.value" placeholder="请输入字典值" :disabled="isHerbProductItem" />
                </el-form-item>
                <el-form-item label="父级字典">
                    <el-tree-select v-model="form.parentId" :data="treeData" :props="treeProps" value-key="id"
                        node-key="id" placeholder="请选择父级字典" clearable filterable check-strictly :disabled="isHerbProductItem" />
                </el-form-item>
                <el-form-item label="描述">
                    <el-input v-model="form.description" placeholder="请输入描述" type="textarea" />
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

<script setup lang="ts" name="dataDictionary">
import { computed, ref, reactive, onMounted, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { CirclePlus, EditPen, Delete } from '@element-plus/icons-vue'
import { dataDictionaryApi } from '@/api/modules/dataDictionary'
import QueryPage from '@/components/QueryPage/index.vue'
import Permission from '@/components/Permission/index.vue'



// 引用
const queryPageRef = ref()

// 状态管理
const dialogVisible = ref(false)
const dialogTitle = ref('')
const dialogType = ref('')

// 树数据
const treeData = ref<any[]>([])
const treeProps = {
    label: 'label',
    value: 'id',
    children: 'children'
}

// 表单数据
const searchForm = reactive({
    code: '',
    name: '',
    parentId: '',
    isActive: ''
})

const form = reactive({
    id: '',
    code: '',
    name: '',
    value: '',
    description: '',
    sortOrder: 0,
    isActive: true,
    parentId: ''
})

const herbProductDictionaryCode = 'CRM_HERB_PRODUCT'

const herbProductRootId = computed(() => {
    const findRoot = (nodes: any[]): any => {
        for (const node of nodes) {
            if (node.code === herbProductDictionaryCode) return node
            const found = findRoot(node.children || [])
            if (found) return found
        }
    }
    return findRoot(treeData.value)?.id || ''
})

const isHerbProductItem = computed(() =>
    form.code === herbProductDictionaryCode || form.parentId === herbProductRootId.value
)

const isHerbProductNode = (row: any) =>
    row.code === herbProductDictionaryCode || row.parentId === herbProductRootId.value

const syncHerbProductFields = () => {
    if (dialogType.value === '新增' && form.parentId === herbProductRootId.value) {
        form.code = form.name
        form.value = form.name
    }
}

// 重置表单
const handleReset = () => {
    searchForm.code = ''
    searchForm.name = ''
    searchForm.parentId = ''
    searchForm.isActive = ''
}

// 打开对话框
const openDialog = (type: string, row?: any) => {
    dialogType.value = type
    dialogTitle.value = type + '数据字典'
    dialogVisible.value = true

    if (type === '新增') {
        form.id = ''
        form.code = ''
        form.name = ''
        form.value = ''
        form.description = ''
        form.sortOrder = 0
        form.isActive = true
        form.parentId = ''
    } else if (type === '编辑' && row) {
        form.id = row.id
        form.code = row.code
        form.name = row.name
        form.value = row.value
        form.description = row.description
        form.sortOrder = row.sortOrder || 0
        form.isActive = row.isActive
        form.parentId = row.parentId || ''
    }
}

const normalizeTreeNode = (node: any): any => {
    const children = Array.isArray(node.children) ? node.children.map(normalizeTreeNode) : []
    return {
        ...node,
        id: node.id,
        label: node.name || node.label || node.code,
        value: node.id,
        children
    }
}

const loadTreeData = async () => {
    try {
        const res = await dataDictionaryApi.getDataDictionaryTree()
        treeData.value = (res.data || []).map(normalizeTreeNode)
    } catch (error) {
        console.error('获取字典树失败:', error)
    }
}

// 提交表单
const submitForm = async () => {
    try {
        if (dialogType.value === '新增') {
            await dataDictionaryApi.addDataDictionary({
                code: form.code,
                name: form.name,
                value: form.value,
                description: form.description,
                sortOrder: form.sortOrder,
                isActive: form.isActive,
                parentId: form.parentId || null
            })
        } else {
            await dataDictionaryApi.updateDataDictionary(form.id, {
                parentId: form.parentId || null,
                name: form.name,
                value: form.value,
                description: form.description,
                sortOrder: form.sortOrder,
                isActive: form.isActive
            })
        }

        dialogVisible.value = false
        await loadTreeData()
        if (queryPageRef.value && typeof queryPageRef.value.getTableList === 'function') {
            await queryPageRef.value.getTableList()
        }

        ElMessage.success(dialogType.value === '新增' ? '新增成功' : '更新成功')
    } catch (error) {
        console.error('操作失败:', error)
        ElMessage.error('操作失败')
    }
}

// 删除数据字典
const deleteDataDictionary = async (row: any) => {
    try {
        await ElMessageBox.confirm(
            '此操作将永久删除该数据字典, 是否继续?',
            '提示',
            {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning'
            }
        )
        await dataDictionaryApi.deleteDataDictionary(row.id)
        ElMessage.success('删除成功')
        await loadTreeData()
        if (queryPageRef.value && typeof queryPageRef.value.getTableList === 'function') {
            await queryPageRef.value.getTableList()
        }
    } catch (error) {
        ElMessage.info('已取消删除')
    }
}

watch(() => searchForm.parentId, (newValue) => {
    if (queryPageRef.value && typeof queryPageRef.value.getTableList === 'function') {
        queryPageRef.value.getTableList()
    }
})

watch(() => form.parentId, () => {
    syncHerbProductFields()
})

onMounted(() => {
    loadTreeData()
})
</script>











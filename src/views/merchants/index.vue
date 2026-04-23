<template>
    <div class="merchants">
        <QueryPage :pagination="pagination" v-model:collapsed="collapsed" @search="handleSearch" @reset="resetSearch"
            @sizeChange="handleSizeChange" @currentChange="handleCurrentChange">
            <!-- 搜索条件 -->
            <template #searchConditions>
                <el-form :model="searchForm" :inline="true">
                    <el-form-item label="商户名称">
                        <el-input v-model="searchForm.name" placeholder="请输入商户名称" />
                    </el-form-item>
                    <el-form-item label="联系电话">
                        <el-input v-model="searchForm.phone" placeholder="请输入联系电话" />
                    </el-form-item>
                    <el-form-item label="状态">
                        <el-select v-model="searchForm.isActive" placeholder="请选择状态">
                            <el-option label="激活" :value="true" />
                            <el-option label="禁用" :value="false" />
                        </el-select>
                    </el-form-item>
                    <el-form-item label="创建日期" v-show="!collapsed">
                        <el-date-picker v-model="searchForm.createDate" type="date" placeholder="选择创建日期" />
                    </el-form-item>
                    <el-form-item label="过期日期" v-show="!collapsed">
                        <el-date-picker v-model="searchForm.expiryDate" type="date" placeholder="选择过期日期" />
                    </el-form-item>
                </el-form>
            </template>

            <!-- 功能按钮 -->
            <template #headerButtons>
                <el-button type="primary" :icon="CirclePlus" @click="openDialog('新增')">新增商户</el-button>
            </template>

            <!-- 表格 -->
            <template #table>
                <el-table v-loading="loading" :data="merchantList" style="width: 100%" border>
                    <el-table-column prop="name" label="商户名称" width="180" />
                    <el-table-column prop="phone" label="联系电话" width="150" />
                    <el-table-column prop="expiryDate" label="过期日期" width="200">
                        <template #default="{ row }">
                            {{ formatDate(row.expiryDate) }}
                        </template>
                    </el-table-column>
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

        <!-- 商户对话框 -->
        <el-dialog v-model="dialogVisible" :title="dialogTitle" width="500px">
            <el-form :model="form" label-width="120px">
                <el-form-item label="商户名称">
                    <el-input v-model="form.name" placeholder="请输入商户名称" />
                </el-form-item>
                <el-form-item label="联系电话">
                    <el-input v-model="form.phone" placeholder="请输入联系电话" />
                </el-form-item>
                <el-form-item label="过期日期">
                    <el-date-picker v-model="form.expiryDate" type="datetime" placeholder="选择过期日期"
                        style="width: 100%" />
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

<script setup lang="ts" name="merchants">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { Merchant } from '@/api/interface'
import { CirclePlus, EditPen, View } from '@element-plus/icons-vue'
import { merchantApi } from '@/api/modules/merchant'
import QueryPage from '@/components/QueryPage/index.vue'

// 状态管理
const loading = ref(false)
const collapsed = ref(true)
const merchantList = ref<Merchant.ResMerchantList[]>([])
const dialogVisible = ref(false)
const dialogTitle = ref('')
const dialogType = ref('')
const currentMerchantId = ref('')

// 表单数据
const searchForm = reactive({
    name: '',
    phone: '',
    isActive: undefined as boolean | undefined,
    createDate: undefined as string | undefined,
    expiryDate: undefined as string | undefined
})

const pagination = reactive({
    currentPage: 1,
    pageSize: 10,
    total: 0
})

const form = reactive<Merchant.ReqMerchantForm>({
    name: '',
    phone: '',
    expiryDate: new Date().toISOString(),
    isActive: true
})

// 工具函数
const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString()
}

// API 调用
const getMerchantList = async () => {

    try {
        const params = {
            page: pagination.currentPage,
            pageSize: pagination.pageSize,
            ...searchForm
        }
        const response = await merchantApi.getMerchantList(params)
        merchantList.value = response.data.list || []
        pagination.total = response.data.totalCount || 0
    } catch (error) {
        ElMessage.error('获取商户列表失败')
    }
}

// 事件处理
const handleSearch = () => {
    getMerchantList()
}

const resetSearch = () => {
    searchForm.name = ''
    searchForm.phone = ''
    searchForm.isActive = undefined
    searchForm.createDate = undefined
    searchForm.expiryDate = undefined
    getMerchantList()
}

const handleSizeChange = (size: number) => {
    getMerchantList()
}

const handleCurrentChange = (current: number) => {
    getMerchantList()
}

const openDialog = (type: string, row?: Merchant.ResMerchantList) => {
    dialogTitle.value = type
    dialogType.value = type

    if (type === '新增') {
        // 重置表单
        Object.assign(form, {
            name: '',
            phone: '',
            expiryDate: new Date().toISOString(),
            isActive: true
        })
        currentMerchantId.value = ''
    } else if (row) {
        // 填充表单数据
        Object.assign(form, {
            name: row.name,
            phone: row.phone,
            expiryDate: row.expiryDate,
            isActive: row.isActive
        })
        currentMerchantId.value = row.id
    }

    dialogVisible.value = true
}

const submitForm = async () => {
    try {
        if (dialogType.value === '新增') {
            await merchantApi.addMerchant(form)
            ElMessage.success('新增商户成功')
        } else if (dialogType.value === '编辑' && currentMerchantId.value) {
            await merchantApi.updateMerchant(currentMerchantId.value, form)
            ElMessage.success('更新商户成功')
        }
        dialogVisible.value = false
        getMerchantList()
    } catch (error) {
        ElMessage.error('操作失败')
    }
}

// 初始化
onMounted(() => {
    getMerchantList()
})
</script>

<style scoped lang="scss">
@import './index.scss';
</style>
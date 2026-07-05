<template>
    <div class="list-page">
        <QueryPage api="/admin/coupons" :searchParam="searchForm" @reset="handleReset" ref="queryPageRef">
            <!-- 搜索条件 -->
            <template #searchConditions>
                <el-form :model="searchForm" :inline="true">
                    <el-form-item label="优惠券名称">
                        <el-input v-model="searchForm.title" placeholder="请输入优惠券名称" />
                    </el-form-item>
                    <el-form-item label="优惠券类型">
                        <el-select v-model="searchForm.couponType" placeholder="请选择类型" style="width: 200px">
                            <el-option label="满减券" value="discount" />
                            <el-option label="折扣券" value="percent" />
                            <el-option label="礼品券" value="gift" />
                        </el-select>
                    </el-form-item>
                </el-form>
            </template>

            <!-- 功能按钮 -->
            <template #headerButtons>
                <el-button type="primary" :icon="CirclePlus" @click="openDialog('新增')">新增优惠券</el-button>
            </template>

            <!-- 表格 -->
            <template #table="{ tableData }">
                <el-table :data="tableData" style="width: 100%" border>
                    <el-table-column prop="title" label="优惠券名称" width="180" />
                    <el-table-column prop="couponType" label="类型" width="120">
                        <template #default="{ row }">
                            <el-tag :type="getTypeTagType(row.couponType)">
                                {{ getTypeLabel(row.couponType) }}
                            </el-tag>
                        </template>
                    </el-table-column>
                    <el-table-column prop="value" label="优惠金额" width="120">
                        <template #default="{ row }">
                            ¥{{ row.value }}
                        </template>
                    </el-table-column>
                    <el-table-column prop="minConsume" label="最低消费" width="120">
                        <template #default="{ row }">
                            ¥{{ row.minConsume }}
                        </template>
                    </el-table-column>
                    <el-table-column prop="validTo" label="有效期至" width="200">
                        <template #default="{ row }">
                            {{ formatDate(row.validTo) }}
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
                            <el-button type="danger" link :icon="Delete" @click="deleteCoupon(row)">删除</el-button>
                        </template>
                    </el-table-column>
                </el-table>
            </template>
        </QueryPage>

        <!-- 优惠券对话框 -->
        <el-dialog v-model="dialogVisible" :title="dialogTitle" width="500px">
            <el-form :model="form" label-width="120px">
                <el-form-item label="优惠券名称">
                    <el-input v-model="form.title" placeholder="请输入优惠券名称" />
                </el-form-item>
                <el-form-item label="优惠券类型">
                    <el-select v-model="form.couponType" placeholder="请选择类型" style="width: 200px">
                        <el-option label="满减券" value="discount" />
                        <el-option label="折扣券" value="percent" />
                        <el-option label="礼品券" value="gift" />
                    </el-select>
                </el-form-item>
                <el-form-item label="优惠金额">
                    <el-input v-model.number="form.value" type="number" placeholder="请输入优惠金额" />
                </el-form-item>
                <el-form-item label="最低消费">
                    <el-input v-model.number="form.minConsume" type="number" placeholder="请输入最低消费" />
                </el-form-item>
                <el-form-item label="有效期至">
                    <el-date-picker v-model="form.validTo" type="datetime" placeholder="选择有效期至" style="width: 100%" />
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

<script setup lang="ts" name="coupons">
import { ref, reactive } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { CirclePlus, EditPen, View, Delete } from '@element-plus/icons-vue'
import { couponApi } from '@/api/modules/coupon'
import QueryPage from '@/components/QueryPage/index.vue'
import { formatDate } from '@/utils'

// 引用
const queryPageRef = ref()

// 状态管理
const dialogVisible = ref(false)
const dialogTitle = ref('')
const dialogType = ref('')
const currentCouponId = ref('')

// 表单数据
const searchForm = reactive({
    title: '',
    couponType: ''
})

const form = reactive({
    title: '',
    couponType: 'discount',
    value: 0,
    minConsume: 0,
    validTo: ''
})


const getTypeLabel = (type: string) => {
    const typeMap: Record<string, string> = {
        discount: '满减券',
        percent: '折扣券',
        gift: '礼品券'
    }
    return typeMap[type] || type
}

const getTypeTagType = (type: string) => {
    const typeMap: Record<string, string> = {
        discount: 'primary',
        percent: 'success',
        gift: 'warning'
    }
    return typeMap[type] || 'default'
}

// 处理重置事件
const handleReset = () => {
    Object.assign(searchForm, {
        title: '',
        couponType: ''
    })
}

// 事件处理
const openDialog = (type: string, row?: any) => {
    dialogTitle.value = type
    dialogType.value = type

    if (type === '新增') {
        Object.assign(form, {
            title: '',
            couponType: 'discount',
            value: 0,
            minConsume: 0,
            validTo: ''
        })
        currentCouponId.value = ''
    } else if (row) {
        Object.assign(form, {
            title: row.title,
            couponType: row.couponType,
            value: row.value,
            minConsume: row.minConsume,
            validTo: row.validTo
        })
        currentCouponId.value = row.id
    }

    dialogVisible.value = true
}

const submitForm = async () => {
    try {
        if (dialogType.value === '新增') {
            await couponApi.addCoupon(form)
            ElMessage.success('新增优惠券成功')
        } else if (dialogType.value === '编辑' && currentCouponId.value) {
            await couponApi.updateCoupon(currentCouponId.value, form)
            ElMessage.success('更新优惠券成功')
        }
        dialogVisible.value = false
        if (queryPageRef.value) {
            queryPageRef.value.getTableList()
        }
    } catch (error) {
        ElMessage.error('操作失败')
    }
}

const deleteCoupon = async (row: any) => {
    try {
        await ElMessageBox.confirm('确定要删除这个优惠券吗？', '删除优惠券', {
            confirmButtonText: '确定',
            cancelButtonText: '取消',
            type: 'warning'
        })
        await couponApi.deleteCoupon(row.id)
        ElMessage.success('删除优惠券成功')
        if (queryPageRef.value) {
            queryPageRef.value.getTableList()
        }
    } catch (error) {
        // 取消删除
    }
}

</script>

<style scoped lang="scss"></style>
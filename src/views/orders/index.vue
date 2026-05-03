<template>
    <div class="list-page">
        <QueryPage api="/admin/orders" :searchParam="searchForm" @reset="handleReset" ref="queryPageRef">
            <!-- 搜索条件 -->
            <template #searchConditions>
                <el-form :model="searchForm" :inline="true">
                    <el-form-item label="订单号">
                        <el-input v-model="searchForm.OrderNo" placeholder="请输入订单号" />
                    </el-form-item>
                    <el-form-item label="状态">
                        <el-select v-model="searchForm.Status" placeholder="请选择状态" style="width: 200px">
                            <el-option label="待支付" value="pending" />
                            <el-option label="已支付" value="paid" />
                            <el-option label="已完成" value="completed" />
                            <el-option label="已取消" value="cancelled" />
                        </el-select>
                    </el-form-item>
                    <el-form-item label="开始日期">
                        <el-date-picker v-model="searchForm.StartDate" type="datetime" placeholder="选择开始日期" />
                    </el-form-item>
                    <el-form-item label="结束日期">
                        <el-date-picker v-model="searchForm.EndDate" type="datetime" placeholder="选择结束日期" />
                    </el-form-item>
                </el-form>
            </template>

            <!-- 表格 -->
            <template #table="{ tableData }">
                <el-table :data="tableData" style="width: 100%" border>
                    <!-- 展开行 -->
                    <el-table-column type="expand">
                        <template #default="{ row }">
                            <div class="expand-content">
                                <h4 class="section-title">订单项</h4>
                                <el-table :data="row.orderItems || []" style="width: 100%" border>
                                    <el-table-column prop="itemName" label="商品名称" />
                                    <el-table-column prop="quantity" label="数量" />
                                    <el-table-column prop="unitPrice" label="单价">
                                        <template #default="{ row }">¥{{ row.unitPrice }}</template>
                                    </el-table-column>
                                    <el-table-column prop="amount" label="金额">
                                        <template #default="{ row }">¥{{ row.amount }}</template>
                                    </el-table-column>
                                </el-table>
                                <div v-if="!row.orderItems?.length" class="no-data">暂无订单项</div>
                            </div>
                        </template>
                    </el-table-column>
                    <el-table-column prop="orderNo" label="订单号" width="200" />
                    <el-table-column prop="shopName" label="门店" />
                    <el-table-column prop="roomNumber" label="房间号" />
                    <el-table-column prop="customerName" label="客户名称" />
                    <el-table-column prop="actualAmount" label="订单金额">
                        <template #default="{ row }">
                            ¥{{ row.actualAmount }}
                        </template>
                    </el-table-column>
                    <el-table-column prop="status" label="状态">
                        <template #default="{ row }">
                            <el-tag :type="getStatusTagType(row.status)">
                                {{ getStatusLabel(row.status) }}
                            </el-tag>
                        </template>
                    </el-table-column>
                    <el-table-column prop="createdAt" label="创建时间">
                        <template #default="{ row }">
                            {{ formatDate(row.createdAt) }}
                        </template>
                    </el-table-column>
                </el-table>
            </template>
        </QueryPage>

        <!-- 订单对话框 -->
        <el-dialog v-model="dialogVisible" :title="dialogTitle" width="500px">
            <el-form :model="form" label-width="120px">
                <el-form-item label="房间ID">
                    <el-input v-model="form.roomId" placeholder="请输入房间ID" />
                </el-form-item>
                <el-form-item label="订单金额">
                    <el-input v-model.number="form.amount" type="number" placeholder="请输入订单金额" />
                </el-form-item>
                <el-form-item label="时长(分钟)">
                    <el-input v-model.number="form.durationMinutes" type="number" placeholder="请输入时长" />
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

<script setup lang="ts" name="orders">
import { ref, reactive, onMounted, watch } from 'vue'
import { ElMessage } from 'element-plus'
import { CirclePlus, EditPen, View, Check } from '@element-plus/icons-vue'
import { orderApi } from '@/api/modules/order'
import QueryPage from '@/components/QueryPage/index.vue'

// 引用
const queryPageRef = ref()

// 状态管理
const dialogVisible = ref(false)
const dialogTitle = ref('')
const dialogType = ref('')
const currentOrderId = ref('')

// 表单数据
const searchForm = reactive({
    OrderNo: '',
    Status: '',
    ShopId: '',
    RoomId: '',
    CustomerId: '',
    StartDate: undefined as string | undefined,
    EndDate: undefined as string | undefined
})

const form = reactive({
    roomId: '',
    amount: 0,
    durationMinutes: 60
})

// 工具函数
const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString()
}

const getStatusLabel = (status: string) => {
    return status || ''
}

const getStatusTagType = (status: string) => {
    const typeMap: Record<string, string> = {
        '待支付': 'warning',
        '已支付': 'primary',
        '已完成': 'success',
        '已取消': 'danger',
        'pending': 'warning',
        'paid': 'primary',
        'completed': 'success',
        'cancelled': 'danger'
    }
    return typeMap[status] || typeMap[(status || '').toLowerCase()] || 'default'
}

// 处理重置事件
const handleReset = () => {
    Object.assign(searchForm, {
        OrderNo: '',
        Status: '',
        ShopId: '',
        RoomId: '',
        CustomerId: '',
        StartDate: undefined,
        EndDate: undefined
    })
}

// 事件处理
const openDialog = (type: string, row?: any) => {
    dialogTitle.value = type
    dialogType.value = type

    if (type === '新增') {
        Object.assign(form, {
            roomId: '',
            amount: 0,
            durationMinutes: 60
        })
        currentOrderId.value = ''
    } else if (row) {
        Object.assign(form, {
            roomId: row.roomId || '',
            amount: row.amount || 0,
            durationMinutes: row.durationMinutes || 60
        })
        currentOrderId.value = row.id
    }

    dialogVisible.value = true
}

const submitForm = async () => {
    try {
        if (dialogType.value === '新增') {
            await orderApi.createOrder(form)
            ElMessage.success('新增订单成功')
        } else if (dialogType.value === '编辑' && currentOrderId.value) {
            ElMessage.info('订单不支持编辑')
            return
        }
        dialogVisible.value = false
        if (queryPageRef.value) {
            queryPageRef.value.getTableList()
        }
    } catch (error) {
        ElMessage.error('操作失败')
    }
}

const settleOrder = async (row: any) => {
    try {
        await orderApi.settleOrder(row.id)
        ElMessage.success('订单结算成功')
        if (queryPageRef.value) {
            queryPageRef.value.getTableList()
        }
    } catch (error) {
        ElMessage.error('结算失败')
    }
}

</script>

<style scoped lang="scss">
.expand-content {
    padding: 20px;
}

.section-title {
    margin: 0 0 12px 0;
    font-size: 14px;
    font-weight: 600;
    color: #303133;
}

.no-data {
    padding: 30px;
    text-align: center;
    color: #999;
}
</style>
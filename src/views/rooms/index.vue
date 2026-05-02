<template>
    <div class="list-page">
        <!-- 查询页面 -->
        <QueryPage api="/admin/rooms" :searchParam="searchForm" @reset="handleReset" ref="queryPageRef">
            <!-- 搜索条件 -->
            <template #searchConditions>
                <el-form :model="searchForm" :inline="true">
                    <el-form-item label="房间号">
                        <el-input v-model="searchForm.roomNumber" placeholder="请输入房间号" />
                    </el-form-item>
                    <el-form-item label="状态">
                        <el-select v-model="searchForm.status" placeholder="请选择状态" style="width: 200px">
                            <el-option label="空闲" value="idle" />
                            <el-option label="使用中" value="occupied" />
                            <el-option label="维护中" value="maintenance" />
                        </el-select>
                    </el-form-item>
                    <el-form-item label="是否启用">
                        <el-select v-model="searchForm.isEnabled" placeholder="请选择" style="width: 200px">
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
                <el-table :data="tableData" style="width: 100%" border :preserve-expanded-content="preserveExpanded"
                    @expand-change="handleExpandChange">
                    <!-- 展开行 -->
                    <el-table-column type="expand">
                        <template #default="{ row }">
                            <div class="expand-content">
                                <!-- 图片 -->
                                <div class="expand-section">
                                    <h4 class="section-title">房间图片</h4>
                                    <el-upload
                                        :file-list="expandedData[row.id]?.images?.map(img => ({ url: img })) || []"
                                        action="#" list-type="picture-card" :auto-upload="false"
                                        :on-change="(file, files) => handleImageChange(row.id, files)"
                                        :on-remove="(file, files) => handleImageRemove(row.id, files)" accept="image/*">
                                        <div class="upload-btn">
                                            <Plus :size="24" />
                                        </div>
                                    </el-upload>
                                </div>

                                <!-- 标签 -->
                                <div class="expand-section">
                                    <h4 class="section-title">标签</h4>
                                    <el-select v-model="(expandedData[row.id] || {}).tags" multiple placeholder="请选择标签"
                                        style="width: 100%" @change="handleTagsChange(row.id)">
                                        <el-option v-for="tag in tags" :key="tag.id" :label="tag.tagName"
                                            :value="tag.id" />
                                    </el-select>
                                </div>

                                <!-- 套餐 -->
                                <div class="expand-section">
                                    <h4 class="section-title">套餐</h4>
                                    <el-select v-model="(expandedData[row.id] || {}).plans" multiple placeholder="请选择套餐"
                                        style="width: 100%" @change="handlePlansChange(row.id)">
                                        <el-option v-for="plan in plans" :key="plan.id"
                                            :label="`${plan.name} ¥${plan.price}`" :value="plan.id" />
                                    </el-select>
                                </div>
                            </div>
                        </template>
                    </el-table-column>

                    <!-- 房间号 -->
                    <el-table-column prop="roomNumber" label="房间号" />

                    <!-- 标签 -->
                    <el-table-column prop="tags" label="标签">
                        <template #default="{ row }">
                            <el-tag v-for="(tag, index) in (row.tags || [])" :key="index" size="small" class="tag-item">
                                {{ typeof tag === 'object' ? tag.tagName : tag }}
                            </el-tag>
                            <span v-if="!row.tags?.length" class="no-tag">无标签</span>
                        </template>
                    </el-table-column>

                    <!-- 所属门店 -->
                    <el-table-column prop="shopName" label="所属门店" />

                    <!-- 套餐 -->
                    <el-table-column prop="plans" label="套餐">
                        <template #default="{ row }">
                            <div v-if="row.plans?.length" class="plans-list">
                                <div v-for="(plan, index) in row.plans" :key="index" class="plan-item">
                                    {{ typeof plan === 'object' ? `${plan.planName || plan.name} ¥${plan.price}` : plan
                                    }}
                                </div>
                            </div>
                            <span v-else class="no-plan">无套餐</span>
                        </template>
                    </el-table-column>

                    <!-- 单价 -->
                    <el-table-column prop="unitPrice" label="单价">
                        <template #default="{ row }">¥{{ row.unitPrice }}</template>
                    </el-table-column>

                    <!-- 状态 -->
                    <el-table-column label="状态">
                        <template #default="{ row }">
                            <el-tag :type="getStatusType(row.status)">
                                {{ getStatusText(row.status) }}
                            </el-tag>
                        </template>
                    </el-table-column>

                    <!-- 是否启用 -->
                    <el-table-column prop="isEnabled" label="是否启用">
                        <template #default="{ row }">
                            <el-switch :model-value="row.isEnabled" disabled />
                        </template>
                    </el-table-column>

                    <!-- 操作 -->
                    <el-table-column label="操作" align="center">
                        <template #default="{ row }">
                            <el-button type="primary" link :icon="EditPen" @click="openDialog('编辑', row)">编辑</el-button>
                        </template>
                    </el-table-column>
                </el-table>
            </template>
        </QueryPage>

        <!-- 房间对话框 -->
        <el-dialog v-model="dialogVisible" :title="dialogTitle">
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
// 导入依赖
import { ref, reactive } from 'vue'
import { ElMessage } from 'element-plus'
import { CirclePlus, EditPen, Setting, Plus } from '@element-plus/icons-vue'
import { roomApi } from '@/api/modules/room'
import { planApi } from '@/api/modules/plan'
import { tagApi } from '@/api/modules/tag'
import QueryPage from '@/components/QueryPage/index.vue'

// 类型定义
interface Plan {
    id: string
    name: string
    price: number
}

interface Tag {
    id: string
    tagName: string
}

// 引用
const queryPageRef = ref()

// 状态管理
const dialogVisible = ref(false)
const dialogTitle = ref('')
const currentRoomId = ref('')
const preserveExpanded = ref(true)

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

// 展开行数据（按房间id存储）
const expandedData = reactive<Record<string, {
    images: string[]
    tags: string[]
    plans: string[]
}>>({})

// 数据列表
const plans = ref<Plan[]>([])
const tags = ref<Tag[]>([])

// 加载数据方法
const loadPlans = async () => {
    if (plans.value.length === 0) {
        const response = await planApi.getPlanList({ pageSize: 100 })
        plans.value = response.data?.list || []
    }
}

const loadTags = async () => {
    if (tags.value.length === 0) {
        const response = await tagApi.getTagList({ pageSize: 100 }) as any
        tags.value = response.data?.list || []
    }
}

// 初始化展开行数据
const initExpandedData = async (row: any) => {
    if (!expandedData[row.id]) {
        await Promise.all([loadPlans(), loadTags()])

        const [roomTags, roomPlans] = await Promise.all([
            roomApi.getRoomTags(row.id),
            roomApi.getRoomPlans(row.id)
        ])

        expandedData[row.id] = {
            images: (row.images || []).slice(),
            tags: roomTags.data?.map((t: any) => t.tagId) || [],
            plans: roomPlans.data?.map((p: any) => p.planId) || []
        }
    }
}

// 工具函数
const getStatusType = (status: string) => {
    const lowerStatus = (status || '').toLowerCase()
    switch (lowerStatus) {
        case 'idle': return 'success'
        case 'occupied': return 'warning'
        case 'maintenance': return 'danger'
        default: return 'info'
    }
}

const getStatusText = (status: string) => {
    const lowerStatus = (status || '').toLowerCase()
    switch (lowerStatus) {
        case 'idle': return '空闲'
        case 'occupied': return '使用中'
        case 'maintenance': return '维护中'
        default: return status
    }
}

// 搜索重置
const handleReset = () => {
    Object.assign(searchForm, {
        roomNumber: '',
        status: '',
        isEnabled: undefined
    })
}

// 展开行事件处理
const handleExpandChange = async (row: any, expandedRows: any[]) => {
    if (expandedRows.includes(row)) {
        await initExpandedData(row)
    }
}

// 房间对话框操作
const openDialog = (type: string, row?: any) => {
    dialogTitle.value = type

    if (type === '新增') {
        Object.assign(form, {
            roomNumber: '',
            shopId: '',
            unitPrice: 0,
            isEnabled: true
        })
        currentRoomId.value = ''
    } else if (row) {
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
        if (dialogTitle.value === '新增') {
            await roomApi.addRoom(form)
            ElMessage.success('新增房间成功')
        } else if (currentRoomId.value) {
            await roomApi.updateRoom(currentRoomId.value, form)
            ElMessage.success('更新房间成功')
        }
        dialogVisible.value = false
        queryPageRef.value?.getTableList()
    } catch {
        ElMessage.error('操作失败')
    }
}

// 展开行操作方法
const handleImageChange = async (roomId: string, files: any[]) => {
    await initExpandedData({ id: roomId })

    // 处理新增的文件（将 file 对象转换为图片 URL）
    const newImages = files.map(file => {
        if (file.url) {
            return file.url
        } else if (file.raw) {
            return URL.createObjectURL(file.raw)
        }
        return file
    })

    if (newImages.length > 10) {
        ElMessage.warning('最多只能上传10张图片')
        return
    }

    expandedData[roomId].images = newImages.slice()

    try {
        const imageUrl = newImages[newImages.length - 1]
        await roomApi.addRoomImage({ roomId, imageUrl })
        ElMessage.success('上传图片成功')
    } catch {
        expandedData[roomId].images.pop()
        ElMessage.error('上传图片失败')
    }
}

const handleImageRemove = async (roomId: string, files: any[]) => {
    await initExpandedData({ id: roomId })

    const remainingImages = files.map(file => {
        if (file.url) {
            return file.url
        }
        return file
    })

    const removedImage = expandedData[roomId].images.find(img => !remainingImages.includes(img))

    expandedData[roomId].images = remainingImages.slice()

    try {
        if (removedImage) {
            await roomApi.deleteRoomImage(removedImage)
        }
        ElMessage.success('删除图片成功')
    } catch {
        expandedData[roomId].images.push(removedImage)
        ElMessage.error('删除图片失败')
    }
}

const handleTagsChange = async (roomId: string) => {
    const tags = expandedData[roomId]?.tags || []
    const existingTags = await roomApi.getRoomTags(roomId)
    const existingTagMap = new Map()
    existingTags.data?.forEach((t: any) => {
        existingTagMap.set(t.tagId, t.id)
    })

    const addTags = tags.filter(tag => !existingTagMap.has(tag))
    const removeTags = [...existingTagMap.keys()].filter(tag => !tags.includes(tag))

    for (const tagId of addTags) {
        try {
            await roomApi.addRoomTag({ roomId, tagId })
        } catch {
            ElMessage.error('添加标签失败')
            return
        }
    }

    for (const tagId of removeTags) {
        const id = existingTagMap.get(tagId)
        try {
            await roomApi.deleteRoomTag(id)
        } catch {
            ElMessage.error('删除标签失败')
            return
        }
    }

    ElMessage.success('更新标签成功')
}

const handlePlansChange = async (roomId: string) => {
    const plans = expandedData[roomId]?.plans || []
    const existingPlans = await roomApi.getRoomPlans(roomId)
    const existingPlanMap = new Map()
    existingPlans.data?.forEach((p: any) => {
        existingPlanMap.set(p.planId, p.id)
    })

    const addPlans = plans.filter(plan => !existingPlanMap.has(plan))
    const removePlans = [...existingPlanMap.keys()].filter(plan => !plans.includes(plan))

    for (const planId of addPlans) {
        try {
            await roomApi.addRoomPlan({ roomId, planId })
        } catch {
            ElMessage.error('添加套餐失败')
            return
        }
    }

    for (const planId of removePlans) {
        const id = existingPlanMap.get(planId)
        try {
            await roomApi.deleteRoomPlan(id)
        } catch {
            ElMessage.error('删除套餐失败')
            return
        }
    }

    ElMessage.success('更新套餐成功')
}
</script>

<style scoped lang="scss">
/* 展开行内容样式 */
.expand-content {
    padding: 20px;
    display: flex;
    gap: 40px;
}

.expand-section {
    flex: 1;
}

.section-title {
    margin: 0 0 12px 0;
    font-size: 14px;
    font-weight: 600;
    color: #303133;
}

.upload-btn {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 100%;
    height: 100%;
    color: #606266;
}

.section-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
}

.section-title {
    font-weight: bold;
    color: #333;
    font-size: 14px;
}

.expand-actions {
    display: flex;
    justify-content: flex-end;
    margin-top: 15px;
}

/* 图片样式 */
.images-grid {
    display: flex;
    flex-wrap: wrap;
    gap: 10px;
}

.expand-image {
    width: 150px;
    height: 150px;
    border-radius: 8px;
    object-fit: cover;
    border: 1px solid #eee;
}

.no-images {
    padding: 30px;
    text-align: center;
    color: #999;
}

.image-item {
    position: relative;
}

.image-item .el-button {
    position: absolute;
    top: 5px;
    right: 5px;
    padding: 2px 6px;
    font-size: 12px;
}

/* 标签样式 */
.tag-item {
    margin-right: 4px;
    margin-bottom: 4px;
}

.no-tag {
    font-size: 12px;
    color: #999;
}

/* 套餐样式 */
.plans-list {
    line-height: 1.8;
}

.plan-item {
    font-size: 12px;
    white-space: normal;
}

.no-plan {
    font-size: 12px;
    color: #999;
}
</style>
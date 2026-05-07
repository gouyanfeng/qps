<template>
    <div class="query-page">
        <!-- 搜索表单 -->
        <div class="card table-search" v-show="isShowSearch">
            <div class="search-container">
                <div class="search-conditions" :class="{ 'collapsed': collapsed }">
                    <slot name="searchConditions"></slot>
                </div>
                <div class="search-buttons">
                    <el-button type="primary" :icon="Search" @click="handleSearch">搜索</el-button>
                    <el-button :icon="Refresh" @click="resetSearch">重置</el-button>
                    <el-button type="primary" link @click="toggleCollapsed">
                        {{ collapsed ? '展开' : '收起' }}
                        <el-icon class="el-icon--right">
                            <component :is="collapsed ? ArrowDown : ArrowUp"></component>
                        </el-icon>
                    </el-button>
                </div>
            </div>
        </div>

        <!-- 卡片容器 -->
        <div class="card table-main">
            <!-- 卡片头部 -->
            <div class="table-header">
                <div class="header-button-lf">
                    <slot name="headerButtons"></slot>
                </div>
                <div class="header-button-ri">
                    <el-button type="primary" link :icon="Search" @click="isShowSearch = !isShowSearch">
                        {{ isShowSearch ? '隐藏搜索' : '显示搜索' }}
                    </el-button>
                </div>
            </div>

            <!-- 表格 -->
            <div class="table-container">
                <slot name="table" :tableData="state.tableData"></slot>
            </div>

            <!-- 分页 -->
            <div class="pagination mt20">
                <el-pagination v-model:current-page="state.pageable.pageNum" v-model:page-size="state.pageable.pageSize"
                    :page-sizes="[10, 20, 50, 100]" layout="total, sizes, prev, pager, next, jumper"
                    :total="state.pageable.total" @size-change="handleSizeChange"
                    @current-change="handleCurrentChange" />
            </div>
        </div>
    </div>
</template>

<script setup lang="ts" name="QueryPage">
import { ref, reactive, computed, toRefs, onMounted } from 'vue'
import { Search, Refresh, ArrowDown, ArrowUp } from '@element-plus/icons-vue'

// 定义 props
const props = defineProps({
    api: {
        type: String,
        required: true
    },
    searchParam: {
        type: Object,
        default: () => ({})
    }
})

// 状态管理
const isShowSearch = ref(true)
const collapsed = ref(true)

// 表格数据状态
const state = reactive({
    // 表格数据
    tableData: [] as any[],
    // 分页数据
    pageable: {
        // 当前页数
        pageNum: 1,
        // 每页显示条数
        pageSize: 10,
        // 总条数
        total: 0,
    },
})

// 分页查询参数
const pageParam = computed(() => {
    return {
        page: state.pageable.pageNum,
        pageSize: state.pageable.pageSize,
    }
})

// 导入 API 实例
import api from '@/api'

// 处理查询参数
const processSearchParam = () => {
    // 处理查询参数，过滤掉空值
    let nowSearchParam: any = {}
    for (let key in props.searchParam) {
        // 某些情况下参数为 false/0 也应该携带参数
        if (
            props.searchParam[key] ||
            props.searchParam[key] === false ||
            props.searchParam[key] === 0
        ) {
            nowSearchParam[key] = props.searchParam[key]
        }
    }
    return nowSearchParam
}

// 获取表格数据
const getTableList = async () => {
    try {
        // 整合分页参数和搜索参数
        const params = {
            ...pageParam.value,
            ...processSearchParam()
        }
        let result = await api.get<any>(props.api, params)

        state.tableData = result.data?.list || []
        // 解构后台返回的分页数据
        state.pageable.total = result.data?.totalCount || 0
    } catch (error) {
        console.error('获取数据失败:', error)
    }
}

// 事件处理
const handleSearch = () => {
    state.pageable.pageNum = 1
    getTableList()
}

// 定义事件
const emit = defineEmits(['reset'])

const resetSearch = () => {
    state.pageable.pageNum = 1
    // 触发重置事件，通知父组件重置搜索参数
    emit('reset')
    // 短暂延迟确保父组件更新了 searchParam
    setTimeout(() => {
        getTableList()
    }, 0)
}

const handleSizeChange = (size: number) => {
    state.pageable.pageNum = 1
    state.pageable.pageSize = size
    getTableList()
}

const handleCurrentChange = (current: number) => {
    state.pageable.pageNum = current
    getTableList()
}

const toggleCollapsed = () => {
    collapsed.value = !collapsed.value
}

// 暴露方法
defineExpose({
    getTableList
})

// 初始化
onMounted(() => {
    getTableList()
})
</script>

<style scoped lang="scss">
.query-page {
    .card {
        margin-bottom: 10px;
    }

    .table-main {
        width: 100%;
    }

    .search-container {
        display: flex;
        flex-wrap: wrap;
        align-items: flex-start;
    }

    .search-conditions {
        flex: 1;
        min-width: 0;
    }

    .search-conditions .el-form {
        display: flex;
        flex-wrap: wrap;
    }


    .search-conditions.collapsed {
        max-height: 40px;
        overflow: hidden;
    }

    .search-conditions.collapsed .el-form-item:not(:first-child) {
        display: none;
    }


    .search-buttons {
        display: flex;
        align-items: center;
        margin-bottom: 10px;
    }

    .search-buttons .el-button {
        margin-right: 5px;
    }

    .table-header {
        height: 30px;
        display: flex;
        justify-content: space-between;
        align-items: center;
    }

    .pagination {
        display: flex;
        justify-content: flex-end;
    }

    .table-container {
        margin-bottom: 20px;
    }
}
</style>
<template>
    <div class="query-page">
        <!-- 搜索表单 -->
        <div class="card table-search" v-show="isShowSearch">
            <div class="search-container">
                <div class="search-conditions">
                    <slot name="searchConditions"></slot>
                </div>
                <div class="search-buttons">
                    <el-button type="primary" :icon="Search" @click="handleSearch">搜索</el-button>
                    <el-button :icon="Refresh" @click="resetSearch">重置</el-button>
                    <el-button type="primary" link @click="toggleCollapsed">
                        {{ props.collapsed ? '展开' : '收起' }}
                        <el-icon class="el-icon--right">
                            <component :is="props.collapsed ? ArrowDown : ArrowUp"></component>
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
                <slot name="table"></slot>
            </div>

            <!-- 分页 -->
            <div class="pagination mt20">
                <el-pagination v-model:current-page="pagination.currentPage" v-model:page-size="pagination.pageSize"
                    :page-sizes="[10, 20, 50, 100]" layout="total, sizes, prev, pager, next, jumper"
                    :total="pagination.total" @size-change="handleSizeChange" @current-change="handleCurrentChange" />
            </div>
        </div>
    </div>
</template>

<script setup lang="ts" name="QueryPage">
import { ref, reactive } from 'vue'
import { Search, Refresh, ArrowDown, ArrowUp } from '@element-plus/icons-vue'

// 定义 props
const props = defineProps({
    pagination: {
        type: Object,
        default: () => ({
            currentPage: 1,
            pageSize: 10,
            total: 0
        })
    },
    collapsed: {
        type: Boolean,
        default: true
    }
})

// 定义 emit
const emit = defineEmits([
    'search',
    'reset',
    'sizeChange',
    'currentChange',
    'update:collapsed'
])

// 状态管理
const isShowSearch = ref(true)

// 事件处理
const handleSearch = () => {
    props.pagination.currentPage = 1
    emit('search')
}

const resetSearch = () => {
    emit('reset')
}

const handleSizeChange = (size: number) => {
    props.pagination.pageSize = size
    emit('sizeChange', size)
}

const handleCurrentChange = (current: number) => {
    props.pagination.currentPage = current
    emit('currentChange', current)
}

const toggleCollapsed = () => {
    emit('update:collapsed', !props.collapsed)
}

// 暴露方法
defineExpose({
    isShowSearch
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

    .search-conditions .el-form-item {
        width: 25%;
        box-sizing: border-box;
        padding-right: 15px;
        margin-bottom: 10px;
    }

    .search-conditions .el-form-item .el-input,
    .search-conditions .el-form-item .el-select,
    .search-conditions .el-form-item .el-date-picker {
        width: 100%;
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
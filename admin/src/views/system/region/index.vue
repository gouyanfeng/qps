<template>
  <div class="list-page">
    <QueryPage api="/admin/china-regions" :searchParam="searchForm" @reset="handleReset">
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
        <span class="header-title-text">行政区划数据</span>
      </template>

      <template #table="{ tableData }">
        <el-table :data="tableData" :fit="true" style="width: 100%" border>
          <el-table-column prop="code" label="区域编码" min-width="140" show-overflow-tooltip />
          <el-table-column prop="name" label="区域名称" min-width="150" show-overflow-tooltip />
          <el-table-column prop="fullName" label="完整名称" min-width="220" show-overflow-tooltip />
          <el-table-column prop="level" label="层级" width="100">
            <template #default="{ row }">{{ levelText(row.level) }}</template>
          </el-table-column>
          <el-table-column label="上级区域" min-width="160" show-overflow-tooltip>
            <template #default="{ row }">
              <span v-if="row.parentCode">{{ row.parentCode }}</span>
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
        </el-table>
      </template>
    </QueryPage>
  </div>
</template>

<script setup lang="ts" name="region">
import { reactive } from "vue";
import QueryPage from "@/components/QueryPage/index.vue";

interface SearchForm {
  code: string;
  name: string;
  level: number | "";
  isActive: boolean | "";
}

const searchForm = reactive<SearchForm>({
  code: "",
  name: "",
  level: "",
  isActive: ""
});

const levelText = (level: number) => {
  const map: Record<number, string> = {
    1: "省",
    2: "市",
    3: "区县"
  };
  return map[level] || "未知";
};

const handleReset = () => {
  searchForm.code = "";
  searchForm.name = "";
  searchForm.level = "";
  searchForm.isActive = "";
};
</script>

<style scoped lang="scss">
.header-title-text {
  font-size: 15px;
  font-weight: 600;
  color: #303133;
}

.text-gray {
  color: #909399;
}
</style>
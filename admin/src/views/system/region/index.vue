<template>
  <div class="list-page">
    <div class="card table-search">
      <div class="search-container">
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
        <div class="search-buttons">
          <el-button type="primary" :icon="Search" @click="handleSearch">搜索</el-button>
          <el-button :icon="Refresh" @click="handleReset">重置</el-button>
        </div>
      </div>
    </div>

    <div class="card table-main">
      <div class="table-header">
        <div class="header-button-lf">行政区划数据</div>
        <div class="header-button-ri">
          <el-button type="primary" link :icon="Refresh" @click="loadRegions">刷新</el-button>
        </div>
      </div>

      <el-table v-loading="loading" :data="pagedRows" :fit="true" style="width: 100%" border>
        <el-table-column prop="code" label="区域编码" min-width="140" show-overflow-tooltip />
        <el-table-column prop="name" label="区域名称" min-width="150" show-overflow-tooltip />
        <el-table-column prop="fullName" label="完整名称" min-width="220" show-overflow-tooltip />
        <el-table-column prop="level" label="层级" width="100">
          <template #default="{ row }">{{ levelText(row.level) }}</template>
        </el-table-column>
        <el-table-column label="上级区域" min-width="160" show-overflow-tooltip>
          <template #default="{ row }">
            <span v-if="parentName(row)">{{ parentName(row) }}</span>
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

      <div class="pagination mt20">
        <el-pagination
          v-model:current-page="pageNum"
          v-model:page-size="pageSize"
          :page-sizes="[10, 20, 50, 100]"
          layout="total, sizes, prev, pager, next, jumper"
          :total="filteredRows.length"
          @size-change="handleSizeChange"
          @current-change="handleCurrentChange"
        />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts" name="region">
import { computed, onMounted, reactive, ref } from "vue";
import { ElMessage } from "element-plus";
import { Refresh, Search } from "@element-plus/icons-vue";
import { regionApi } from "@/api/modules/region";

interface ChinaRegion {
  code: string;
  name: string;
  fullName: string;
  level: number;
  parentCode?: string;
  sortOrder: number;
  isActive: boolean;
}

const loading = ref(false);
const allRegions = ref<ChinaRegion[]>([]);
const pageNum = ref(1);
const pageSize = ref(10);

const searchForm = reactive<{
  code: string;
  name: string;
  level: number | "";
  isActive: boolean | "";
}>({
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

const regionNameByCode = computed(() => {
  const map = new Map<string, string>();
  allRegions.value.forEach(region => map.set(region.code, region.name));
  return map;
});

const parentName = (row: ChinaRegion) => row.parentCode ? regionNameByCode.value.get(row.parentCode) || row.parentCode : "";

const filteredRows = computed(() => {
  return allRegions.value.filter(region => {
    if (searchForm.code && !region.code.includes(searchForm.code.trim())) return false;
    if (searchForm.name) {
      const keyword = searchForm.name.trim();
      if (!region.name.includes(keyword) && !region.fullName.includes(keyword)) return false;
    }
    if (searchForm.level !== "" && region.level !== searchForm.level) return false;
    if (searchForm.isActive !== "" && region.isActive !== searchForm.isActive) return false;
    return true;
  });
});

const pagedRows = computed(() => {
  const start = (pageNum.value - 1) * pageSize.value;
  return filteredRows.value.slice(start, start + pageSize.value);
});

const loadRegions = async () => {
  loading.value = true;
  try {
    const res = await regionApi.getChinaRegionList({ activeOnly: false });
    allRegions.value = res.data || [];
  } catch (error) {
    console.error("加载行政区划失败:", error);
    ElMessage.error("加载地区数据失败");
  } finally {
    loading.value = false;
  }
};

const handleSearch = () => {
  pageNum.value = 1;
};

const handleReset = () => {
  searchForm.code = "";
  searchForm.name = "";
  searchForm.level = "";
  searchForm.isActive = "";
  pageNum.value = 1;
};

const handleSizeChange = (size: number) => {
  pageSize.value = size;
  pageNum.value = 1;
};

const handleCurrentChange = (current: number) => {
  pageNum.value = current;
};

onMounted(() => {
  loadRegions();
});
</script>

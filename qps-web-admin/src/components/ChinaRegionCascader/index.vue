<template>
  <el-cascader
    v-model="selectedCodes"
    :options="regionOptions"
    :props="cascaderProps"
    :loading="loading"
    clearable
    filterable
    :placeholder="placeholder"
    class="china-region-cascader"
    @change="handleChange"
    @visible-change="handleVisibleChange"
  />
</template>

<script setup lang="ts">
import { onMounted, ref, watch } from "vue";
import { ElMessage } from "element-plus";
import { regionApi } from "@/api/modules/region";

interface ChinaRegion {
  code: string;
  name: string;
  fullName: string;
  level: number;
  parentCode?: string | null;
  sortOrder: number;
}

interface RegionNode {
  value: string;
  label: string;
  code: string;
  name: string;
  level: number;
  parentCode?: string | null;
  sortOrder: number;
  children?: RegionNode[];
}

const props = withDefaults(defineProps<{
  modelValue?: string[];
  placeholder?: string;
}>(), {
  modelValue: () => [],
  placeholder: "请选择省 / 市 / 区县",
});

const emit = defineEmits<{
  (event: "update:modelValue", value: string[]): void;
  (event: "change", value: string[]): void;
}>();

let cachedOptions: RegionNode[] | null = null;
let cachedNodeMap: Map<string, RegionNode> | null = null;

const loading = ref(false);
const selectedCodes = ref<string[]>([]);
const regionOptions = ref<RegionNode[]>([]);
const nodeMap = ref<Map<string, RegionNode>>(new Map());
const cascaderProps = {
  checkStrictly: true,
  emitPath: true,
};

const buildRegionTree = (regions: ChinaRegion[]) => {
  const sortedRegions = regions
    .filter(region => region.code && region.name)
    .sort((a, b) => (a.level - b.level) || ((a.sortOrder ?? 0) - (b.sortOrder ?? 0)) || a.code.localeCompare(b.code));
  const map = new Map<string, RegionNode>();
  const tree: RegionNode[] = [];

  sortedRegions.forEach(region => {
    map.set(region.code, {
      value: region.code,
      label: region.name,
      code: region.code,
      name: region.name,
      level: region.level,
      parentCode: region.parentCode,
      sortOrder: region.sortOrder ?? 0,
      children: [],
    });
  });

  sortedRegions.forEach(region => {
    const node = map.get(region.code);
    if (!node) return;
    const parent = region.parentCode ? map.get(region.parentCode) : null;
    if (parent) {
      parent.children?.push(node);
    } else {
      tree.push(node);
    }
  });

  const trimEmptyChildren = (nodes: RegionNode[]): RegionNode[] => nodes.map(node => {
    if (!node.children?.length) {
      const { children, ...leaf } = node;
      return leaf;
    }
    return {
      ...node,
      children: trimEmptyChildren(node.children),
    };
  });

  return {
    tree: trimEmptyChildren(tree),
    map,
  };
};

const findCodePathByNames = (names: string[]) => {
  if (names.length === 0) return [];

  let currentNodes = regionOptions.value;
  const codes: string[] = [];

  for (const name of names) {
    const node = currentNodes.find(item => item.name === name);
    if (!node) return [];
    codes.push(node.code);
    currentNodes = node.children || [];
  }

  return codes;
};

const syncSelectedCodes = () => {
  selectedCodes.value = findCodePathByNames(props.modelValue || []);
};

const loadRegions = async () => {
  if (cachedOptions && cachedNodeMap) {
    regionOptions.value = cachedOptions;
    nodeMap.value = cachedNodeMap;
    syncSelectedCodes();
    return;
  }

  loading.value = true;
  try {
    const res = await regionApi.getChinaRegionList({ activeOnly: true });
    const { tree, map } = buildRegionTree(res.data || []);
    cachedOptions = tree;
    cachedNodeMap = map;
    regionOptions.value = tree;
    nodeMap.value = map;
    syncSelectedCodes();
  } catch (error) {
    console.error("加载中国行政区划失败:", error);
    ElMessage.error("加载地区数据失败");
  } finally {
    loading.value = false;
  }
};

const handleChange = (value: string[] | string) => {
  const codes = Array.isArray(value) ? value : [];
  const names = codes
    .map(code => nodeMap.value.get(code)?.name)
    .filter(Boolean) as string[];

  emit("update:modelValue", names);
  emit("change", names);
};

const handleVisibleChange = (visible: boolean) => {
  if (visible && regionOptions.value.length === 0 && !loading.value) {
    loadRegions();
  }
};

watch(() => props.modelValue, () => {
  syncSelectedCodes();
}, { deep: true });

onMounted(loadRegions);
</script>

<style scoped lang="scss">
.china-region-cascader {
  width: 100%;
}
</style>



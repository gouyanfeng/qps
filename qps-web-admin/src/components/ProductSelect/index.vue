<template>
  <el-select
    v-model="selectedValue"
    :multiple="multiple"
    :collapse-tags="collapseTags"
    :collapse-tags-tooltip="collapseTagsTooltip"
    :max-collapse-tags="maxCollapseTags"
    filterable
    remote
    clearable
    :disabled="disabled"
    :placeholder="placeholder"
    :remote-method="loadOptions"
    :loading="loading"
    @visible-change="handleVisibleChange"
  >
    <el-option v-for="item in options" :key="item.value" :label="item.label" :value="item.value" />
  </el-select>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { crmVendorApi } from "@/api/modules/crmVendor";

const props = withDefaults(defineProps<{
  modelValue?: string | string[];
  placeholder?: string;
  disabled?: boolean;
  multiple?: boolean;
  collapseTags?: boolean;
  collapseTagsTooltip?: boolean;
  maxCollapseTags?: number;
}>(), {
  modelValue: "",
  placeholder: "请选择品类",
  disabled: false,
  multiple: false,
  collapseTags: false,
  collapseTagsTooltip: false,
  maxCollapseTags: 1,
});

const emit = defineEmits<{ "update:modelValue": [value: string | string[]] }>();
const options = ref<any[]>([]);
const loading = ref(false);
const selectedValue = computed({
  get: () => props.modelValue,
  set: value => emit("update:modelValue", value ?? (props.multiple ? [] : "")),
});

const loadOptions = async (keyword = "") => {
  loading.value = true;
  try {
    const result = await crmVendorApi.getBusinessEntityAttributeOptions({
      entityType: "CRM_PURCHASE_DEMAND",
      attributeCode: "PURCHASE_PRODUCT",
      keyword,
      pageSize: 100,
    });
    options.value = result.data || [];
  } finally {
    loading.value = false;
  }
};

const handleVisibleChange = (visible: boolean) => {
  if (visible && options.value.length === 0) void loadOptions();
};

onMounted(() => void loadOptions());
</script>

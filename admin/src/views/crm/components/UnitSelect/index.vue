<template>
  <el-select v-model="selectedValue" :placeholder="placeholder" :clearable="clearable" :filterable="filterable" :allow-create="allowCreate" :disabled="disabled" :style="{ width }">
    <el-option v-for="unit in options" :key="unit" :label="unit" :value="unit" />
  </el-select>
</template>

<script setup lang="ts">
import { computed } from "vue";
import { QUANTITY_UNITS, PRICE_UNITS } from "./options";

const props = withDefaults(
  defineProps<{
    modelValue?: string;
    type?: "quantity" | "price";
    placeholder?: string;
    clearable?: boolean;
    filterable?: boolean;
    allowCreate?: boolean;
    disabled?: boolean;
    width?: string;
  }>(),
  {
    modelValue: "",
    type: "quantity",
    placeholder: "单位",
    clearable: true,
    filterable: true,
    allowCreate: true,
    disabled: false,
    width: "100%",
  }
);

const emit = defineEmits<{ "update:modelValue": [value: string] }>();

const options = computed(() => (props.type === "price" ? PRICE_UNITS : QUANTITY_UNITS));
const selectedValue = computed({
  get: () => props.modelValue,
  set: value => emit("update:modelValue", value ?? ""),
});
</script>

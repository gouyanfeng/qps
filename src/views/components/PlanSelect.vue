<template>
    <el-select v-model="selectedPlanIds" multiple :placeholder="placeholder" :style="{ width: width }"
        @change="handleChange">
        <el-option v-for="plan in plans" :key="plan.id" :label="`${plan.name} ¥${plan.price}`" :value="plan.id" />
    </el-select>
</template>

<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { planApi } from '@/api/modules/plan'

const props = withDefaults(defineProps<{
    modelValue?: string[]
    placeholder?: string
    width?: string
}>(), {
    placeholder: '请选择套餐',
    width: '200px'
})

const emit = defineEmits<{
    (e: 'update:modelValue', value: string[]): void
}>()

const plans = ref<{ id: string; name: string; price: number }[]>([])
const selectedPlanIds = ref<string[]>([])

const handleChange = (val: string[]) => {
    emit('update:modelValue', val)
}

const loadPlans = async () => {
    const response = await planApi.getPlanList({ pageSize: 100 })
    plans.value = response.data?.list || []
    // 加载完成后同步值
    if (props.modelValue && props.modelValue.length > 0) {
        selectedPlanIds.value = props.modelValue.filter(id => plans.value.some(p => p.id === id))
    }
}

watch(() => props.modelValue, (val) => {
    if (val && val.length > 0 && plans.value.length > 0) {
        selectedPlanIds.value = val.filter(id => plans.value.some(p => p.id === id))
    } else if (!val || val.length === 0) {
        selectedPlanIds.value = []
    }
}, { deep: true })

onMounted(() => loadPlans())
</script>
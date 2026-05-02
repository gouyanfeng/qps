<template>
    <el-select v-model="selectedTags" :multiple="true" :placeholder="placeholder" :style="{ width: width }"
        @change="handleChange">
        <el-option v-for="tag in tags" :key="tag.id || tag.tagName || tag" :label="tag.tagName || tag"
            :value="tag.tagName || tag" />
    </el-select>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import { tagApi } from '@/api/modules/tag'

const props = withDefaults(defineProps<{
    modelValue?: string[]
    placeholder?: string
    width?: string
}>(), {
    placeholder: '请选择标签',
    width: '200px'
})

const emit = defineEmits<{
    (e: 'update:modelValue', value: string[]): void
}>()

const tags = ref<{ id: string; tagName: string }[]>([])
const selectedTags = ref<string[]>([])

const loadTags = async () => {
    try {
        const response = await tagApi.getTagList({ pageSize: 100 }) as any
        tags.value = response.data?.list || []
    } catch (error) {
        console.error('加载标签列表失败', error)
    }
}

const handleChange = (value: string[]) => {
    emit('update:modelValue', value)
}

watch(() => props.modelValue, (val) => {
    selectedTags.value = val || []
}, { immediate: true, deep: true })

loadTags()
</script>
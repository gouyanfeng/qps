<template>
    <el-select v-model="modelValue" :placeholder="placeholder" :disabled="disabled" :clearable="clearable"
        @change="handleChange">
        <el-option v-for="role in roles" :key="role.value" :label="role.label" :value="role.value" />
    </el-select>
</template>

<script setup lang="ts" name="RoleSelect">
import { computed, onMounted } from 'vue'
import { useRoleStore } from '@/stores/modules/role'

// Props
const props = defineProps({
    modelValue: {
        type: String,
        default: ''
    },
    placeholder: {
        type: String,
        default: '请选择角色'
    },
    disabled: {
        type: Boolean,
        default: false
    },
    clearable: {
        type: Boolean,
        default: true
    }
})

// Emits
const emit = defineEmits(['update:modelValue', 'change'])

// Stores
const roleStore = useRoleStore()

// 从 store 获取角色数据
const roles = computed(() => roleStore.rolesGet)

// 处理选择变化
const handleChange = (value: string) => {
    emit('update:modelValue', value)
    emit('change', value)
}

// 初始化
onMounted(async () => {
    // 从 store 获取角色列表
    await roleStore.getRoles()
})
</script>

<style scoped lang="scss">
// 组件样式</style>
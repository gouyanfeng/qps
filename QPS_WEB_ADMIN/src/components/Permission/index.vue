<template>
  <slot v-if="hasPermission" />
</template>

<script setup lang="ts">
import { computed } from "vue";
import { useAuthStore } from "@/stores/modules/auth";

const props = defineProps<{
  code: string;
}>();

const authStore = useAuthStore();
const hasPermission = computed(() => authStore.userPermissions.includes(props.code));
</script>

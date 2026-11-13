<template>
    <div class="jobs-dashboard">
        <iframe v-if="dashboardUrl" :src="dashboardUrl" class="jobs-frame" title="Hangfire Dashboard" />
        <el-empty v-else description="未获取到登录令牌，无法加载任务看板" />
    </div>
</template>

<script setup lang="ts">
// 定时任务看板：内嵌后端 Hangfire Dashboard（/jobs-dashboard）。
// 远程访问需 ?access_token= 携带 admin JWT（后端 QpsDashboardAuthorizationFilter 校验），
// 本机访问（回环地址）后端直接放行，token 仅作生产环境用途。
import { computed } from "vue";
import { API_BASE_URL } from "@/utils";
import { useUserStore } from "@/stores/modules/user";

const userStore = useUserStore();

const dashboardUrl = computed(() =>
    userStore.token ? `${API_BASE_URL}/jobs-dashboard?access_token=${userStore.token}` : ""
);
</script>

<style scoped lang="scss">
.jobs-dashboard {
    height: 100%;
}

.jobs-frame {
    display: block;
    width: 100%;
    // 占满 el-main 内容区：视口高度减去顶栏与内边距
    height: calc(100vh - 110px);
    border: none;
    border-radius: var(--el-border-radius-base);
    background: var(--el-bg-color);
}
</style>

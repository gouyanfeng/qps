<template>
  <div class="home-welcome">
    <div class="welcome-body">
      <section class="greeting-card">
        <h1 class="greeting-title">{{ greeting }}，{{ userName }}</h1>
        <p class="greeting-sub">欢迎回来，今天是 {{ today }}</p>
      </section>

      <section v-if="entries.length" class="entry-section">
        <h2 class="section-title">快捷入口</h2>
        <div class="entry-grid">
          <div v-for="entry in entries" :key="entry.path" class="entry-card" @click="router.push(entry.path)">
            <div class="entry-icon">
              <el-icon :size="22">
                <component :is="entry.icon" />
              </el-icon>
            </div>
            <div class="entry-info">
              <div class="entry-name">{{ entry.title }}</div>
              <div class="entry-desc">{{ entry.desc }}</div>
            </div>
            <el-icon class="entry-arrow"><ArrowRight /></el-icon>
          </div>
        </div>
      </section>
    </div>
  </div>
</template>

<script setup lang="ts" name="home">
import { computed } from "vue";
import { useRouter } from "vue-router";
import { ArrowRight } from "@element-plus/icons-vue";
import { useUserStore } from "@/stores/modules/user";
import { useAuthStore } from "@/stores/modules/auth";

interface MenuLeaf {
  title: string;
  path: string;
  icon: string;
  group: string;
  desc: string;
}

const router = useRouter();
const userName = computed(() => useUserStore().userInfo.name);
const authStore = useAuthStore();

const greeting = computed(() => {
  const hour = new Date().getHours();
  if (hour < 6) return "凌晨好";
  if (hour < 12) return "上午好";
  if (hour < 18) return "下午好";
  return "晚上好";
});

const today = new Date().toLocaleDateString("zh-CN", {
  year: "numeric",
  month: "long",
  day: "numeric",
  weekday: "long",
});

// 从真实菜单取每个顶级分组下的第一个可见叶子菜单，作为快捷入口（菜单变化自动跟随，无业务硬编码）
const entries = computed<MenuLeaf[]>(() =>
  authStore
    .showMenuListGet!.slice(0, 3)
    .map<MenuLeaf | null>(group => {
      const firstLeaf = (group.children ?? []).find(item => !item.meta.isHide) ?? (group.meta.isHide ? null : group);
      if (!firstLeaf) return null;
      return {
        group: group.meta.title,
        title: group.meta.title,
        desc: firstLeaf.meta.title,
        path: firstLeaf.path,
        icon: group.meta.icon,
      };
    })
    .filter((item): item is MenuLeaf => item !== null)
);
</script>

<style scoped lang="scss">
@import "./index.scss";
</style>

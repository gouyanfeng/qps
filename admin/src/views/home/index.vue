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
.home-welcome {
  min-height: calc(100vh - 96px);
  padding: 18px 20px 28px;
  background: var(--el-bg-color-page);
}

.welcome-body {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.greeting-card {
  padding: 28px 32px;
  border-radius: var(--el-border-radius-base);
  background: var(--el-bg-color);
}

.greeting-title {
  margin: 0 0 6px;
  font-size: 20px;
  font-weight: 600;
  color: var(--el-text-color-primary);
}

.greeting-sub {
  margin: 0;
  font-size: 13px;
  color: var(--el-text-color-secondary);
}

.section-title {
  margin: 0;
  font-size: 16px;
  font-weight: 600;
  color: var(--el-text-color-primary);
}

.entry-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 14px;
}

.entry-card {
  display: flex;
  align-items: center;
  gap: 14px;
  padding: 20px 22px;
  border-radius: var(--el-border-radius-base);
  background: var(--el-bg-color);
  cursor: pointer;
  transition: box-shadow 0.2s ease, transform 0.2s ease;

  &:hover {
    box-shadow: var(--el-box-shadow-light);
    transform: translateY(-2px);

    .entry-arrow {
      color: var(--el-color-primary);
    }
  }
}

.entry-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  width: 44px;
  height: 44px;
  border-radius: var(--el-border-radius-base);
  background: var(--el-color-primary-light-9);
  color: var(--el-color-primary);
}

.entry-info {
  flex: 1;
  min-width: 0;
}

.entry-name {
  margin-bottom: 4px;
  font-size: 15px;
  font-weight: 600;
  color: var(--el-text-color-primary);
}

.entry-desc {
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.entry-arrow {
  color: var(--el-text-color-placeholder);
  transition: color 0.2s ease;
}

@media (max-width: 900px) {
  .entry-grid {
    grid-template-columns: 1fr;
  }
}
</style>

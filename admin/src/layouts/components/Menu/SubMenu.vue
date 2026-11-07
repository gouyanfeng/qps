<template>
  <template v-for="subItem in menuList" :key="subItem.path">
    <el-sub-menu v-if="subItem.children?.length" :index="subItem.path">
      <template #title>
        <el-icon v-if="subItem.meta.icon">
          <component :is="subItem.meta.icon"></component>
        </el-icon>
        <span class="sle">{{ subItem.meta.title }}</span>
      </template>
      <SubMenu :menu-list="subItem.children" />
    </el-sub-menu>
    <el-menu-item v-else :index="subItem.path" @click="handleClickMenu(subItem)">
      <el-icon v-if="subItem.meta.icon">
        <component :is="subItem.meta.icon"></component>
      </el-icon>
      <template #title>
        <span class="sle">{{ subItem.meta.title }}</span>
      </template>
    </el-menu-item>
  </template>
</template>

<script setup lang="ts">
import { useRouter } from "vue-router";

defineProps<{ menuList: Menu.MenuOptions[] }>();

const router = useRouter();
const handleClickMenu = (subItem: Menu.MenuOptions) => {
  if (subItem.meta.isLink) return window.open(subItem.meta.isLink, "_blank");
  router.push(subItem.path);
};
</script>

<style lang="scss">
/* 左侧栏纵向菜单：选中态为整条圆角块，颜色全部取系统主题变量
   顶部横向菜单（el-menu--horizontal）与折叠弹出层保持原生样式 */
.el-menu:not(.el-menu--horizontal):not(.el-menu--popup) {
  .el-menu-item,
  .el-sub-menu__title {
    height: 42px;
    line-height: 42px;
    margin: 2px 8px;
    /* 跟随按钮圆角，但上限为块高的一半（42px / 2 = 21px），避免大圆角下形状失真 */
    border-radius: min(var(--el-border-radius-base), 21px);
    transition: background-color 0.2s ease, color 0.2s ease;
  }

  .el-menu-item {
    position: relative;

    &:hover {
      color: var(--el-menu-hover-text-color);
      background-color: var(--el-menu-hover-bg-color);
    }

    &.is-active {
      color: var(--el-color-white);
      background-color: var(--el-color-primary);

      .el-icon {
        color: var(--el-color-white);
      }

      /* 参考图选中块右侧的白色指示点 */
      &::after {
        position: absolute;
        top: 50%;
        right: 14px;
        width: 5px;
        height: 5px;
        margin-top: -2.5px;
        content: "";
        background-color: var(--el-color-white);
        border-radius: 50%;
        opacity: 0.9;
      }
    }
  }

  .el-sub-menu__title {
    &:hover {
      color: var(--el-menu-hover-text-color);
      background-color: var(--el-menu-hover-bg-color);
    }
  }
}

/* 折叠态：只保留居中的主色图标块，与展开态保持一致的主色 */
.el-menu--collapse {
  .el-menu-item,
  .el-sub-menu__title {
    display: flex;
    justify-content: center;
    margin: 2px 8px;
    padding: 0 !important;

    .el-icon {
      margin-right: 0;
    }
  }

  /* 折叠时 el-menu-item 内部仍有 tooltip 触发器，清掉其左右 padding 才能与其它图标同轴 */
  .el-menu-item > .el-menu-tooltip__trigger {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 100%;
    padding: 0 !important;
  }

  .is-active > .el-sub-menu__title {
    color: var(--el-color-white) !important;
    background-color: var(--el-color-primary) !important;
  }
}
</style>



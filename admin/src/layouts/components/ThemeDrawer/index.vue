<template>
  <el-drawer v-model="drawerVisible" title="布局设置" size="290px">
    <!-- 布局样式 -->
    <el-divider class="divider" content-position="center">
      <el-icon><Notification /></el-icon>
      布局样式
    </el-divider>
    <div class="layout-box">
      <el-tooltip effect="dark" content="纵向" placement="top" :show-after="200">
        <div :class="['layout-item layout-vertical', { 'is-active': layout == 'vertical' }]" @click="setLayout('vertical')">
          <div class="layout-dark"></div>
          <div class="layout-container">
            <div class="layout-light"></div>
            <div class="layout-content"></div>
          </div>
          <el-icon v-if="layout == 'vertical'">
            <CircleCheckFilled />
          </el-icon>
        </div>
      </el-tooltip>
      <el-tooltip effect="dark" content="经典" placement="top" :show-after="200">
        <div :class="['layout-item layout-classic', { 'is-active': layout == 'classic' }]" @click="setLayout('classic')">
          <div class="layout-dark"></div>
          <div class="layout-container">
            <div class="layout-light"></div>
            <div class="layout-content"></div>
          </div>
          <el-icon v-if="layout == 'classic'">
            <CircleCheckFilled />
          </el-icon>
        </div>
      </el-tooltip>
      <el-tooltip effect="dark" content="横向" placement="top" :show-after="200">
        <div :class="['layout-item layout-transverse', { 'is-active': layout == 'transverse' }]" @click="setLayout('transverse')">
          <div class="layout-dark"></div>
          <div class="layout-content"></div>
          <el-icon v-if="layout == 'transverse'">
            <CircleCheckFilled />
          </el-icon>
        </div>
      </el-tooltip>
      <el-tooltip effect="dark" content="分栏" placement="top" :show-after="200">
        <div :class="['layout-item layout-columns', { 'is-active': layout == 'columns' }]" @click="setLayout('columns')">
          <div class="layout-dark"></div>
          <div class="layout-light"></div>
          <div class="layout-content"></div>
          <el-icon v-if="layout == 'columns'">
            <CircleCheckFilled />
          </el-icon>
        </div>
      </el-tooltip>
    </div>
    <div class="theme-item">
      <span>
        侧边栏反转色
        <el-tooltip effect="dark" content="侧边栏颜色变为深色模式" placement="top">
          <el-icon><QuestionFilled /></el-icon>
        </el-tooltip>
      </span>
      <el-switch v-model="asideInverted" @change="setAsideTheme" />
    </div>
    <div class="theme-item mb50">
      <span>
        头部反转色
        <el-tooltip effect="dark" content="头部颜色变为深色模式" placement="top">
          <el-icon><QuestionFilled /></el-icon>
        </el-tooltip>
      </span>
      <el-switch v-model="headerInverted" @change="setHeaderTheme" />
    </div>

    <!-- 全局主题 -->
    <el-divider class="divider" content-position="center">
      <el-icon><ColdDrink /></el-icon>
      全局主题
    </el-divider>
    <div class="theme-item">
      <span>主题颜色</span>
      <el-color-picker v-model="primary" :predefine="colorList" @change="changePrimary" />
    </div>
    <div class="theme-item">
      <span>
        按钮圆角
        <el-tooltip effect="dark" content="按钮、输入框及菜单选中块共用此圆角" placement="top">
          <el-icon><QuestionFilled /></el-icon>
        </el-tooltip>
      </span>
      <el-radio-group v-model="borderRadius" size="small" @change="handleBorderRadiusChange">
        <el-radio-button :value="0">0</el-radio-button>
        <el-radio-button :value="8">8</el-radio-button>
        <el-radio-button :value="16">16</el-radio-button>
        <el-radio-button :value="32">32</el-radio-button>
      </el-radio-group>
    </div>
    <div class="theme-item">
      <span>圆角预览（{{ borderRadius }}px）</span>
      <el-button type="primary">按钮</el-button>
    </div>
    <div class="theme-item">
      <span>暗黑模式</span>
      <SwitchDark />
    </div>
    <div class="theme-item">
      <span>灰色模式</span>
      <el-switch v-model="isGrey" @change="changeGreyOrWeak('grey', !!$event)" />
    </div>
    <div class="theme-item mb40">
      <span>色弱模式</span>
      <el-switch v-model="isWeak" @change="changeGreyOrWeak('weak', !!$event)" />
    </div>

    <!-- 界面设置 -->
    <el-divider class="divider" content-position="center">
      <el-icon><Setting /></el-icon>
      界面设置
    </el-divider>
    <div class="theme-item">
      <span>菜单折叠</span>
      <el-switch v-model="isCollapse" />
    </div>
    <div class="theme-item">
      <span>菜单手风琴</span>
      <el-switch v-model="accordion" />
    </div>
    <div class="theme-item">
      <span>水印</span>
      <el-switch v-model="watermark" />
    </div>
    <div class="theme-item">
      <span>面包屑</span>
      <el-switch v-model="breadcrumb" />
    </div>
    <div class="theme-item">
      <span>面包屑图标</span>
      <el-switch v-model="breadcrumbIcon" />
    </div>
    <div class="theme-item">
      <span>标签栏</span>
      <el-switch v-model="tabs" />
    </div>
    <div class="theme-item">
      <span>标签栏图标</span>
      <el-switch v-model="tabsIcon" />
    </div>
    <div class="theme-item">
      <span>页脚</span>
      <el-switch v-model="footer" />
    </div>
  </el-drawer>
</template>

<script setup lang="ts">
import { ref } from "vue";
import { storeToRefs } from "pinia";
import { useTheme } from "@/hooks/useTheme";
import { useGlobalStore } from "@/stores/modules/global";
import { LayoutType } from "@/stores/interface";
import { DEFAULT_PRIMARY } from "@/config";
import mittBus from "@/utils/mittBus";
import SwitchDark from "@/components/SwitchDark/index.vue";

const { changePrimary, changeBorderRadius, changeGreyOrWeak, setAsideTheme, setHeaderTheme } = useTheme();

const globalStore = useGlobalStore();
const {
  layout,
  primary,
  borderRadius,
  isGrey,
  isWeak,
  asideInverted,
  headerInverted,
  isCollapse,
  accordion,
  watermark,
  breadcrumb,
  breadcrumbIcon,
  tabs,
  tabsIcon,
  footer
} = storeToRefs(globalStore);

// 预定义主题颜色
const colorList = [
  DEFAULT_PRIMARY,
  "#daa96e",
  "#0c819f",
  "#409eff",
  "#27ae60",
  "#ff5c93",
  "#e74c3c",
  "#fd726d",
  "#f39c12",
  "#9b59b6"
];

// 设置布局方式
const setLayout = (val: LayoutType) => {
  globalStore.setGlobalState("layout", val);
  setAsideTheme();
};

// 按钮圆角：写入 --el-border-radius-base，按钮/输入框/菜单选中块同步生效
const handleBorderRadiusChange = (val: string | number | boolean) => {
  changeBorderRadius(Number(val));
};

// 打开主题设置
const drawerVisible = ref(false);
mittBus.on("openThemeDrawer", () => (drawerVisible.value = true));
</script>

<style scoped lang="scss">
@import "./index.scss";
</style>



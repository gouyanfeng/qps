<template>
    <el-container class="layout">
        <el-aside>
            <div class="aside-box" :style="{ width: isCollapse ? '65px' : '210px' }">
                <div class="logo flx-center">

                    <span v-show="!isCollapse" class="logo-text">{{ title }}</span>
                </div>
                <el-scrollbar>
                    <el-menu :router="false" :default-active="activeMenu" :collapse="isCollapse"
                        :unique-opened="accordion" :collapse-transition="false">
                        <el-sub-menu index="1">
                            <template #title>
                                <el-icon>
                                    <location />
                                </el-icon>
                                <span>Navigator One</span>
                            </template>
                            <el-menu-item-group title="Group One">
                                <el-menu-item index="1-1">item one</el-menu-item>
                                <el-menu-item index="1-2">item two</el-menu-item>
                            </el-menu-item-group>
                            <el-menu-item-group title="Group Two">
                                <el-menu-item index="1-3">item three</el-menu-item>
                            </el-menu-item-group>
                            <el-sub-menu index="1-4">
                                <template #title>item four</template>
                                <el-menu-item index="1-4-1">item one</el-menu-item>
                            </el-sub-menu>
                        </el-sub-menu>
                        <el-menu-item index="2">
                            <el-icon><icon-menu /></el-icon>
                            <span>Navigator Two</span>
                        </el-menu-item>
                        <el-menu-item index="3" disabled>
                            <el-icon>
                                <document />
                            </el-icon>
                            <span>Navigator Three</span>
                        </el-menu-item>
                        <el-menu-item index="4">
                            <el-icon>
                                <setting />
                            </el-icon>
                            <span>Navigator Four</span>
                        </el-menu-item>
                    </el-menu>
                </el-scrollbar>
            </div>
        </el-aside>
        <el-container>
            <el-header>
                <h1>Header</h1>
                <!-- <ToolBarLeft />
                <ToolBarRight /> -->
            </el-header>
            <el-main>
                <RouterView />
            </el-main>
        </el-container>
    </el-container>
</template>

<script lang="ts" setup>

// 引入 Element Plus 图标组件  菜单展开变慢

import {
    Document,
    Menu as IconMenu,
    Location,
    Setting,
} from '@element-plus/icons-vue'

import { ref } from 'vue';
const title = 'QPS';
const activeMenu = '/dashboard';
const isCollapse = ref(false);
const accordion = ref(true);

</script>
<style scoped lang="scss">
@import "./index.scss";
</style>

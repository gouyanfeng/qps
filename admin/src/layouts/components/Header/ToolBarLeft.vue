<template>
  <div class="tool-bar-lf">
    <CollapseIcon id="collapseIcon" />
    <Breadcrumb v-show="globalStore.breadcrumb" id="breadcrumb" />
  </div>
</template>

<script setup lang="ts">
import { useGlobalStore } from "@/stores/modules/global";
import CollapseIcon from "./components/CollapseIcon.vue";
import Breadcrumb from "./components/Breadcrumb.vue";
const globalStore = useGlobalStore();
</script>

<style scoped lang="scss">
@import "./ToolBarLeft.scss";
</style>



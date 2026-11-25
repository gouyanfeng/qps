<template>
  <div class="maximize" @click="exitMaximize">
    <i :class="'iconfont icon-tuichu'"></i>
  </div>
</template>

<script setup lang="ts">
import { useGlobalStore } from "@/stores/modules/global";

const globalStore = useGlobalStore();
const exitMaximize = () => {
  globalStore.setGlobalState("maximize", false);
};
</script>

<style scoped lang="scss">
@import "./Maximize.scss";
</style>



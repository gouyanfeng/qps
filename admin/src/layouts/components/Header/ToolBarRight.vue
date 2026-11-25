<template>
  <div class="tool-bar-ri">
    <div class="header-icon">
      <AssemblySize id="assemblySize" />
      <Language id="language" />
      <SearchMenu id="searchMenu" />
      <ThemeSetting id="themeSetting" />
      <Message id="message" />
      <Fullscreen id="fullscreen" />
    </div>
    <span class="username">{{ username }}</span>
    <Avatar />
  </div>
</template>

<script setup lang="ts">
import { computed } from "vue";
import { useUserStore } from "@/stores/modules/user";
import AssemblySize from "./components/AssemblySize.vue";
import Language from "./components/Language.vue";
import SearchMenu from "./components/SearchMenu.vue";
import ThemeSetting from "./components/ThemeSetting.vue";
import Message from "./components/Message.vue";
import Fullscreen from "./components/Fullscreen.vue";
import Avatar from "./components/Avatar.vue";

const userStore = useUserStore();
const username = computed(() => userStore.userInfo.name);
</script>

<style scoped lang="scss">
@import "./ToolBarRight.scss";
</style>



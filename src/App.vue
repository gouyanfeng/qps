<template>
  <Layouts />
</template>
<script lang="ts" setup>
import Layouts from "./Views/Layouts/index.vue";
import { useTheme } from "./hooks/useTheme";

console.log("App.vue loaded");
// init theme
const { initTheme } = useTheme();
initTheme();

console.log("Theme initialized");
</script>
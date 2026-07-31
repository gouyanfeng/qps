<template>
  <div class="home">
    <section class="home-panel">
      <h1>System Console</h1>
      <p>Welcome back. Use the system menu to manage users, roles, permissions, and dictionaries.</p>
    </section>
  </div>
</template>

<script setup lang="ts" name="home">
</script>

<style scoped lang="scss">
.home {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: calc(100vh - 120px);
  padding: 24px;
}

.home-panel {
  width: min(720px, 100%);
  padding: 32px;
  border: 1px solid var(--el-border-color-light);
  border-radius: 8px;
  background: var(--el-bg-color);
  text-align: center;
}

.home-panel h1 {
  margin: 0 0 12px;
  font-size: 28px;
  font-weight: 600;
  color: var(--el-text-color-primary);
}

.home-panel p {
  margin: 0;
  font-size: 15px;
  color: var(--el-text-color-regular);
}
</style>



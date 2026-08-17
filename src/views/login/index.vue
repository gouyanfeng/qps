<template>
  <div class="login-page">
    <!-- 主体卡片 -->
    <main class="login-main flx-center">
      <div class="login-card">
        <!-- 左侧：平台展示 -->
        <div class="login-showcase">
          <div class="showcase-inner">
            <div class="showcase-logo">
              <img src="@/assets/images/logo.svg" alt="logo" />
              <p>客户管理 · 销售协同 · 数据驱动</p>
            </div>
            <FeaturePentagon />
          </div>
        </div>

        <!-- 右侧：登录表单 -->
        <div class="login-form-wrap">
          <LoginForm />
        </div>
      </div>
    </main>

    <!-- 底部版权 -->
    <footer class="login-footer">
      <p>Copyright 2018-2026 QPS Technology Co.,Ltd. All Rights Reserved 京ICP备XXXXXXXX号</p>
    </footer>
  </div>
</template>

<script setup lang="ts" name="login">
import LoginForm from "./components/LoginForm.vue";
import FeaturePentagon from "./components/FeaturePentagon.vue";
</script>

<style scoped lang="scss">
@import "./index.scss";
</style>

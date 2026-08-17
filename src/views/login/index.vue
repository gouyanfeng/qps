<template>
  <div class="login-page">
    <main class="login-main">
      <div class="login-card">
        <div class="login-showcase">
          <div class="showcase-inner">
            <h2 class="brand-title">QPS</h2>
            <p class="brand-subtitle">智能客户关系管理平台</p>
            <p class="brand-slogan">让销售更高效 · 让决策更智能</p>
            <div class="feature-list">
              <div class="feature-item">
                <el-icon :size="16" color="#41b883"><User /></el-icon>
                <span>客户全生命周期管理</span>
              </div>
              <div class="feature-item">
                <el-icon :size="16" color="#41b883"><TrendCharts /></el-icon>
                <span>销售数据实时分析</span>
              </div>
              <div class="feature-item">
                <el-icon :size="16" color="#41b883"><ChatDotRound /></el-icon>
                <span>跟进记录智能提醒</span>
              </div>
            </div>
          </div>
          <div class="deco-shape deco-shape--1"></div>
          <div class="deco-shape deco-shape--2"></div>
          <div class="deco-shape deco-shape--3"></div>
        </div>

        <div class="login-form-wrap">
          <LoginForm />
        </div>
      </div>

      <footer class="login-footer">
        <p>Copyright 2018-2026 QPS Technology Co.,Ltd.</p>
      </footer>
    </main>
  </div>
</template>

<script setup lang="ts" name="login">
import LoginForm from "./components/LoginForm.vue";
import { User, TrendCharts, ChatDotRound } from "@element-plus/icons-vue";
</script>

<style scoped lang="scss">
@import "./index.scss";
</style>
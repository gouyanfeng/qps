<template>
  <div class="login-page">
    <aside class="login-aside">
      <div class="aside-grid"></div>
      <div class="aside-glow-top"></div>
      <div class="aside-glow-bottom"></div>

      <div class="aside-brand">
        <div class="brand-logo">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2.2" class="brand-icon">
            <path d="M12 2L2 7l10 5 10-5-10-5zM2 17l10 5 10-5M2 12l10 5 10-5" />
          </svg>
        </div>
        <div class="brand-text">
          <span class="brand-name">{{ TITLE }}</span>
          <span class="brand-badge">Cloud Pro</span>
        </div>
        <div class="status-pill">
          <span class="status-dot"></span>
          <span>SYSTEM OPERATIONAL</span>
        </div>
      </div>

      <div class="aside-hero">
    
        <h1 class="hero-title">
          面向超大规模集群的
          <span class="hero-title-accent">云原生架构管理中枢</span>
        </h1>
        <p class="hero-desc">
          为关键业务提供毫秒级遥测、弹性治理与安全合规体系，助力工程团队掌控每一行基础设施代码。
        </p>
        <div class="hero-stats">
          <div class="stat-item">
            <div class="stat-value">99.995%</div>
            <div class="stat-label">SLA 可靠性保证</div>
          </div>
          <div class="stat-item">
            <div class="stat-value">&lt; 12ms</div>
            <div class="stat-label">全球调度延迟</div>
          </div>
          <div class="stat-item">
            <div class="stat-value">SOC 2</div>
            <div class="stat-label">Type II 认证</div>
          </div>
        </div>
      </div>

      <div class="aside-footer">
        <span>© 2025 QPS Cloud Systems Inc.</span>
        <div class="footer-meta">
          <span>AP-EAST-1A</span>
          <span class="footer-dot">·</span>
          <span>安全态势感知</span>
        </div>
      </div>
    </aside>

    <main class="login-main">
      <div class="main-toolbar">
        <nav class="toolbar-breadcrumb">
          <a class="breadcrumb-item is-link" href="javascript:void(0)">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" class="breadcrumb-icon">
              <path d="M10 19l-7-7m0 0l7-7m-7 7h18" />
            </svg>
            返回门户主页
          </a>
          <span class="breadcrumb-sep">/</span>
          <a class="breadcrumb-item is-link" href="javascript:void(0)">帮助中心</a>
          <span class="breadcrumb-sep">/</span>
          <span class="breadcrumb-item is-current">中文(简体)</span>
        </nav>
      </div>

      <div class="main-form-area">
        <div class="form-card">
          <LoginForm />
        </div>
      </div>
    </main>
  </div>
</template>

<script setup lang="ts" name="login">
import LoginForm from "./components/LoginForm.vue";
import { DEFAULT_TITLE as TITLE } from "@/config";
</script>

<style scoped lang="scss">
@import "./index.scss";
</style>
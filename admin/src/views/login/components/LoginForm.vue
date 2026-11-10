<template>
  <div class="login-form-content">
    <div class="login-form-header">
      <h2 class="login-form-title">系统登录</h2>
      <p class="login-form-subtitle">请输入您的企业通行凭证以验证身份</p>
    </div>

    <div class="login-tabs">
      <button type="button" class="tab-btn" :class="{ 'is-active': activeTab === 'account' }" @click="activeTab = 'account'">
        密码登录
      </button>
      <button type="button" class="tab-btn" :class="{ 'is-active': activeTab === 'wechat' }" @click="activeTab = 'wechat'">
        微信登录
      </button>
    </div>

    <div v-show="activeTab === 'account'">
      <el-form ref="loginFormRef" label-position="left" :model="loginForm" :rules="loginRules" @keyup.enter="login(loginFormRef)">
        <el-form-item prop="username">
          <template #label>账号</template>
          <el-input
            v-model="loginForm.username"
            placeholder="name@company.com"
            maxlength="32"
            size="large"
          />
        </el-form-item>

        <el-form-item prop="password">
          <template #label>密码</template>
          <el-input
            v-model="loginForm.password"
            type="password"
            placeholder="请输入登录密码"
            show-password
            autocomplete="new-password"
            maxlength="20"
            size="large"
          />
        </el-form-item>

        <div class="form-row-between">
          <div class="form-row-left">
            <el-checkbox v-model="rememberMe">记住设备 (7天内免登录)</el-checkbox>
            <a class="forgot-link" href="javascript:void(0)">忘记密码？</a>
          </div>
          <span class="tls-indicator">
            <span class="tls-dot"></span>
            TLS 1.3 端到端加密
          </span>
        </div>

        <el-form-item>
          <el-button class="login-submit" type="primary" :loading="loading" @click="login(loginFormRef)">
            <span>登录控制台</span>
            <svg class="submit-arrow" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2">
              <path d="M14 5l7 7m0 0l-7 7m7-7H3" />
            </svg>
          </el-button>
        </el-form-item>
      </el-form>
    </div>

    <div v-show="activeTab === 'wechat'" class="wechat-login">
      <div class="qr-placeholder">
        <el-icon :size="48" color="#07c160"><ChatDotSquare /></el-icon>
        <p>请使用微信扫一扫登录</p>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, onBeforeUnmount } from "vue";
import { useRouter } from "vue-router";
import { HOME_URL } from "@/config";
import { Login } from "@/api/interface";
import { ElNotification } from "element-plus";
import { clearUserPermissionsCache, loginApi } from "@/api/modules/login";
import { useUserStore } from "@/stores/modules/user";
import { useTabsStore } from "@/stores/modules/tabs";
import { initDynamicRouter } from "@/routers/modules/dynamicRouter";
import { ChatDotSquare } from "@element-plus/icons-vue";
import type { ElForm } from "element-plus";

const router = useRouter();
const userStore = useUserStore();
const tabsStore = useTabsStore();

const activeTab = ref("account");
const rememberMe = ref(false);

type FormInstance = InstanceType<typeof ElForm>;
const loginFormRef = ref<FormInstance>();
const loginRules = reactive({
  username: [{ required: true, message: "请输入账号", trigger: "blur" }],
  password: [{ required: true, message: "请输入密码", trigger: "blur" }]
});

const loading = ref(false);
const loginForm = reactive<Login.ReqLoginForm>({
  username: "admin",
  password: "123456"
});

const login = (formEl: FormInstance | undefined) => {
  if (!formEl) return;
  formEl.validate(async valid => {
    if (!valid) return;
    loading.value = true;
    try {
      const { data } = await loginApi({
        ...loginForm,
        password: loginForm.password
      });
      userStore.setToken(data.token);
      userStore.setUserInfo({ name: data.realName, userId: data.userId, role: data.role });
      clearUserPermissionsCache(data.userId);

      await initDynamicRouter();
      tabsStore.setTabs([]);
      router.push(HOME_URL);

      ElNotification({
        title: "登录成功",
        message: "欢迎登录 QPS 客户关系管理系统",
        type: "success",
        duration: 3000
      });
    } finally {
      loading.value = false;
    }
  });
};

onMounted(() => {
  document.onkeydown = (e: KeyboardEvent) => {
    if (e.code === "Enter" || e.code === "enter" || e.code === "NumpadEnter") {
      if (loading.value || activeTab.value !== "account") return;
      login(loginFormRef.value);
    }
  };
});

onBeforeUnmount(() => {
  document.onkeydown = null;
});
</script>

<style scoped lang="scss">
@import "../index.scss";

.password-label-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;

  .forgot-link {
    font-size: 12px;
    font-weight: 500;
    color: #4f46e5;
    cursor: pointer;
    text-decoration: none;
    transition: all 0.15s;

    &:hover {
      color: #4338ca;
      text-decoration: underline;
    }
  }
}
</style>
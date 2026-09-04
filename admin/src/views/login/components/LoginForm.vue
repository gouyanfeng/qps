<template>
  <div class="login-form-content">
    <el-tabs v-model="activeTab" class="login-tabs" stretch>
      <el-tab-pane label="账号密码登录" name="account">
        <el-form ref="loginFormRef" :model="loginForm" :rules="loginRules" size="large" @keyup.enter="login(loginFormRef)">
          <el-form-item prop="username">
            <el-input v-model="loginForm.username" placeholder="请输入账号 / 手机号" maxlength="32">
              <template #prefix>
                <el-icon class="el-input__icon"><User /></el-icon>
              </template>
            </el-input>
          </el-form-item>

          <el-form-item prop="password">
            <el-input v-model="loginForm.password" type="password" placeholder="请输入登录密码" show-password autocomplete="new-password" maxlength="20">
              <template #prefix>
                <el-icon class="el-input__icon"><Lock /></el-icon>
              </template>
            </el-input>
          </el-form-item>

          <el-form-item class="remember-row">
            <el-checkbox v-model="rememberMe">记住账号</el-checkbox>
            <span class="forgot-link">忘记密码?</span>
          </el-form-item>

          <el-form-item>
            <el-button class="login-submit" type="primary" size="large" :loading="loading" @click="login(loginFormRef)">
              登 录
            </el-button>
          </el-form-item>
        </el-form>
      </el-tab-pane>

      <el-tab-pane label="微信登录" name="wechat">
        <div class="wechat-login">
          <div class="qr-placeholder">
            <el-icon :size="48" color="#07c160"><ChatDotSquare /></el-icon>
            <p>请使用微信扫一扫登录</p>
          </div>
        </div>
      </el-tab-pane>
    </el-tabs>
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
import { useKeepAliveStore } from "@/stores/modules/keepAlive";
import { initDynamicRouter } from "@/routers/modules/dynamicRouter";
import { User, Lock, ChatDotSquare } from "@element-plus/icons-vue";
import type { ElForm } from "element-plus";
import md5 from "md5";

const router = useRouter();
const userStore = useUserStore();
const tabsStore = useTabsStore();
const keepAliveStore = useKeepAliveStore();

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
      keepAliveStore.setKeepAliveName([]);
      router.push(HOME_URL);

      ElNotification({
        title: "登录成功",
        message: "欢迎登录 JUNAN 客户关系管理系统",
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
</style>

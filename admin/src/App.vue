<template>
  <el-config-provider :locale="locale" :size="assemblySize" :button="buttonConfig">
    <router-view></router-view>
    <button v-show="assistantVisible && !assistantOpen" class="data-assistant-tab" type="button" @click="openDataAssistant">数据助手</button>
    <button v-show="assistantVisible && assistantOpen" class="data-assistant-collapse" type="button" @click="closeDataAssistant">收起</button>
  </el-config-provider>
</template>

<script setup lang="ts">
import { onBeforeUnmount, onMounted, reactive, computed, ref, watchEffect } from "vue";
import { useRoute } from "vue-router";
import { useI18n } from "vue-i18n";
import { getBrowserLang } from "@/utils";
import { useTheme } from "@/hooks/useTheme";
import { ElConfigProvider } from "element-plus";
import { LanguageType } from "@/stores/interface";
import { useGlobalStore } from "@/stores/modules/global";
import { LOGIN_URL } from "@/config";
import en from "element-plus/es/locale/lang/en";
import zhCn from "element-plus/es/locale/lang/zh-cn";

const globalStore = useGlobalStore();
const route = useRoute();

// init theme
const { initTheme } = useTheme();
initTheme();

// init language
const i18n = useI18n();

onMounted(() => {
  const language = globalStore.language ?? getBrowserLang();
  i18n.locale.value = language;
  globalStore.setGlobalState("language", language as LanguageType);
  window.addEventListener("message", handleAssistantMessage);
});

// element language
const locale = computed(() => {
  if (globalStore.language == "zh") return zhCn;
  if (globalStore.language == "en") return en;
  return getBrowserLang() == "zh" ? zhCn : en;
});

// element assemblySize
const assemblySize = computed(() => globalStore.assemblySize);

// element button config
const buttonConfig = reactive({ autoInsertSpace: false });

const difyToken = "O9BNPIV1JQDEpOKG";
const difyBaseUrl = "http://192.168.0.105:8080";
const assistantLoaded = ref(false);
const assistantOpen = ref(false);
const assistantVisible = computed(() => route.path.toLocaleLowerCase() !== LOGIN_URL);
let assistantWindowObserver: MutationObserver | null = null;
let closeAssistantTimer: ReturnType<typeof setTimeout> | null = null;
let assistantReadyTimer: ReturnType<typeof setTimeout> | null = null;

watchEffect(() => {
  document.documentElement.classList.toggle("data-assistant-hidden", !assistantVisible.value);
});

const handleAssistantMessage = (event: MessageEvent) => {
  if (event.origin !== difyBaseUrl) return;
  if (event.data?.type !== "dify-chatbot-iframe-ready") return;

  scheduleAssistantReady(350);
};

const markAssistantReady = () => {
  if (assistantReadyTimer) {
    clearTimeout(assistantReadyTimer);
    assistantReadyTimer = null;
  }
  document.documentElement.classList.add("data-assistant-ready");
};

const scheduleAssistantReady = (delay = 0) => {
  if (assistantReadyTimer) clearTimeout(assistantReadyTimer);
  assistantReadyTimer = setTimeout(markAssistantReady, delay);
};

const syncAssistantOpenState = () => {
  if (document.documentElement.classList.contains("data-assistant-closing")) return;

  const assistantWindow = document.getElementById("dify-chatbot-bubble-window");
  const isOpen = assistantWindow ? getComputedStyle(assistantWindow).display !== "none" : false;
  assistantOpen.value = isOpen;
  document.documentElement.classList.toggle("data-assistant-open", isOpen);
};

const watchAssistantWindow = () => {
  const assistantWindow = document.getElementById("dify-chatbot-bubble-window");
  if (!assistantWindow || assistantWindowObserver) return;

  syncAssistantOpenState();
  assistantWindowObserver = new MutationObserver(syncAssistantOpenState);
  assistantWindowObserver.observe(assistantWindow, {
    attributes: true,
    attributeFilter: ["style", "class"]
  });
};

const openDataAssistant = () => {
  if (closeAssistantTimer) {
    clearTimeout(closeAssistantTimer);
    closeAssistantTimer = null;
  }
  document.documentElement.classList.remove("data-assistant-closing");

  if (assistantLoaded.value) {
    assistantOpen.value = true;
    document.documentElement.classList.add("data-assistant-open");
    document.getElementById("dify-chatbot-bubble-button")?.click();
    setTimeout(syncAssistantOpenState, 100);
    return;
  }

  document.documentElement.classList.remove("data-assistant-ready");

  (window as any).difyChatbotConfig = {
    token: difyToken,
    baseUrl: difyBaseUrl,
    routeSegment: "agent",
    dynamicScript: true,
    inputs: {},
    systemVariables: {},
    userVariables: {}
  };

  const script = document.createElement("script");
  script.id = difyToken;
  script.src = `${difyBaseUrl}/embed.min.js`;
  script.defer = true;
  script.onload = () => {
    assistantLoaded.value = true;
    setTimeout(() => {
      assistantOpen.value = true;
      document.documentElement.classList.add("data-assistant-open");
      document.getElementById("dify-chatbot-bubble-button")?.click();
      watchAssistantWindow();
      syncAssistantOpenState();
      scheduleAssistantReady(1200);
    }, 100);
  };
  document.body.appendChild(script);
};

const closeDataAssistant = () => {
  document.documentElement.classList.add("data-assistant-closing");
  closeAssistantTimer = setTimeout(() => {
    document.getElementById("dify-chatbot-bubble-button")?.click();
    document.documentElement.classList.remove("data-assistant-closing");
    closeAssistantTimer = null;
    syncAssistantOpenState();
  }, 220);
};

onBeforeUnmount(() => {
  if (closeAssistantTimer) clearTimeout(closeAssistantTimer);
  if (assistantReadyTimer) clearTimeout(assistantReadyTimer);
  window.removeEventListener("message", handleAssistantMessage);
  assistantWindowObserver?.disconnect();
  document.documentElement.classList.remove("data-assistant-open");
  document.documentElement.classList.remove("data-assistant-closing");
  document.documentElement.classList.remove("data-assistant-ready");
  document.documentElement.classList.remove("data-assistant-hidden");
});
</script>

<style scoped lang="scss">
@import "./App.scss";
</style>

<style lang="scss">
@import "./App-second.scss";
</style>


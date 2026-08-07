<template>
  <el-config-provider :locale="locale" :size="assemblySize" :button="buttonConfig">
    <router-view></router-view>
    <button class="data-assistant-button" type="button" @click="openDataAssistant">数据助手</button>
  </el-config-provider>
</template>

<script setup lang="ts">
import { onMounted, reactive, computed, ref } from "vue";
import { useI18n } from "vue-i18n";
import { getBrowserLang } from "@/utils";
import { useTheme } from "@/hooks/useTheme";
import { ElConfigProvider } from "element-plus";
import { LanguageType } from "@/stores/interface";
import { useGlobalStore } from "@/stores/modules/global";
import en from "element-plus/es/locale/lang/en";
import zhCn from "element-plus/es/locale/lang/zh-cn";

const globalStore = useGlobalStore();

// init theme
const { initTheme } = useTheme();
initTheme();

// init language
const i18n = useI18n();

onMounted(() => {
  const language = globalStore.language ?? getBrowserLang();
  i18n.locale.value = language;
  globalStore.setGlobalState("language", language as LanguageType);
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

const difyToken = "SIEMfSwavwBX6Lsy";
const difyBaseUrl = "http://192.168.0.105:8080";
const assistantLoaded = ref(false);

const openDataAssistant = () => {
  if (assistantLoaded.value) {
    document.getElementById("dify-chatbot-bubble-button")?.click();
    return;
  }

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
    setTimeout(() => document.getElementById("dify-chatbot-bubble-button")?.click(), 100);
  };
  document.body.appendChild(script);
};
</script>

<style scoped>
.data-assistant-button {
  position: fixed;
  right: 24px;
  bottom: 24px;
  z-index: 3000;
  height: 44px;
  padding: 0 18px;
  border: 0;
  border-radius: 22px;
  background: #1c64f2;
  box-shadow: 0 10px 24px rgb(28 100 242 / 28%);
  color: #ffffff;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
}

.data-assistant-button:hover {
  background: #1557d8;
}
</style>

<style>
#dify-chatbot-bubble-button {
  position: fixed !important;
  right: 24px !important;
  bottom: 24px !important;
  width: 44px !important;
  height: 44px !important;
  opacity: 0 !important;
  pointer-events: none !important;
  background-color: #1c64f2 !important;
}

#dify-chatbot-bubble-window {
  width: 24rem !important;
  height: 40rem !important;
}
</style>


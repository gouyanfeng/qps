<template>
  <el-config-provider :locale="locale" :size="assemblySize" :button="buttonConfig">
    <router-view></router-view>
    <button v-show="!assistantOpen" class="data-assistant-tab" type="button" @click="openDataAssistant">数据助手</button>
    <button v-show="assistantOpen" class="data-assistant-collapse" type="button" @click="closeDataAssistant">收起</button>
  </el-config-provider>
</template>

<script setup lang="ts">
import { onBeforeUnmount, onMounted, reactive, computed, ref } from "vue";
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
const assistantOpen = ref(false);
let assistantWindowObserver: MutationObserver | null = null;

const syncAssistantOpenState = () => {
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
  if (assistantLoaded.value) {
    document.getElementById("dify-chatbot-bubble-button")?.click();
    setTimeout(syncAssistantOpenState, 100);
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
    setTimeout(() => {
      document.getElementById("dify-chatbot-bubble-button")?.click();
      watchAssistantWindow();
      syncAssistantOpenState();
    }, 100);
  };
  document.body.appendChild(script);
};

const closeDataAssistant = () => {
  document.getElementById("dify-chatbot-bubble-button")?.click();
  setTimeout(syncAssistantOpenState, 100);
};

onBeforeUnmount(() => {
  assistantWindowObserver?.disconnect();
  document.documentElement.classList.remove("data-assistant-open");
});
</script>

<style scoped>
.data-assistant-tab {
  position: fixed;
  right: 0;
  top: 50%;
  z-index: 3000;
  width: 36px;
  min-height: 104px;
  padding: 12px 8px;
  border: 0;
  border-radius: 12px 0 0 12px;
  background: #1c64f2;
  box-shadow: -8px 12px 28px rgb(28 100 242 / 26%);
  color: #ffffff;
  font-size: 14px;
  font-weight: 600;
  line-height: 1.2;
  writing-mode: vertical-rl;
  cursor: pointer;
  transform: translateY(-50%);
  transition:
    transform 0.18s ease,
    box-shadow 0.18s ease,
    background 0.18s ease;
  animation: data-assistant-tab-enter 0.22s ease-out;
}

.data-assistant-tab:hover {
  background: #1557d8;
  box-shadow: -10px 16px 32px rgb(28 100 242 / 34%);
  transform: translateY(-50%) translateX(-2px);
}

.data-assistant-tab:active {
  transform: translateY(-50%) translateX(0) scale(0.98);
}

.data-assistant-collapse {
  position: fixed;
  right: min(420px, calc(100vw - 32px));
  top: 50%;
  z-index: 2147483647;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  min-height: 72px;
  padding: 10px 6px;
  border: 1px solid rgb(226 232 240 / 90%);
  border-right: 0;
  border-radius: 10px 0 0 10px;
  background: rgb(255 255 255 / 96%);
  box-shadow: -8px 12px 24px rgb(15 23 42 / 12%);
  color: #334155;
  font-size: 13px;
  font-weight: 600;
  line-height: 1.2;
  writing-mode: vertical-rl;
  cursor: pointer;
  transform: translateY(-50%);
  transition:
    transform 0.16s ease,
    box-shadow 0.16s ease,
    color 0.16s ease,
    background 0.16s ease;
  animation: data-assistant-collapse-enter 0.2s ease-out;
}

.data-assistant-collapse:hover {
  background: #f8fafc;
  box-shadow: -10px 16px 28px rgb(15 23 42 / 16%);
  color: #1e40af;
  transform: translateY(-50%) translateX(-2px);
}

.data-assistant-collapse:active {
  transform: translateY(-50%) scale(0.98);
}

@keyframes data-assistant-tab-enter {
  from {
    opacity: 0;
    transform: translateY(-50%) translateX(12px);
  }

  to {
    opacity: 1;
    transform: translateY(-50%) translateX(0);
  }
}

@keyframes data-assistant-collapse-enter {
  from {
    opacity: 0;
    transform: translateY(-50%) translateX(12px);
  }

  to {
    opacity: 1;
    transform: translateY(-50%) translateX(0);
  }
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

.data-assistant-open #dify-chatbot-bubble-button {
  right: min(420px, calc(100vw - 32px)) !important;
  top: 50% !important;
  bottom: auto !important;
}

#dify-chatbot-bubble-window {
  top: 50% !important;
  right: 0 !important;
  width: min(420px, calc(100vw - 32px)) !important;
  max-width: none !important;
  height: min(640px, calc(100vh - 96px)) !important;
  max-height: none !important;
  border-radius: 12px 0 0 12px !important;
  box-shadow: -18px 0 42px rgb(15 23 42 / 18%) !important;
  transform: translateY(-50%);
}

.data-assistant-open #dify-chatbot-bubble-window {
  animation: data-assistant-drawer-enter 0.22s ease-out;
  transform-origin: right center;
}

@keyframes data-assistant-drawer-enter {
  from {
    transform: translateY(-50%) translateX(24px);
  }

  to {
    transform: translateY(-50%) translateX(0);
  }
}

@media (prefers-reduced-motion: reduce) {
  .data-assistant-tab,
  .data-assistant-collapse,
  .data-assistant-open #dify-chatbot-bubble-window {
    animation: none !important;
    transition: none !important;
  }

  .data-assistant-tab:hover,
  .data-assistant-collapse:hover,
  .data-assistant-tab:active,
  .data-assistant-collapse:active {
    transform: translateY(-50%);
  }
}
</style>


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
let closeAssistantTimer: ReturnType<typeof setTimeout> | null = null;

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
  assistantWindowObserver?.disconnect();
  document.documentElement.classList.remove("data-assistant-open");
  document.documentElement.classList.remove("data-assistant-closing");
});
</script>

<style scoped>
.data-assistant-tab {
  position: fixed;
  right: 0;
  top: 76%;
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
    transform 0.2s cubic-bezier(0.22, 1, 0.36, 1),
    box-shadow 0.2s ease,
    background 0.2s ease;
  animation: data-assistant-tab-enter 0.24s cubic-bezier(0.22, 1, 0.36, 1);
  will-change: transform;
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
  right: 420px;
  bottom: 308px;
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
  transition:
    transform 0.18s cubic-bezier(0.22, 1, 0.36, 1),
    box-shadow 0.18s ease,
    color 0.18s ease,
    background 0.18s ease;
  animation: data-assistant-collapse-enter 0.24s cubic-bezier(0.22, 1, 0.36, 1);
  will-change: transform;
}

.data-assistant-collapse:hover {
  background: #f8fafc;
  box-shadow: -10px 16px 28px rgb(15 23 42 / 16%);
  color: #1e40af;
  transform: translateX(-2px);
}

.data-assistant-collapse:active {
  transform: scale(0.98);
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
    transform: translateX(12px);
  }

  to {
    opacity: 1;
    transform: translateX(0);
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
  right: 420px !important;
  top: auto !important;
  bottom: 322px !important;
}

#dify-chatbot-bubble-window {
  top: auto !important;
  right: 0 !important;
  bottom: 24px !important;
  width: 420px !important;
  max-width: none !important;
  height: 640px !important;
  max-height: none !important;
  border-radius: 12px 0 0 12px !important;
  box-shadow: -18px 0 42px rgb(15 23 42 / 18%) !important;
}

.data-assistant-open #dify-chatbot-bubble-window {
  animation: data-assistant-drawer-enter 0.24s cubic-bezier(0.22, 1, 0.36, 1);
  transform-origin: right center;
  will-change: transform;
}

.data-assistant-closing #dify-chatbot-bubble-window {
  animation: data-assistant-drawer-leave 0.2s cubic-bezier(0.4, 0, 1, 1) forwards;
  pointer-events: none !important;
}

.data-assistant-closing .data-assistant-collapse {
  animation: data-assistant-collapse-leave 0.16s cubic-bezier(0.4, 0, 1, 1) forwards;
  pointer-events: none;
}

@keyframes data-assistant-drawer-enter {
  from {
    opacity: 0.96;
    transform: translateX(24px);
  }

  to {
    opacity: 1;
    transform: translateX(0);
  }
}

@keyframes data-assistant-drawer-leave {
  from {
    opacity: 1;
    transform: translateX(0);
  }

  to {
    opacity: 0;
    transform: translateX(28px);
  }
}

@keyframes data-assistant-collapse-leave {
  from {
    opacity: 1;
    transform: translateX(0);
  }

  to {
    opacity: 0;
    transform: translateX(10px);
  }
}

@media (prefers-reduced-motion: reduce) {
  .data-assistant-tab,
  .data-assistant-collapse,
  .data-assistant-open #dify-chatbot-bubble-window,
  .data-assistant-closing #dify-chatbot-bubble-window {
    animation: none !important;
    transition: none !important;
  }

  .data-assistant-tab:hover,
  .data-assistant-collapse:hover,
  .data-assistant-tab:active,
  .data-assistant-collapse:active {
    transform: none;
  }
}
</style>


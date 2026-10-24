import { createSSRApp } from "vue";
import App from "./App.vue";
import TDesign from 'tdesign-mobile-vue';
import { Icon } from 'tdesign-icons-vue-next/esm/svg-sprite/index.js';
import 'tdesign-mobile-vue/es/style/index.css';

// 关闭从 tdesign.gtimg.com CDN 拉取图标
(Icon as any).props.loadDefaultIcons.default = false;

export function createApp() {
  const app = createSSRApp(App);
  app.use(TDesign);
  return {
    app,
  };
}
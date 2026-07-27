import "@/styles/reset.scss";
import "element-plus/dist/index.css";
import "element-plus/theme-chalk/dark/css-vars.css";
import "@/styles/common.scss";
import "@/assets/iconfont/iconfont.scss";
import "@/assets/fonts/font.scss";
import "@/styles/element-dark.scss";
import "@/styles/element.scss";

import { createApp } from "vue";
import * as Icons from "@element-plus/icons-vue";
import App from "@/App.vue";
import ElementPlus from "element-plus";
import router from "@/routers";
import pinia from "@/stores";
import I18n from "@/languages/index";

const app = createApp(App);

// register the element Icons component
Object.keys(Icons).forEach((key) => {
  app.component(key, Icons[key as keyof typeof Icons]);
});

app.use(router);
app.use(ElementPlus);
app.use(pinia);
app.use(I18n);
app.mount("#app");

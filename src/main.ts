import { createApp } from 'vue'
import {  createRouter ,createWebHashHistory} from 'vue-router'


// reset style sheet
import "@/styles/reset.scss";
// CSS common style sheet
import "@/styles/common.scss";
// iconfont css
import "@/assets/iconfont/iconfont.scss";
// font css
import "@/assets/fonts/font.scss";
// element css
import "element-plus/dist/index.css";
// element dark css
import "element-plus/theme-chalk/dark/css-vars.css";
// custom element dark css
import "@/styles/element-dark.scss";
// custom element css
import "@/styles/element.scss";

import App from './App.vue'
import ElementPlus from 'element-plus'
import 'element-plus/dist/index.css'
import 'element-plus/theme-chalk/dark/css-vars.css'

// pinia store
import pinia from "@/stores";

import HelloWorld from './components/HelloWorld.vue'
import AboutView from './components/AboutView.vue'




const routes = [
  { path: '/', component: HelloWorld },
  { path: '/about', component: AboutView },
]

const router = createRouter({
  history: createWebHashHistory(),
  routes,
})

const app = createApp(App)
app.use(router)
app.use(ElementPlus)
app.use(pinia)
app.mount('#app')

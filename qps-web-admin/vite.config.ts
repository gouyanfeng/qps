/*
 * Vite 配置文件
 * 用于配置 Vite 开发服务器和构建选项
 */

// 导入 Vite 的 defineConfig 函数
import { defineConfig } from "vite";
// 导入 Vue 插件
import vue from "@vitejs/plugin-vue";
import vueJsx from "@vitejs/plugin-vue-jsx"; // 1. 引入插件;
// 导入 path 模块，用于处理文件路径
import path from "path";

// 导出 Vite 配置
// https://vite.dev/config/
export default defineConfig({
  // 插件配置
  plugins: [
    // 使用 Vue 插件
    vue(),
    vueJsx(), // 2. 启用 JSX 支持
  ],

  // 解析配置
  resolve: {
    // 路径别名配置
    alias: {
      // 将 @ 映射到项目的 src 目录
      "@": path.resolve(__dirname, "./src"),
    },
  },

  // CSS 配置
  css: {
    // 预处理器配置
    preprocessorOptions: {
      // SCSS 配置
      scss: {
        // 使用现代编译器（这是为了支持下面的 silenceDeprecations）
        // api: 'modern-compiler',
        // 屏蔽关于 @import 的废弃警告
        silenceDeprecations: ["import"],
      },
    },
  },

  // 开发服务器配置
  server: {
    // 监听所有网络接口，允许通过 IP / 局域网访问
    host: true,
    // 固定端口，避免每次启动变化
    port: 5173,
    // 端口被占用时报错而不自动 +1，便于排查
    strictPort: false,
  },
});

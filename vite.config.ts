/*
 * Vite 配置文件
 * 用于配置 Vite 开发服务器和构建选项
 */

// 导入 Vite 的 defineConfig 函数
import { defineConfig } from 'vite'
// 导入 Vue 插件
import vue from '@vitejs/plugin-vue'
// 导入 path 模块，用于处理文件路径
import path from 'path'

// 导出 Vite 配置
// https://vite.dev/config/
export default defineConfig({
  // 插件配置
  plugins: [
    // 使用 Vue 插件
    vue()
  ],
  
  // 解析配置
  resolve: {
    // 路径别名配置
    alias: {
      // 将 @ 映射到项目的 src 目录
      '@': path.resolve(__dirname, './src')
    }
  },
  
  // 开发服务器配置
  server: {
    // 代理配置
    proxy: {
      // 配置 /api 路径的代理
      "/api": {
        // 代理目标地址
        target: 'https://mock.mengxuegu.com/mock/629d727e6163854a32e8307e',
        // 是否改变请求头中的 Origin
        changeOrigin: true,
        // 是否验证 SSL 证书
        secure: false,
        // 重写路径，将 /api 前缀移除
        rewrite: (path) => path.replace(/^\/api/, "")
      }
    }
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
        silenceDeprecations: ['import'],
      },
    },
  },
})
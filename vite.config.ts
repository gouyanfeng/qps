import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import path from 'path' // 1. 引入 path 模块

// https://vite.dev/config/
export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      // 2. 配置别名
      '@': path.resolve(__dirname, './src')
    }
  }
})

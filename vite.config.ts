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
  },
  server: {
    proxy: {
      // 3. 配置代理  
      "/api": {
        target: 'https://mock.mengxuegu.com/mock/629d727e6163854a32e8307e',
        changeOrigin: true,
        secure: false,
        rewrite: (path) => path.replace(/^\/api/, "")
      }
    }
  }, css: {
    preprocessorOptions: {
      scss: {
        // 使用现代编译器（这是为了支持下面的 silenceDeprecations）
        // api: 'modern-compiler', 
        // 屏蔽关于 @import 的废弃警告
        silenceDeprecations: ['import'],
      },
    },
  },
})

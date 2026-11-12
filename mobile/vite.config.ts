import { defineConfig } from "vite";
import uni from "@dcloudio/vite-plugin-uni";

// https://vitejs.dev/config/
export default defineConfig({
  // H5 容器直接暴露 5174 端口、按根路径托管（不再有网关 /h5/ 前缀），
  // 产物按根路径构建（base 默认 /），本地 dev 同样用 http://localhost:5174 访问。
  envDir: "..",
  plugins: [uni()],
  server: {
    port: 5174,
    strictPort: true,
    host: '0.0.0.0'
  },
  css: {
    preprocessorOptions: {
      scss: {
        silenceDeprecations: ["legacy-js-api"],
      },
    },
  },
});
import { defineConfig } from "vite";
import uni from "@dcloudio/vite-plugin-uni";

// https://vitejs.dev/config/
export default defineConfig({
  // H5 固定挂网关 /h5/ 子路径下，但网关转发时会剥掉前缀，
  // 因此产物按根路径构建（base 默认 /），本地 dev 也用 http://localhost:5174 访问。
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
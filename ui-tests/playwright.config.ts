import { defineConfig, devices } from "@playwright/test";
import path from "node:path";

const rootDir = import.meta.dirname;
const { deviceScaleFactor: _deviceScaleFactor, ...desktopEdge } = devices["Desktop Edge"];

// 支持通过环境变量切换到远端部署环境做全流程回归
const adminBaseUrl = process.env.ADMIN_BASE_URL || "http://127.0.0.1:5173";

export default defineConfig({
  testDir: rootDir,
  testMatch: /.*\.spec\.(mjs|ts)/,
  // COMFY_INCLUDE=1 时放行 comfy-images 用例（默认忽略，避免常规回归触发 ComfyUI 出图）
  testIgnore: process.env.COMFY_INCLUDE === "1" ? /system-management\.real-ui\.spec\.ts/ : /(system-management\.real-ui\.spec\.ts|comfy-images)/,
  timeout: 120_000,
  expect: {
    timeout: 15_000
  },
  fullyParallel: false,
  workers: 1,
  reporter: [
    ["list"],
    ["html", { outputFolder: path.join(rootDir, "playwright-output", "report"), open: "never" }]
  ],
  outputDir: path.join(rootDir, "playwright-output", "test-results"),
  use: {
    ...desktopEdge,
    baseURL: adminBaseUrl,
    channel: "msedge",
    headless: false,
    viewport: null,
    launchOptions: {
      args: ["--start-maximized"],
    },
    actionTimeout: 20_000,
    navigationTimeout: 45_000,
    screenshot: "only-on-failure",
    trace: "retain-on-failure"
  },
});
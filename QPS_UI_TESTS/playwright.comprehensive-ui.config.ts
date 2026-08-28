import { defineConfig, devices } from "../QPS_WEB_ADMIN/node_modules/@playwright/test";

export default defineConfig({
  testDir: ".",
  testMatch: "*.spec.ts",
  timeout: 60_000,
  expect: {
    timeout: 10_000
  },
  workers: 1,
  fullyParallel: false,
  reporter: [["list"], ["html", { outputFolder: "playwright-report-comprehensive-ui", open: "never" }]],
  use: {
    baseURL: "http://127.0.0.1:5174",
    trace: "retain-on-failure",
    screenshot: "only-on-failure",
    video: "off"
  },
  webServer: [
    {
      command: "dotnet run --project E:\\Code\\QPS\\QPS-HT\\src\\4.QPS.WebAPI\\QPS.WebAPI.csproj --urls http://127.0.0.1:20004",
      url: "http://127.0.0.1:20004/swagger/index.html",
      reuseExistingServer: true,
      timeout: 120_000
    },
    {
      command: "powershell -NoProfile -Command \"$env:VITE_API_URL='http://127.0.0.1:20004/api'; npm --prefix E:\\Code\\QPS\\QPS_WEB_ADMIN run dev -- --host 127.0.0.1 --port 5174\"",
      url: "http://127.0.0.1:5174",
      reuseExistingServer: true,
      timeout: 120_000
    }
  ],
  projects: [
    {
      name: "chrome",
      use: {
        ...devices["Desktop Chrome"],
        channel: "chrome"
      }
    }
  ]
});

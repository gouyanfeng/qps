import { expect, test } from "../QPS_WEB_ADMIN/node_modules/@playwright/test";
import { attachFailureWatch, setupMockedApp, loginWithMockedAdmin, crmPermissions, pageRoutes } from "./helpers";

test.describe("auth and shell UI", () => {
  test("login page and authenticated shell render correctly", async ({ page }) => {
    const failures = attachFailureWatch(page);
    await setupMockedApp(page);

    await page.goto("/login");
    await expect(page.locator(".login-container")).toBeVisible();
    await expect(page.locator('input[placeholder*="admin"]')).toHaveValue("admin");
    await expect(page.locator('input[type="password"]')).toHaveValue("123456");
    await page.getByRole("button", { name: /登录/ }).click();

    await expect(page).toHaveURL(/#\/home\/index/);
    await expect(page.locator("#watermark")).toBeVisible();
    await expect(page.getByRole("tab", { name: "首页" })).toBeVisible();
    expect(failures).toEqual([]);
  });

  test("all configured pages render without visible runtime failures", async ({ page }) => {
    const failures = attachFailureWatch(page);
    await setupMockedApp(page);
    await loginWithMockedAdmin(page, [...crmPermissions, "CRM_VENDOR"]);

    for (const route of pageRoutes) {
      await page.goto(`/#${route.path}`);
      await page.waitForLoadState("domcontentloaded");
      await expect(page.locator("#app")).toBeVisible();
      await expect(page.locator(".el-loading-mask")).toHaveCount(0);
      await expect(page.locator("body")).not.toContainText("Cannot read");
      await expect(page).toHaveTitle(new RegExp(route.title === "首页" ? "QPS|首页" : "QPS"));
    }

    expect(failures).toEqual([]);
  });

  test("common query controls search reset collapse and pagination are usable", async ({ page }) => {
    const failures = attachFailureWatch(page);
    await setupMockedApp(page);
    await loginWithMockedAdmin(page, [...crmPermissions, "CRM_VENDOR"]);
    await page.goto("/#/crm/herb-base");

    await expect(page.getByPlaceholder("基地 / 主体 / 联系人 / 电话")).toBeVisible();
    await page.getByPlaceholder("基地 / 主体 / 联系人 / 电话").fill("Codex");
    await page.getByRole("button", { name: "搜索", exact: true }).click();
    await expect(page.locator(".el-table__body-wrapper tr", { hasText: "Codex测试药材公司" })).toBeVisible();

    await page.getByRole("button", { name: "重置" }).click();
    await expect(page.getByPlaceholder("基地 / 主体 / 联系人 / 电话")).toHaveValue("");

    await page.getByRole("button", { name: /展开/ }).click();
    await expect(page.locator(".el-form-item", { hasText: "状态" })).toBeVisible();
    await page.getByRole("button", { name: /收起/ }).click();
    await page.getByRole("button", { name: "隐藏搜索" }).click();
    await expect(page.getByPlaceholder("基地 / 主体 / 联系人 / 电话")).toBeHidden();
    await page.getByRole("button", { name: "显示搜索" }).click();
    await expect(page.getByPlaceholder("基地 / 主体 / 联系人 / 电话")).toBeVisible();

    await expect(page.locator(".el-pagination")).toBeVisible();
    expect(failures).toEqual([]);
  });
});

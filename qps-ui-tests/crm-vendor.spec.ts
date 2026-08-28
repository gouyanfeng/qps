import { expect, test } from "../QPS_WEB_ADMIN/node_modules/@playwright/test";
import { attachFailureWatch, setupMockedApp, loginWithMockedAdmin, crmPermissions } from "./helpers";

test.describe("crm vendor UI", () => {
  test("vendor list and detail drawer render purchasing information", async ({ page }) => {
    const failures = attachFailureWatch(page);
    await setupMockedApp(page);
    await loginWithMockedAdmin(page, [...crmPermissions, "CRM_VENDOR"]);
    await page.goto("/#/crm/vendor");

    await expect(page.locator(".el-table__body-wrapper tr", { hasText: "Codex测试药材厂商" })).toBeVisible();
    await page.getByRole("button", { name: "详情" }).click();
    const drawer = page.locator(".vendor-drawer");
    await expect(drawer).toBeVisible();
    await expect(drawer.getByRole("heading", { name: "Codex测试药材厂商" })).toBeVisible();
    await expect(drawer.getByText("李经理").first()).toBeVisible();
    await expect(drawer.getByText("13900000001").first()).toBeVisible();
    await expect(drawer.getByText("黄芪").first()).toBeVisible();
    await expect(drawer.getByText("当归").first()).toBeVisible();
    await expect(drawer.getByText("重点厂商")).toBeVisible();
    await expect(drawer.getByRole("heading", { name: "联系人" })).toBeVisible();
    await expect(drawer.getByRole("heading", { name: "采购品类" })).toBeVisible();
    await expect(drawer.getByRole("heading", { name: "采购计划" })).toBeVisible();
    await expect(drawer.getByText("黄芪年度采购计划").first()).toBeVisible();
    expect(failures).toEqual([]);
  });
});

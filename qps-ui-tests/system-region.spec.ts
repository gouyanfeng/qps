import { expect, test } from "../QPS_WEB_ADMIN/node_modules/@playwright/test";
import { attachFailureWatch, setupMockedApp, loginWithMockedAdmin, crmPermissions } from "./helpers";

test.describe("system region UI", () => {
  test("address region page renders SystemChinaRegions data", async ({ page }) => {
    const failures = attachFailureWatch(page);
    await setupMockedApp(page);
    await loginWithMockedAdmin(page, [...crmPermissions, "CRM_VENDOR"]);

    let legacyRegionApiCalls = 0;
    await page.route("**/api/admin/regions**", async route => {
      legacyRegionApiCalls += 1;
      await route.fulfill({
        status: 500,
        contentType: "application/json",
        body: JSON.stringify({ code: 500, msg: "legacy SystemRegions API must not be used", data: null })
      });
    });

    await page.goto("/#/system/region");

    await expect(page.getByPlaceholder("请输入区域编码")).toBeVisible();
    await expect(page.getByPlaceholder("请输入区域名称")).toBeVisible();
    await expect(page.locator(".el-table__body-wrapper tr", { hasText: "620000" })).toContainText("甘肃省");
    await expect(page.locator(".el-table__body-wrapper tr", { hasText: "620000" })).toContainText("省");
    await expect(page.locator(".el-table__body-wrapper tr", { hasText: "621100" })).toContainText("定西市");
    await expect(page.locator(".el-table__body-wrapper tr", { hasText: "621100" })).toContainText("市");
    await expect(page.locator(".el-table__body-wrapper tr", { hasText: "621100" })).toContainText("甘肃省");
    await expect(page.locator(".el-table__body-wrapper tr", { hasText: "621122" })).toContainText("甘肃省定西市陇西县");
    await expect(page.getByRole("button", { name: "新增区域" })).toHaveCount(0);
    await expect(page.getByRole("button", { name: "编辑" })).toHaveCount(0);

    await page.getByPlaceholder("请输入区域名称").fill("陇西");
    await page.getByRole("button", { name: "搜索" }).click();
    await expect(page.locator(".el-table__body-wrapper tr", { hasText: "621122" })).toBeVisible();
    await expect(page.locator(".el-table__body-wrapper tr", { hasText: "620000" })).toHaveCount(0);

    expect(legacyRegionApiCalls).toBe(0);
    expect(failures).toEqual([]);
  });
});

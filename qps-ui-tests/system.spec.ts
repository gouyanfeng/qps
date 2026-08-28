import { expect, test } from "../QPS_WEB_ADMIN/node_modules/@playwright/test";
import { attachFailureWatch, setupMockedApp, loginWithMockedAdmin, crmPermissions } from "./helpers";

test.describe("system pages UI", () => {
  test("system list pages expose search table and add edit dialogs", async ({ page }) => {
    const failures = attachFailureWatch(page);
    await setupMockedApp(page);
    await loginWithMockedAdmin(page, [...crmPermissions, "CRM_VENDOR"]);

    await page.goto("/#/system/role");
    await expect(page.getByPlaceholder("请输入角色名称")).toBeVisible();
    await expect(page.locator(".el-table__body-wrapper tr", { hasText: "管理员" })).toBeVisible();
    await page.getByRole("button", { name: "新增角色" }).click();
    await expect(page.getByRole("dialog", { name: "新增" })).toBeVisible();
    await page.getByRole("dialog", { name: "新增" }).getByRole("button", { name: "取消" }).click();
    await page.locator(".el-table__body-wrapper tr", { hasText: "管理员" }).getByRole("button", { name: "编辑" }).click();
    await expect(page.getByRole("dialog", { name: "编辑" })).toBeVisible();
    await page.getByRole("dialog", { name: "编辑" }).getByRole("button", { name: "取消" }).click();

    await page.goto("/#/system/users");
    await expect(page.getByPlaceholder("请输入用户名")).toBeVisible();
    await expect(page.locator(".el-table__body-wrapper tr").first()).toBeVisible();
    await page.getByRole("button", { name: "新增用户" }).click();
    await expect(page.getByRole("dialog", { name: "新增" })).toBeVisible();
    await expect(page.getByPlaceholder("请输入密码")).toBeVisible();
    await page.getByRole("dialog", { name: "新增" }).getByRole("button", { name: "取消" }).click();

    await page.goto("/#/system/dataDictionary");
    await expect(page.getByPlaceholder("请输入字典编码")).toBeVisible();
    await expect(page.locator(".el-table__body-wrapper tr", { hasText: "来源平台" })).toBeVisible();
    await page.getByRole("button", { name: "新增字典" }).click();
    await expect(page.getByRole("dialog", { name: "新增数据字典" })).toBeVisible();
    await page.getByRole("dialog", { name: "新增数据字典" }).getByRole("button", { name: "取消" }).click();

    expect(failures).toEqual([]);
  });

  test("permission operation log and error pages are usable", async ({ page }) => {
    const failures = attachFailureWatch(page);
    await setupMockedApp(page);
    await loginWithMockedAdmin(page, [...crmPermissions, "CRM_VENDOR"]);

    await page.goto("/#/system/permission");
    await expect(page.getByText("角色列表")).toBeVisible();
    await page.locator(".role-item", { hasText: "管理员" }).click();
    await expect(page.getByText("管理员 - 权限配置")).toBeVisible();
    await expect(page.getByText("修改记录")).toBeVisible();

    await page.goto("/#/system/operationLog");
    await expect(page.getByPlaceholder("请输入实体类型")).toBeVisible();
    await expect(page.locator(".el-table__body-wrapper tr", { hasText: "CrmHerbBaseSubject" })).toBeVisible();
    await page.getByRole("button", { name: "查看" }).click();
    await expect(page.getByRole("dialog", { name: "变更内容" })).toBeVisible();
    await expect(page.getByText("subjectName")).toBeVisible();

    for (const path of ["/403", "/404", "/500"]) {
      await page.goto(`/#${path}`);
      await expect(page.locator("#app")).toBeVisible();
      await expect(page.locator("body")).toContainText(path.replace("/", ""));
    }

    expect(failures).toEqual([]);
  });
});

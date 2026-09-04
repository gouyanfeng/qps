import { expect, test } from "@playwright/test";
import { adminPermissions, loginWithMockedAdmin, mockCrmHerbBaseApi } from "./helpers/mockApi";

test.beforeEach(async ({ page }) => {
  await mockCrmHerbBaseApi(page);
  await loginWithMockedAdmin(page);
});

test("herb base list renders dictionary labels and herb base operations", async ({ page }) => {
  await page.goto("/#/crm/herb-base");

  const herbBaseRow = page.locator(".el-table__body-wrapper tr", { hasText: "Codex测试种植基地" });

  await expect(herbBaseRow).toBeVisible();
  await expect(herbBaseRow.getByText("Codex测试药材公司")).toBeVisible();
  await expect(herbBaseRow.getByText("百度地图")).toBeVisible();
  await expect(herbBaseRow.getByText("黄芪")).toBeVisible();
  await expect(herbBaseRow.getByText("张经理")).toBeVisible();
  await expect(herbBaseRow.getByText("销售一号")).toBeVisible();
  await expect(page.getByRole("button", { name: "详情" })).toBeVisible();
  await expect(page.getByRole("button", { name: "分配" }).first()).toBeVisible();
  await expect(page.getByRole("button", { name: "记录沟通" })).toBeVisible();
  await expect(page.getByRole("button", { name: "编辑" })).toBeVisible();
});

test("herb base assignment supports bulk selection and refreshes owner", async ({ page }) => {
  await page.goto("/#/crm/herb-base");

  await page.getByRole("button", { name: "分配" }).first().click();
  await expect(page.getByText("请选择要分配的药材基地")).toBeVisible();

  await page.locator(".el-table__header-wrapper .el-checkbox").click();
  await page.getByRole("button", { name: "分配" }).first().click();

  const dialog = page.getByRole("dialog", { name: "分配负责人" });
  await expect(dialog).toBeVisible();
  await dialog.locator(".el-select").click();
  await page.getByText("销售二号").click();
  await dialog.getByPlaceholder("请输入分配备注").fill("批量分配跟进");
  await dialog.getByRole("button", { name: "保存" }).click();

  await expect(page.getByText("分配成功")).toBeVisible();
  const herbBaseRow = page.locator(".el-table__body-wrapper tr", { hasText: "Codex测试种植基地" });
  await expect(herbBaseRow.getByText("销售二号")).toBeVisible();
});

test("herb base assignment button follows server permissions", async ({ page }) => {
  const permissionsWithoutAssign = adminPermissions.filter(permission => permission !== "CRM_HERB_BASE_ASSIGN");
  let permissionsRequests = 0;

  await page.unroute("**/api/admin/auth/login");
  await page.unroute("**/api/admin/auth/user-permissions");
  await page.goto("/login");
  await page.evaluate(() => window.localStorage.clear());
  await page.reload();

  await loginWithMockedAdmin(page, permissionsWithoutAssign, () => {
    permissionsRequests += 1;
  });
  await page.goto("/#/crm/herb-base");

  await expect(page.getByRole("button", { name: "分配" })).toHaveCount(0);
  expect(permissionsRequests).toBe(1);

  await page.evaluate(() => {
    window.localStorage.setItem(
      "qps-user-permissions-v4-11111111-1111-1111-1111-111111111111",
      JSON.stringify({
        timestamp: Date.now(),
        ttl: 60 * 60 * 1000,
        data: ["HOME", "CRM_HERB_BASE", "CRM_HERB_BASE_ASSIGN"]
      })
    );
    window.localStorage.removeItem("qps-user");
  });
  await page.goto("/login");
  await page.getByRole("button", { name: /登录/ }).click();
  await expect(page).not.toHaveURL(/\/login$/);

  expect(permissionsRequests).toBe(2);
});

test("herb base detail drawer renders profile contacts and follow records", async ({ page }) => {
  await page.goto("/#/crm/herb-base");

  await page.getByRole("button", { name: "详情" }).click();

  const drawer = page.locator(".el-drawer", { hasText: "Codex测试种植基地" });

  await expect(drawer.getByRole("heading", { name: "Codex测试种植基地" })).toBeVisible();
  await expect(drawer.getByText("Codex测试药材公司")).toBeVisible();
  await expect(drawer.getByRole("heading", { name: "药材基地资料" })).toBeVisible();
  await expect(drawer.getByRole("heading", { name: "联系人" })).toBeVisible();
  await expect(drawer.getByRole("heading", { name: "沟通记录" })).toBeVisible();
  await expect(drawer.getByRole("heading", { name: "流转记录" })).toBeVisible();
  await expect(drawer.getByText("首阳镇中药材市场1号")).toBeVisible();
  await expect(drawer.getByText("zhang-crm")).toBeVisible();
  await expect(drawer.getByText("客户确认下周继续沟通采购计划")).toBeVisible();
  await expect(drawer.getByText("销售一号 -> 销售二号")).toBeVisible();
  await expect(drawer.getByText("批量分配跟进")).toBeVisible();
});

test("add herb base dialog uses source select without exposing source id", async ({ page }) => {
  await page.goto("/#/crm/herb-base");

  await page.getByRole("button", { name: "新增药材基地" }).click();

  const dialog = page.getByRole("dialog", { name: "新增药材基地" });
  await expect(dialog).toBeVisible();
  await expect(dialog.getByText("基地名称")).toBeVisible();
  await expect(dialog.getByText("主体名称")).toBeVisible();
  await expect(dialog.getByText("来源")).toBeVisible();
  await expect(dialog.locator(".el-select").filter({ hasText: "百度地图" })).toBeVisible();
  await expect(dialog.getByText("来源ID")).toHaveCount(0);
});

test("edit herb base dialog uses source select and china region data", async ({ page }) => {
  await page.goto("/#/crm/herb-base");

  await page.getByRole("button", { name: "编辑" }).click();

  const dialog = page.getByRole("dialog", { name: "编辑药材基地" });
  await expect(dialog).toBeVisible();
  await expect(dialog.getByText("来源")).toBeVisible();
  await expect(dialog.locator(".el-select").filter({ hasText: "百度地图" })).toBeVisible();
  await expect(dialog.getByText("来源ID")).toHaveCount(0);

  await dialog.locator(".el-cascader").click();
  await expect(page.locator(".el-cascader-panel").getByText("甘肃省")).toBeVisible();
});




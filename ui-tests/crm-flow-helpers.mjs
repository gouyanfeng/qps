import { mkdirSync } from "node:fs";
import { dirname } from "node:path";
import { expect } from "@playwright/test";

export const createRunData = () => {
  const suffix = new Date().toISOString().replace(/\D/g, "").slice(0, 14);
  return {
    suffix,
    vendorName: `Codex详情组件厂商-${suffix}`,
    vendorRemark: `Codex厂商详情组件全流程-${suffix}`,
    vendorSourceUrl: `https://example.test/vendor/${suffix}`,
    vendorPurchaseDemandName: `Codex采购需求-${suffix}`,
    vendorPurchaseRemark: `Codex采购需求备注-${suffix}`,
    baseName: `Codex详情组件基地-${suffix}`,
    detailBaseName: `Codex详情组件基地明细-${suffix}`,
    baseRemark: `Codex基地详情组件全流程-${suffix}`,
    baseWechat: `wx_base_${suffix.slice(-8)}`,
    vendorWechat: `wx_vendor_${suffix.slice(-8)}`,
    contactName: `Codex联系人-${suffix.slice(-6)}`,
    contactPhone: `139${suffix.slice(-8)}`,
    baseContactPhone: `138${suffix.slice(-8)}`,
    supplyProducts: ["黄芪", "当归", "党参", "甘草"],
    deleteSupplyProduct: "白芍",
  };
};

export const shot = async (page, suite, name) => {
  const path = `E:/Code/QPS/ui-tests/playwright-output/${suite}/${name}.png`;
  mkdirSync(dirname(path), { recursive: true });
  await page.screenshot({ path, fullPage: true });
};

export const waitIdle = async page => {
  await page.waitForLoadState("networkidle").catch(() => {});
  await page.locator(".el-loading-mask").first().waitFor({ state: "hidden", timeout: 10_000 }).catch(() => {});
};

export const expectToast = async (page, text) => {
  await expect(page.getByText(text).last()).toBeVisible();
};

export const login = async page => {
  await page.goto("/login");
  const accountTab = page.getByRole("button", { name: "密码登录" });
  if (await accountTab.isVisible().catch(() => false)) {
    await accountTab.click();
  }
  await page.getByPlaceholder("name@company.com").fill("admin");
  await page.getByPlaceholder("请输入登录密码").fill("123456");
  await page.getByRole("button", { name: "登录控制台" }).click();
  await expect(page).not.toHaveURL(/\/login$/);
  await waitIdle(page);
};

export const openMenu = async (page, name, urlPattern) => {
  await page.keyboard.press("Escape").catch(() => {});
  await page.waitForTimeout(100);

  const direct = page.getByRole("menuitem", { name, exact: true });
  if (await direct.isVisible().catch(() => false)) {
    await direct.click();
  } else {
    const horizontalSubs = page.locator(".el-menu--horizontal > .el-sub-menu");
    const hcount = await horizontalSubs.count();
    let found = false;
    for (let i = 0; i < hcount; i++) {
      await horizontalSubs.nth(i).hover();
      await page.waitForTimeout(300);
      const target = page.locator(".el-menu--popup:visible").getByRole("menuitem", { name, exact: true }).last();
      if (await target.isVisible().catch(() => false)) {
        await target.click();
        found = true;
        break;
      }
    }
    if (!found) {
      const verticalSubs = page.locator(".el-menu:not(.el-menu--horizontal) > .el-sub-menu");
      const vcount = await verticalSubs.count();
      for (let i = 0; i < vcount; i++) {
        await verticalSubs.nth(i).locator(".el-sub-menu__title").click();
        await page.waitForTimeout(300);
        const target = page.getByRole("menuitem", { name, exact: true }).last();
        if (await target.isVisible().catch(() => false)) {
          await target.click();
          found = true;
          break;
        }
      }
    }
    if (!found) throw new Error(`Menu item "${name}" not found after expanding all sub-menus`);
  }

  await expect(page).toHaveURL(urlPattern);
  await waitIdle(page);
};

export const closeDrawer = async page => {
  const drawer = page.locator(".el-drawer").last();
  if (await drawer.isVisible().catch(() => false)) {
    await drawer.locator(".drawer-close-button").click();
    await drawer.waitFor({ state: "hidden" });
  }
};

export const resetFilters = async page => {
  const resetButton = page.getByRole("button", { name: "重置", exact: true }).first();
  if (await resetButton.isVisible().catch(() => false)) {
    await resetButton.click();
    await waitIdle(page);
  }
};

export const formItemByLabel = (scope, label) => scope.locator(".el-form-item").filter({ hasText: label }).first();

export const fillByLabel = async (scope, label, value) => {
  await formItemByLabel(scope, label).locator("input:not([type='hidden']), textarea").first().fill(value);
};

export const chooseSelectByLabel = async (page, scope, label, option) => {
  await formItemByLabel(scope, label).locator(".el-select").first().click();
  // 只从当前展开的下拉中选，避免命中已关闭弹窗残留的隐藏选项
  await page.locator(".el-select-dropdown:visible").last().getByRole("option", { name: option }).last().click();
};

export const chooseProductByLabel = async (page, scope, label, product) => {
  const item = formItemByLabel(scope, label);
  await item.locator(".el-select").first().click();
  await item.locator("input:not([type='hidden'])").first().fill(product);
  await page.waitForTimeout(800);
  // 品类存在近似项（黄芪 / 蒙古黄芪），必须精确匹配
  await page
    .locator(".el-select-dropdown:visible")
    .last()
    .locator(".el-select-dropdown__item")
    .filter({ hasText: new RegExp(`^\\s*${product}\\s*$`) })
    .first()
    .click();
};

// 多选品类下拉（基地供应品类）：逐项输入过滤后再点选，避免选项超过 100 条时目标不在首屏
export const chooseProductsByLabel = async (page, scope, label, products) => {
  const item = formItemByLabel(scope, label);
  const input = item.locator("input:not([type='hidden'])").first();
  await item.locator(".el-select").first().click();
  for (const product of products) {
    await input.fill(product);
    // 品类下拉是 remote 异步搜索，必须等目标选项真正出现再点，固定延时存在竞态
    const option = page
      .locator(".el-select-dropdown:visible")
      .last()
      .locator(".el-select-dropdown__item")
      .filter({ hasText: new RegExp(`^\\s*${product}\\s*$`) })
      .first();
    await expect(option).toBeVisible({ timeout: 15_000 });
    await option.click();
    await page.waitForTimeout(200);
  }
  await page.keyboard.press("Escape");
  // 多选下拉常驻展开，点弹窗标题区域强制收起，避免遮挡后续表单项
  await page.locator(".el-dialog:visible .el-dialog__header").last().click({ position: { x: 10, y: 10 } });
  await page.locator(".el-select-dropdown:visible").last().waitFor({ state: "hidden", timeout: 3_000 }).catch(() => {});
  await page.waitForTimeout(200);
  // 折叠标签模式下只显示首个标签，逐项内容由保存后的页面断言校验
  await expect(item.locator(".el-tag").first()).toBeVisible();
};

// Every editable field in CRM forms must be exercised, including region cascaders.
export const chooseRegionByLabel = async (page, scope, label, regions) => {
  const item = formItemByLabel(scope, label);
  await item.locator(".el-cascader").first().click();
  for (const region of regions) {
    // 只取当前可见的级联面板，避免命中已关闭弹窗残留的隐藏节点
    const node = page
      .locator(".el-cascader__dropdown:visible")
      .last()
      .locator(".el-cascader-node")
      .filter({ hasText: new RegExp(`^\\s*${region}\\s*$`) })
      .first();
    await node.waitFor({ state: "visible", timeout: 5_000 });
    await node.click();
    await page.waitForTimeout(300);
  }
  await page.keyboard.press("Escape");
  await page.locator(".el-cascader__dropdown:visible").last().waitFor({ state: "hidden", timeout: 3_000 }).catch(() => {});
};

export const setSwitchByLabel = async (scope, label, checked) => {
  const input = formItemByLabel(scope, label).locator("input[type='checkbox']").first();
  if ((await input.isChecked()) !== checked) {
    await formItemByLabel(scope, label).locator(".el-switch").first().click();
  }
};

export const searchListKeyword = async (page, keyword) => {
  await page.getByPlaceholder(/基地|厂商|主体|联系人|电话/).first().fill(keyword);
  await page.getByRole("button", { name: "搜索", exact: true }).click();
  await waitIdle(page);
};

export const searchFollowTask = async (page, keyword = "") => {
  await page.getByPlaceholder("名称、联系人或电话").fill(keyword);
  await page.getByRole("button", { name: "搜索", exact: true }).click();
  await waitIdle(page);
};

export const tableBody = page => page.locator(".el-table__body-wrapper");

export const detailDrawer = page => page.locator(".el-drawer").filter({ hasText: /详情/ }).last();

export const expectVisibleText = async (scope, text) => {
  await expect(scope.getByText(text).first()).toBeVisible();
};

export const clickFirstRowAction = async (page, name) => {
  await tableBody(page).getByRole("button", { name }).first().click();
  await waitIdle(page);
};
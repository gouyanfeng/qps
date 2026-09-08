import { expect, test } from "@playwright/test";
import {
  chooseProductByLabel,
  chooseRegionByLabel,
  chooseSelectByLabel,
  clickFirstRowAction,
  detailDrawer,
  expectToast,
  expectVisibleText,
  fillByLabel,
  formItemByLabel,
  login,
  openMenu,
  resetFilters,
  shot,
  tableBody,
  waitIdle,
} from "./crm-flow-helpers.mjs";

const suffix = new Date().toISOString().replace(/\D/g, "").slice(0, 14);
const prefix = `QPS全流程_撮合_${suffix}`;
const data = {
  productName: "黄芪粉",
  vendorName: `${prefix}_厂商`,
  demandName: `${prefix}_黄芪需求`,
  subjectName: `${prefix}_基地主体`,
  baseName: `${prefix}_基地`,
  specification: `撮合统货_${suffix}`,
  quality: `撮合无硫_${suffix}`,
  nextAction: "核实数量",
  eliminationReason: `规格不符合本轮黄芪需求_${suffix}`,
  closeReason: `本轮人工撮合完成归档_${suffix}`,
};

const chooseSelectInCell = async (page, cell, option) => {
  await cell.locator(".el-select").first().click();
  await page.getByRole("option", { name: option }).last().click();
};

const fillDateTime = async (input, value) => {
  await input.click();
  await input.press("Control+A");
  await input.fill(value.replace("T", " "));
  await input.press("Enter");
  await input.press("Tab");
};

const fillDateTimeByLabel = async (scope, label, value) => {
  const input = formItemByLabel(scope, label).locator(".el-date-editor input").first();
  await fillDateTime(input, value);
  await expect(input).not.toHaveValue("");
};

const chooseVendor = async (page, dialog, vendorName) => {
  const field = formItemByLabel(dialog, "需求方");
  await field.locator(".el-select").first().click();
  await field.locator("input:not([type='hidden'])").first().fill(vendorName);
  await page.getByRole("option", { name: vendorName }).last().click();
};

const fillDemandItem = async (page, dialog) => {
  const cells = dialog.locator(".item-table .el-table__body-wrapper tbody tr").first().locator("td");
  await cells.nth(0).locator(".el-select").first().click();
  await cells.nth(0).locator("input:not([type='hidden'])").first().fill(data.productName);
  await page.getByRole("option", { name: data.productName }).last().click();
  await cells.nth(1).locator("input:not([type='hidden'])").first().fill("300");
  await chooseSelectInCell(page, cells.nth(2), "公斤");
  await cells.nth(3).locator("input:not([type='hidden'])").first().fill(data.specification);
  await cells.nth(4).locator("input:not([type='hidden'])").first().fill(data.quality);
  await cells.nth(5).locator("input:not([type='hidden'])").first().fill("35");
  await chooseSelectInCell(page, cells.nth(6), "元/公斤");
  await fillDateTime(cells.nth(7).locator(".el-date-editor input").first(), "2026-12-30T15:00:00");
  await cells.nth(8).locator("input:not([type='hidden'])").first().fill(`${prefix}_采购明细`);
};

const baseCardByName = (drawer, name) =>
  drawer.getByText(name, { exact: true }).locator("xpath=ancestor::*[contains(concat(' ', normalize-space(@class), ' '), ' base-detail-card ')][1]");

const supplyRows = baseCard => baseCard.locator(".supply-table .el-table__body-wrapper tbody tr");

const addActiveSupply = async (page, baseCard, index) => {
  await baseCard.getByRole("button", { name: "新增供应" }).click();
  const dialog = page.getByRole("dialog", { name: "新增供应信息" });
  await expect(dialog).toBeVisible();
  await chooseProductByLabel(page, dialog, "品类", data.productName);
  await fillByLabel(dialog, "可供量", `${300 + index * 100}`);
  await formItemByLabel(dialog, "可供量").getByPlaceholder("单位").fill("公斤");
  await fillByLabel(dialog, "预期价格", `${30 + index}`);
  await formItemByLabel(dialog, "预期价格").getByPlaceholder("单位").fill("元/公斤");
  await fillByLabel(dialog, "规格", data.specification);
  await fillByLabel(dialog, "质量要求", data.quality);
  await fillByLabel(dialog, "供货周期", `撮合现货_${suffix}_${index + 1}`);
  await fillByLabel(dialog, "核实日期", "2026-09-05");
  await fillByLabel(dialog, "有效截止", "2026-12-31");
  await fillByLabel(dialog, "备注", `${prefix}_黄芪供应_${index + 1}`);
  await dialog.getByRole("button", { name: "保存" }).click();
  await expectToast(page, "供应信息已保存");
  await waitIdle(page);

  const row = supplyRows(baseCard).filter({ hasText: `撮合现货_${suffix}_${index + 1}` }).first();
  await expect(row).toBeVisible();
  await row.getByRole("button", { name: /^(供应)?操作$/ }).click();
  await page.getByRole("menuitem", { name: "设为有效" }).last().click();
  await expectToast(page, "供应信息状态已更新");
  await waitIdle(page);
  await expect(row).toContainText("有效");
};

test("人工供需撮合机会：真实页面创建、推进和完整回查", async ({ page }) => {
  test.setTimeout(240_000);
  const browserErrors = [];
  page.on("pageerror", error => browserErrors.push(`未处理异常：${error.message}`));
  page.on("console", message => {
    if (message.type() === "error") browserErrors.push(`控制台错误：${message.text()}`);
  });
  page.on("response", response => {
    if (response.url().includes("/api/") && response.status() >= 400) {
      browserErrors.push(`HTTP ${response.status()}：${response.url()}`);
    }
  });

  await login(page);

  await openMenu(page, "厂商管理", /#\/crm\/vendor$/);
  await resetFilters(page);
  await page.getByRole("button", { name: "新增厂商" }).click();
  let dialog = page.getByRole("dialog", { name: "新增厂商" });
  await fillByLabel(dialog, "厂商名称", data.vendorName);
  await chooseSelectByLabel(page, dialog, "优先级", "中");
  await fillByLabel(dialog, "备注", `${prefix}_真实UI验收`);
  await dialog.getByRole("button", { name: "保存" }).click();
  await expect(page.getByText(/新增成功|创建成功/).last()).toBeVisible();
  await waitIdle(page);
  await shot(page, "crm-05-match-opportunity", "01-vendor-created");

  const vendorSearch = page.getByPlaceholder("厂商 / 联系人 / 电话 / 品类");
  await vendorSearch.fill(data.vendorName);
  await page.getByRole("button", { name: "搜索", exact: true }).click();
  await waitIdle(page);
  await clickFirstRowAction(page, "详情");
  const vendorDrawer = detailDrawer(page);
  await expect(vendorDrawer.getByText("厂商详情", { exact: true })).toBeVisible();
  const purchaseSection = vendorDrawer.locator(".detail-card").filter({ hasText: "采购需求" }).first();
  await expect(purchaseSection.getByRole("button", { name: "开始撮合" })).toHaveCount(0);
  await expect(purchaseSection.getByText("撮合进度", { exact: true })).toHaveCount(0);
  await purchaseSection.getByRole("button", { name: "新增采购需求" }).click();
  dialog = page.getByRole("dialog", { name: "新增采购需求" });
  await expect(dialog).toBeVisible();
  await fillByLabel(dialog, "采购需求名称", data.demandName);
  await fillDateTimeByLabel(dialog, "提出日期", "2026-09-05T09:00:00");
  await fillDateTimeByLabel(dialog, "期望到货日期", "2026-12-31T10:00:00");
  await chooseRegionByLabel(page, dialog, "收货地", ["陕西省", "榆林市", "横山区"]);
  await fillByLabel(dialog, "详细地址", `${prefix}_收货仓库`);
  await fillByLabel(dialog, "来源网页", `https://example.test/${suffix}`);
  await fillByLabel(dialog, "总体备注", `${prefix}_采购需求`);
  await fillDemandItem(page, dialog);
  await dialog.getByRole("button", { name: "保存" }).click();
  await expectToast(page, "采购需求已保存");
  await waitIdle(page);

  let demandRow = purchaseSection.locator(".purchase-demand-table .el-table__body-wrapper tbody tr").filter({ hasText: data.demandName }).first();
  await expect(demandRow).toBeVisible();
  await demandRow.getByRole("button", { name: "确认有效" }).click();
  await expectToast(page, "状态已更新");
  await waitIdle(page);
  demandRow = purchaseSection.locator(".purchase-demand-table .el-table__body-wrapper tbody tr").filter({ hasText: data.demandName }).first();
  await expect(demandRow).toContainText("有效");
  await shot(page, "crm-05-match-opportunity", "02-demand-active");

  await openMenu(page, "基地管理", /#\/crm\/herb-base$/);
  await resetFilters(page);
  await page.getByRole("button", { name: "新增药材基地" }).click();
  dialog = page.getByRole("dialog", { name: "新增基地" });
  await fillByLabel(dialog, "基地名称", data.baseName);
  await fillByLabel(dialog, "规模", "200");
  await chooseRegionByLabel(page, dialog, "地区", ["陕西省", "榆林市", "横山区"]);
  await fillByLabel(dialog, "详细地址", `${prefix}_基地地址`);
  await chooseSelectByLabel(page, dialog, "来源", "手工录入");
  await fillByLabel(dialog, "备注", `${prefix}_基地`);
  await dialog.getByRole("button", { name: "创建" }).click();
  await expectToast(page, "创建成功");
  await waitIdle(page);

  const baseSearch = page.getByPlaceholder("基地 / 主体 / 联系人 / 电话");
  await baseSearch.fill(data.baseName);
  await page.getByRole("button", { name: "搜索", exact: true }).click();
  await waitIdle(page);
  await tableBody(page).locator("tbody tr").filter({ hasText: data.baseName }).first().getByRole("button", { name: "详情" }).click();
  const baseDrawer = detailDrawer(page);
  await expect(baseDrawer.getByText("基地主体详情", { exact: true })).toBeVisible();
  await baseDrawer.getByRole("button", { name: "编辑主体" }).click();
  dialog = page.getByRole("dialog", { name: "编辑主体" });
  await fillByLabel(dialog, "主体名称", data.subjectName);
  await fillByLabel(dialog, "主体类型", "合作社");
  await chooseSelectByLabel(page, dialog, "状态", "跟进中");
  await dialog.getByRole("button", { name: "保存" }).click();
  await expectToast(page, "主体已保存");
  await waitIdle(page);
  await expectVisibleText(baseDrawer, data.subjectName);

  const baseCard = baseCardByName(baseDrawer, data.baseName);
  await addActiveSupply(page, baseCard, 0);
  await addActiveSupply(page, baseCard, 1);
  await expect(supplyRows(baseCard).filter({ hasText: `撮合现货_${suffix}_` })).toHaveCount(2);
  await shot(page, "crm-05-match-opportunity", "03-active-supplies");

  await openMenu(page, "采购需求", /#\/crm\/purchase-demand$/);
  await resetFilters(page);
  await expect(page.getByRole("button", { name: "新增采购需求" })).toHaveCount(0);
  const demandSearch = page.getByPlaceholder("编号 / 需求名称 / 厂商");
  await demandSearch.fill(data.demandName);
  await page.getByRole("button", { name: "搜索", exact: true }).click();
  await waitIdle(page);
  demandRow = tableBody(page).locator("tbody tr").filter({ hasText: data.demandName }).first();
  await expect(demandRow.getByRole("button", { name: "编辑" })).toHaveCount(0);
  await demandRow.getByRole("button", { name: "开始撮合" }).click();
  await expect(page).toHaveURL(/#\/crm\/match-opportunity\?demandId=/);
  dialog = page.getByRole("dialog", { name: "选择采购明细" });
  await expect(dialog).toBeVisible();
  const demandItemRow = dialog.locator(".el-table__body-wrapper tbody tr");
  await expect(demandItemRow).toHaveCount(1);
  await demandItemRow.first().click();
  await dialog.getByRole("button", { name: "继续" }).click();
  await expect(page.getByText("候选筛选", { exact: true })).toBeVisible();
  await expectVisibleText(page, data.specification);
  await expect(formItemByLabel(page, "品类").locator(".el-select")).toHaveCSS("width", "240px");

  await fillByLabel(page, "规格", data.specification);
  await fillByLabel(page, "质量", data.quality);
  await page.getByRole("button", { name: "查询", exact: true }).click();
  await waitIdle(page);
  const candidateTable = page.locator(".candidate-selector .el-table__body-wrapper tbody");
  await expect(candidateTable.getByText(data.subjectName, { exact: true })).toHaveCount(2);
  await candidateTable.locator("tr").filter({ hasText: `撮合现货_${suffix}_1` }).locator(".el-checkbox").click();
  await candidateTable.locator("tr").filter({ hasText: `撮合现货_${suffix}_2` }).locator(".el-checkbox").click();
  await chooseSelectByLabel(page, page, "下一步动作", "其他");
  await expect(formItemByLabel(page, "其他待办说明")).toBeVisible();
  await fillByLabel(page, "其他待办说明", `${prefix}_创建待办说明`);
  await chooseSelectByLabel(page, page, "下一步动作", data.nextAction);
  await expect(formItemByLabel(page, "其他待办说明")).toHaveCount(0);
  await fillDateTimeByLabel(page, "计划时间", "2026-12-20T10:30:00");
  await shot(page, "crm-05-match-opportunity", "04-candidates-selected");
  await page.getByRole("button", { name: "创建撮合机会" }).click();
  await expectToast(page, "撮合机会已创建");
  await waitIdle(page);

  const opportunityDrawer = page.locator(".el-drawer").filter({ hasText: "撮合机会详情" }).last();
  await expect(opportunityDrawer.getByText("撮合机会详情", { exact: true })).toBeVisible();
  await expectVisibleText(opportunityDrawer, "推进中");
  await expectVisibleText(opportunityDrawer, data.vendorName);
  await expectVisibleText(opportunityDrawer, data.specification);
  const candidateSection = opportunityDrawer
    .locator("section.detail-section")
    .filter({ hasText: /^候选/ });
  const opportunityCandidateRows = candidateSection.locator(".el-table__body-wrapper tbody tr");
  await expect(opportunityCandidateRows).toHaveCount(2);
  await expect(opportunityCandidateRows.filter({ hasText: "待评估" })).toHaveCount(2);
  await expect(opportunityDrawer.locator(".activity-timeline .el-timeline-item")).toHaveCount(3);
  await expectVisibleText(opportunityDrawer, "创建机会");
  await expectVisibleText(opportunityDrawer, "加入候选");
  await shot(page, "crm-05-match-opportunity", "05-opportunity-created");

  await opportunityCandidateRows.nth(0).getByRole("button", { name: "中选" }).click();
  await expectToast(page, "候选已中选");
  await waitIdle(page);
  await expect(opportunityCandidateRows.nth(0)).toContainText("中选");

  await opportunityCandidateRows.nth(1).getByRole("button", { name: "淘汰" }).click();
  const eliminateDialog = page.locator(".el-message-box");
  await expect(eliminateDialog).toBeVisible();
  await eliminateDialog.locator("input:not([type='hidden'])").first().fill(data.eliminationReason);
  await eliminateDialog.getByRole("button", { name: /^(确定|确认|OK)$/ }).click();
  await expectToast(page, "候选已淘汰");
  await waitIdle(page);
  await expect(opportunityCandidateRows.nth(1)).toContainText("淘汰");
  await expect(opportunityCandidateRows.nth(1)).toContainText(data.eliminationReason);
  await shot(page, "crm-05-match-opportunity", "06-candidates-maintained");

  await opportunityDrawer.getByRole("button", { name: "推进机会" }).click();
  dialog = page.getByRole("dialog", { name: "推进机会" });
  await chooseSelectByLabel(page, dialog, "阶段", "商务沟通");
  await fillByLabel(dialog, "本次结果", "已核实数量、规格、质量和交期，可以进入商务沟通。");
  await chooseSelectByLabel(page, dialog, "下一步动作", "沟通价格");
  await fillDateTimeByLabel(dialog, "计划时间", "2026-12-20T10:30:00");
  await dialog.getByRole("button", { name: "确认推进" }).click();
  await expectToast(page, "撮合机会已推进");
  await waitIdle(page);
  await expectVisibleText(opportunityDrawer, "商务沟通");
  await expectVisibleText(opportunityDrawer, "沟通价格");

  await opportunityDrawer.getByRole("button", { name: "暂停" }).click();
  await expectToast(page, "撮合机会已暂停");
  await waitIdle(page);
  await expectVisibleText(opportunityDrawer, "暂停");
  await opportunityDrawer.getByRole("button", { name: "推进机会" }).click();
  dialog = page.getByRole("dialog", { name: "推进机会" });
  await fillByLabel(dialog, "本次结果", "恢复机会，继续沟通价格和交付条件。");
  await fillDateTimeByLabel(dialog, "计划时间", "2026-12-21T10:30:00");
  await dialog.getByRole("button", { name: "恢复并推进" }).click();
  await expectToast(page, "撮合机会已恢复并推进");
  await waitIdle(page);
  await expectVisibleText(opportunityDrawer, "推进中");

  await opportunityDrawer.getByRole("button", { name: "推进机会" }).click();
  dialog = page.getByRole("dialog", { name: "推进机会" });
  await chooseSelectByLabel(page, dialog, "阶段", "意向确认");
  await fillByLabel(dialog, "本次结果", "商务条件已确认，进入合作意向确认。 ");
  await chooseSelectByLabel(page, dialog, "下一步动作", "确认中选供应");
  await fillDateTimeByLabel(dialog, "计划时间", "2026-12-22T10:30:00");
  await dialog.getByRole("button", { name: "确认推进" }).click();
  await expectToast(page, "撮合机会已推进");
  await waitIdle(page);
  await expectVisibleText(opportunityDrawer, "意向确认");

  await opportunityDrawer.getByRole("button", { name: "关闭", exact: true }).click();
  const closeDialog = page.getByRole("dialog", { name: "关闭撮合机会" });
  await expect(closeDialog).toBeVisible();
  await chooseSelectByLabel(page, closeDialog, "关闭结果", "撮合成功");
  await closeDialog.getByRole("button", { name: "确认关闭" }).click();
  await expectToast(page, "撮合机会已关闭");
  await waitIdle(page);
  await expectVisibleText(opportunityDrawer, "已关闭");
  await opportunityDrawer.getByRole("button", { name: "刷新" }).click();
  await expectToast(page, "详情已刷新");
  await waitIdle(page);
  await expect(opportunityDrawer.locator(".activity-timeline .el-timeline-item")).toHaveCount(10);
  for (const action of ["创建机会", "加入候选", "中选候选", "淘汰候选", "推进", "暂停", "关闭"]) {
    await expectVisibleText(opportunityDrawer, action);
  }
  await shot(page, "crm-05-match-opportunity", "07-closed-and-refreshed");

  await openMenu(page, "采购需求", /#\/crm\/purchase-demand$/);
  await resetFilters(page);
  await demandSearch.fill(data.demandName);
  await page.getByRole("button", { name: "搜索", exact: true }).click();
  await waitIdle(page);
  demandRow = tableBody(page).locator("tbody tr").filter({ hasText: data.demandName }).first();
  await expect(demandRow).toContainText("有效");
  await expect(demandRow).toContainText("已发起 1/1");
  await expect(demandRow).toContainText("已结束");
  await shot(page, "crm-05-match-opportunity", "08-demand-progress-recheck");

  // 缓存的撮合机会页再次接收同一需求时，仍须重新打开采购明细选择弹窗。
  await demandRow.getByRole("button", { name: "开始撮合" }).click();
  await expect(page).toHaveURL(/#\/crm\/match-opportunity\?demandId=/);
  dialog = page.getByRole("dialog", { name: "选择采购明细" });
  await expect(dialog).toBeVisible();
  expect(browserErrors, browserErrors.join("\n")).toEqual([]);
});

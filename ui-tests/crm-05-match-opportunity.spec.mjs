import { expect, test } from "@playwright/test";
import {
  chooseRegionByLabel,
  chooseSelectByLabel,
  clickFirstRowAction,
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
  productName: "黄芪",
  specification: `撮合统货_${suffix}`,
  quality: `撮合无硫_${suffix}`,
  vendorNames: {
    a: `${prefix}_厂商甲`,
    b: `${prefix}_厂商乙`,
    c: `${prefix}_厂商丙`,
  },
  demandNames: {
    a: `${prefix}_黄芪需求甲`,
    b: `${prefix}_黄芪需求乙`,
    c: `${prefix}_黄芪需求丙`,
  },
  subjectName: `${prefix}_基地主体`,
  baseName: `${prefix}_基地`,
  nextAction: "核实数量",
  closeReason: `本轮人工撮合完成归档_${suffix}`,
  // 药厂采购明细数量
  demandQuantities: { a: 300, b: 400, c: 200 },
  // 基地可供量（700 预留给需求取消场景，不被其他机会占用）
  quantities: [300, 400, 500, 600, 700],
  // 第二个基地：三个不同供应品类各 1 条，用于验证列表按基地合并与品类展示
  displayBaseName: `${prefix}_品类基地`,
  displayQuantities: [150, 160, 170],
  displayProducts: ["黄芪", "当归", "党参"],
  supplyCycle: `撮合现货_${suffix}`,
};

const supplyCycleOf = index => `${data.supplyCycle}_${index + 1}`;

// ---------- 基础表单 helper ----------

const chooseSelectInCell = async (page, cell, option) => {
  await cell.locator(".el-select").first().click();
  // 单位存在“公斤 / 元/公斤”这类近似项，必须精确匹配
  await page
    .locator(".el-select-dropdown:visible")
    .last()
    .locator(".el-select-dropdown__item")
    .filter({ hasText: new RegExp(`^\\s*${option.replace("/", "\\/")}\\s*$`) })
    .first()
    .click();
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

// 单位已封装为可搜索下拉（UnitSelect / el-select allow-create）：输入后必须回车确认
const fillUnitByLabel = async (scope, label, unit) => {
  const input = formItemByLabel(scope, label).locator(".el-select input").first();
  await input.fill(unit);
  await input.press("Enter");
};

const chooseProductInCell = async (page, cell, productName) => {
  await cell.locator(".el-select").first().click();
  await cell.locator("input:not([type='hidden'])").first().fill(productName);
  await page.waitForTimeout(800);
  // 品类存在“黄芪 / 蒙古黄芪”近似项，必须精确匹配，否则供需两侧品类不一致
  await page
    .locator(".el-select-dropdown:visible")
    .last()
    .locator(".el-select-dropdown__item")
    .filter({ hasText: new RegExp(`^\\s*${productName}\\s*$`) })
    .first()
    .click();
};

// 远程下拉（需求厂商 / 基地主体）：输入关键词后从当前展开的下拉中精确选择
const chooseRemoteSelectByLabel = async (page, scope, label, keyword, optionText) => {
  const item = formItemByLabel(scope, label);
  await item.locator(".el-select").first().click();
  await item.locator("input:not([type='hidden'])").first().fill(keyword);
  await page.waitForTimeout(900);
  const option = page
    .locator(".el-select-dropdown:visible")
    .last()
    .locator(".el-select-dropdown__item")
    .filter({ hasText: optionText })
    .first();
  await expect(option).toBeVisible();
  await option.click();
  await page.waitForTimeout(300);
};

const demandItemProducts = ["黄芪", "当归", "党参"];

const fillDemandItem = async (page, dialog, quantity) => {
  for (let index = 0; index < demandItemProducts.length; index += 1) {
    if (index > 0) await dialog.getByRole("button", { name: "新增明细" }).click();
    const product = demandItemProducts[index];
    const cells = dialog.locator(".item-table .el-table__body-wrapper tbody tr").nth(index).locator("td");
    await chooseProductInCell(page, cells.nth(0), product);
    await cells.nth(1).locator("input:not([type='hidden'])").first().fill(String(quantity + index * 10));
    await chooseSelectInCell(page, cells.nth(2), "公斤");
    await cells.nth(3).locator("input:not([type='hidden'])").first().fill(data.specification);
    await cells.nth(4).locator("input:not([type='hidden'])").first().fill(data.quality);
    await cells.nth(5).locator("input:not([type='hidden'])").first().fill("35");
    await chooseSelectInCell(page, cells.nth(6), "元/公斤");
    await fillDateTime(cells.nth(7).locator(".el-date-editor input").first(), "2026-12-30T15:00:00");
    await cells.nth(8).locator("input:not([type='hidden'])").first().fill(`${prefix}_采购明细_${product}`);
  }
};

// ---------- 页面级 helper ----------

const openBaseDetail = async page => {
  await openMenu(page, "基地管理", /#\/crm\/herb-base$/);
  await resetFilters(page);
  const baseSearch = page.getByPlaceholder("基地 / 主体 / 联系人 / 电话");
  await baseSearch.fill(data.baseName);
  await page.getByRole("button", { name: "搜索", exact: true }).click();
  await waitIdle(page);
  await tableBody(page).locator("tbody tr").filter({ hasText: data.baseName }).first().getByRole("button", { name: "详情" }).click();
  const drawer = page.locator(".el-drawer").filter({ hasText: "基地主体详情" }).last();
  await expect(drawer.getByText("基地主体详情", { exact: true })).toBeVisible();
  await waitIdle(page);
  return drawer;
};

// 基地详情已改为 el-tabs 按基地分页，供应表格位于激活的 tab-pane 内
const openBaseTab = async (drawer, baseName) => {
  const tab = drawer.locator(".base-detail-tabs .el-tabs__item").filter({ hasText: baseName }).first();
  await tab.scrollIntoViewIfNeeded();
  await tab.click();
  const pane = drawer.locator(".base-detail-tabs .el-tab-pane:visible").first();
  await expect(pane.locator(".supply-table")).toBeVisible();
  return pane;
};

const supplyRowOf = (pane, index) =>
  pane.locator(".supply-table .el-table__body-wrapper tbody tr").filter({ hasText: supplyCycleOf(index) }).first();

const runSupplyAction = async (page, pane, index, label) => {
  const row = supplyRowOf(pane, index);
  await expect(row).toBeVisible();
  await row.getByRole("button", { name: /^(供应)?操作$/ }).click();
  await page.getByRole("menuitem", { name: label }).last().click();
  await waitIdle(page);
};

const addActiveSupply = async (page, pane, index, product = data.productName) => {
  await pane.getByRole("button", { name: "新增供应" }).click();
  const dialog = page.getByRole("dialog", { name: "新增供应信息" });
  await expect(dialog).toBeVisible();
  await chooseSelectByLabel(page, dialog, "品类", product);
  await fillByLabel(dialog, "可供量", `${data.quantities[index]}`);
  await fillUnitByLabel(dialog, "可供量", "公斤");
  await fillByLabel(dialog, "预期价格", `${30 + index}`);
  await fillUnitByLabel(dialog, "预期价格", "元/公斤");
  await fillByLabel(dialog, "规格", data.specification);
  await fillByLabel(dialog, "质量要求", data.quality);
  await fillByLabel(dialog, "产新期", "9月-11月");
  await fillByLabel(dialog, "供货周期", supplyCycleOf(index));
  await fillByLabel(dialog, "核实日期", "2026-09-05");
  await fillByLabel(dialog, "有效截止", "2026-12-31");
  await fillByLabel(dialog, "备注", `${prefix}_供应_${index + 1}`);
  await dialog.getByRole("button", { name: "保存" }).click();
  await expectToast(page, "供应信息已保存");
  await waitIdle(page);
  await runSupplyAction(page, pane, index, "设为有效");
  await expectToast(page, "供应信息状态已更新");
  await waitIdle(page);
  await expect(supplyRowOf(pane, index)).toContainText("有效");
};

const matchDrawer = page => page.locator(".el-drawer").filter({ hasText: "撮合机会详情" }).last();
const candidateCards = drawer => drawer.locator(".detail-columns .detail-column").last().locator(".entity-card");
const initiatorCards = drawer => drawer.locator(".detail-columns .detail-column").first().locator(".entity-card");
const candidateCardByQuantity = (drawer, quantity) => candidateCards(drawer).filter({ hasText: `${quantity}公斤` }).first();

const refreshMatchDrawer = async page => {
  await matchDrawer(page).getByRole("button", { name: "刷新" }).click();
  await expectToast(page, "详情已刷新");
  await waitIdle(page);
};

const closeMatchDrawer = async page => {
  await matchDrawer(page).locator("button[title='关闭详情']").first().click();
  await page.locator(".el-drawer").filter({ hasText: "撮合机会详情" }).first().waitFor({ state: "hidden" });
  await waitIdle(page);
};

const appendCandidates = async (page, { quantities = [], vendorNames = [] }) => {
  await matchDrawer(page).getByRole("button", { name: "追加候选" }).click();
  const dialog = page.getByRole("dialog", { name: "候选筛选" });
  await expect(dialog).toBeVisible();
  await fillByLabel(dialog, "规格", data.specification);
  await dialog.getByRole("button", { name: "查询" }).click();
  await waitIdle(page);
  const rows = dialog.locator(".candidate-selector .el-table__body-wrapper tbody tr");
  for (const vendorName of vendorNames) {
    await rows.filter({ hasText: vendorName }).first().locator(".el-checkbox").click();
  }
  for (const quantity of quantities) {
    await rows.filter({ hasText: data.baseName }).filter({ hasText: `${quantity}公斤` }).first().locator(".el-checkbox").click();
  }
  await dialog.getByRole("button", { name: "确定" }).click();
  await expectToast(page, "候选已追加");
  await waitIdle(page);
  await refreshMatchDrawer(page);
};

// 候选卡片现只保留「中选 / 移除」，移除即删除子表记录、释放明细恢复空闲。
// removeCandidate 按候选卡片上的数量定位，点「移除」并在确认框确定。
const removeCandidate = async (page, quantity) => {
  const card = candidateCardByQuantity(matchDrawer(page), quantity);
  await expect(card).toBeVisible();
  await card.getByRole("button", { name: "移除" }).click();
  const confirm = page.locator(".el-message-box").last();
  await expect(confirm).toBeVisible();
  await confirm.getByRole("button", { name: /^(确定|确认|OK)$/ }).click();
  await expectToast(page, "候选已移除");
  await waitIdle(page);
};

const openAdvanceDialog = async page => {
  await matchDrawer(page).getByRole("button", { name: "推进机会" }).click();
  const dialog = page.getByRole("dialog", { name: "推进机会" });
  await expect(dialog).toBeVisible();
  await waitIdle(page);
  return dialog;
};

const advanceOpportunity = async (page, { stage, result, nextAction, remark, planAt, confirmText = "确认推进" }) => {
  const dialog = await openAdvanceDialog(page);
  if (stage) await chooseSelectByLabel(page, dialog, "阶段", stage);
  await fillByLabel(dialog, "本次结果", result);
  await chooseSelectByLabel(page, dialog, "下一步动作", nextAction);
  if (remark) await fillByLabel(dialog, "其他待办说明", remark);
  await fillDateTimeByLabel(dialog, "计划时间", planAt);
  await dialog.getByRole("button", { name: confirmText }).click();
};

const closeOpportunity = async (page, { result, reason }) => {
  await matchDrawer(page).getByRole("button", { name: "关闭", exact: true }).click();
  const dialog = page.getByRole("dialog", { name: "关闭撮合机会" });
  await expect(dialog).toBeVisible();
  await chooseSelectByLabel(page, dialog, "关闭结果", result);
  if (reason) await fillByLabel(dialog, "关闭原因", reason);
  await dialog.getByRole("button", { name: "确认关闭" }).click();
  return dialog;
};

const filterMatchList = async (page, { status, source, productName, vendorName, subjectName } = {}) => {
  await openMenu(page, "撮合机会", /#\/crm\/match-opportunity$/);
  await resetFilters(page);
  if (status) await chooseSelectByLabel(page, page, "状态", status);
  if (source) await chooseSelectByLabel(page, page, "发起方向", source);
  if (productName) await fillByLabel(page, "品类", productName);
  if (vendorName) await chooseRemoteSelectByLabel(page, page, "需求厂商", vendorName, vendorName);
  if (subjectName) await chooseRemoteSelectByLabel(page, page, "基地主体", subjectName, subjectName);
  await page.getByRole("button", { name: "搜索", exact: true }).click();
  await waitIdle(page);
};

const openOpportunityDetail = async (page, rowText) => {
  const row = tableBody(page).locator("tbody tr").filter({ hasText: rowText }).first();
  await expect(row).toBeVisible();
  await row.getByRole("button", { name: "详情" }).click();
  await expect(matchDrawer(page).getByText("撮合机会详情", { exact: true })).toBeVisible();
  await waitIdle(page);
};

const searchPurchase = async (page, keyword) => {
  await openMenu(page, "采购列表", /#\/crm\/purchase/);
  await resetFilters(page);
  await page.getByPlaceholder("编号 / 需求名称 / 厂商").fill(keyword);
  await page.getByRole("button", { name: "搜索", exact: true }).click();
  await waitIdle(page);
  return tableBody(page).locator("tbody tr").filter({ hasText: keyword }).first();
};

// 采购列表发起撮合与供应列表一致：不跳转，先创建只含发起方（当前明细）的机会并弹详情，候选在详情内追加
const openDemandCandidateSelector = async (page, keyword) => {
  await openMenu(page, "采购列表", /#\/crm\/purchase/);
  await resetFilters(page);
  await page.getByPlaceholder("编号 / 需求名称 / 厂商").fill(keyword);
  await page.getByRole("button", { name: "搜索", exact: true }).click();
  await waitIdle(page);
  const itemRow = tableBody(page).locator("tbody tr").filter({ hasText: keyword }).first();
  await itemRow.getByRole("button", { name: "发起撮合" }).click();
  await expect(page).toHaveURL(/#\/crm\/purchase/);
  await expectToast(page, "撮合机会已创建");
  await waitIdle(page);
  await expect(matchDrawer(page).getByText("撮合机会详情", { exact: true })).toBeVisible();

  await matchDrawer(page).getByRole("button", { name: "追加候选" }).click();
  const selector = page.getByRole("dialog", { name: "候选筛选" });
  await expect(selector).toBeVisible();
  await fillByLabel(selector, "规格", data.specification);
  await selector.getByRole("button", { name: "查询" }).click();
  await waitIdle(page);
  return selector;
};

const createOpportunityFromSelector = async (page, selector, quantities) => {
  const rows = selector.locator(".candidate-selector .el-table__body-wrapper tbody tr");
  for (const quantity of quantities) {
    await rows.filter({ hasText: data.baseName }).filter({ hasText: `${quantity}公斤` }).first().locator(".el-checkbox").click();
  }
  await selector.getByRole("button", { name: "确定" }).click();
  await expectToast(page, "候选已追加");
  await waitIdle(page);
  await refreshMatchDrawer(page);
};

const searchFollowTask = async (page, keyword) => {
  await openMenu(page, "我的待办", /#\/crm\/follow-tasks$/);
  await resetFilters(page);
  await chooseSelectByLabel(page, page, "对象类型", "撮合机会");
  await page.getByPlaceholder("名称、联系人或电话").fill(keyword);
  await page.getByRole("button", { name: "搜索", exact: true }).click();
  await waitIdle(page);
  return tableBody(page).locator("tbody tr").filter({ hasText: keyword });
};

const createVendor = async (page, vendorName) => {
  await page.getByRole("button", { name: "新增厂商" }).click();
  const dialog = page.getByRole("dialog", { name: "新增厂商" });
  await fillByLabel(dialog, "厂商名称", vendorName);
  await chooseSelectByLabel(page, dialog, "优先级", "中");
  await fillByLabel(dialog, "备注", `${prefix}_真实UI验收`);
  await dialog.getByRole("button", { name: "保存" }).click();
  await expect(page.getByText(/新增成功|创建成功/).last()).toBeVisible();
  await waitIdle(page);
};

const addPurchaseDemand = async (page, vendorName, demandName, quantity) => {
  const vendorSearch = page.getByPlaceholder("厂商 / 联系人 / 电话 / 品类");
  await vendorSearch.fill(vendorName);
  await page.getByRole("button", { name: "搜索", exact: true }).click();
  await waitIdle(page);
  await clickFirstRowAction(page, "详情");
  const vendorDrawer = page.locator(".el-drawer").filter({ hasText: "厂商详情" }).last();
  await expect(vendorDrawer.getByText("厂商详情", { exact: true })).toBeVisible();
  const purchaseSection = vendorDrawer.locator(".purchase-demand-card");
  const addDemandBtn = purchaseSection.getByRole("button", { name: "新增采购列表" });
  await addDemandBtn.scrollIntoViewIfNeeded();
  await addDemandBtn.click();
  const dialog = page.getByRole("dialog", { name: "新增采购列表" });
  await expect(dialog).toBeVisible();
  await fillByLabel(dialog, "采购列表名称", demandName);
  await fillDateTimeByLabel(dialog, "提出日期", "2026-09-05T09:00:00");
  await fillDateTimeByLabel(dialog, "期望到货日期", "2026-12-31T10:00:00");
  await chooseRegionByLabel(page, dialog, "收货地", ["陕西省", "西安市", "新城区"]);
  await fillByLabel(dialog, "详细地址", `${prefix}_收货仓库`);
  await fillByLabel(dialog, "来源网页", `https://example.test/${suffix}`);
  await fillByLabel(dialog, "总体备注", `${prefix}_采购需求`);
  await fillDemandItem(page, dialog, quantity);
  await dialog.getByRole("button", { name: "保存" }).click();
  await expectToast(page, "采购列表已保存");
  await waitIdle(page);

  let demandRow = purchaseSection.locator(".purchase-demand-table .el-table__body-wrapper tbody tr").filter({ hasText: demandName }).first();
  await expect(demandRow).toBeVisible();
  await demandRow.getByRole("button", { name: "确认有效" }).click();
  await expectToast(page, "状态已更新");
  await waitIdle(page);
  demandRow = purchaseSection.locator(".purchase-demand-table .el-table__body-wrapper tbody tr").filter({ hasText: demandName }).first();
  await expect(demandRow).toContainText("有效");
  await vendorDrawer.locator("button[title='关闭详情']").click();
  await vendorDrawer.waitFor({ state: "hidden" });
  await waitIdle(page);
};

test("人工供需撮合机会：双侧多对多候选、全部状态流转、三种关闭结果与完整回查", async ({ page }) => {
  test.setTimeout(900_000);
  const browserErrors = [];
  page.on("pageerror", error => browserErrors.push(`未处理异常：${error.message}`));
  page.on("console", message => {
    if (message.type() === "error") browserErrors.push(`控制台错误：${message.text()} @ ${message.location()?.url || ""}`);
  });
  page.on("response", response => {
    if (response.url().includes("/api/") && response.status() >= 400) {
      browserErrors.push(`HTTP ${response.status()}：${response.url()}`);
    }
  });

  await login(page);

  // ================= 数据准备：3 家厂商 + 3 条有效采购列表 =================
  await openMenu(page, "厂商管理", /#\/crm\/vendor$/);
  await resetFilters(page);
  await createVendor(page, data.vendorNames.a);
  await createVendor(page, data.vendorNames.b);
  await createVendor(page, data.vendorNames.c);
  await addPurchaseDemand(page, data.vendorNames.a, data.demandNames.a, data.demandQuantities.a);
  await addPurchaseDemand(page, data.vendorNames.b, data.demandNames.b, data.demandQuantities.b);
  await addPurchaseDemand(page, data.vendorNames.c, data.demandNames.c, data.demandQuantities.c);
  await shot(page, "crm-05-match-opportunity", "01-vendors-and-demands");

  // ================= 数据准备：基地主体 + 4 条有效供应 =================
  await openMenu(page, "基地管理", /#\/crm\/herb-base$/);
  await resetFilters(page);
  await page.getByRole("button", { name: "新增药材基地" }).click();
  let dialog = page.getByRole("dialog", { name: "新增基地" });
  await fillByLabel(dialog, "基地名称", data.baseName);
  await fillByLabel(dialog, "规模", "200");
  await chooseRegionByLabel(page, dialog, "地区", ["陕西省", "西安市", "新城区"]);
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
  const baseDrawer = page.locator(".el-drawer").filter({ hasText: "基地主体详情" }).last();
  await expect(baseDrawer.getByText("基地主体详情", { exact: true })).toBeVisible();
  await baseDrawer.getByRole("button", { name: "编辑主体" }).click();
  dialog = page.getByRole("dialog", { name: "编辑主体" });
  await fillByLabel(dialog, "主体名称", data.subjectName);
  await fillByLabel(dialog, "主体类型", "合作社");
  await chooseSelectByLabel(page, dialog, "状态", "跟进中");
  await fillByLabel(dialog, "备注", `${prefix}_主体备注`);
  await dialog.getByRole("button", { name: "保存" }).click();
  await expectToast(page, "主体已保存");
  await waitIdle(page);
  await expectVisibleText(baseDrawer, data.subjectName);

  const basePane = await openBaseTab(baseDrawer, data.baseName);
  for (let index = 0; index < data.quantities.length; index += 1) {
    await addActiveSupply(page, basePane, index, data.productName);
  }
  await expect(basePane.locator(".supply-table .el-table__body-wrapper tbody tr").filter({ hasText: data.supplyCycle })).toHaveCount(data.quantities.length);
  await shot(page, "crm-05-match-opportunity", "02-base-supplies");

  // ---------- 第二个基地：三品类各 1 条供应 ----------
  await baseDrawer.locator("button[title='关闭详情']").click();
  await baseDrawer.waitFor({ state: "hidden" });
  await waitIdle(page);
  await page.getByRole("button", { name: "新增药材基地" }).click();
  dialog = page.getByRole("dialog", { name: "新增基地" });
  await fillByLabel(dialog, "基地名称", data.displayBaseName);
  await fillByLabel(dialog, "规模", "120");
  await chooseRegionByLabel(page, dialog, "地区", ["陕西省", "西安市", "新城区"]);
  await fillByLabel(dialog, "详细地址", `${prefix}_品类基地地址`);
  await chooseSelectByLabel(page, dialog, "来源", "手工录入");
  await fillByLabel(dialog, "备注", `${prefix}_品类基地`);
  await dialog.getByRole("button", { name: "创建" }).click();
  await expectToast(page, "创建成功");
  await waitIdle(page);
  await baseSearch.fill(data.displayBaseName);
  await page.getByRole("button", { name: "搜索", exact: true }).click();
  await waitIdle(page);
  await tableBody(page).locator("tbody tr").filter({ hasText: data.displayBaseName }).first().getByRole("button", { name: "详情" }).click();
  const displayBaseDrawer = page.locator(".el-drawer").filter({ hasText: "基地主体详情" }).last();
  await expect(displayBaseDrawer.getByText("基地主体详情", { exact: true })).toBeVisible();
  await waitIdle(page);
  const displayPane = await openBaseTab(displayBaseDrawer, data.displayBaseName);
  for (let index = 0; index < data.displayQuantities.length; index += 1) {
    await addActiveSupply(page, displayPane, index, data.displayProducts[index]);
  }
  await expect(displayPane.locator(".supply-table .el-table__body-wrapper tbody tr").filter({ hasText: data.supplyCycle })).toHaveCount(data.displayQuantities.length);
  await expectVisibleText(displayPane.locator(".supply-table"), "黄芪");
  await expectVisibleText(displayPane.locator(".supply-table"), "当归");
  await expectVisibleText(displayPane.locator(".supply-table"), "党参");
  await displayBaseDrawer.locator("button[title='关闭详情']").click();
  await displayBaseDrawer.waitFor({ state: "hidden" });
  await waitIdle(page);

  // 供应列表：三品类各 1 条，且同基地单元格合并
  await openMenu(page, "供应列表", /#\/crm\/herb-base-supply$/);
  await resetFilters(page);
  await fillByLabel(page, "基地", data.displayBaseName);
  await page.getByRole("button", { name: "搜索", exact: true }).click();
  await waitIdle(page);
  const displayRows = tableBody(page).locator("tbody tr");
  await expect(displayRows.filter({ hasText: "黄芪" })).toHaveCount(1);
  await expect(displayRows.filter({ hasText: "当归" })).toHaveCount(1);
  await expect(displayRows.filter({ hasText: "党参" })).toHaveCount(1);
  await expect(displayRows.first()).toContainText(data.displayBaseName);
  await expect(displayRows.nth(1)).not.toContainText(data.displayBaseName);
  await shot(page, "crm-05-match-opportunity", "02b-supply-categories");

  // ================= O1 供应侧：300 发起，候选 3 家药厂需求 =================
  await openMenu(page, "供应列表", /#\/crm\/herb-base-supply$/);
  await resetFilters(page);
  await fillByLabel(page, "基地", data.baseName);
  await page.getByRole("button", { name: "搜索", exact: true }).click();
  await waitIdle(page);
  const baseSupplyRows = tableBody(page).locator("tbody tr");
  await expect(baseSupplyRows.filter({ hasText: "黄芪" })).toHaveCount(5);
  // 同基地单元格合并后，基地文案只在首行出现
  await expect(baseSupplyRows.first()).toContainText(data.baseName);
  await expect(baseSupplyRows.nth(1)).not.toContainText(data.baseName);
  const supplyRow = tableBody(page).locator("tbody tr").filter({ hasText: `${data.quantities[0]}公斤` }).first();
  await expect(supplyRow).toBeVisible();
  await supplyRow.getByRole("button", { name: "发起撮合" }).click();
  // 发起撮合不跳转：立即创建机会并弹出撮合详情
  await expect(page).toHaveURL(/#\/crm\/herb-base-supply$/);
  await expectToast(page, "撮合机会已创建");
  await waitIdle(page);
  await expect(matchDrawer(page).getByText("撮合机会详情", { exact: true })).toBeVisible();
  await expectVisibleText(matchDrawer(page), "供给信息");
  await expect(initiatorCards(matchDrawer(page))).toHaveCount(1);
  await expectVisibleText(initiatorCards(matchDrawer(page)).first(), data.baseName);
  await expect(candidateCards(matchDrawer(page))).toHaveCount(0);
  // 候选在详情内追加：3 家药厂采购需求
  await matchDrawer(page).getByRole("button", { name: "追加候选" }).click();
  const appendDialog = page.getByRole("dialog", { name: "候选筛选" });
  await expect(appendDialog).toBeVisible();
  const appendRows = appendDialog.locator(".candidate-selector .el-table__body-wrapper tbody tr");
  for (const demandName of [data.demandNames.a, data.demandNames.b, data.demandNames.c]) {
    await appendRows.filter({ hasText: demandName }).first().locator(".el-checkbox").click();
  }
  await appendDialog.getByRole("button", { name: "确定" }).click();
  await expectToast(page, "候选已追加");
  await waitIdle(page);
  await refreshMatchDrawer(page);
  await expect(candidateCards(matchDrawer(page))).toHaveCount(3);
  await expect(candidateCards(matchDrawer(page)).filter({ hasText: "待评估" })).toHaveCount(3);
  await expectVisibleText(candidateCards(matchDrawer(page)).filter({ hasText: data.vendorNames.a }).first(), data.vendorNames.a);
  await shot(page, "crm-05-match-opportunity", "03-supply-side-candidates");

  // 候选卡片现只保留「中选 / 移除」：覆盖中选与移除，移除后明细释放恢复空闲可重新追加
  await removeCandidate(page, data.demandQuantities.c);
  await expect(candidateCards(matchDrawer(page))).toHaveCount(2);
  await appendCandidates(page, { vendorNames: [data.demandNames.c] });
  await expect(candidateCards(matchDrawer(page))).toHaveCount(3);

  await candidateCardByQuantity(matchDrawer(page), data.demandQuantities.a).getByRole("button", { name: "中选" }).click();
  await expectToast(page, "候选已中选");
  await waitIdle(page);
  await expectVisibleText(candidateCardByQuantity(matchDrawer(page), data.demandQuantities.a), "中选");

  // 阶段限制：非意向确认阶段不能撮合成功
  dialog = await closeOpportunity(page, { result: "撮合成功" });
  await expectToast(page, "仅意向确认阶段可以撮合成功关闭");
  await dialog.getByRole("button", { name: "取消", exact: true }).click();
  await waitIdle(page);

  // 未成交前必须先释放中选
  dialog = await closeOpportunity(page, { result: "未成交", reason: `${prefix}_供应侧未成交` });
  await expectToast(page, "未成交或需求取消前请先将中选候选改为保留");
  await dialog.getByRole("button", { name: "取消", exact: true }).click();
  await waitIdle(page);

  // 推进：验证「其他」待办说明显隐
  dialog = await openAdvanceDialog(page);
  await chooseSelectByLabel(page, dialog, "阶段", "商务沟通");
  await fillByLabel(dialog, "本次结果", "已核实数量、规格、质量和交期，可以进入商务沟通。");
  await chooseSelectByLabel(page, dialog, "下一步动作", "其他");
  await expect(formItemByLabel(dialog, "其他待办说明")).toBeVisible();
  await fillByLabel(dialog, "其他待办说明", `${prefix}_创建待办说明`);
  await chooseSelectByLabel(page, dialog, "下一步动作", "等待对方反馈");
  await expect(formItemByLabel(dialog, "其他待办说明")).toHaveCount(0);
  await fillDateTimeByLabel(dialog, "计划时间", "2026-12-20T10:30:00");
  await dialog.getByRole("button", { name: "确认推进" }).click();
  await expectToast(page, "撮合机会已推进");
  await waitIdle(page);
  await expectVisibleText(matchDrawer(page), "商务沟通");
  await expectVisibleText(matchDrawer(page), "等待对方反馈");

  // 暂停：候选维护入口全部隐藏
  await matchDrawer(page).getByRole("button", { name: "暂停" }).click();
  await expectToast(page, "撮合机会已暂停");
  await waitIdle(page);
  await expect(matchDrawer(page).getByRole("button", { name: "追加候选" })).toHaveCount(0);
  await expect(matchDrawer(page).getByRole("button", { name: "移除" })).toHaveCount(0);
  await expect(matchDrawer(page).getByRole("button", { name: "中选" })).toHaveCount(0);

  // 恢复并推进到意向确认
  await advanceOpportunity(page, {
    stage: "意向确认",
    result: "恢复机会，商务条件已确认，进入合作意向确认。",
    nextAction: "确认中选供应",
    planAt: "2026-12-21T10:30:00",
    confirmText: "恢复并推进",
  });
  await expectToast(page, "撮合机会已恢复并推进");
  await waitIdle(page);
  await expectVisibleText(matchDrawer(page), "推进中");
  await expectVisibleText(matchDrawer(page), "意向确认");

  // 逐条移除候选：仅保留已中选的药厂甲
  await removeCandidate(page, data.demandQuantities.b);
  await removeCandidate(page, data.demandQuantities.c);
  await expect(candidateCards(matchDrawer(page))).toHaveCount(1);
  await expectVisibleText(candidateCards(matchDrawer(page)).first(), data.vendorNames.a);
  await expectVisibleText(candidateCards(matchDrawer(page)).first(), "中选");

  // 撮合成功关闭
  await closeOpportunity(page, { result: "撮合成功" });
  await expectToast(page, "撮合机会已关闭");
  await waitIdle(page);
  await expectVisibleText(matchDrawer(page), "已成交");
  await expect(matchDrawer(page).getByRole("button", { name: "追加候选" })).toHaveCount(0);
  await expect(matchDrawer(page).getByRole("button", { name: "推进机会" })).toHaveCount(0);
  await refreshMatchDrawer(page);
  for (const action of ["创建机会", "加入候选", "移除候选", "中选候选", "推进", "暂停", "关闭"]) {
    await expectVisibleText(matchDrawer(page), action);
  }
  await shot(page, "crm-05-match-opportunity", "04-supply-side-closed");
  await closeMatchDrawer(page);

  // ================= O2 需求侧：厂商乙 400 发起，候选 3 条基地供应 =================
  let selector = await openDemandCandidateSelector(page, data.demandNames.b);
  await createOpportunityFromSelector(page, selector, [data.quantities[1], data.quantities[2], data.quantities[3]]);
  await expectVisibleText(matchDrawer(page), "需求信息");
  await expectVisibleText(initiatorCards(matchDrawer(page)).first(), data.vendorNames.b);
  await expect(candidateCards(matchDrawer(page))).toHaveCount(3);

  // 候选基地卡片现只保留「中选 / 移除」：中选 400，移除 500 后再追加回来验证释放可重新参与
  await candidateCardByQuantity(matchDrawer(page), data.quantities[1]).getByRole("button", { name: "中选" }).click();
  await expectToast(page, "候选已中选");
  await waitIdle(page);
  await expectVisibleText(candidateCardByQuantity(matchDrawer(page), data.quantities[1]), "中选");

  await removeCandidate(page, data.quantities[2]);
  await expect(candidateCards(matchDrawer(page))).toHaveCount(2);
  // 移除后该供应恢复空闲，候选筛选可再次选到
  await appendCandidates(page, { quantities: [data.quantities[2]] });
  await expect(candidateCards(matchDrawer(page))).toHaveCount(3);
  await shot(page, "crm-05-match-opportunity", "05-demand-side-candidates");

  // 供应列表撮合状态回查
  await openMenu(page, "供应列表", /#\/crm\/herb-base-supply$/);
  await resetFilters(page);
  await chooseSelectByLabel(page, page, "品类", data.productName);
  await chooseSelectByLabel(page, page, "撮合状态", "撮合中");
  await fillByLabel(page, "基地", data.baseName);
  await page.getByRole("button", { name: "搜索", exact: true }).click();
  await waitIdle(page);
  const occupiedRow = tableBody(page).locator("tbody tr").filter({ hasText: `${data.quantities[1]}公斤` }).first();
  await expect(occupiedRow).toContainText("撮合中");

  // 推进到商务沟通，验证我的待办联动
  await filterMatchList(page, { source: "药厂", productName: data.productName, vendorName: data.vendorNames.b });
  await openOpportunityDetail(page, data.vendorNames.b);
  await advanceOpportunity(page, {
    stage: "商务沟通",
    result: "商务条件初步沟通完成，等待对方反馈。",
    nextAction: "沟通价格",
    planAt: "2026-12-20T10:30:00",
  });
  await expectToast(page, "撮合机会已推进");
  await waitIdle(page);

  let todoRows = await searchFollowTask(page, data.vendorNames.b);
  let todoRow = todoRows.first();
  await expect(todoRow).toContainText("撮合机会");
  await expect(todoRow).toContainText("沟通价格");
  await todoRow.getByRole("button", { name: "推进" }).click();
  dialog = page.getByRole("dialog", { name: "推进机会" });
  await expect(dialog).toBeVisible();
  await waitIdle(page);
  await fillByLabel(dialog, "本次结果", "已完成价格沟通，等待对方书面反馈。");
  await chooseSelectByLabel(page, dialog, "下一步动作", "等待对方反馈");
  await fillDateTimeByLabel(dialog, "计划时间", "2026-12-22T10:30:00");
  await dialog.getByRole("button", { name: "确认推进" }).click();
  await expectToast(page, "撮合机会已推进");
  await waitIdle(page);
  todoRows = await searchFollowTask(page, data.vendorNames.b);
  await expect(todoRows.first()).toContainText("等待对方反馈");
  await shot(page, "crm-05-match-opportunity", "07-todo-advanced");

  // 从待办进入详情：暂停后待办消失
  await todoRows.first().getByRole("button", { name: "详情" }).click();
  await expect(matchDrawer(page).getByText("撮合机会详情", { exact: true })).toBeVisible();
  await waitIdle(page);
  await matchDrawer(page).getByRole("button", { name: "暂停" }).click();
  await expectToast(page, "撮合机会已暂停");
  await waitIdle(page);
  await expectVisibleText(matchDrawer(page), "暂停");
  await expect(matchDrawer(page).getByRole("button", { name: "追加候选" })).toHaveCount(0);

  todoRows = await searchFollowTask(page, data.vendorNames.b);
  await expect(todoRows).toHaveCount(0);

  // 恢复并推进到意向确认
  await filterMatchList(page, { source: "药厂", productName: data.productName, vendorName: data.vendorNames.b });
  await openOpportunityDetail(page, data.vendorNames.b);
  await advanceOpportunity(page, {
    stage: "意向确认",
    result: "恢复机会，进入合作意向确认。",
    nextAction: "确认中选供应",
    planAt: "2026-12-23T10:30:00",
    confirmText: "恢复并推进",
  });
  await expectToast(page, "撮合机会已恢复并推进");
  await waitIdle(page);
  await expectVisibleText(matchDrawer(page), "意向确认");

  // 撮合成功前未成交拦截（保持中选，保持机会未关闭）
  dialog = await closeOpportunity(page, { result: "未成交", reason: data.closeReason });
  await expectToast(page, "未成交或需求取消前请先将中选候选改为保留");
  await dialog.getByRole("button", { name: "取消", exact: true }).click();
  await waitIdle(page);

  // ================= O3 需求侧：厂商丙 200 发起，验证进候选即撮合中与需求取消 =================
  selector = await openDemandCandidateSelector(page, data.demandNames.c);
  const occupiedCandidateRows = selector.locator(".candidate-selector .el-table__body-wrapper tbody tr");
  // 进候选即撮合中：已被其他未关闭机会带入的供应不再出现在候选列表
  await expect(occupiedCandidateRows.filter({ hasText: `${data.quantities[1]}公斤` })).toHaveCount(0);
  await expect(occupiedCandidateRows.filter({ hasText: `${data.quantities[2]}公斤` })).toHaveCount(0);
  await expect(occupiedCandidateRows.filter({ hasText: `${data.quantities[3]}公斤` })).toHaveCount(0);
  await createOpportunityFromSelector(page, selector, [data.quantities[4]]);
  await expect(candidateCards(matchDrawer(page))).toHaveCount(1);

  await closeOpportunity(page, { result: "需求取消", reason: `${prefix}_药厂需求取消` });
  await expectToast(page, "撮合机会已关闭");
  await waitIdle(page);
  await expectVisibleText(matchDrawer(page), "已关闭");
  await expectVisibleText(matchDrawer(page), "需求取消");
  await expect(matchDrawer(page).getByRole("button", { name: "关闭", exact: true })).toHaveCount(0);
  await shot(page, "crm-05-match-opportunity", "08-demand-cancelled");
  await closeMatchDrawer(page);

  // ================= O2 收尾：撮合成功关闭 =================
  await filterMatchList(page, { source: "药厂", productName: data.productName, vendorName: data.vendorNames.b });
  await openOpportunityDetail(page, data.vendorNames.b);
  await closeOpportunity(page, { result: "撮合成功" });
  await expectToast(page, "撮合机会已关闭");
  await waitIdle(page);
  await expectVisibleText(matchDrawer(page), "已成交");
  await expect(matchDrawer(page).getByRole("button", { name: "追加候选" })).toHaveCount(0);
  await refreshMatchDrawer(page);
  await shot(page, "crm-05-match-opportunity", "09-demand-side-closed");
  await closeMatchDrawer(page);

  // ================= O4 供应侧：新增需求后选候选创建，直接未成交关闭 =================
  const unmatchedDemandName = `${prefix}_黄芪需求丁`;
  await openMenu(page, "厂商管理", /#\/crm\/vendor$/);
  await resetFilters(page);
  await addPurchaseDemand(page, data.vendorNames.a, unmatchedDemandName, 150);
  await openMenu(page, "供应列表", /#\/crm\/herb-base-supply$/);
  await resetFilters(page);
  await chooseSelectByLabel(page, page, "品类", data.productName);
  await fillByLabel(page, "基地", data.baseName);
  await page.getByRole("button", { name: "搜索", exact: true }).click();
  await waitIdle(page);
  const unmatchedSupplyRow = tableBody(page)
    .locator("tbody tr")
    .filter({ hasText: `${data.quantities[2]}公斤` })
    .first();
  await expect(unmatchedSupplyRow).toBeVisible();
  await unmatchedSupplyRow.getByRole("button", { name: "发起撮合" }).click();
  // 发起撮合不跳转：立即创建机会并弹出撮合详情
  await expect(page).toHaveURL(/#\/crm\/herb-base-supply$/);
  await expectToast(page, "撮合机会已创建");
  await waitIdle(page);
  await expect(matchDrawer(page).getByText("撮合机会详情", { exact: true })).toBeVisible();
  await expectVisibleText(matchDrawer(page), "供给信息");
  // 候选在详情内追加：新增的 150 公斤采购需求
  await matchDrawer(page).getByRole("button", { name: "追加候选" }).click();
  const unmatchedAppendDialog = page.getByRole("dialog", { name: "候选筛选" });
  await expect(unmatchedAppendDialog).toBeVisible();
  await unmatchedAppendDialog.locator(".candidate-selector .el-table__body-wrapper tbody tr").filter({ hasText: unmatchedDemandName }).first().locator(".el-checkbox").click();
  await unmatchedAppendDialog.getByRole("button", { name: "确定" }).click();
  await expectToast(page, "候选已追加");
  await waitIdle(page);
  await refreshMatchDrawer(page);
  await expect(candidateCards(matchDrawer(page))).toHaveCount(1);
  await expectVisibleText(candidateCards(matchDrawer(page)).first(), data.vendorNames.a);
  await expectVisibleText(candidateCards(matchDrawer(page)).first(), "150公斤");
  await closeOpportunity(page, { result: "未成交", reason: `${prefix}_供应侧未成交归档` });
  await expectToast(page, "撮合机会已关闭");
  await waitIdle(page);
  await expectVisibleText(matchDrawer(page), "已关闭");
  await expectVisibleText(matchDrawer(page), "未成交");
  await closeMatchDrawer(page);

  // ================= 列表回查：撮合机会多条件筛选 =================
  await filterMatchList(page, { status: "已关闭", source: "药厂", vendorName: data.vendorNames.b });
  const closedDemandRow = tableBody(page).locator("tbody tr").filter({ hasText: data.vendorNames.b }).first();
  await expect(closedDemandRow).toBeVisible();
  await expect(closedDemandRow).toContainText("已成交");
  await expect(closedDemandRow).toContainText(data.baseName);
  await expect(closedDemandRow.getByRole("button", { name: "推进" })).toHaveCount(0);

  await filterMatchList(page, { status: "已关闭", source: "基地", subjectName: data.subjectName });
  const closedSupplyRows = tableBody(page).locator("tbody tr").filter({ hasText: data.subjectName });
  const closedSupplyMatched = closedSupplyRows
    .filter({ hasText: data.vendorNames.a })
    .filter({ hasText: `${data.quantities[0]}公斤` })
    .first();
  await expect(closedSupplyMatched).toBeVisible();
  await expect(closedSupplyMatched).toContainText("已成交");
  const closedSupplyUnmatched = closedSupplyRows
    .filter({ hasText: data.vendorNames.a })
    .filter({ hasText: `${data.quantities[2]}公斤` })
    .first();
  await expect(closedSupplyUnmatched).toBeVisible();
  await expect(closedSupplyUnmatched).toContainText("已关闭");
  await shot(page, "crm-05-match-opportunity", "10-opportunity-list");

  await filterMatchList(page, { status: "推进中" });
  await expect(tableBody(page).locator("tbody tr").filter({ hasText: prefix })).toHaveCount(0);

  // ================= 采购列表回查：完成 / 取消后的撮合状态 =================
  let demandRow = await searchPurchase(page, data.demandNames.b);
  await expect(demandRow).toContainText("已采购完成");
  await expect(demandRow.getByRole("button", { name: /开始撮合|进入撮合/ })).toHaveCount(0);
  await shot(page, "crm-05-match-opportunity", "11-purchase-completed");

  demandRow = await searchPurchase(page, data.demandNames.c);
  await expect(demandRow).toContainText("已结束");
  await expect(demandRow.getByRole("button", { name: /开始撮合|进入撮合/ })).toHaveCount(0);

  // ================= 供应列表回查：成交锁定与未中选空闲 =================
  await openMenu(page, "供应列表", /#\/crm\/herb-base-supply$/);
  await resetFilters(page);
  await chooseSelectByLabel(page, page, "品类", data.productName);
  await fillByLabel(page, "基地", data.baseName);
  await page.getByRole("button", { name: "搜索", exact: true }).click();
  await waitIdle(page);
  // O2 中选的 400 已成交，供应明细冻结；500/600 未中选，机会关闭后恢复空闲
  const soldSupplyRow = tableBody(page).locator("tbody tr").filter({ hasText: `${data.quantities[1]}公斤` }).first();
  await expect(soldSupplyRow).toContainText("成交");
  const releasedSupplyRow = tableBody(page).locator("tbody tr").filter({ hasText: `${data.quantities[2]}公斤` }).first();
  await expect(releasedSupplyRow).toContainText("空闲");
  await shot(page, "crm-05-match-opportunity", "12-supply-recheck");

  // ================= O5 差额自动生成：撮合成功时按双方较小数量成交，余量自动生成明细 =================
  // 采购 250 vs 供应 700：供应侧差额 450 自动生成同基地的有效供应信息
  const surplusDemandName = `${prefix}_黄芪需求超额供应`;
  await openMenu(page, "厂商管理", /#\/crm\/vendor$/);
  await resetFilters(page);
  await addPurchaseDemand(page, data.vendorNames.a, surplusDemandName, 250);
  selector = await openDemandCandidateSelector(page, surplusDemandName);
  await createOpportunityFromSelector(page, selector, [data.quantities[4]]);
  await candidateCardByQuantity(matchDrawer(page), data.quantities[4]).getByRole("button", { name: "中选" }).click();
  await expectToast(page, "候选已中选");
  await waitIdle(page);
  await advanceOpportunity(page, {
    stage: "商务沟通",
    result: "差额验证：进入商务沟通。",
    nextAction: "沟通价格",
    planAt: "2026-12-20T10:00:00",
  });
  await expectToast(page, "撮合机会已推进");
  await waitIdle(page);
  await advanceOpportunity(page, {
    stage: "意向确认",
    result: "差额验证：进入意向确认。",
    nextAction: "确认中选供应",
    planAt: "2026-12-23T10:30:00",
  });
  await expectToast(page, "撮合机会已推进");
  await waitIdle(page);
  await closeOpportunity(page, { result: "撮合成功" });
  await expectToast(page, "撮合机会已关闭");
  await waitIdle(page);
  await expectVisibleText(matchDrawer(page), "已成交");
  await closeMatchDrawer(page);

  await openMenu(page, "供应列表", /#\/crm\/herb-base-supply$/);
  await resetFilters(page);
  await chooseSelectByLabel(page, page, "品类", data.productName);
  await fillByLabel(page, "基地", data.baseName);
  await page.getByRole("button", { name: "搜索", exact: true }).click();
  await waitIdle(page);
  const surplusSupplyRow = tableBody(page).locator("tbody tr").filter({ hasText: "450公斤" }).first();
  await expect(surplusSupplyRow).toContainText("有效");
  await expect(surplusSupplyRow).toContainText("空闲");
  await shot(page, "crm-05-match-opportunity", "13-balance-supply");

  // 采购 700 vs 供应 500：采购侧差额 200 自动生成同单据的新采购明细
  const shortageDemandName = `${prefix}_黄芪需求供应不足`;
  await openMenu(page, "厂商管理", /#\/crm\/vendor$/);
  await resetFilters(page);
  await addPurchaseDemand(page, data.vendorNames.a, shortageDemandName, 700);
  selector = await openDemandCandidateSelector(page, shortageDemandName);
  await createOpportunityFromSelector(page, selector, [data.quantities[2]]);
  await candidateCardByQuantity(matchDrawer(page), data.quantities[2]).getByRole("button", { name: "中选" }).click();
  await expectToast(page, "候选已中选");
  await waitIdle(page);
  await advanceOpportunity(page, {
    stage: "商务沟通",
    result: "差额验证：进入商务沟通。",
    nextAction: "沟通价格",
    planAt: "2026-12-20T10:00:00",
  });
  await expectToast(page, "撮合机会已推进");
  await waitIdle(page);
  await advanceOpportunity(page, {
    stage: "意向确认",
    result: "差额验证：进入意向确认。",
    nextAction: "确认中选供应",
    planAt: "2026-12-23T10:30:00",
  });
  await expectToast(page, "撮合机会已推进");
  await waitIdle(page);
  await closeOpportunity(page, { result: "撮合成功" });
  await expectToast(page, "撮合机会已关闭");
  await waitIdle(page);
  await expectVisibleText(matchDrawer(page), "已成交");
  await closeMatchDrawer(page);

  await searchPurchase(page, shortageDemandName);
  const shortageItemRow = tableBody(page).locator("tbody tr").filter({ hasText: "200公斤" }).first();
  await expect(shortageItemRow).toContainText("空闲");
  await shot(page, "crm-05-match-opportunity", "14-balance-demand");

  // ================= O6 厂商详情：联系人 / 沟通记录 / 流转记录 =================
  const contactName = `${prefix}_厂商联系人`;
  const followContent = `${prefix}_厂商沟通记录`;
  const transferOutRemark = `${prefix}_厂商分配`;
  const transferBackRemark = `${prefix}_厂商退回`;
  await openMenu(page, "厂商管理", /#\/crm\/vendor$/);
  await resetFilters(page);
  await page.getByPlaceholder("厂商 / 联系人 / 电话 / 品类").fill(data.vendorNames.a);
  await page.getByRole("button", { name: "搜索", exact: true }).click();
  await waitIdle(page);
  await clickFirstRowAction(page, "详情");
  const vendorDrawer = page.locator(".el-drawer").filter({ hasText: "厂商详情" }).last();
  await expect(vendorDrawer.getByText("厂商详情", { exact: true })).toBeVisible();

  // 联系人：新增后出现在联系人列表
  await vendorDrawer.getByRole("button", { name: "新增联系人" }).first().click();
  dialog = page.getByRole("dialog", { name: "新增联系人" });
  await expect(dialog).toBeVisible();
  await fillByLabel(dialog, "姓名", contactName);
  await fillByLabel(dialog, "电话", "13900001234");
  await chooseSelectByLabel(page, dialog, "电话类型", "手机");
  await chooseSelectByLabel(page, dialog, "角色", "负责人");
  await fillByLabel(dialog, "备注", `${prefix}_厂商联系人备注`);
  await dialog.getByRole("button", { name: "保存" }).click();
  await expectToast(page, "联系人已保存");
  await waitIdle(page);
  const contactsSection = vendorDrawer.locator(".detail-card").filter({ hasText: "联系人" }).first();
  await expectVisibleText(contactsSection, contactName);
  await expectVisibleText(contactsSection, "13900001234");

  // 沟通记录：记录后出现在沟通时间线
  await vendorDrawer.getByRole("button", { name: "记录" }).first().click();
  dialog = page.getByRole("dialog", { name: "记录沟通" });
  await expect(dialog).toBeVisible();
  await chooseSelectByLabel(page, dialog, "联系人", contactName);
  await chooseSelectByLabel(page, dialog, "沟通方式", "微信");
  await chooseSelectByLabel(page, dialog, "沟通结果", "已接通");
  await chooseSelectByLabel(page, dialog, "意向等级", "A");
  await fillByLabel(dialog, "沟通内容", followContent);
  await fillByLabel(dialog, "下次跟进", "2026-12-31 11:30:00");
  await dialog.getByRole("button", { name: "保存" }).click();
  await expectToast(page, "沟通记录已保存");
  await waitIdle(page);
  await expectVisibleText(vendorDrawer, followContent);
  await expectVisibleText(vendorDrawer, "微信");

  // 流转记录：分配与退回都要留痕，刷新后仍可回查
  await vendorDrawer.getByRole("button", { name: /转交|分配/ }).click();
  dialog = page.getByRole("dialog", { name: /转交跟进人|分配跟进人/ });
  await dialog.locator(".el-select").first().click();
  await page.getByRole("option").last().click();
  await fillByLabel(dialog, "备注", transferOutRemark);
  await dialog.getByRole("button", { name: "保存" }).click();
  await expectToast(page, "流转成功");
  await waitIdle(page);
  await vendorDrawer.getByRole("button", { name: "退回" }).click();
  dialog = page.getByRole("dialog", { name: "退回待分配池" });
  await fillByLabel(dialog, "备注", transferBackRemark);
  await dialog.getByRole("button", { name: "保存" }).click();
  await expectToast(page, "流转成功");
  await waitIdle(page);
  await vendorDrawer.getByRole("button", { name: "刷新" }).click();
  await expectToast(page, "厂商详情已刷新");
  await waitIdle(page);
  await expect(vendorDrawer.locator(".follow-card .el-timeline-item")).toHaveCount(1);
  await expect(vendorDrawer.locator(".transfer-card").getByText(transferOutRemark)).toHaveCount(1);
  await expect(vendorDrawer.locator(".transfer-card").getByText(transferBackRemark)).toHaveCount(1);
  await shot(page, "crm-05-match-opportunity", "15-vendor-detail-records");

  // ================= O7 基地详情：联系人 / 沟通记录 / 流转记录 =================
  const baseContactName = `${prefix}_基地联系人`;
  const baseFollowContent = `${prefix}_基地沟通记录`;
  const baseTransferOutRemark = `${prefix}_基地分配`;
  const baseTransferBackRemark = `${prefix}_基地退回`;
  const baseDetailDrawer = await openBaseDetail(page);

  // 联系人：新增后出现在联系人列表
  await baseDetailDrawer.getByRole("button", { name: "新增联系人" }).first().click();
  dialog = page.getByRole("dialog", { name: "新增联系人" });
  await expect(dialog).toBeVisible();
  await fillByLabel(dialog, "姓名", baseContactName);
  await fillByLabel(dialog, "电话", "13800001234");
  await chooseSelectByLabel(page, dialog, "电话类型", "手机");
  await chooseSelectByLabel(page, dialog, "角色", "基地负责人");
  await fillByLabel(dialog, "备注", `${prefix}_基地联系人备注`);
  await dialog.getByRole("button", { name: "保存" }).click();
  await expectToast(page, "联系人已保存");
  await waitIdle(page);
  const baseContactsSection = baseDetailDrawer.locator(".detail-card").filter({ hasText: "联系人" }).first();
  await expectVisibleText(baseContactsSection, baseContactName);
  await expectVisibleText(baseContactsSection, "13800001234");

  // 沟通记录：记录后出现在沟通时间线
  await baseDetailDrawer.getByRole("button", { name: "记录沟通" }).first().click();
  dialog = page.getByRole("dialog", { name: "记录沟通" });
  await expect(dialog).toBeVisible();
  await chooseSelectByLabel(page, dialog, "联系人", baseContactName);
  await chooseSelectByLabel(page, dialog, "沟通方式", "电话");
  await chooseSelectByLabel(page, dialog, "沟通结果", "已接通");
  await chooseSelectByLabel(page, dialog, "意向等级", "A");
  await fillByLabel(dialog, "沟通内容", baseFollowContent);
  await fillByLabel(dialog, "下次跟进", "2026-12-31 15:30:00");
  await dialog.getByRole("button", { name: "保存" }).click();
  await expectToast(page, "沟通记录已保存");
  await waitIdle(page);
  await expectVisibleText(baseDetailDrawer, baseFollowContent);
  await expectVisibleText(baseDetailDrawer, "电话");

  // 流转记录：分配与退回都要留痕，刷新后仍可回查
  await baseDetailDrawer.getByRole("button", { name: /转交|分配/ }).click();
  dialog = page.getByRole("dialog", { name: /转交跟进人|分配跟进人/ });
  await dialog.locator(".el-select").first().click();
  await page.getByRole("option").last().click();
  await fillByLabel(dialog, "备注", baseTransferOutRemark);
  await dialog.getByRole("button", { name: "保存" }).click();
  await expectToast(page, "流转成功");
  await waitIdle(page);
  await baseDetailDrawer.getByRole("button", { name: "退回" }).click();
  dialog = page.getByRole("dialog", { name: "退回待分配池" });
  await fillByLabel(dialog, "备注", baseTransferBackRemark);
  await dialog.getByRole("button", { name: "保存" }).click();
  await expectToast(page, "流转成功");
  await waitIdle(page);
  await baseDetailDrawer.getByRole("button", { name: "刷新" }).click();
  await expectToast(page, "基地主体详情已刷新");
  await waitIdle(page);
  await expect(baseDetailDrawer.locator(".follow-card .el-timeline-item")).toHaveCount(1);
  await expect(baseDetailDrawer.locator(".transfer-card").getByText(baseTransferOutRemark)).toHaveCount(1);
  await expect(baseDetailDrawer.locator(".transfer-card").getByText(baseTransferBackRemark)).toHaveCount(1);
  await shot(page, "crm-05-match-opportunity", "16-base-detail-records");

  const knownNoise = [
    /未成交或需求取消前请先将中选候选改为保留/,
    /仅意向确认阶段可以撮合成功关闭/,
    /供应信息已在其他撮合机会中中选/,
    /Unhandled error during execution of native event handler/,
    /Uncaught \(in promise\)/,
    /ResizeObserver loop/,
    /^未处理异常：Object$/,
    /favicon\.ico/,
  ];
  const relevantErrors = browserErrors.filter(error => !knownNoise.some(pattern => pattern.test(error)));
  expect(relevantErrors, relevantErrors.join("\n")).toEqual([]);
});

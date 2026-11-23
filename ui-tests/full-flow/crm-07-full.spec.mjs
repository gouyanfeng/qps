import { expect, test } from "@playwright/test";
import {
  chooseProductByLabel,
  chooseProductsByLabel,
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
  setSwitchByLabel,
  shot,
  tableBody,
  waitIdle,
} from "../crm-flow-helpers.mjs";

const suffix = new Date().toISOString().replace(/\D/g, "").slice(0, 14);
const prefix = `QPS全流程_CRM_${suffix}`;
const data = {
  productName: "黄芪",
  specification: `全流程统货_${suffix}`,
  quality: `全流程无硫_${suffix}`,
  supplyCycle: `全流程现货_${suffix}`,
  // 两条有效供应参与撮合，第三条保持「待确认」验证未核实供应不进入候选
  supplyQuantities: [500, 100, 200],
  demandQuantity: 600,
  subjectName: `${prefix}_合作社`,
  baseName: `${prefix}_基地`,
  baseContactName: `${prefix}_基地联系人`,
  baseContactPhone: `138${suffix.slice(-8)}`,
  baseFollowContent: `${prefix}_基地首次沟通`,
  vendorName: `${prefix}_药厂`,
  vendorContactName: `${prefix}_采购经理`,
  vendorContactPhone: `139${suffix.slice(-8)}`,
  vendorFollowContent: `${prefix}_厂商首次沟通`,
  demandName: `${prefix}_采购列表`,
  todoFollowContent: `${prefix}_待办二次沟通`,
  transferRemark: `${prefix}_流转备注`,
};

const supplyCycleOf = index => `${data.supplyCycle}_${index + 1}`;

// ---------- 基础表单 helper ----------

const chooseSelectInCell = async (page, cell, option) => {
  await cell.locator(".el-select").first().click();
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

const fillUnitByLabel = async (page, scope, label, unit) => {
  const item = formItemByLabel(scope, label);
  const input = item.locator(".el-select input").first();
  await item.locator(".el-select").first().click();
  await input.fill(unit);
  await page.waitForTimeout(600);
  await page
    .locator(".el-select-dropdown:visible")
    .last()
    .locator(".el-select-dropdown__item")
    .filter({ hasText: new RegExp(`^\\s*${unit.replace("/", "\\/")}\\s*$`) })
    .first()
    .click();
  await expect(item).toContainText(unit);
};

const chooseProductInCell = async (page, cell, productName) => {
  await cell.locator(".el-select").first().click();
  await cell.locator("input:not([type='hidden'])").first().fill(productName);
  await page.waitForTimeout(800);
  await page
    .locator(".el-select-dropdown:visible")
    .last()
    .locator(".el-select-dropdown__item")
    .filter({ hasText: new RegExp(`^\\s*${productName}\\s*$`) })
    .first()
    .click();
};

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

// 列表页筛选下拉：抽屉关闭后的首次点击会被残留的 clickoutside 吞掉，
// 因此点击后校验下拉是否真正展开，未展开则重试
const chooseFilterSelectByLabel = async (page, label, option) => {
  const item = formItemByLabel(page, label);
  let dropdown = null;
  for (let attempt = 0; attempt < 3 && !dropdown; attempt += 1) {
    await item.locator(".el-select").first().click();
    const visible = page.locator(".el-select-dropdown:visible").last();
    const opened = await expect(visible)
      .toBeVisible({ timeout: 1_000 })
      .then(() => true)
      .catch(() => false);
    if (opened) dropdown = visible;
    else await page.waitForTimeout(300);
  }
  if (!dropdown) throw new Error(`下拉「${label}」未能展开`);
  await dropdown.getByRole("option", { name: option }).last().click();
};

// 采购明细表格列会随业务演进漂移，按表头文字定位列
const demandColumnIndex = async (dialog, label) => {
  const headers = dialog.locator(".item-table .el-table__header-wrapper thead tr").first().locator("th");
  const count = await headers.count();
  for (let index = 0; index < count; index += 1) {
    const text = (await headers.nth(index).innerText()).trim();
    if (text === label) return index;
  }
  throw new Error(`采购明细表格未找到列：${label}`);
};

const fillDemandItem = async (page, dialog, quantity) => {
  const columns = {
    product: await demandColumnIndex(dialog, "采购品类"),
    quantity: await demandColumnIndex(dialog, "数量"),
    quantityUnit: await demandColumnIndex(dialog, "单位"),
    specification: await demandColumnIndex(dialog, "规格要求"),
    quality: await demandColumnIndex(dialog, "质量要求"),
    targetPrice: await demandColumnIndex(dialog, "目标价格"),
    priceUnit: await demandColumnIndex(dialog, "价格单位"),
    deliveryAt: await demandColumnIndex(dialog, "明细交期"),
    remark: await demandColumnIndex(dialog, "明细备注"),
  };
  const cells = dialog.locator(".item-table .el-table__body-wrapper tbody tr").nth(0).locator("td");
  await chooseProductInCell(page, cells.nth(columns.product), data.productName);
  await cells.nth(columns.quantity).locator("input:not([type='hidden'])").first().fill(String(quantity));
  await chooseSelectInCell(page, cells.nth(columns.quantityUnit), "公斤");
  await cells.nth(columns.specification).locator("input:not([type='hidden'])").first().fill(data.specification);
  await cells.nth(columns.quality).locator("input:not([type='hidden'])").first().fill(data.quality);
  await cells.nth(columns.targetPrice).locator("input:not([type='hidden'])").first().fill("35");
  await chooseSelectInCell(page, cells.nth(columns.priceUnit), "元/公斤");
  await fillDateTime(cells.nth(columns.deliveryAt).locator(".el-date-editor input").first(), "2026-12-30T15:00:00");
  await cells.nth(columns.remark).locator("input:not([type='hidden'])").first().fill(`${prefix}_采购明细`);
};

// ---------- 页面级 helper ----------

const baseSearchInput = page => page.getByPlaceholder("基地 / 主体 / 联系人 / 电话");
const vendorSearchInput = page => page.getByPlaceholder("厂商 / 联系人 / 电话 / 品类");

const baseDrawer = page => page.locator(".el-drawer").filter({ hasText: "基地主体详情" }).last();
const vendorDrawer = page => page.locator(".el-drawer").filter({ hasText: "厂商详情" }).last();
const matchDrawer = page => page.locator(".el-drawer").filter({ hasText: "撮合机会详情" }).last();

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
};

const addSupply = async (page, pane, index, { active }) => {
  await pane.getByRole("button", { name: "新增供应" }).click();
  const dialog = page.getByRole("dialog", { name: "新增供应信息" });
  await expect(dialog).toBeVisible();
  await chooseProductByLabel(page, dialog, "品类", data.productName);
  await fillByLabel(dialog, "可供量", `${data.supplyQuantities[index]}`);
  await fillUnitByLabel(page, dialog, "可供量", "公斤");
  await fillByLabel(dialog, "预期价格", `${30 + index}`);
  await fillUnitByLabel(page, dialog, "预期价格", "元/公斤");
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
  if (!active) {
    await expect(supplyRowOf(pane, index)).toContainText("待确认");
    return;
  }
  await runSupplyAction(page, pane, index, "设为有效");
  await expectToast(page, "供应信息状态已更新");
  await waitIdle(page);
  await expect(supplyRowOf(pane, index)).toContainText("有效");
};

const openBaseDetailByName = async (page, baseName) => {
  await openMenu(page, "基地管理", /#\/crm\/herb-base$/);
  await resetFilters(page);
  await baseSearchInput(page).fill(baseName);
  await page.getByRole("button", { name: "搜索", exact: true }).click();
  await waitIdle(page);
  await tableBody(page)
    .locator("tbody tr")
    .filter({ hasText: baseName })
    .first()
    .getByRole("button", { name: "详情" })
    .click();
  await expect(baseDrawer(page).getByText("基地主体详情", { exact: true })).toBeVisible();
  await waitIdle(page);
  return baseDrawer(page);
};

const openVendorDetail = async (page, vendorName) => {
  await openMenu(page, "厂商管理", /#\/crm\/vendor$/);
  await resetFilters(page);
  await vendorSearchInput(page).fill(vendorName);
  await page.getByRole("button", { name: "搜索", exact: true }).click();
  await waitIdle(page);
  await clickFirstRowAction(page, "详情");
  await expect(vendorDrawer(page).getByText("厂商详情", { exact: true })).toBeVisible();
  await waitIdle(page);
  return vendorDrawer(page);
};

const closeDetailDrawer = async (page, drawer) => {
  await drawer.locator("button[title='关闭详情']").first().click();
  await drawer.waitFor({ state: "hidden" });
  await waitIdle(page);
};

const recordFollow = async (page, drawer, { contact, method, result, intent, content, nextFollowAt }) => {
  await drawer.getByRole("button", { name: "记录" }).first().click();
  const dialog = page.getByRole("dialog", { name: "记录沟通" });
  await expect(dialog).toBeVisible();
  if (contact) await chooseSelectByLabel(page, dialog, "联系人", contact);
  await chooseSelectByLabel(page, dialog, "方式", method);
  await chooseSelectByLabel(page, dialog, "结果", result);
  // 沟通结果选中「有意向」后，该表单项文本已含「意向」，必须用「意向等级」精确定位
  await chooseSelectByLabel(page, dialog, "意向等级", intent);
  await fillByLabel(dialog, "沟通内容", content);
  await fillByLabel(dialog, "下次跟进", nextFollowAt);
  await dialog.getByRole("button", { name: "保存" }).click();
  await expectToast(page, "沟通记录已保存");
  await waitIdle(page);
};

const transferOwner = async (page, drawer, remark) => {
  await drawer.getByRole("button", { name: /转交|分配/ }).click();
  const dialog = page.getByRole("dialog", { name: /转交跟进人|分配跟进人/ });
  await expect(dialog).toBeVisible();
  await dialog.locator(".el-select").first().click();
  await page.getByRole("option").last().click();
  await fillByLabel(dialog, "备注", remark);
  await dialog.getByRole("button", { name: "保存" }).click();
  await expectToast(page, "流转成功");
  await waitIdle(page);
};

// 撮合详情已改为上下分区：第一张卡片是发起方，第二张卡片是候选方
const initiatorCards = drawer => drawer.locator(".detail-card").first().locator(".card-stack .entity-card");
const candidateCards = drawer => drawer.locator(".detail-card").filter({ hasText: "候选基地" }).first().locator(".card-row .entity-card");
const candidateCardByQuantity = (drawer, quantity) => candidateCards(drawer).filter({ hasText: `${quantity}公斤` }).first();

const refreshMatchDrawer = async page => {
  await matchDrawer(page).getByRole("button", { name: "刷新" }).click();
  await expectToast(page, "详情已刷新");
  await waitIdle(page);
};

const appendCandidatesByQuantity = async (page, quantities) => {
  await matchDrawer(page).getByRole("button", { name: "追加候选" }).click();
  const dialog = page.getByRole("dialog", { name: "候选筛选" });
  await expect(dialog).toBeVisible();
  await fillByLabel(dialog, "规格", data.specification);
  await dialog.getByRole("button", { name: "查询" }).click();
  await waitIdle(page);
  const rows = dialog.locator(".candidate-selector .el-table__body-wrapper tbody tr");
  for (const quantity of quantities) {
    await rows.filter({ hasText: data.baseName }).filter({ hasText: `${quantity}公斤` }).first().locator(".el-checkbox").click();
  }
  await dialog.getByRole("button", { name: "确定" }).click();
  await expectToast(page, "候选已追加");
  await waitIdle(page);
  await refreshMatchDrawer(page);
};

const selectCandidate = async (page, quantity) => {
  const card = candidateCardByQuantity(matchDrawer(page), quantity);
  await expect(card).toBeVisible();
  await card.getByRole("button", { name: "中选" }).click();
  await expectToast(page, "候选已中选");
  await waitIdle(page);
};

const advanceOpportunity = async (page, { stage, result, nextAction, remark, planAt }) => {
  await matchDrawer(page).getByRole("button", { name: "推进机会" }).click();
  const dialog = page.getByRole("dialog", { name: "推进机会" });
  await expect(dialog).toBeVisible();
  await waitIdle(page);
  if (stage) await chooseSelectByLabel(page, dialog, "阶段", stage);
  await fillByLabel(dialog, "本次结果", result);
  await chooseSelectByLabel(page, dialog, "下一步动作", nextAction);
  // 「其他待办说明」仅在下一步动作选「其他」时出现，其余动作不填
  if (remark && nextAction === "其他") await fillByLabel(dialog, "其他待办说明", remark);
  await fillDateTimeByLabel(dialog, "计划时间", planAt);
  await dialog.getByRole("button", { name: "确认推进" }).click();
  await expectToast(page, "撮合机会已推进");
  await waitIdle(page);
};

const closeOpportunityAsDeal = async page => {
  await matchDrawer(page).getByRole("button", { name: "关闭", exact: true }).click();
  const dialog = page.getByRole("dialog", { name: "关闭撮合机会" });
  await expect(dialog).toBeVisible();
  await chooseSelectByLabel(page, dialog, "关闭结果", "撮合成功");
  await dialog.getByRole("button", { name: "确认关闭" }).click();
  await expectToast(page, "撮合机会已关闭");
  await waitIdle(page);
};

const openPurchaseList = async (page, keyword) => {
  await openMenu(page, "采购列表", /#\/crm\/purchase/);
  await resetFilters(page);
  await page.getByPlaceholder("编号 / 需求名称 / 厂商").fill(keyword);
  await page.getByRole("button", { name: "搜索", exact: true }).click();
  await waitIdle(page);
  return tableBody(page).locator("tbody tr").filter({ hasText: keyword }).first();
};

const openTodoList = async (page, entityType, keyword) => {
  await openMenu(page, "我的待办", /#\/crm\/follow-tasks$/);
  await resetFilters(page);
  await chooseFilterSelectByLabel(page, "对象类型", entityType);
  await page.getByPlaceholder("名称、联系人或电话").fill(keyword);
  await page.getByRole("button", { name: "搜索", exact: true }).click();
  await waitIdle(page);
  return tableBody(page).locator("tbody tr").filter({ hasText: keyword });
};

test("CRM 全流程：基地建档 → 厂商采购 → 撮合推进 → 待办跟进 → 成交关闭与全链路回查", async ({ page }) => {
  test.setTimeout(900_000);
  const browserErrors = [];
  page.on("pageerror", error => browserErrors.push(`未处理异常：${error.message}`));
  page.on("console", message => {
    // favicon 缺失属于本地开发服务的静态资源噪声，不计入业务异常
    const location = message.location()?.url || "";
    if (message.type() === "error" && !location.includes("favicon.ico") && !message.text().includes("favicon.ico")) {
      browserErrors.push(`控制台错误：${message.text()} @ ${location}`);
    }
  });
  page.on("response", response => {
    if (response.url().includes("/api/") && response.status() >= 400) {
      browserErrors.push(`HTTP ${response.status()}：${response.url()}`);
    }
  });

  await login(page);

  // ================= 1. 基地管理：基地 + 供应 + 联系人 + 沟通 =================
  await openMenu(page, "基地管理", /#\/crm\/herb-base$/);
  await resetFilters(page);
  await page.getByRole("button", { name: "新增药材基地" }).click();
  let dialog = page.getByRole("dialog", { name: "新增基地" });
  await expect(dialog).toBeVisible();
  await fillByLabel(dialog, "基地名称", data.baseName);
  await fillByLabel(dialog, "规模", "300");
  await chooseProductsByLabel(page, dialog, "供应品类", [data.productName]);
  await chooseRegionByLabel(page, dialog, "地区", ["陕西省", "西安市", "新城区"]);
  await fillByLabel(dialog, "详细地址", `${prefix}_基地地址`);
  await chooseSelectByLabel(page, dialog, "来源", "手工录入");
  await fillByLabel(dialog, "备注", `${prefix}_基地备注`);
  await dialog.getByRole("button", { name: "创建" }).click();
  await expectToast(page, "创建成功");
  await waitIdle(page);

  let drawer = await openBaseDetailByName(page, data.baseName);
  await drawer.getByRole("button", { name: "编辑主体" }).click();
  dialog = page.getByRole("dialog", { name: "编辑主体" });
  await expect(dialog).toBeVisible();
  await fillByLabel(dialog, "主体名称", data.subjectName);
  await fillByLabel(dialog, "主体类型", "合作社");
  await chooseSelectByLabel(page, dialog, "状态", "跟进中");
  await fillByLabel(dialog, "备注", `${prefix}_主体备注`);
  await dialog.getByRole("button", { name: "保存" }).click();
  await expectToast(page, "主体已保存");
  await waitIdle(page);
  await expectVisibleText(drawer, data.subjectName);

  const pane = await openBaseTab(drawer, data.baseName);
  await addSupply(page, pane, 0, { active: true });
  await addSupply(page, pane, 1, { active: true });
  await addSupply(page, pane, 2, { active: false });
  await expect(pane.locator(".supply-table .el-table__body-wrapper tbody tr").filter({ hasText: data.supplyCycle })).toHaveCount(3);

  await drawer.getByRole("button", { name: "新增联系人" }).first().click();
  dialog = page.getByRole("dialog", { name: "新增联系人" });
  await expect(dialog).toBeVisible();
  await fillByLabel(dialog, "姓名", data.baseContactName);
  await fillByLabel(dialog, "电话", data.baseContactPhone);
  await chooseSelectByLabel(page, dialog, "电话类型", "手机");
  await fillByLabel(dialog, "微信", `wx_base_${suffix.slice(-8)}`);
  await chooseSelectByLabel(page, dialog, "角色", "基地负责人");
  await setSwitchByLabel(dialog, "主联系人", true);
  await fillByLabel(dialog, "备注", `${prefix}_基地联系人备注`);
  await dialog.getByRole("button", { name: "保存" }).click();
  await expectToast(page, "联系人已保存");
  await waitIdle(page);
  await expectVisibleText(drawer, data.baseContactName);

  await recordFollow(page, drawer, {
    contact: data.baseContactName,
    method: "电话",
    result: "有意向",
    intent: "A",
    content: data.baseFollowContent,
    nextFollowAt: "2026-12-15 09:30:00",
  });
  await expectVisibleText(drawer, data.baseFollowContent);
  await shot(page, "crm-07-full", "01-base-detail");

  // 基地待办：新增联系人 + 沟通后自动生成跟进任务
  await closeDetailDrawer(page, drawer);
  let todoRows = await openTodoList(page, "基地主体", data.subjectName);
  await expect(todoRows).toHaveCount(1);
  await expect(todoRows.first()).toContainText("有意向");
  await shot(page, "crm-07-full", "02-base-todo");

  // ================= 2. 厂商管理：厂商 + 联系人 + 采购列表 =================
  await openMenu(page, "厂商管理", /#\/crm\/vendor$/);
  await resetFilters(page);
  await page.getByRole("button", { name: "新增厂商" }).click();
  dialog = page.getByRole("dialog", { name: "新增厂商" });
  await expect(dialog).toBeVisible();
  await fillByLabel(dialog, "厂商名称", data.vendorName);
  await chooseSelectByLabel(page, dialog, "优先级", "高");
  await fillByLabel(dialog, "备注", `${prefix}_厂商备注`);
  await dialog.getByRole("button", { name: "保存" }).click();
  await expect(page.getByText(/新增成功|创建成功/).last()).toBeVisible();
  await waitIdle(page);

  let vendorDrawerInstance = await openVendorDetail(page, data.vendorName);
  await vendorDrawerInstance.getByRole("button", { name: "新增联系人" }).first().click();
  dialog = page.getByRole("dialog", { name: "新增联系人" });
  await expect(dialog).toBeVisible();
  await fillByLabel(dialog, "姓名", data.vendorContactName);
  await fillByLabel(dialog, "电话", data.vendorContactPhone);
  await chooseSelectByLabel(page, dialog, "电话类型", "手机");
  await fillByLabel(dialog, "微信", `wx_vendor_${suffix.slice(-8)}`);
  await chooseSelectByLabel(page, dialog, "角色", "负责人");
  await setSwitchByLabel(dialog, "主联系人", true);
  await fillByLabel(dialog, "备注", `${prefix}_厂商联系人备注`);
  await dialog.getByRole("button", { name: "保存" }).click();
  await expectToast(page, "联系人已保存");
  await waitIdle(page);
  await expectVisibleText(vendorDrawerInstance, data.vendorContactName);

  const purchaseSection = vendorDrawerInstance.locator(".purchase-demand-card");
  const addDemandBtn = purchaseSection.getByRole("button", { name: "新增采购列表" });
  await addDemandBtn.scrollIntoViewIfNeeded();
  await addDemandBtn.click();
  dialog = page.getByRole("dialog", { name: "新增采购列表" });
  await expect(dialog).toBeVisible();
  await fillByLabel(dialog, "采购列表名称", data.demandName);
  await fillDateTimeByLabel(dialog, "提出日期", "2026-09-05T09:00:00");
  await fillDateTimeByLabel(dialog, "期望到货日期", "2026-12-31T10:00:00");
  await chooseRegionByLabel(page, dialog, "收货地", ["陕西省", "西安市", "新城区"]);
  await fillByLabel(dialog, "详细地址", `${prefix}_收货仓库`);
  await fillByLabel(dialog, "来源网页", `https://example.test/${suffix}`);
  await fillByLabel(dialog, "总体备注", `${prefix}_采购需求`);
  await fillDemandItem(page, dialog, data.demandQuantity);
  await dialog.getByRole("button", { name: "保存" }).click();
  await expectToast(page, "采购列表已保存");
  await waitIdle(page);

  const demandRow = purchaseSection.locator(".purchase-demand-table .el-table__body-wrapper tbody tr").filter({ hasText: data.demandName }).first();
  await expect(demandRow).toBeVisible();
  await expect(demandRow).toContainText("空闲");

  await recordFollow(page, vendorDrawerInstance, {
    contact: data.vendorContactName,
    method: "微信",
    result: "已接通",
    intent: "B",
    content: data.vendorFollowContent,
    nextFollowAt: "2026-12-16 14:00:00",
  });
  await expectVisibleText(vendorDrawerInstance, data.vendorFollowContent);
  await shot(page, "crm-07-full", "03-vendor-detail");
  await closeDetailDrawer(page, vendorDrawerInstance);

  // ================= 3. 采购列表发起撮合 → 追加候选 → 中选 =================
  const itemRow = await openPurchaseList(page, data.demandName);
  await expect(itemRow).toBeVisible();
  await itemRow.getByRole("button", { name: "发起撮合" }).click();
  await expect(page).toHaveURL(/#\/crm\/purchase/);
  await expectToast(page, "撮合机会已创建");
  await waitIdle(page);
  await expect(matchDrawer(page).getByText("撮合机会详情", { exact: true })).toBeVisible();
  await expect(initiatorCards(matchDrawer(page))).toHaveCount(1);
  await expect(candidateCards(matchDrawer(page))).toHaveCount(0);

  // 未核实的「待确认」供应不应出现在候选里：只追加两条有效供应
  await appendCandidatesByQuantity(page, [data.supplyQuantities[0], data.supplyQuantities[1]]);
  await expect(candidateCards(matchDrawer(page))).toHaveCount(2);
  await expect(candidateCards(matchDrawer(page)).filter({ hasText: `${data.supplyQuantities[2]}公斤` })).toHaveCount(0);
  await shot(page, "crm-07-full", "04-candidates");

  await selectCandidate(page, data.supplyQuantities[0]);
  await selectCandidate(page, data.supplyQuantities[1]);
  await refreshMatchDrawer(page);
  await expect(candidateCards(matchDrawer(page)).filter({ hasText: "中选" })).toHaveCount(2);

  // ================= 4. 阶段推进：匹配核实 → 商务沟通 =================
  await advanceOpportunity(page, {
    stage: "商务沟通",
    result: "已完成基地产能与药厂需求数量核对",
    nextAction: "沟通价格",
    planAt: "2026-12-18T10:30:00",
  });
  await refreshMatchDrawer(page);
  await expectVisibleText(matchDrawer(page), "商务沟通");
  await shot(page, "crm-07-full", "05-advanced");

  // ================= 5. 我的待办：撮合推进 + 厂商沟通 =================
  await closeDetailDrawer(page, matchDrawer(page));
  todoRows = await openTodoList(page, "撮合机会", data.vendorName);
  await expect(todoRows).toHaveCount(1);
  await todoRows.first().getByRole("button", { name: "推进" }).click();
  dialog = page.getByRole("dialog", { name: "推进机会" });
  await expect(dialog).toBeVisible();
  await waitIdle(page);
  await fillByLabel(dialog, "本次结果", "价格区间已对齐，等待药厂回传采购确认");
  await chooseSelectByLabel(page, dialog, "下一步动作", "等待对方反馈");
  await fillDateTimeByLabel(dialog, "计划时间", "2026-12-20T09:00:00");
  await dialog.getByRole("button", { name: "确认推进" }).click();
  await expectToast(page, "撮合机会已推进");
  await waitIdle(page);

  todoRows = await openTodoList(page, "撮合机会", data.vendorName);
  await expect(todoRows).toHaveCount(1);
  await expect(todoRows.first()).toContainText("等待对方反馈");

  todoRows = await openTodoList(page, "厂商", data.vendorName);
  await expect(todoRows).toHaveCount(1);
  await todoRows.first().getByRole("button", { name: "记录沟通" }).click();
  dialog = page.getByRole("dialog", { name: "记录沟通" });
  await expect(dialog).toBeVisible();
  await chooseSelectByLabel(page, dialog, "方式", "电话");
  await chooseSelectByLabel(page, dialog, "结果", "有意向");
  await chooseSelectByLabel(page, dialog, "意向等级", "A");
  await fillByLabel(dialog, "沟通内容", data.todoFollowContent);
  await fillByLabel(dialog, "下次跟进", "2026-12-22 11:00:00");
  await dialog.getByRole("button", { name: "保存" }).click();
  await expectToast(page, "沟通记录已保存");
  await waitIdle(page);
  todoRows = await openTodoList(page, "厂商", data.vendorName);
  await expect(todoRows.first()).toContainText("有意向");
  await shot(page, "crm-07-full", "06-todo");

  // ================= 6. 待办详情推进到意向确认并成交关闭 =================
  todoRows = await openTodoList(page, "撮合机会", data.vendorName);
  await todoRows.first().getByRole("button", { name: "详情" }).click();
  await expect(matchDrawer(page).getByText("撮合机会详情", { exact: true })).toBeVisible();
  await waitIdle(page);
  await advanceOpportunity(page, {
    stage: "意向确认",
    result: "药厂确认采购 600 公斤黄芪",
    nextAction: "确认中选供应",
    planAt: "2026-12-23T15:00:00",
  });
  await refreshMatchDrawer(page);
  await expectVisibleText(matchDrawer(page), "意向确认");
  await closeOpportunityAsDeal(page);
  await expect(matchDrawer(page).locator(".title-row").getByText("已成交")).toBeVisible();
  await shot(page, "crm-07-full", "07-deal");
  await closeDetailDrawer(page, matchDrawer(page));

  // 成交后撮合待办自动关闭
  todoRows = await openTodoList(page, "撮合机会", data.vendorName);
  await expect(todoRows).toHaveCount(0);

  // ================= 7. 全链路回查：采购列表 / 供应列表 / 基地成交记录 / 撮合列表 =================
  const closedItemRow = await openPurchaseList(page, data.demandName);
  await expect(closedItemRow).toBeVisible();
  await expect(closedItemRow).toContainText("已成交 1/1");
  await expect(closedItemRow).toContainText("成交");
  await expect(closedItemRow.locator(".matched")).toHaveText(String(data.demandQuantity));
  await shot(page, "crm-07-full", "08-purchase-deal");

  await openMenu(page, "供应列表", /#\/crm\/herb-base-supply$/);
  await resetFilters(page);
  await fillByLabel(page, "基地", data.baseName);
  await page.getByRole("button", { name: "搜索", exact: true }).click();
  await waitIdle(page);
  const supplyRows = tableBody(page).locator("tbody tr");
  await expect(supplyRows).toHaveCount(3);
  await expect(supplyRows.filter({ hasText: `${data.supplyQuantities[2]}公斤` }).first()).toContainText("空闲");
  await chooseFilterSelectByLabel(page, "撮合状态", "成交");
  await page.getByRole("button", { name: "搜索", exact: true }).click();
  await waitIdle(page);
  const dealSupplyRows = tableBody(page).locator("tbody tr");
  await expect(dealSupplyRows).toHaveCount(2);
  await expect(dealSupplyRows.filter({ hasText: `${data.supplyQuantities[0]}公斤` })).toHaveCount(1);
  await expect(dealSupplyRows.filter({ hasText: `${data.supplyQuantities[1]}公斤` })).toHaveCount(1);
  await shot(page, "crm-07-full", "09-supply-deal");

  drawer = await openBaseDetailByName(page, data.baseName);
  await expectVisibleText(drawer, "成交记录");
  // 成交记录按「中选供应」逐条生成，本次两条有效供应都中选，因此是两条记录
  await expect(drawer.locator(".deal-list .deal-item")).toHaveCount(2);
  await expect(drawer.locator(".deal-list .deal-item").first()).toContainText(data.vendorName);
  const dealPane = await openBaseTab(drawer, data.baseName);
  // 基地明细供应表只有「状态」列，成交后中选供应变为已售罄，未参与撮合的仍为有效
  const dealSupplyRowsInPane = dealPane.locator(".supply-table .el-table__body-wrapper tbody tr").filter({ hasText: data.supplyCycle });
  await expect(dealSupplyRowsInPane).toHaveCount(3);
  await expect(dealSupplyRowsInPane.filter({ hasText: "已售罄" })).toHaveCount(2);

  // 基地流转：转交跟进人后流转记录留痕
  await transferOwner(page, drawer, `${data.transferRemark}_基地`);
  await expectVisibleText(drawer, `${data.transferRemark}_基地`);
  await shot(page, "crm-07-full", "10-base-transfer");
  await closeDetailDrawer(page, drawer);

  await openMenu(page, "撮合机会", /#\/crm\/match-opportunity$/);
  await resetFilters(page);
  await chooseFilterSelectByLabel(page, "状态", "已关闭");
  await chooseFilterSelectByLabel(page, "发起方向", "药厂");
  await fillByLabel(page, "品类", data.productName);
  await chooseRemoteSelectByLabel(page, page, "需求厂商", data.vendorName, data.vendorName);
  await page.getByRole("button", { name: "搜索", exact: true }).click();
  await waitIdle(page);
  const matchRows = tableBody(page).locator("tbody tr");
  await expect(matchRows).toHaveCount(1);
  await expect(matchRows.first()).toContainText(data.vendorName);
  await expect(matchRows.first()).toContainText(data.productName);
  await expect(matchRows.first()).toContainText("已成交");
  await expect(matchRows.first()).toContainText("药厂");
  await shot(page, "crm-07-full", "11-match-deal");

  // 厂商流转：转交后流转记录留痕
  vendorDrawerInstance = await openVendorDetail(page, data.vendorName);
  await expectVisibleText(vendorDrawerInstance, data.demandName);
  await transferOwner(page, vendorDrawerInstance, `${data.transferRemark}_厂商`);
  await expectVisibleText(vendorDrawerInstance, `${data.transferRemark}_厂商`);
  await shot(page, "crm-07-full", "12-vendor-transfer");
  await closeDetailDrawer(page, vendorDrawerInstance);

  expect(browserErrors, browserErrors.join("\n")).toEqual([]);
});
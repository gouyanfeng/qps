import { expect, test } from "@playwright/test";
import {
  chooseProductByLabel,
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
  vendorName: `${prefix}_厂商`,
  demandName: `${prefix}_黄芪需求`,
  subjectName: `${prefix}_基地主体`,
  baseName: `${prefix}_基地`,
  specification: `撮合统货_${suffix}`,
  quality: `撮合无硫_${suffix}`,
  nextAction: "核实数量",
  eliminationReason: `规格不符合本轮黄芪需求_${suffix}`,
  closeReason: `本轮人工撮合完成归档_${suffix}`,
  quantities: [300, 400, 500],
  updatedQuantity: 350,
};

const supplyCycleOf = index => `撮合现货_${suffix}_${index + 1}`;

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

const fillDemandItem = async (page, dialog) => {
  const cells = dialog.locator(".item-table .el-table__body-wrapper tbody tr").first().locator("td");
  await cells.nth(0).locator(".el-select").first().click();
  await cells.nth(0).locator("input:not([type='hidden'])").first().fill(data.productName);
  await page.waitForTimeout(800);
  // 品类存在“黄芪 / 蒙古黄芪”近似项，必须精确匹配，否则供需两侧品类不一致
  await page
    .locator(".el-select-dropdown:visible")
    .last()
    .locator(".el-select-dropdown__item")
    .filter({ hasText: new RegExp(`^\\s*${data.productName}\\s*$`) })
    .first()
    .click();
  await cells.nth(1).locator("input:not([type='hidden'])").first().fill(String(data.quantities[0]));
  await chooseSelectInCell(page, cells.nth(2), "公斤");
  await cells.nth(3).locator("input:not([type='hidden'])").first().fill(data.specification);
  await cells.nth(4).locator("input:not([type='hidden'])").first().fill(data.quality);
  await cells.nth(5).locator("input:not([type='hidden'])").first().fill("35");
  await chooseSelectInCell(page, cells.nth(6), "元/公斤");
  await fillDateTime(cells.nth(7).locator(".el-date-editor input").first(), "2026-12-30T15:00:00");
  await cells.nth(8).locator("input:not([type='hidden'])").first().fill(`${prefix}_采购明细`);
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

const addActiveSupply = async (page, pane, index) => {
  await pane.getByRole("button", { name: "新增供应" }).click();
  const dialog = page.getByRole("dialog", { name: "新增供应信息" });
  await expect(dialog).toBeVisible();
  await chooseProductByLabel(page, dialog, "品类", data.productName);
  await fillByLabel(dialog, "可供量", `${data.quantities[index]}`);
  await formItemByLabel(dialog, "可供量").getByPlaceholder("单位").fill("公斤");
  await fillByLabel(dialog, "预期价格", `${30 + index}`);
  await formItemByLabel(dialog, "预期价格").getByPlaceholder("单位").fill("元/公斤");
  await fillByLabel(dialog, "规格", data.specification);
  await fillByLabel(dialog, "质量要求", data.quality);
  await fillByLabel(dialog, "产新期", "9月-11月");
  await fillByLabel(dialog, "供货周期", supplyCycleOf(index));
  await fillByLabel(dialog, "核实日期", "2026-09-05");
  await fillByLabel(dialog, "有效截止", "2026-12-31");
  await fillByLabel(dialog, "备注", `${prefix}_黄芪供应_${index + 1}`);
  await dialog.getByRole("button", { name: "保存" }).click();
  await expectToast(page, "供应信息已保存");
  await waitIdle(page);

  const row = pane.locator(".supply-table .el-table__body-wrapper tbody tr").filter({ hasText: supplyCycleOf(index) }).first();
  await expect(row).toBeVisible();
  await row.getByRole("button", { name: /^(供应)?操作$/ }).click();
  await page.getByRole("menuitem", { name: "设为有效" }).last().click();
  await expectToast(page, "供应信息状态已更新");
  await waitIdle(page);
  await expect(row).toContainText("有效");
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

const appendCandidates = async (page, quantities) => {
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

const advanceOpportunity = async (page, { stage, result, nextAction, remark, planAt, confirmText = "确认推进" }) => {
  await matchDrawer(page).getByRole("button", { name: "推进机会" }).click();
  const dialog = page.getByRole("dialog", { name: "推进机会" });
  await expect(dialog).toBeVisible();
  if (stage) await chooseSelectByLabel(page, dialog, "阶段", stage);
  await fillByLabel(dialog, "本次结果", result);
  await chooseSelectByLabel(page, dialog, "下一步动作", nextAction);
  if (remark) await fillByLabel(dialog, "其他待办说明", remark);
  await fillDateTimeByLabel(dialog, "计划时间", planAt);
  await dialog.getByRole("button", { name: confirmText }).click();
};

test("人工供需撮合机会：双侧多对多候选、推进、关闭与完整回查", async ({ page }) => {
  test.setTimeout(420_000);
  const browserErrors = [];
  page.on("pageerror", error => browserErrors.push(`未处理异常：${error.message}`));
  page.on("console", message => {
    if (message.type() === "error") browserErrors.push(`控制台错误：${message.text()} @ ${message.location().url}`);
  });
  page.on("response", response => {
    if (response.url().includes("/api/") && response.status() >= 400) {
      browserErrors.push(`HTTP ${response.status()}：${response.url()}`);
    }
  });

  await login(page);

  // ---------- 数据准备：厂商 + 采购列表 + 基地主体 + 三条有效供应 ----------
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
  const vendorDrawer = page.locator(".el-drawer").filter({ hasText: "厂商详情" }).last();
  await expect(vendorDrawer.getByText("厂商详情", { exact: true })).toBeVisible();
  const purchaseSection = vendorDrawer.locator(".purchase-demand-card");
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
  await fillDemandItem(page, dialog);
  await dialog.getByRole("button", { name: "保存" }).click();
  await expectToast(page, "采购列表已保存");
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
    await addActiveSupply(page, basePane, index);
  }
  await expect(basePane.locator(".supply-table .el-table__body-wrapper tbody tr").filter({ hasText: `撮合现货_${suffix}_` })).toHaveCount(3);
  await shot(page, "crm-05-match-opportunity", "03-active-supplies");

  // ---------- 供应侧：从供应列表发起，候选为药厂采购需求 ----------
  await openMenu(page, "供应列表", /#\/crm\/herb-base-supply$/);
  await resetFilters(page);
  await chooseSelectByLabel(page, page, "品类", data.productName);
  await page.getByRole("button", { name: "搜索", exact: true }).click();
  await waitIdle(page);
  const supplyRow = tableBody(page).locator("tbody tr").filter({ hasText: data.baseName }).filter({ hasText: `${data.quantities[0]}公斤` }).first();
  await expect(supplyRow).toBeVisible();
  await supplyRow.getByRole("button", { name: "发起撮合" }).click();
  await expectToast(page, "已发起撮合，请在详情页选择候选药厂");
  await waitIdle(page);
  await expect(page).toHaveURL(/#\/crm\/match-opportunity\?detailId=/);
  await expect(matchDrawer(page).getByText("撮合机会详情", { exact: true })).toBeVisible();
  await expectVisibleText(matchDrawer(page), "供给信息");
  await expect(initiatorCards(matchDrawer(page))).toHaveCount(1);
  await expectVisibleText(initiatorCards(matchDrawer(page)).first(), data.baseName);
  await expect(matchDrawer(page).getByText("暂无候选药厂")).toBeVisible();

  await matchDrawer(page).getByRole("button", { name: "追加候选" }).click();
  dialog = page.getByRole("dialog", { name: "候选筛选" });
  await expect(dialog).toBeVisible();
  const demandCandidateRow = dialog.locator(".candidate-selector .el-table__body-wrapper tbody tr").filter({ hasText: data.demandName }).first();
  await expect(demandCandidateRow).toBeVisible();
  await demandCandidateRow.locator(".el-checkbox").click();
  await dialog.getByRole("button", { name: "确定" }).click();
  await expectToast(page, "候选已追加");
  await waitIdle(page);
  await refreshMatchDrawer(page);
  await expect(candidateCards(matchDrawer(page))).toHaveCount(1);
  await expectVisibleText(candidateCards(matchDrawer(page)).first(), data.vendorName);
  await expectVisibleText(candidateCards(matchDrawer(page)).first(), "待评估");

  await candidateCards(matchDrawer(page)).first().getByRole("button", { name: "中选" }).click();
  await expectToast(page, "候选已中选");
  await waitIdle(page);
  await expectVisibleText(candidateCards(matchDrawer(page)).first(), "中选");

  // 未成交前必须先释放中选，验证业务拦截文案
  await matchDrawer(page).getByRole("button", { name: "关闭", exact: true }).click();
  dialog = page.getByRole("dialog", { name: "关闭撮合机会" });
  await expect(dialog).toBeVisible();
  await chooseSelectByLabel(page, dialog, "关闭结果", "未成交");
  await fillByLabel(dialog, "关闭原因", `${prefix}_供应侧未成交`);
  await dialog.getByRole("button", { name: "确认关闭" }).click();
  await expectToast(page, "未成交或需求取消前请先将中选候选改为保留");
  await dialog.getByRole("button", { name: "取消", exact: true }).click();
  await waitIdle(page);

  await candidateCards(matchDrawer(page)).first().getByRole("button", { name: "保留" }).click();
  await expectToast(page, "候选已保留");
  await waitIdle(page);

  await matchDrawer(page).getByRole("button", { name: "关闭", exact: true }).click();
  dialog = page.getByRole("dialog", { name: "关闭撮合机会" });
  await chooseSelectByLabel(page, dialog, "关闭结果", "未成交");
  await fillByLabel(dialog, "关闭原因", data.closeReason);
  await dialog.getByRole("button", { name: "确认关闭" }).click();
  await expectToast(page, "撮合机会已关闭");
  await waitIdle(page);
  await expectVisibleText(matchDrawer(page), "已关闭");
  await expect(matchDrawer(page).getByRole("button", { name: "追加候选" })).toHaveCount(0);
  await expect(matchDrawer(page).getByRole("button", { name: "推进机会" })).toHaveCount(0);
  await shot(page, "crm-05-match-opportunity", "04-supply-side-closed");
  await matchDrawer(page).locator("button[title='关闭详情']").first().click();
  await page.locator(".el-drawer").filter({ hasText: "撮合机会详情" }).first().waitFor({ state: "hidden" });
  await waitIdle(page);

  // ---------- 需求侧：采购列表一键发起空机会，再追加候选 ----------
  await openMenu(page, "采购列表", /#\/crm\/purchase/);
  await resetFilters(page);
  const demandSearch = page.getByPlaceholder("编号 / 需求名称 / 厂商");
  await demandSearch.fill(data.demandName);
  await page.getByRole("button", { name: "搜索", exact: true }).click();
  await waitIdle(page);
  demandRow = tableBody(page).locator("tbody tr").filter({ hasText: data.demandName }).first();
  await expect(demandRow.getByRole("button", { name: "编辑" })).toHaveCount(0);
  await demandRow.getByRole("button", { name: "开始撮合" }).click();
  await expectToast(page, "已发起撮合，请在详情页选择候选基地");
  await waitIdle(page);
  await expect(page).toHaveURL(/#\/crm\/match-opportunity\?detailId=/);
  await expect(matchDrawer(page).getByText("撮合机会详情", { exact: true })).toBeVisible();
  await expectVisibleText(matchDrawer(page), "需求信息");
  await expectVisibleText(initiatorCards(matchDrawer(page)).first(), data.vendorName);
  await expect(matchDrawer(page).getByText("暂无候选基地")).toBeVisible();

  await appendCandidates(page, data.quantities);
  await expect(candidateCards(matchDrawer(page))).toHaveCount(3);
  await expect(candidateCards(matchDrawer(page)).filter({ hasText: "待评估" })).toHaveCount(3);

  // 保留 / 修改 / 中选 / 淘汰 / 恢复
  await candidateCardByQuantity(matchDrawer(page), data.quantities[1]).getByRole("button", { name: "保留" }).click();
  await expectToast(page, "候选已保留");
  await waitIdle(page);
  await expectVisibleText(candidateCardByQuantity(matchDrawer(page), data.quantities[1]), "保留");

  await candidateCardByQuantity(matchDrawer(page), data.quantities[0]).getByRole("button", { name: "修改" }).click();
  dialog = page.getByRole("dialog", { name: "修改候选" });
  await expect(dialog).toBeVisible();
  await formItemByLabel(dialog, "可供量").locator("input").first().fill(String(data.updatedQuantity));
  await fillByLabel(dialog, "规格", `${data.specification}_修订`);
  await dialog.getByRole("button", { name: "保存" }).click();
  await expectToast(page, "候选已修改");
  await waitIdle(page);
  await expectVisibleText(candidateCardByQuantity(matchDrawer(page), data.updatedQuantity), `${data.specification}_修订`);

  const eliminatedCard = candidateCardByQuantity(matchDrawer(page), data.quantities[2]);
  await eliminatedCard.getByRole("button", { name: "中选" }).click();
  await expectToast(page, "候选已中选");
  await waitIdle(page);
  await expectVisibleText(eliminatedCard, "中选");
  await eliminatedCard.getByRole("button", { name: "淘汰" }).click();
  const eliminateDialog = page.locator(".el-message-box");
  await expect(eliminateDialog).toBeVisible();
  await eliminateDialog.locator("input:not([type='hidden'])").first().fill(data.eliminationReason);
  await eliminateDialog.getByRole("button", { name: /^(确定|确认|OK)$/ }).click();
  await expectToast(page, "候选已淘汰");
  await waitIdle(page);
  await expectVisibleText(eliminatedCard, data.eliminationReason);
  await eliminatedCard.getByRole("button", { name: "恢复" }).click();
  await expectToast(page, "候选已恢复为待评估");
  await waitIdle(page);
  await expectVisibleText(eliminatedCard, "待评估");
  await shot(page, "crm-05-match-opportunity", "05-candidates-maintained");

  // 重新选择候选：去掉 500 公斤那条，仅保留两条
  await matchDrawer(page).getByRole("button", { name: "重新选择候选" }).click();
  dialog = page.getByRole("dialog", { name: "重新选择候选" });
  await expect(dialog).toBeVisible();
  await fillByLabel(dialog, "规格", data.specification);
  await dialog.getByRole("button", { name: "查询" }).click();
  await waitIdle(page);
  const replaceRows = dialog.locator(".candidate-selector .el-table__body-wrapper tbody tr");
  await replaceRows.filter({ hasText: data.baseName }).filter({ hasText: `${data.quantities[2]}公斤` }).first().locator(".el-checkbox").click();
  await dialog.getByRole("button", { name: "确定" }).click();
  await expectToast(page, "候选已重新选择");
  await waitIdle(page);
  await refreshMatchDrawer(page);
  await expect(candidateCards(matchDrawer(page))).toHaveCount(2);

  // 重新选择候选会按供应当前快照重建候选，修改过的可供量回到 300 公斤
  await candidateCardByQuantity(matchDrawer(page), data.quantities[0]).getByRole("button", { name: "中选" }).click();
  await expectToast(page, "候选已中选");
  await waitIdle(page);
  await expectVisibleText(candidateCardByQuantity(matchDrawer(page), data.quantities[0]), "中选");
  await shot(page, "crm-05-match-opportunity", "06-candidates-selected");

  // 推进：匹配核实 -> 商务沟通，顺带验证「其他」待办说明的显隐
  await advanceOpportunity(page, {
    stage: "商务沟通",
    result: "已核实数量、规格、质量和交期，可以进入商务沟通。",
    nextAction: "其他",
    remark: `${prefix}_创建待办说明`,
    planAt: "2026-12-20T10:30:00",
    confirmText: "确认推进",
  });
  // 上一行提交的是「其他」，改为常规动作再提交一次
  await advanceOpportunity(page, {
    result: "商务条件初步沟通完成，等待对方反馈。",
    nextAction: "等待对方反馈",
    planAt: "2026-12-20T10:30:00",
  });
  await expectToast(page, "撮合机会已推进");
  await waitIdle(page);
  await expectVisibleText(matchDrawer(page), "商务沟通");
  await expectVisibleText(matchDrawer(page), "等待对方反馈");
  await shot(page, "crm-05-match-opportunity", "07-opportunity-advanced");

  // 我的待办：撮合机会行可原地推进
  await openMenu(page, "我的待办", /#\/crm\/follow-tasks$/);
  await resetFilters(page);
  await chooseSelectByLabel(page, page, "对象类型", "撮合机会");
  await page.getByRole("button", { name: "搜索", exact: true }).click();
  await waitIdle(page);
  const opportunityTaskRow = tableBody(page).locator("tbody tr").filter({ hasText: data.vendorName }).first();
  await expect(opportunityTaskRow).toContainText("撮合机会");
  await expect(opportunityTaskRow).toContainText("等待对方反馈");
  await opportunityTaskRow.getByRole("button", { name: "推进" }).click();
  await expect(page.getByRole("dialog", { name: "推进机会" })).toBeVisible();
  await page.getByRole("dialog", { name: "推进机会" }).getByRole("button", { name: "取消" }).click();
  await waitIdle(page);

  // 回到机会详情：暂停 -> 恢复并推进
  await openMenu(page, "撮合机会", /#\/crm\/match-opportunity$/);
  await chooseSelectByLabel(page, page, "发起方向", "药厂");
  await fillByLabel(page, "品类", data.productName);
  await page.getByRole("button", { name: "搜索", exact: true }).click();
  await waitIdle(page);
  const demandOpportunityRow = tableBody(page).locator("tbody tr").filter({ hasText: data.vendorName }).first();
  await expect(demandOpportunityRow).toContainText("药厂");
  await demandOpportunityRow.getByRole("button", { name: "详情" }).click();
  await expect(matchDrawer(page).getByText("撮合机会详情", { exact: true })).toBeVisible();

  await matchDrawer(page).getByRole("button", { name: "暂停" }).click();
  await expectToast(page, "撮合机会已暂停");
  await waitIdle(page);
  await expectVisibleText(matchDrawer(page), "暂停");
  await expect(matchDrawer(page).getByRole("button", { name: "追加候选" })).toHaveCount(0);

  await advanceOpportunity(page, {
    result: "恢复机会，继续沟通价格和交付条件。",
    nextAction: "沟通价格",
    planAt: "2026-12-21T10:30:00",
    confirmText: "恢复并推进",
  });
  await expectToast(page, "撮合机会已恢复并推进");
  await waitIdle(page);
  await expectVisibleText(matchDrawer(page), "推进中");

  await advanceOpportunity(page, {
    stage: "意向确认",
    result: "商务条件已确认，进入合作意向确认。",
    nextAction: "确认中选供应",
    planAt: "2026-12-22T10:30:00",
  });
  await expectToast(page, "撮合机会已推进");
  await waitIdle(page);
  await expectVisibleText(matchDrawer(page), "意向确认");

  // 关闭：先验证未成交拦截，再走撮合成功
  await matchDrawer(page).getByRole("button", { name: "关闭", exact: true }).click();
  dialog = page.getByRole("dialog", { name: "关闭撮合机会" });
  await chooseSelectByLabel(page, dialog, "关闭结果", "未成交");
  await fillByLabel(dialog, "关闭原因", data.closeReason);
  await dialog.getByRole("button", { name: "确认关闭" }).click();
  await expectToast(page, "未成交或需求取消前请先将中选候选改为保留");
  await dialog.getByRole("button", { name: "取消", exact: true }).click();
  await waitIdle(page);

  await matchDrawer(page).getByRole("button", { name: "关闭", exact: true }).click();
  dialog = page.getByRole("dialog", { name: "关闭撮合机会" });
  await chooseSelectByLabel(page, dialog, "关闭结果", "撮合成功");
  await dialog.getByRole("button", { name: "确认关闭" }).click();
  await expectToast(page, "撮合机会已关闭");
  await waitIdle(page);
  await expectVisibleText(matchDrawer(page), "已成交");
  await refreshMatchDrawer(page);
  for (const action of ["创建机会", "加入候选", "保留候选", "中选候选", "淘汰候选", "恢复候选", "推进", "暂停", "关闭"]) {
    await expectVisibleText(matchDrawer(page), action);
  }
  await shot(page, "crm-05-match-opportunity", "08-closed-and-refreshed");

  // 列表回查：已关闭 + 撮合成功显示「已成交」
  await openMenu(page, "撮合机会", /#\/crm\/match-opportunity$/);
  await resetFilters(page);
  await chooseSelectByLabel(page, page, "状态", "已关闭");
  await chooseSelectByLabel(page, page, "发起方向", "药厂");
  await fillByLabel(page, "品类", data.productName);
  await page.getByRole("button", { name: "搜索", exact: true }).click();
  await waitIdle(page);
  const closedDemandRow = tableBody(page).locator("tbody tr").filter({ hasText: data.vendorName }).first();
  await expect(closedDemandRow).toBeVisible();
  await expect(closedDemandRow).toContainText("已成交");
  await expect(closedDemandRow).toContainText(data.baseName);
  await chooseSelectByLabel(page, page, "发起方向", "基地");
  await page.getByRole("button", { name: "搜索", exact: true }).click();
  await waitIdle(page);
  const closedSupplyRow = tableBody(page).locator("tbody tr").filter({ hasText: data.baseName }).first();
  await expect(closedSupplyRow).toBeVisible();
  await expect(closedSupplyRow).toContainText("基地");
  await expect(closedSupplyRow).toContainText("已关闭");
  await shot(page, "crm-05-match-opportunity", "09-opportunity-list");

  // 采购列表回查：撮合成功关闭后不再可发起
  await openMenu(page, "采购列表", /#\/crm\/purchase/);
  await resetFilters(page);
  await demandSearch.fill(data.demandName);
  await page.getByRole("button", { name: "搜索", exact: true }).click();
  await waitIdle(page);
  demandRow = tableBody(page).locator("tbody tr").filter({ hasText: data.demandName }).first();
  await expect(demandRow).toContainText("已采购完成");
  await expect(demandRow.getByRole("button", { name: "开始撮合" })).toHaveCount(0);
  await shot(page, "crm-05-match-opportunity", "10-demand-progress-recheck");

  const knownNoise = [
    /未成交或需求取消前请先将中选候选改为保留/,
    /Unhandled error during execution of native event handler/,
    /ResizeObserver loop/,
    /^未处理异常：Object$/,
    /favicon\.ico/,
  ];
  const relevantErrors = browserErrors.filter(error => !knownNoise.some(pattern => pattern.test(error)));
  expect(relevantErrors, relevantErrors.join("\n")).toEqual([]);
});

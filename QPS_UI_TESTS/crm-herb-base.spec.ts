import { expect, test } from "../QPS_WEB_ADMIN/node_modules/@playwright/test";
import { attachFailureWatch, setupMockedApp, mockLoginApi, mockCommonApis, mockHerbBaseOperationApi, loginWithMockedAdmin, crmPermissions, herbBaseId, ok, listPayload } from "./helpers";

test.describe("crm herb base UI", () => {
  test("herb base detail and key dialogs are usable", async ({ page }) => {
    const failures = attachFailureWatch(page);
    await setupMockedApp(page);
    await loginWithMockedAdmin(page, crmPermissions);
    await page.goto("/#/crm/herb-base");

    await page.getByRole("button", { name: "详情" }).first().click();
    const drawer = page.getByRole("dialog").filter({ has: page.getByRole("heading", { name: "基地明细" }) });
    await expect(drawer).toBeVisible();
    await expect(drawer.getByRole("heading", { name: "Codex测试药材公司" })).toBeVisible();
    await expect(drawer.getByText("Codex测试药材公司")).toBeVisible();
    await expect(drawer.locator(".head-meta")).toContainText("甘肃省定西市陇西县");
    await expect(drawer.locator(".detail-profile-panel")).toContainText("首阳镇中药材市场1号");
    await expect(drawer.locator(".detail-profile-panel")).toContainText("甘肃省 / 定西市 / 陇西县");
    await expect(drawer.getByText("张经理").first()).toBeVisible();
    await expect(drawer.getByText("13800000001").first()).toBeVisible();
    await expect(drawer.getByText("zhang-crm")).toBeVisible();
    await expect(drawer.getByText("客户确认下周继续沟通采购计划")).toBeVisible();
    await expect(drawer.getByText("销售一号 至 销售二号")).toBeVisible();
    await expect(drawer.getByText("批量分配跟进")).toBeVisible();
    await expect(drawer.getByRole("heading", { name: "基地明细" })).toBeVisible();
    await expect(drawer.getByRole("heading", { name: "联系人" })).toBeVisible();
    await expect(drawer.getByRole("heading", { name: "沟通记录" })).toBeVisible();
    await expect(drawer.getByRole("heading", { name: "流转记录" })).toBeVisible();
    await expect(drawer.locator(".head-main .eyebrow")).toHaveCount(0);
    await expect(drawer.locator(".drawer-layout")).toHaveCSS("background-color", "rgb(255, 255, 255)");
    await expect(drawer.locator(".summary-card")).toHaveCount(6);
    await expect(drawer.locator(".detail-card")).toHaveCount(4);
    await expect(drawer.locator(".detail-main-products")).toHaveCount(1);
    await expect(drawer.locator(".detail-summary-owner")).toContainText("张经理");
    await expect(drawer.locator(".detail-summary-owner")).toContainText("13800000001");
    await expect(drawer.locator(".drawer-head")).toContainText("Codex测试药材公司");
    await expect(drawer.locator(".drawer-head")).toContainText("重点跟进");
    await expect(drawer.locator(".detail-contacts-panel")).toContainText("张经理");
    await expect(drawer.locator(".detail-contacts-panel")).toContainText("zhang-crm");
    await expect(drawer.locator(".detail-follow-panel")).toContainText("客户确认下周继续沟通采购计划");
    await expect(drawer.locator(".detail-transfer-panel")).toContainText("操作人");
    await expect(drawer.locator(".detail-transfer-panel")).toContainText("测试管理员");

    await drawer.getByRole("button", { name: "新增联系人" }).click();
    await expect(page.getByRole("dialog", { name: "新增联系人" })).toBeVisible();
    await page.getByRole("dialog", { name: "新增联系人" }).getByRole("button", { name: "取消" }).click();

    await drawer.getByRole("button", { name: "记录", exact: true }).click();
    await expect(page.getByRole("dialog", { name: "记录沟通" })).toBeVisible();
    await page.getByRole("dialog", { name: "记录沟通" }).getByRole("button", { name: "取消" }).click();

    await drawer.getByRole("button", { name: "编辑主体" }).click();
    await expect(page.getByRole("dialog", { name: "编辑主体" })).toBeVisible();
    await expect(page.getByRole("dialog", { name: "编辑主体" }).getByText("来源ID")).toHaveCount(0);
    await page.getByRole("dialog", { name: "编辑主体" }).getByRole("button", { name: "取消" }).click();

    await page.keyboard.press("Escape");
    await expect(drawer).toBeHidden();
    expect(failures).toEqual([]);
  });

  test("herb base subject detail title uses subject name without mixing base detail name", async ({ page }) => {
    const failures = attachFailureWatch(page);
    await mockLoginApi(page, crmPermissions);
    await mockCommonApis(page);
    await mockHerbBaseOperationApi(page);
    await page.route("**/api/admin/crm/**", async route => {
      const request = route.request();
      const url = new URL(request.url());
      const pathname = url.pathname;

      const baseWithoutBaseName = {
        id: herbBaseId,
        subjectName: "Codex主体兜底名称",
        subjectType: "合作社",
        mainProducts: ["HUANG_QI"],
        grade: "A",
        score: 88,
        status: "FOLLOWING",
        ownerUserName: "销售一号",
        remark: "重点跟进",
        primaryContactName: "张经理",
        primaryContactPhone: "13800000001",
        lastFollowAt: "2026-07-27T09:30:00",
        lastFollowResult: "INTERESTED",
        nextFollowAt: "2026-07-30T10:00:00",
        baseCount: 1,
        totalScale: 120,
        regions: ["甘肃省定西市陇西县"],
        herbBases: [{
          id: "11111111-aaaa-aaaa-aaaa-111111111111",
          baseName: "",
          herbBaseName: "",
          mainProducts: ["HUANG_QI"],
          scale: 120,
          province: "甘肃省",
          city: "定西市",
          area: "陇西县",
          address: "首阳镇中药材市场1号",
          sourcePlatform: "BAIDU_MAP"
        }],
        contacts: [],
        followRecords: [],
        transferRecords: []
      };

      if (request.method() === "GET" && pathname === "/api/admin/crm/herb-base-subjects") {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify(listPayload([baseWithoutBaseName]))
        });
        return;
      }

      if (request.method() === "GET" && pathname === `/api/admin/crm/herb-base-subjects/${herbBaseId}`) {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify(ok(baseWithoutBaseName))
        });
        return;
      }

      await route.fallback();
    });
    await loginWithMockedAdmin(page, crmPermissions);
    await page.goto("/#/crm/herb-base");

    const row = page.locator(".el-table__body-wrapper tr", { hasText: "Codex主体兜底名称" });
    await expect(row.locator("td").nth(1)).toContainText("Codex主体兜底名称");
    await expect(row.locator("td").nth(2)).toContainText("1");

    await row.getByRole("button", { name: "详情" }).first().click();
    const drawer = page.getByRole("dialog").filter({ has: page.getByRole("heading", { name: "基地明细" }) });
    await expect(drawer.getByRole("heading", { name: "Codex主体兜底名称" })).toBeVisible();
    expect(failures).toEqual([]);
  });

  test("herb base detail renders empty related sections without runtime failures", async ({ page }) => {
    const failures = attachFailureWatch(page);
    await mockLoginApi(page, crmPermissions);
    await mockCommonApis(page);
    await mockHerbBaseOperationApi(page);
    await page.route("**/api/admin/crm/**", async route => {
      const request = route.request();
      const pathname = new URL(request.url()).pathname;
      const emptySubject = {
        id: herbBaseId,
        subjectName: "Codex空数据主体",
        subjectType: "合作社",
        mainProducts: [],
        grade: "B",
        score: 0,
        status: "PENDING",
        ownerUserName: "",
        remark: "",
        primaryContactName: "",
        primaryContactPhone: "",
        lastFollowResult: "",
        nextFollowAt: null,
        baseCount: 0,
        totalScale: null,
        regions: [],
        herbBases: [],
        contacts: [],
        followRecords: [],
        transferRecords: []
      };

      if (request.method() === "GET" && pathname === "/api/admin/crm/herb-base-subjects") {
        await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(listPayload([emptySubject])) });
        return;
      }

      if (request.method() === "GET" && pathname === `/api/admin/crm/herb-base-subjects/${herbBaseId}`) {
        await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(ok(emptySubject)) });
        return;
      }

      await route.fallback();
    });
    await loginWithMockedAdmin(page, crmPermissions);
    await page.goto("/#/crm/herb-base");

    await page.locator(".el-table__body-wrapper tr", { hasText: "Codex空数据主体" }).getByRole("button", { name: "详情" }).first().click();
    const drawer = page.getByRole("dialog").filter({ has: page.getByRole("heading", { name: "基地明细" }) });
    await expect(drawer.getByRole("heading", { name: "Codex空数据主体" })).toBeVisible();
    await expect(drawer.getByText("暂无基地明细")).toBeVisible();
    await expect(drawer.locator(".detail-contacts-panel")).toContainText("No Data");
    await expect(drawer.getByText("暂无沟通记录")).toBeVisible();
    await expect(drawer.getByText("暂无流转记录")).toBeVisible();
    expect(failures).toEqual([]);
  });

  test("herb base forms show required validation messages", async ({ page }) => {
    const failures = attachFailureWatch(page);
    await mockLoginApi(page, crmPermissions);
    await mockCommonApis(page);
    await mockHerbBaseOperationApi(page);
    await loginWithMockedAdmin(page, crmPermissions);
    await page.goto("/#/crm/herb-base");

    await page.getByRole("button", { name: "新增药材基地" }).click();
    await page.getByRole("dialog", { name: "新增基地" }).getByRole("button", { name: "创建" }).click();
    await expect(page.getByText("请输入基地名称")).toBeVisible();
    await page.getByRole("dialog", { name: "新增基地" }).getByRole("button", { name: "取消" }).click();

    await page.getByRole("button", { name: "详情" }).first().click();
    const drawer = page.locator(".customer-drawer");
    await drawer.getByRole("button", { name: "编辑主体" }).click();
    const subjectDialog = page.getByRole("dialog", { name: "编辑主体" });
    await subjectDialog.getByPlaceholder("请输入主体名称").fill("");
    await subjectDialog.getByRole("button", { name: "保存" }).click();
    await expect(page.getByText("请输入主体名称")).toBeVisible();
    await subjectDialog.getByRole("button", { name: "取消" }).click();

    await drawer.getByRole("button", { name: "新增联系人" }).click();
    await page.getByRole("dialog", { name: "新增联系人" }).getByRole("button", { name: "保存" }).click();
    await expect(page.getByText("请填写联系人姓名或电话")).toBeVisible();
    await page.getByRole("dialog", { name: "新增联系人" }).getByRole("button", { name: "取消" }).click();

    await drawer.getByRole("button", { name: "记录", exact: true }).click();
    await page.getByRole("dialog", { name: "记录沟通" }).getByRole("button", { name: "保存" }).click();
    await expect(page.getByText("请选择沟通结果")).toBeVisible();
    expect(failures).toEqual([]);
  });

  test("herb base assignment permission hides and shows controls", async ({ page }) => {
    const permissionsWithoutAssign = crmPermissions.filter(permission => permission !== "CRM_HERB_BASE_ASSIGN");
    const failures = attachFailureWatch(page);
    await mockCommonApis(page);
    await mockHerbBaseOperationApi(page);
    await loginWithMockedAdmin(page, permissionsWithoutAssign);
    await page.goto("/#/crm/herb-base");

    await expect(page.getByRole("button", { name: "分配" })).toHaveCount(0);
    expect(failures).toEqual([]);
  });

  test("herb base operations submit add edit assign contact follow and status changes", async ({ page }) => {
    const failures = attachFailureWatch(page);
    await mockLoginApi(page, crmPermissions);
    await mockCommonApis(page);
    await mockHerbBaseOperationApi(page);
    await loginWithMockedAdmin(page, crmPermissions);
    await page.goto("/#/crm/herb-base");

    await page.getByRole("button", { name: "新增药材基地" }).click();
    const addDialog = page.getByRole("dialog", { name: "新增基地" });
    await expect(addDialog).toBeVisible();
    await addDialog.getByPlaceholder("请输入基地名称").fill("新增测试基地");
    await addDialog.getByRole("button", { name: "创建" }).click();
    await expect(page.getByText("创建成功")).toBeVisible();

    await page.getByRole("button", { name: "详情" }).first().click();
    const subjectDrawer = page.locator(".customer-drawer");
    await expect(subjectDrawer).toBeVisible();
    await subjectDrawer.getByRole("button", { name: "编辑主体" }).click();
    const editDialog = page.getByRole("dialog", { name: "编辑主体" });
    await expect(editDialog).toBeVisible();
    await editDialog.getByPlaceholder("请输入主体名称").fill("Codex测试药材公司编辑");
    await editDialog.getByRole("button", { name: "保存" }).click();
    await expect(page.getByText("主体已保存")).toBeVisible();

    const drawer = subjectDrawer;
    await expect(drawer).toBeVisible();

    await drawer.getByRole("button", { name: "新增联系人" }).click();
    const addContactDialog = page.getByRole("dialog", { name: "新增联系人" });
    await addContactDialog.getByPlaceholder("联系人姓名").fill("赵主任");
    await addContactDialog.getByPlaceholder("联系电话").fill("13800000003");
    await addContactDialog.getByPlaceholder("微信号").fill("zhao-crm");
    await addContactDialog.getByRole("button", { name: "保存" }).click();
    await expect(page.getByText("联系人已保存")).toBeVisible();
    await expect(drawer.getByText("赵主任")).toBeVisible();
    await expect(drawer.getByText("13800000003")).toBeVisible();

    await drawer.locator(".el-table__body-wrapper tr", { hasText: "王采购" }).getByRole("button", { name: "设为主" }).click();
    await expect(page.getByText("主联系人已更新")).toBeVisible();
    await expect(drawer.getByText("王采购").first()).toBeVisible();

    await drawer.getByRole("button", { name: "记录", exact: true }).click();
    const followDialog = page.getByRole("dialog", { name: "记录沟通" });
    await followDialog.locator(".el-form-item", { hasText: "结果" }).locator(".el-select").click();
    await page.getByText("已接通").click();
    await followDialog.getByPlaceholder("记录销售跟进要点").fill("新增测试沟通内容");
    await followDialog.getByRole("button", { name: "保存" }).click();
    await expect(page.getByText("沟通记录已保存")).toBeVisible();
    await expect(drawer.getByText("新增测试沟通内容")).toBeVisible();

    await drawer.getByRole("button", { name: "分配" }).click();
    const assignDialog = page.getByRole("dialog", { name: "分配负责人" });
    await assignDialog.locator(".el-select").click();
    await page.locator(".el-select-dropdown__item", { hasText: "销售二号" }).click();
    await assignDialog.getByPlaceholder("请输入分配备注").fill("操作流分配备注");
    await assignDialog.getByRole("button", { name: "保存" }).click();
    await expect(page.getByText("分配成功")).toBeVisible();
    await expect(drawer.getByText("销售二号").first()).toBeVisible();

    await drawer.getByRole("button", { name: "标记成交" }).click();
    await expect(page.getByText("药材基地状态已更新").first()).toBeVisible();
    await expect(drawer.getByText("已成交").first()).toBeVisible();
    await drawer.getByRole("button", { name: "标记流失" }).click();
    await expect(page.getByText("药材基地状态已更新").first()).toBeVisible();
    await expect(drawer.getByText("已流失").first()).toBeVisible();

    expect(failures).toEqual([]);
  });
});

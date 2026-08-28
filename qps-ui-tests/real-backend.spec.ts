import { expect, test, type APIRequestContext } from "../QPS_WEB_ADMIN/node_modules/@playwright/test";
import { attachFailureWatch, realLogin, realApiBaseURL, pick } from "./helpers";

async function findSubjectWithContacts(request: APIRequestContext, headers: Record<string, string>) {
  for (let page = 1; page <= 30; page += 1) {
    const listResponse = await request.get(`${realApiBaseURL}/admin/crm/herb-base-subjects?page=${page}&pageSize=10`, { headers });
    expect(listResponse.ok(), await listResponse.text()).toBeTruthy();

    const listBody = await listResponse.json();
    expect(listBody.code).toBe(200);

    for (const item of listBody.data?.list || []) {
      const contactsResponse = await request.get(`${realApiBaseURL}/admin/crm/herb-base-subjects/${item.id}/contacts`, { headers });
      expect(contactsResponse.ok(), await contactsResponse.text()).toBeTruthy();

      const contactsBody = await contactsResponse.json();
      expect(contactsBody.code).toBe(200);

      const contacts = contactsBody.data || [];
      if (contacts.length > 0) {
        return { item, contacts };
      }
    }
  }

  return null;
}

test.describe("real backend UI smoke", () => {
  test("real address region maintenance has region API data and renders it", async ({ page, request }) => {
    const failures = attachFailureWatch(page);
    const token = await realLogin(request);
    const headers = { Authorization: `Bearer ${token}` };

    const regionResponse = await request.get(`${realApiBaseURL}/admin/china-regions?activeOnly=false`, { headers });
    expect(regionResponse.ok(), await regionResponse.text()).toBeTruthy();
    const regionBody = await regionResponse.json();
    expect(regionBody.code).toBe(200);
    const regions = regionBody.data || [];
    expect(regions.length, "地址区域维护真实接口 /admin/china-regions 应返回至少一条中国行政区划数据").toBeGreaterThan(0);

    const firstRegion = regions[0];
    const firstRegionCode = pick(firstRegion, "code", "Code");
    const firstRegionName = pick(firstRegion, "name", "Name");
    expect(firstRegionCode).toBeTruthy();
    expect(firstRegionName).toBeTruthy();

    await page.goto("/login");
    await page.evaluate(() => window.localStorage.clear());
    await page.reload();
    await page.getByRole("button", { name: /登录/ }).click();
    await expect(page).toHaveURL(/#\/home\/index/);

    await page.goto("/#/system/region");
    await expect(page.getByPlaceholder("请输入区域编码")).toBeVisible();
    await expect(page.locator(".el-table__body-wrapper")).toContainText(firstRegionCode);
    await expect(page.locator(".el-table__body-wrapper")).toContainText(firstRegionName);
    expect(failures).toEqual([]);
  });

  test("real API exposes every endpoint required by herb base detail", async ({ request }) => {
    const token = await realLogin(request);
    const headers = { Authorization: `Bearer ${token}` };

    const listResponse = await request.get(`${realApiBaseURL}/admin/crm/herb-base-subjects?page=1&pageSize=1`, { headers });
    expect(listResponse.ok(), await listResponse.text()).toBeTruthy();

    const listBody = await listResponse.json();
    expect(listBody.code).toBe(200);
    const firstSubject = listBody.data?.list?.[0];
    expect(firstSubject?.id).toBeTruthy();

    const detailEndpoints = [
      `/admin/crm/herb-base-subjects/${firstSubject.id}`,
      `/admin/crm/herb-base-subjects/${firstSubject.id}/contacts`,
      `/admin/crm/herb-base-subjects/${firstSubject.id}/follow-records`
    ];

    for (const endpoint of detailEndpoints) {
      const response = await request.get(`${realApiBaseURL}${endpoint}`, { headers });
      expect(response.ok(), `${endpoint} -> ${response.status()} ${await response.text()}`).toBeTruthy();

      const body = await response.json();
      expect(body.code, endpoint).toBe(200);
    }
  });

  test("real frontend opens herb base detail drawer and renders real API data", async ({ page, request }) => {
    const failures = attachFailureWatch(page);
    const token = await realLogin(request);
    const headers = { Authorization: `Bearer ${token}` };
    const candidate = await findSubjectWithContacts(request, headers);
    expect(candidate, "真实库前 30 页应至少有一条带联系人数据的基地主体，用于覆盖详情页联系人渲染").toBeTruthy();
    const expectedListItem = candidate!.item;

    const [detailResponse, contactsResponse, followRecordsResponse] = await Promise.all([
      request.get(`${realApiBaseURL}/admin/crm/herb-base-subjects/${expectedListItem.id}`, { headers }),
      request.get(`${realApiBaseURL}/admin/crm/herb-base-subjects/${expectedListItem.id}/contacts`, { headers }),
      request.get(`${realApiBaseURL}/admin/crm/herb-base-subjects/${expectedListItem.id}/follow-records`, { headers })
    ]);
    expect(detailResponse.ok(), await detailResponse.text()).toBeTruthy();
    expect(contactsResponse.ok(), await contactsResponse.text()).toBeTruthy();
    expect(followRecordsResponse.ok(), await followRecordsResponse.text()).toBeTruthy();

    const expectedDetail = (await detailResponse.json()).data;
    const expectedContacts = (await contactsResponse.json()).data || [];
    const expectedFollowRecords = (await followRecordsResponse.json()).data || [];
    const expectedTransfers = expectedDetail?.transferRecords || [];
    const expectedBaseName = pick(expectedDetail, "subjectName", "SubjectName")
      || pick(expectedListItem, "subjectName", "SubjectName");
    const expectedSubjectName = pick(expectedDetail, "subjectName", "SubjectName");
    const expectedAddress = pick(expectedDetail, "address", "Address");
    const expectedPrimaryContactName = pick(expectedDetail, "primaryContactName", "PrimaryContactName");
    const expectedPrimaryContactPhone = pick(expectedDetail, "primaryContactPhone", "PrimaryContactPhone");
    const firstContactName = pick(expectedContacts[0], "contactName", "ContactName");
    const firstContactPhone = pick(expectedContacts[0], "phone", "Phone");
    const firstFollowContent = pick(expectedFollowRecords[0], "content", "Content");
    const firstTransferRemark = pick(expectedTransfers[0], "remark", "Remark");
    const firstTransferOperatorUserName = pick(expectedTransfers[0], "operatorUserName", "OperatorUserName");

    await page.goto("/login");
    await page.evaluate(() => window.localStorage.clear());
    await page.reload();
    await page.getByRole("button", { name: /登录/ }).click();
    await expect(page).toHaveURL(/#\/home\/index/);

    await page.goto("/#/crm/herb-base");
    await expect(page.locator(".el-table__body-wrapper tr").first()).toBeVisible();
    await page.getByPlaceholder("基地 / 主体 / 联系人 / 电话").fill(expectedSubjectName || expectedBaseName);
    await page.getByRole("button", { name: "搜索", exact: true }).click();
    await expect(page.locator(".el-table__body-wrapper")).toContainText(expectedSubjectName || expectedBaseName);
    await page.locator(".el-table__body-wrapper tr", { hasText: expectedSubjectName || expectedBaseName })
      .first()
      .getByRole("button", { name: "详情" })
      .click();

    const drawer = page.locator(".customer-drawer");
    await expect(drawer).toBeVisible();
    expect(expectedContacts.length).toBeGreaterThan(0);
    if (expectedBaseName) await expect(drawer.getByRole("heading", { name: expectedBaseName })).toBeVisible();
    if (expectedSubjectName) await expect(drawer.getByText(expectedSubjectName).first()).toBeVisible();
    if (expectedAddress) await expect(drawer.getByText(expectedAddress).first()).toBeVisible();
    if (expectedPrimaryContactName) await expect(drawer.getByText(expectedPrimaryContactName).first()).toBeVisible();
    if (expectedPrimaryContactPhone) await expect(drawer.getByText(expectedPrimaryContactPhone).first()).toBeVisible();
    if (firstContactName) await expect(drawer.getByText(firstContactName).first()).toBeVisible();
    if (firstContactPhone) await expect(drawer.getByText(firstContactPhone).first()).toBeVisible();
    if (firstFollowContent) await expect(drawer.getByText(firstFollowContent).first()).toBeVisible();
    if (firstTransferRemark) await expect(drawer.getByText(firstTransferRemark).first()).toBeVisible();
    if (expectedTransfers.length > 0) {
      expect(firstTransferOperatorUserName).toBeTruthy();
      await expect(drawer.getByText(`操作人 ${firstTransferOperatorUserName}`).first()).toBeVisible();
    }
    await expect(drawer.getByRole("heading", { name: "基地明细" })).toBeVisible();
    await expect(drawer.getByRole("heading", { name: "联系人" })).toBeVisible();
    await expect(drawer.getByRole("heading", { name: "沟通记录" })).toBeVisible();
    await expect(drawer.getByRole("heading", { name: "流转记录" })).toBeVisible();
    expect(failures).toEqual([]);
  });
});

import { expect, type APIRequestContext, type Page } from "../QPS_WEB_ADMIN/node_modules/@playwright/test";
import { adminPermissions, herbBaseId, loginWithMockedAdmin, mockLoginApi } from "../QPS_WEB_ADMIN/e2e/helpers/mockApi";

const realApiBaseURL = process.env.QPS_REAL_API_URL ?? "http://127.0.0.1:20004/api";
const crmPermissions = [
  ...adminPermissions,
  "SYSTEM_PERMISSION_EDIT",
  "SYSTEM_USER_ADD",
  "SYSTEM_USER_EDIT",
  "CRM_FOLLOW",
  "CRM_HERB_BASE_CONTACT_ADD",
  "CRM_HERB_BASE_CONTACT_EDIT",
  "CRM_HERB_BASE_CONTACT_PRIMARY",
  "CRM_HERB_BASE_STATUS"
];

const ok = (data: unknown) => ({
  code: 200,
  msg: "success",
  data
});

const pageRoutes = [
  { path: "/home/index", title: "首页" },
  { path: "/system/role", title: "角色设置" },
  { path: "/system/permission", title: "权限设置" },
  { path: "/system/users", title: "用户管理" },
  { path: "/system/dataDictionary", title: "数据字典" },
  { path: "/system/region", title: "地址区域维护" },
  { path: "/system/operationLog", title: "操作日志" },
  { path: "/crm/herb-base", title: "基地管理" },
  { path: "/crm/vendor", title: "厂商管理" },
  { path: "/403", title: "403页面" },
  { path: "/404", title: "404页面" },
  { path: "/500", title: "500页面" }
];

const crmVendor = {
  id: "99999999-9999-9999-9999-999999999999",
  vendorName: "Codex测试药材厂商",
  normalizedVendorName: "codex测试药材厂商",
  priorityLevel: "High",
  primaryContactName: "李经理",
  primaryContactPhone: "13900000001",
  purchasePlanCount: 1,
  productCount: 2,
  contactCount: 1,
  latestPurchaseTime: "2026-07-30T09:30:00",
  latestPurchasePlanName: "黄芪年度采购计划",
  remark: "重点厂商",
  createdAt: "2026-07-20T08:00:00",
  updatedAt: "2026-07-30T09:30:00",
  contacts: [
    {
      id: "99999999-9999-9999-9999-999999999991",
      contactName: "李经理",
      phone: "13900000001",
      phoneType: "MOBILE",
      roleName: "PURCHASE",
      status: "VALID",
      isPrimary: true
    }
  ],
  products: [
    { id: "99999999-9999-9999-9999-999999999992", productName: "黄芪", remark: "常采" },
    { id: "99999999-9999-9999-9999-999999999993", productName: "当归", remark: "季节性采购" }
  ],
  purchasePlans: []
};

const listPayload = (list: unknown[] = []) => ok({
  list,
  totalCount: list.length,
  totalPages: 1,
  currentPage: 1,
  pageSize: 10
});

const pick = (item: any, ...keys: string[]) => {
  for (const key of keys) {
    if (item?.[key]) return item[key];
  }
  return "";
};

function attachFailureWatch(page: Page) {
  const failures: string[] = [];

  page.on("console", msg => {
    if (msg.type() === "error") failures.push(`console error: ${msg.text()}`);
  });
  page.on("pageerror", error => failures.push(`page error: ${error.message}`));
  page.on("response", response => {
    if (response.url().includes("/api/") && response.status() >= 400) {
      failures.push(`HTTP ${response.status()}: ${response.url()}`);
    }
  });

  return failures;
}

async function mockCommonApis(page: Page) {
  const roles = [
    { id: "role-admin", name: "管理员", code: "admin" },
    { id: "role-sales", name: "销售", code: "sales" }
  ];
  const users = [
    {
      id: "user-admin",
      username: "admin",
      realName: "系统管理员",
      roleId: "role-admin",
      roleName: "管理员",
      isActive: true,
      createdAt: "2026-07-20T08:00:00"
    }
  ];
  const regions = [
    { id: "region-gansu", code: "620000", name: "甘肃省", fullName: "甘肃省", level: 1, parentId: "", parentName: "", sortOrder: 1, isActive: true },
    { id: "region-dingxi", code: "621100", name: "定西市", fullName: "甘肃省定西市", level: 2, parentId: "region-gansu", parentName: "甘肃省", sortOrder: 1, isActive: true }
  ];
  const dictionaries = [
    { id: "dict-source", code: "SOURCE_PLATFORM", name: "来源平台", value: "BAIDU_MAP", description: "百度地图", parentId: "", parentName: "", sortOrder: 1, isActive: true }
  ];
  const operationLogs = [
    {
      id: "op-1",
      createdAt: "2026-07-31T10:00:00",
      actionType: "Update",
      entityType: "CrmHerbBaseSubject",
      entityId: herbBaseId,
      operatorName: "系统管理员",
      requestPath: "/api/admin/crm/herb-base-subjects",
      ipAddress: "127.0.0.1",
      changeJson: JSON.stringify({ subjectName: { old: "旧主体", new: "新主体" } })
    }
  ];

  await page.route("**/api/admin/permissions/tree", async route => {
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify(ok([
        {
          id: "perm-system",
          permissionCode: "SYSTEM",
          permissionName: "系统设置",
          code: "SYSTEM",
          name: "系统设置",
          permissionType: "MENU",
          children: [
            { id: "perm-system-user", permissionCode: "SYSTEM_USER", permissionName: "用户管理", code: "SYSTEM_USER", name: "用户管理", permissionType: "MENU", children: [] },
            { id: "perm-crm-herb-base", permissionCode: "CRM_HERB_BASE", permissionName: "基地管理", code: "CRM_HERB_BASE", name: "基地管理", permissionType: "MENU", children: [] }
          ]
        }
      ]))
    });
  });

  await page.route("**/api/admin/permissions", async route => {
    if (route.request().method() === "PUT") {
      await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(ok(true)) });
      return;
    }

    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify(ok([{ roleCode: "admin", permissionCodes: ["SYSTEM"] }]))
    });
  });

  await page.route("**/api/admin/china-regions**", async route => {
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify(ok([
        { code: "620000", name: "甘肃省", fullName: "甘肃省", level: 1, parentCode: "", sortOrder: 1, isActive: true },
        { code: "621100", name: "定西市", fullName: "甘肃省定西市", level: 2, parentCode: "620000", sortOrder: 1, isActive: true },
        { code: "621122", name: "陇西县", fullName: "甘肃省定西市陇西县", level: 3, parentCode: "621100", sortOrder: 1, isActive: true }
      ]))
    });
  });

  await page.route("**/api/admin/data-dictionaries/tree", async route => {
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify(ok([]))
    });
  });

  await page.route("**/api/admin/{roles,users,regions,data-dictionaries,operation-logs}**", async route => {
    const request = route.request();
    const pathname = new URL(request.url()).pathname;

    if (request.method() !== "GET") {
      await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(ok(true)) });
      return;
    }

    if (new URL(route.request().url()).pathname.endsWith("/data-dictionaries/tree")) {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(ok([]))
      });
      return;
    }

    const payloadByPath = pathname.includes("/roles")
      ? roles
      : pathname.includes("/users")
        ? users
        : pathname.includes("/regions")
          ? regions
          : pathname.includes("/data-dictionaries")
            ? dictionaries
            : pathname.includes("/operation-logs")
              ? operationLogs
              : [];

    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify(listPayload(payloadByPath))
    });
  });
}

async function mockVendorApi(page: Page) {
  await page.route("**/api/admin/crm/vendors**", async route => {
    const request = route.request();
    const url = new URL(request.url());
    const pathname = url.pathname;

    if (request.method() === "GET" && pathname === `/api/admin/crm/vendors/${crmVendor.id}`) {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(ok(crmVendor))
      });
      return;
    }

    if (request.method() === "GET" && pathname === `/api/admin/crm/vendors/${crmVendor.id}/purchase-plans`) {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(listPayload([
          {
            id: "99999999-9999-9999-9999-999999999994",
            purchasePlanName: "黄芪年度采购计划",
            purchaseTime: "2026-07-30T09:30:00",
            products: "黄芪 100kg",
            pageUrl: "https://example.com"
          }
        ]))
      });
      return;
    }

    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify(listPayload([crmVendor]))
    });
  });
}

async function mockHerbBaseOperationApi(page: Page) {
  const ownerUsers = [
    {
      id: "11111111-1111-1111-1111-111111111111",
      username: "sales01",
      realName: "销售一号",
      role: "Sales",
      isActive: true
    },
    {
      id: "22222222-2222-2222-2222-222222222222",
      username: "sales02",
      realName: "销售二号",
      role: "Sales",
      isActive: true
    }
  ];
  let base: any = {
    id: herbBaseId,
    baseName: "Codex测试种植基地",
    subjectName: "Codex测试药材公司",
    herbBaseName: "Codex测试种植基地",
    mainProduct: "HUANG_QI",
    mainProducts: ["HUANG_QI"],
    grade: "A",
    score: 92,
    province: "甘肃省",
    city: "定西市",
    area: "陇西县",
    address: "首阳镇中药材市场1号",
    lat: null,
    lng: null,
    sourcePlatform: "BAIDU_MAP",
    sourceId: 10001,
    status: "FOLLOWING",
    ownerUserId: "11111111-1111-1111-1111-111111111111",
    ownerUserName: "销售一号",
    remark: "重点跟进",
    primaryContactName: "张经理",
    primaryContactPhone: "13800000001",
    lastFollowAt: "2026-07-27T09:30:00",
    lastFollowResult: "INTERESTED",
    nextFollowAt: "2026-07-30T10:00:00",
    createdAt: "2026-07-20T08:00:00",
    updatedAt: "2026-07-27T09:30:00"
  };
  let contacts = [
    {
      id: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      herbBaseId,
      contactName: "张经理",
      phone: "13800000001",
      phoneType: "MOBILE",
      wechat: "zhang-crm",
      roleName: "OWNER",
      isPrimary: true,
      status: "VALID",
      remark: "",
      createdAt: "2026-07-20T08:00:00",
      updatedAt: "2026-07-20T08:00:00"
    },
    {
      id: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbc",
      herbBaseId,
      contactName: "王采购",
      phone: "13800000002",
      phoneType: "MOBILE",
      wechat: "wang-buy",
      roleName: "PURCHASE",
      isPrimary: false,
      status: "VALID",
      remark: "",
      createdAt: "2026-07-21T08:00:00",
      updatedAt: "2026-07-21T08:00:00"
    }
  ];
  let followRecords = [
    {
      id: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
      herbBaseId,
      contactId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      contactName: "张经理",
      followType: "PHONE",
      followResult: "INTERESTED",
      intentLevel: "A",
      content: "客户确认下周继续沟通采购计划",
      nextFollowAt: "2026-07-30T10:00:00",
      operatorUserId: "11111111-1111-1111-1111-111111111111",
      createdAt: "2026-07-27T09:30:00"
    }
  ];
  let transferRecords = [
    {
      id: "ffffffff-ffff-ffff-ffff-ffffffffffff",
      entityType: "CRM_HERB_BASE",
      entityId: herbBaseId,
      herbBaseId,
      fromOwnerUserId: "11111111-1111-1111-1111-111111111111",
      fromOwnerUserName: "销售一号",
      toOwnerUserId: "22222222-2222-2222-2222-222222222222",
      toOwnerUserName: "销售二号",
      operatorUserId: "11111111-1111-1111-1111-111111111111",
      operatorUserName: "测试管理员",
      remark: "批量分配跟进",
      createdAt: "2026-07-28T10:30:00"
    }
  ];
  const subject = () => ({
    ...base,
    subjectType: "合作社",
    baseCount: base.herbBases?.length ?? 1,
    totalScale: 320,
    regions: ["甘肃省定西市陇西县"],
    herbBases: base.herbBases ?? [
      {
        id: "11111111-aaaa-aaaa-aaaa-111111111111",
        herbBaseSubjectId: herbBaseId,
        baseName: base.baseName,
        herbBaseName: base.herbBaseName,
        mainProducts: base.mainProducts,
        scale: 320,
        province: base.province,
        city: base.city,
        area: base.area,
        address: base.address,
        sourcePlatform: base.sourcePlatform,
        remark: base.remark
      }
    ],
    contacts,
    followRecords,
    transferRecords
  });

  await page.route("**/api/admin/users**", async route => {
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify(ok({
        list: ownerUsers,
        totalCount: ownerUsers.length,
        totalPages: 1,
        currentPage: 1,
        pageSize: 100
      }))
    });
  });

  await page.route("**/api/admin/crm/contacts/**", async route => {
    const request = route.request();
    const pathname = new URL(request.url()).pathname;
    const contactId = pathname.split("/")[4];

    if (request.method() === "PUT") {
      const payload = request.postDataJSON();
      contacts = contacts.map(contact => contact.id === contactId ? { ...contact, ...payload, updatedAt: "2026-07-31T10:00:00" } : contact);
      await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(ok(contacts.find(contact => contact.id === contactId))) });
      return;
    }

    if (request.method() === "PATCH" && pathname.endsWith("/primary")) {
      contacts = contacts.map(contact => ({ ...contact, isPrimary: contact.id === contactId }));
      const primary = contacts.find(contact => contact.id === contactId);
      base = { ...base, primaryContactName: primary?.contactName || "", primaryContactPhone: primary?.phone || "" };
      await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(ok(primary)) });
      return;
    }

    if (request.method() === "PATCH" && pathname.endsWith("/status")) {
      const payload = request.postDataJSON();
      contacts = contacts.map(contact => contact.id === contactId ? { ...contact, status: payload.status, remark: payload.remark } : contact);
      await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(ok(contacts.find(contact => contact.id === contactId))) });
      return;
    }

    await route.fallback();
  });

  await page.route("**/api/admin/crm/**", async route => {
    const request = route.request();
    const url = new URL(request.url());
    const pathname = url.pathname;
    const method = request.method();

    if (pathname.startsWith("/api/admin/crm/herb-base-subjects")) {
      if (method === "PATCH" && pathname === "/api/admin/crm/herb-base-subjects/assign-owner") {
        const payload = request.postDataJSON();
        const owner = ownerUsers.find(user => user.id === payload.ownerUserId);
        const previousOwnerName = base.ownerUserName;
        base = { ...base, ownerUserId: owner?.id ?? null, ownerUserName: owner?.realName ?? null };
        transferRecords = [
          {
            id: "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
            entityType: "CRM_HERB_BASE_SUBJECT",
            entityId: herbBaseId,
            fromOwnerUserName: previousOwnerName,
            toOwnerUserName: owner?.realName ?? "未分配",
            operatorUserName: "测试管理员",
            remark: payload.remark || "",
            createdAt: "2026-07-31T10:00:00"
          },
          ...transferRecords
        ];
        await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(ok([subject()])) });
        return;
      }

      if (method === "GET" && pathname === "/api/admin/crm/herb-base-subjects") {
        await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(listPayload([subject()])) });
        return;
      }

      if (method === "GET" && pathname === `/api/admin/crm/herb-base-subjects/${herbBaseId}`) {
        await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(ok(subject())) });
        return;
      }

      if (method === "PUT" && pathname === `/api/admin/crm/herb-base-subjects/${herbBaseId}`) {
        base = { ...base, ...request.postDataJSON() };
        await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(ok(subject())) });
        return;
      }

      if (method === "POST" && pathname === `/api/admin/crm/herb-base-subjects/${herbBaseId}/contacts`) {
        const contact = {
          id: "cccccccc-cccc-cccc-cccc-cccccccccccc",
          status: "VALID",
          createdAt: "2026-07-31T10:00:00",
          updatedAt: "2026-07-31T10:00:00",
          ...request.postDataJSON()
        };
        contacts = [...contacts, contact];
        await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(ok(contact)) });
        return;
      }

      if (method === "POST" && pathname === `/api/admin/crm/herb-base-subjects/${herbBaseId}/follow-records`) {
        const payload = request.postDataJSON();
        const contact = contacts.find(item => item.id === payload.contactId);
        const record = {
          id: "dddddddd-dddd-dddd-dddd-dddddddddddd",
          contactName: contact?.contactName || "未指定联系人",
          createdAt: "2026-07-31T10:00:00",
          ...payload
        };
        followRecords = [record, ...followRecords];
        base = { ...base, lastFollowResult: payload.followResult, lastFollowAt: record.createdAt, nextFollowAt: payload.nextFollowAt };
        await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(ok(record)) });
        return;
      }
    }

    if (method === "PATCH" && pathname === "/api/admin/crm/herb-bases/assign-owner") {
      const payload = request.postDataJSON();
      const owner = ownerUsers.find(user => user.id === payload.ownerUserId);
      const previousOwnerName = base.ownerUserName;
      base = { ...base, ownerUserId: owner?.id ?? null, ownerUserName: owner?.realName ?? null };
      transferRecords = [
        {
          id: "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
          entityType: "CRM_HERB_BASE",
          entityId: herbBaseId,
          fromOwnerUserName: previousOwnerName,
          toOwnerUserName: owner?.realName ?? "未分配",
          operatorUserName: "测试管理员",
          remark: payload.remark || "",
          createdAt: "2026-07-31T10:00:00"
        },
        ...transferRecords
      ];
      await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(ok([base])) });
      return;
    }

    if (method === "GET" && pathname === `/api/admin/crm/herb-bases/${herbBaseId}/owner-transfers`) {
      await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(ok(transferRecords)) });
      return;
    }

    if (method === "GET" && pathname === `/api/admin/crm/herb-bases/${herbBaseId}/contacts`) {
      await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(ok(contacts)) });
      return;
    }

    if (method === "POST" && pathname === `/api/admin/crm/herb-bases/${herbBaseId}/contacts`) {
      const payload = request.postDataJSON();
      const contact = {
        id: "cccccccc-cccc-cccc-cccc-cccccccccccc",
        herbBaseId,
        status: "VALID",
        createdAt: "2026-07-31T10:00:00",
        updatedAt: "2026-07-31T10:00:00",
        ...payload
      };
      if (contact.isPrimary) {
        contacts = contacts.map(item => ({ ...item, isPrimary: false }));
        base = { ...base, primaryContactName: contact.contactName, primaryContactPhone: contact.phone };
      }
      contacts = [...contacts, contact];
      await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(ok(contact)) });
      return;
    }

    if (method === "GET" && pathname === `/api/admin/crm/herb-bases/${herbBaseId}/follow-records`) {
      await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(ok(followRecords)) });
      return;
    }

    if (method === "POST" && pathname === `/api/admin/crm/herb-bases/${herbBaseId}/follow-records`) {
      const payload = request.postDataJSON();
      const contact = contacts.find(item => item.id === payload.contactId);
      const record = {
        id: "dddddddd-dddd-dddd-dddd-dddddddddddd",
        herbBaseId,
        contactName: contact?.contactName || "未指定联系人",
        createdAt: "2026-07-31T10:00:00",
        ...payload
      };
      followRecords = [record, ...followRecords];
      base = { ...base, lastFollowResult: payload.followResult, lastFollowAt: record.createdAt, nextFollowAt: payload.nextFollowAt };
      await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(ok(record)) });
      return;
    }

    if (method === "GET" && pathname === `/api/admin/crm/herb-bases/${herbBaseId}`) {
      await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(ok(base)) });
      return;
    }

    if (method === "PUT" && pathname === `/api/admin/crm/herb-bases/${herbBaseId}`) {
      const payload = request.postDataJSON();
      base = { ...base, ...payload, baseName: payload.baseName || payload.herbBaseName || base.baseName, herbBaseName: payload.herbBaseName || payload.baseName || base.herbBaseName };
      await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(ok(base)) });
      return;
    }

    if (method === "POST" && pathname === "/api/admin/crm/herb-bases") {
      const payload = request.postDataJSON();
      const created = {
        ...base,
        id: "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
        ...payload,
        baseName: payload.baseName || payload.herbBaseName,
        herbBaseName: payload.herbBaseName || payload.baseName
      };
      await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(ok(created)) });
      return;
    }

    if (method !== "GET" || pathname !== "/api/admin/crm/herb-bases") {
      await route.fallback();
      return;
    }

    await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(listPayload([base])) });
  });
}

async function setupMockedApp(page: Page) {
  await mockLoginApi(page, [...crmPermissions, "CRM_VENDOR"]);
  await mockCommonApis(page);
  await mockHerbBaseOperationApi(page);
  await mockVendorApi(page);
}

async function realLogin(request: APIRequestContext) {
  const response = await request.post(`${realApiBaseURL}/admin/auth/login`, {
    data: {
      username: "admin",
      password: "123456"
    }
  });
  expect(response.ok(), await response.text()).toBeTruthy();

  const body = await response.json();
  expect(body.code).toBe(200);
  expect(body.data?.token).toBeTruthy();
  return body.data.token as string;
}

export {
  adminPermissions,
  herbBaseId,
  loginWithMockedAdmin,
  mockLoginApi,
  realApiBaseURL,
  crmPermissions,
  pageRoutes,
  ok,
  listPayload,
  pick,
  attachFailureWatch,
  mockCommonApis,
  mockVendorApi,
  mockHerbBaseOperationApi,
  setupMockedApp,
  realLogin
};

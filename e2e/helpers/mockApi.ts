import { expect, type Page } from "@playwright/test";

const ok = (data: unknown) => ({
  code: 200,
  msg: "success",
  data
});

export const herbBaseId = "60892f4d-8811-4724-916d-cc606dc0f022";

export const crmHerbBase = {
  id: herbBaseId,
  parentId: null,
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

const activeUsers = [
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

export const adminPermissions = [
  "HOME",
  "SYSTEM",
  "SYSTEM_ROLE",
  "SYSTEM_ROLE_ADD",
  "SYSTEM_ROLE_EDIT",
  "SYSTEM_ROLE_DELETE",
  "SYSTEM_PERMISSION",
  "SYSTEM_USER",
  "SYSTEM_DATA_DICTIONARY",
  "SYSTEM_DATA_DICTIONARY_ADD",
  "SYSTEM_DATA_DICTIONARY_EDIT",
  "SYSTEM_DATA_DICTIONARY_DELETE",
  "SYSTEM_REGION",
  "SYSTEM_REGION_ADD",
  "SYSTEM_REGION_EDIT",
  "SYSTEM_REGION_DELETE",
  "SYSTEM_OPERATION_LOG",
  "CRM_HERB_BASE",
  "CRM_HERB_BASE_ADD",
  "CRM_HERB_BASE_EDIT",
  "CRM_HERB_BASE_DELETE",
  "CRM_HERB_BASE_ASSIGN"
];

export async function mockLoginApi(page: Page, permissions = adminPermissions, onPermissionsRequest?: () => void) {
  await page.route("**/api/admin/auth/login", async route => {
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify(ok({
        token: "playwright-token",
        userId: "11111111-1111-1111-1111-111111111111",
        username: "admin",
        realName: "测试管理员",
        role: "admin"
      }))
    });
  });

  await page.route("**/api/admin/auth/user-permissions", async route => {
    onPermissionsRequest?.();
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify(ok({ permissions }))
    });
  });
}

export async function mockCrmHerbBaseApi(page: Page) {
  let assignedHerbBase = { ...crmHerbBase };

  await page.route("**/api/admin/users**", async route => {
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify(ok({
        list: activeUsers,
        totalCount: activeUsers.length,
        totalPages: 1,
        currentPage: 1,
        pageSize: 100
      }))
    });
  });

  await page.route("**/api/admin/crm/herb-bases**", async route => {
    const request = route.request();
    const url = new URL(request.url());
    const method = request.method();
    const pathname = url.pathname;

    if (method === "PATCH" && pathname === "/api/admin/crm/herb-bases/assign-owner") {
      const payload = request.postDataJSON();
      const owner = activeUsers.find(user => user.id === payload.ownerUserId);
      assignedHerbBase = {
        ...assignedHerbBase,
        ownerUserId: owner?.id ?? null,
        ownerUserName: owner?.realName ?? null
      };
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(ok([assignedHerbBase]))
      });
      return;
    }

    if (method === "GET" && pathname === `/api/admin/crm/herb-bases/${herbBaseId}/owner-transfers`) {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(ok([
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
        ]))
      });
      return;
    }

    if (method === "GET" && pathname === `/api/admin/crm/herb-bases/${herbBaseId}/contacts`) {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(ok([
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
          }
        ]))
      });
      return;
    }

    if (method === "POST" && pathname === `/api/admin/crm/herb-bases/${herbBaseId}/contacts`) {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(ok({
          id: "cccccccc-cccc-cccc-cccc-cccccccccccc",
          herbBaseId,
          ...request.postDataJSON()
        }))
      });
      return;
    }

    if (method === "GET" && pathname === `/api/admin/crm/herb-bases/${herbBaseId}/follow-records`) {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(ok([
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
        ]))
      });
      return;
    }

    if (method === "POST" && pathname === `/api/admin/crm/herb-bases/${herbBaseId}/follow-records`) {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(ok({
          id: "dddddddd-dddd-dddd-dddd-dddddddddddd",
          herbBaseId,
          ...request.postDataJSON()
        }))
      });
      return;
    }

    if (method === "GET" && pathname === `/api/admin/crm/herb-bases/${herbBaseId}`) {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(ok(assignedHerbBase))
      });
      return;
    }

    if (method === "PUT" && pathname === `/api/admin/crm/herb-bases/${herbBaseId}`) {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(ok({
          ...assignedHerbBase,
          ...request.postDataJSON()
        }))
      });
      return;
    }

    if (method === "POST" && pathname === "/api/admin/crm/herb-bases") {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(ok({
          ...assignedHerbBase,
          id: "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
          ...request.postDataJSON()
        }))
      });
      return;
    }

    if (method !== "GET" || pathname !== "/api/admin/crm/herb-bases") {
      await route.fallback();
      return;
    }

    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify(ok({
        list: [assignedHerbBase],
        totalCount: 1,
        totalPages: 1,
        currentPage: 1,
        pageSize: 10
      }))
    });
  });

  await page.route("**/api/admin/china-regions**", async route => {
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify(ok([
        {
          code: "620000",
          name: "甘肃省",
          fullName: "甘肃省",
          level: 1,
          parentCode: "",
          isActive: true
        },
        {
          code: "621100",
          name: "定西市",
          fullName: "甘肃省定西市",
          level: 2,
          parentCode: "620000",
          isActive: true
        },
        {
          code: "621122",
          name: "陇西县",
          fullName: "甘肃省定西市陇西县",
          level: 3,
          parentCode: "621100",
          isActive: true
        }
      ]))
    });
  });
}

export async function loginWithMockedAdmin(page: Page, permissions = adminPermissions, onPermissionsRequest?: () => void) {
  await mockLoginApi(page, permissions, onPermissionsRequest);
  await page.goto("/login");
  await page.getByRole("button", { name: /登录/ }).click();
  await expect(page).not.toHaveURL(/\/login$/);
}




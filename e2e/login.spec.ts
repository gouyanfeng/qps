import { expect, test } from "@playwright/test";
import { mockLoginApi } from "./helpers/mockApi";

test("login page renders the default login form", async ({ page }) => {
  await page.goto("/login");

  await expect(page.locator(".login-container")).toBeVisible();
  await expect(page.locator('input[placeholder*="admin"]')).toHaveValue("admin");
  await expect(page.locator('input[type="password"]')).toHaveValue("123456");
  await expect(page.getByRole("button", { name: /登录/ })).toBeVisible();
});

test("login redirects to the authenticated app shell", async ({ page }) => {
  await mockLoginApi(page);
  await page.goto("/login");

  await page.getByRole("button", { name: /登录/ }).click();

  await expect(page).toHaveURL(/#\/home\/index/);
  await expect(page.locator("#watermark")).toBeVisible();
});



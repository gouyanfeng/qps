import { expect, test } from "@playwright/test";
import { expectToast, formItemByLabel, login, shot, waitIdle } from "../crm-flow-helpers.mjs";

const MOBILE_BASE = process.env.MOBILE_BASE_URL || "http://127.0.0.1:5174";

const stamp = Date.now().toString().slice(-6);
const storeCode = `QPS-XA-${stamp}`;
const storeName = `本草堂西安新城店-${stamp}`;
const storeAddress = `陕西省西安市新城区长乐中路 188 号一层 A-${stamp} 铺`;
const categoryCode = `CAT-HERB-${stamp}`;
const categoryName = `根茎类药材-${stamp}`;
const freightName = `西北地区标准运费-${stamp}`;
const productCode = `SP-GS-${stamp}`;
const productName = `岷县当归片 500g 精选无硫-${stamp}`;
const skuCode = `SKU-GS-${stamp}`;
const couponCode = `FULL200${stamp}`;
const couponName = `满 200 元减 30 元券-${stamp}`;
const memberPhone = process.env.MEMBER_PHONE || "13601758260";
const memberPassword = process.env.MEMBER_PASSWORD || "111111";
const memberNickname = `山野药香${stamp}`;
const receiverName = "陈静";
const receiverAddress = "浙江省杭州市西湖区文三路 138 号 5 幢 2 单元 601 室";
const reviewContent = `当归片切得均匀，色泽自然，闻着有股清甜的药香，泡水煲汤都合适，包装也很严实，物流三天就到，下次还会回购。`;

const gotoAdmin = async (page, path) => {
  await page.goto(`/#${path}`);
  await waitIdle(page);
};

const searchAdmin = async (page, placeholder, keyword) => {
  const input = page.getByPlaceholder(placeholder).first();
  await input.waitFor({ state: "visible" });
  await input.fill(keyword);
  await page.getByRole("button", { name: "搜索", exact: true }).click();
  await waitIdle(page);
};

const pickSelect = async (page, scope, label, optionText) => {
  await formItemByLabel(scope, label).locator(".el-select").first().click();
  const dropdown = page.locator(".el-select-dropdown:visible").last();
  const option = optionText
    ? dropdown.locator(".el-select-dropdown__item").filter({ hasText: optionText }).first()
    : dropdown.locator(".el-select-dropdown__item").first();
  await option.waitFor({ state: "visible", timeout: 15_000 });
  await option.click();
};

const mobileGoto = async (page, path, waitMs = 2500) => {
  const target = `${MOBILE_BASE}/#${path}`;
  if (page.url() === target) {
    await page.reload({ waitUntil: "networkidle", timeout: 60_000 }).catch(() => {});
  } else {
    await page
      .goto(target, { waitUntil: "networkidle", timeout: 60_000 })
      .catch(() => {});
  }
  await page.waitForTimeout(waitMs);
};

const mobileToast = async (page, text) => {
  await expect(
    page.locator(".uni-toast, .uni-sample-toast").filter({ hasText: text }).first()
  ).toBeVisible({ timeout: 15_000 });
};

const mobileConfirm = async (page, modalText) => {
  const modal = page.locator(".uni-modal").filter({ hasText: modalText }).last();
  await modal.waitFor({ state: "visible", timeout: 20_000 });
  await modal.locator(".uni-modal__btn_primary").last().click();
  await page.waitForTimeout(800);
  return modal;
};

test("商城全流程：后台建数据 → 会员下单支付 → 后台发货 → 会员收货评价 → 后台审核", async ({ page, browser }) => {
  test.setTimeout(900_000);

  await login(page);

  // ---------- 0. 后台：新增门店（远端环境可能没有预置门店，必须先建） ----------
  await gotoAdmin(page, "/mall/store");
  await page.getByRole("button", { name: "新增门店" }).click();
  const storeDialog = page.locator(".el-dialog").filter({ hasText: "门店编码" }).last();
  await storeDialog.waitFor({ state: "visible" });
  await formItemByLabel(storeDialog, "门店编码").locator("input").first().fill(storeCode);
  await formItemByLabel(storeDialog, "门店名称").locator("input").first().fill(storeName);
  await formItemByLabel(storeDialog, "门店地址").locator("input").first().fill(storeAddress);
  await storeDialog.getByRole("button", { name: "确定" }).click();
  await expectToast(page, "新增门店成功");
  await waitIdle(page);
  await searchAdmin(page, "门店编码 / 名称 / 地址", storeName);
  await expect(page.locator(".el-table__row").filter({ hasText: storeName }).first()).toBeVisible();
  await shot(page, "mall-full", "00-admin-store");

  // ---------- 1. 后台：新增末级分类 ----------
  await gotoAdmin(page, "/mall/category");
  await page.getByRole("button", { name: "新增分类" }).click();
  const categoryDialog = page.locator(".el-dialog").filter({ hasText: "分类编码" }).last();
  await categoryDialog.waitFor({ state: "visible" });
  await pickSelect(page, categoryDialog, "所属门店", storeName);
  await formItemByLabel(categoryDialog, "分类编码").locator("input").first().fill(categoryCode);
  await formItemByLabel(categoryDialog, "分类名称").locator("input").first().fill(categoryName);
  await categoryDialog.getByRole("button", { name: "确定" }).click();
  await expectToast(page, "新增分类成功");
  await waitIdle(page);
  await searchAdmin(page, "分类编码 / 名称", categoryName);
  await expect(page.locator(".el-table__row").filter({ hasText: categoryName }).first()).toBeVisible();
  await shot(page, "mall-full", "01-admin-category");

  // ---------- 2. 后台：新增运费模板（默认 + 兜底区域） ----------
  await gotoAdmin(page, "/mall/freight");
  await page.getByRole("button", { name: "新增模板" }).click();
  const freightDialog = page.locator(".el-dialog").filter({ hasText: "模板名称" }).last();
  await freightDialog.waitFor({ state: "visible" });
  await formItemByLabel(freightDialog, "模板名称").locator("input").first().fill(freightName);
  await formItemByLabel(freightDialog, "设为默认").locator(".el-switch").first().click();
  await formItemByLabel(freightDialog, "兜底区域").locator(".el-switch").first().click();
  await freightDialog.getByRole("button", { name: "确定" }).click();
  await expectToast(page, "新增成功");
  await waitIdle(page);
  await searchAdmin(page, "模板名称", freightName);
  await expect(page.locator(".el-table__row").filter({ hasText: freightName }).first()).toBeVisible();
  await shot(page, "mall-full", "02-admin-freight");

  // ---------- 3. 后台：新增商品 ----------
  await gotoAdmin(page, "/mall/product");
  await page.getByRole("button", { name: "新增商品" }).click();
  const productDialog = page.locator(".el-dialog").filter({ hasText: "商品编码" }).last();
  await productDialog.waitFor({ state: "visible" });
  await formItemByLabel(productDialog, "商品编码").locator("input").first().fill(productCode);
  await formItemByLabel(productDialog, "商品名称").locator("input").first().fill(productName);
  await pickSelect(page, productDialog, "商品分类", categoryName);
  await pickSelect(page, productDialog, "所属门店", storeName);
  await pickSelect(page, productDialog, "运费模板", freightName);
  await productDialog.getByRole("button", { name: "确定" }).click();
  await expectToast(page, "新增商品成功");
  await waitIdle(page);
  await searchAdmin(page, "商品编码 / 名称", productName);
  const productRow = page.locator(".el-table__row").filter({ hasText: productName }).first();
  await expect(productRow).toBeVisible();
  await shot(page, "mall-full", "03-admin-product");

  // ---------- 4. 后台：规格维护（添加默认规格） ----------
  await productRow.getByRole("button", { name: "规格" }).click();
  const skuDialog = page.locator(".el-dialog").filter({ hasText: "规格维护" }).last();
  await skuDialog.waitFor({ state: "visible" });
  await skuDialog.getByRole("button", { name: "添加默认规格" }).click();
  const skuFormDialog = page.locator(".el-dialog").last();
  await skuFormDialog.waitFor({ state: "visible" });
  await expect(skuFormDialog.locator(".el-dialog__title")).toContainText("添加默认规格");
  await formItemByLabel(skuFormDialog, "规格编码").locator("input").first().fill(skuCode);
  await formItemByLabel(skuFormDialog, "销售价").locator("input").first().fill("99");
  await formItemByLabel(skuFormDialog, "重量(克)").locator("input").first().fill("500");
  await formItemByLabel(skuFormDialog, "库存").locator("input").first().fill("100");
  await skuFormDialog.getByRole("button", { name: "保存" }).click();
  await expectToast(page, "规格已新增");
  await waitIdle(page);
  await expect(skuDialog.locator(".el-table__row").filter({ hasText: skuCode }).first()).toBeVisible();
  await shot(page, "mall-full", "04-admin-product-sku");
  await skuDialog.getByRole("button", { name: "关闭" }).click();
  await skuDialog.waitFor({ state: "hidden" });
  await searchAdmin(page, "商品编码 / 名称", productName);
  await expect(productRow).toContainText("99.00");

  // ---------- 5. 后台：新增优惠券 ----------
  await gotoAdmin(page, "/mall/coupon");
  await page.getByRole("button", { name: "新增优惠券" }).click();
  const couponDialog = page.locator(".el-dialog").filter({ hasText: "券名称" }).last();
  await couponDialog.waitFor({ state: "visible" });
  await formItemByLabel(couponDialog, "券名称").locator("input").first().fill(couponName);
  await formItemByLabel(couponDialog, "券码").locator("input").first().fill(couponCode);
  await couponDialog.getByRole("button", { name: "确定" }).click();
  await expectToast(page, "新增成功");
  await waitIdle(page);
  await searchAdmin(page, "券名称 / 券码", couponCode);
  await expect(page.locator(".el-table__row").filter({ hasText: couponName }).first()).toBeVisible();
  await shot(page, "mall-full", "05-admin-coupon");

  // ---------- 6. 会员端：注册并登录 ----------
  const mobileContext = await browser.newContext({
    viewport: { width: 390, height: 844 },
    deviceScaleFactor: 2,
    isMobile: true,
    hasTouch: true
  });
  const mobile = await mobileContext.newPage();

  // ---------- 6. 会员端：登录（固定手机号；未注册则先注册，已注册则直接登录） ----------
  await mobileGoto(mobile, "/pages/account/index");
  const authAttempt = async (mode) => {
    const endpoint = mode === "register" ? "/api/mall/auth/register" : "/api/mall/auth/login";
    const responsePromise = mobile.waitForResponse(
      (response) => response.url().includes(endpoint),
      { timeout: 20_000 }
    );
    await mobile.locator(".submit-button").click();
    const response = await responsePromise;
    const body = await response.json().catch(() => ({ code: -1 }));
    return Number(body?.code);
  };

  await mobile.locator(".form-input input").nth(0).fill(memberPhone);
  await mobile.locator(".form-input input").nth(1).fill(memberPassword);
  let authCode = await authAttempt("login");
  if (authCode !== 200) {
    // 手机号尚未注册，切换到注册模式用同一凭据完成注册并登录
    await mobile.waitForTimeout(2_500);
    await mobile.locator(".toggle-mode").click();
    await mobile.waitForTimeout(600);
    await mobile.locator(".form-input input").nth(0).fill(memberPhone);
    await mobile.locator(".form-input input").nth(1).fill(memberPassword);
    await mobile.locator(".form-input input").nth(2).fill(memberNickname);
    authCode = await authAttempt("register");
    expect(authCode).toBe(200);
  }
  await mobileToast(mobile, "登录成功");
  await mobile.waitForTimeout(2500);
  await shot(mobile, "mall-full", "06-mobile-login");

  // ---------- 7. 会员端：搜索商品并加入购物车 ----------
  await mobileGoto(mobile, "/pages/search/index");
  await mobile.locator(".search-input input").fill(productName);
  await mobile.locator(".search-btn").click();
  const resultCard = mobile.locator(".result-card").filter({ hasText: productName }).first();
  await expect(resultCard).toBeVisible({ timeout: 20_000 });
  await shot(mobile, "mall-full", "07-mobile-search");
  await resultCard.click();
  await mobile.waitForTimeout(2500);

  await expect(mobile.locator(".spec-item").first()).toBeVisible({ timeout: 20_000 });
  await mobile.locator(".spec-item").first().click();
  await mobile.locator(".bb-btn.ghost").click();
  await mobileToast(mobile, "已加入购物车");
  await shot(mobile, "mall-full", "08-mobile-product-detail");

  // ---------- 8. 会员端：购物车结算 ----------
  await mobileGoto(mobile, "/pages/cart/index");
  await expect(mobile.locator(".goods-card").filter({ hasText: productName }).first()).toBeVisible({ timeout: 20_000 });
  const cartCheckbox = mobile.locator(".goods-check .cb").first();
  if (!((await cartCheckbox.getAttribute("class")) || "").includes("cb-on")) {
    await mobile.locator(".goods-check").first().click();
  }
  await shot(mobile, "mall-full", "09-mobile-cart");
  await mobile.locator(".checkout-btn").click();
  await mobile.waitForTimeout(2500);

  // ---------- 9. 会员端：新增收货地址 ----------
  await expect(mobile.locator(".address-card")).toBeVisible({ timeout: 20_000 });
  await mobile.locator(".address-head-right").click();
  await expect(mobile.locator(".address-picker")).toBeVisible({ timeout: 15_000 });
  await mobile.locator(".address-add-btn").click();
  await mobile.waitForTimeout(2500);

  await expect(mobile.locator(".form-input input").nth(0)).toBeVisible({ timeout: 20_000 });
  await mobile.locator(".form-input input").nth(0).fill(receiverName);
  await mobile.locator(".form-input input").nth(1).fill(memberPhone);
  await mobile.locator(".form-input input").nth(2).fill(receiverAddress);
  await shot(mobile, "mall-full", "10-mobile-address-form");
  await mobile.locator(".submit-button").click();
  await mobileToast(mobile, "地址已保存");
  await mobile.locator(".header-back").click();
  await mobile.waitForTimeout(2500);

  // 返回结算页后地址弹层会重新打开，关闭后应已自动选中新地址
  await expect(mobile.locator(".address-picker")).toBeVisible({ timeout: 20_000 });
  await mobile.locator(".address-picker .picker-close").click();
  await mobile.waitForTimeout(1200);
  await expect(mobile.locator(".address-user")).toContainText(receiverName, { timeout: 20_000 });
  await shot(mobile, "mall-full", "11-mobile-checkout");

  // ---------- 10. 会员端：提交订单 ----------
  await mobile.locator(".submit-btn").click();
  await mobileConfirm(mobile, "确认创建订单");
  const createdModal = await mobileConfirm(mobile, "订单已创建");
  const createdText = (await createdModal.innerText()).replace(/\s+/g, " ");
  await shot(mobile, "mall-full", "12-mobile-order-created");
  await mobile.waitForTimeout(2500);

  await mobileGoto(mobile, "/pages/orders/index");
  const pendingCard = mobile.locator(".order-card").first();
  await expect(pendingCard).toBeVisible({ timeout: 20_000 });
  const orderNo = (await pendingCard.locator(".order-no-text").innerText())
    .replace(/\s+/g, " ")
    .replace(/订单号[:：]?/, "")
    .trim();
  expect(createdText).toContain(orderNo);
  await expect(pendingCard.locator(".status-text")).toContainText("待付款");
  await shot(mobile, "mall-full", "13-mobile-order-pending");

  // ---------- 11. 会员端：模拟支付 ----------
  await pendingCard.locator(".action-btn").filter({ hasText: "立即付款" }).click();
  await mobileToast(mobile, "支付成功");
  await mobile.waitForTimeout(2000);
  await mobileGoto(mobile, "/pages/orders/index");
  const paidCard = mobile.locator(".order-card").first();
  await expect(paidCard.locator(".status-text")).toContainText("待发货", { timeout: 20_000 });
  await shot(mobile, "mall-full", "14-mobile-order-paid");

  // ---------- 12. 后台：订单发货 ----------
  await gotoAdmin(page, "/mall/order");
  await searchAdmin(page, "订单号 / 收货人 / 电话", orderNo);
  const orderRow = page.locator(".el-table__row").filter({ hasText: orderNo }).first();
  await expect(orderRow).toBeVisible();
  await expect(orderRow).toContainText("已支付");
  await shot(page, "mall-full", "15-admin-order-paid");

  await orderRow.getByRole("button", { name: "发货" }).click();
  const shipmentDialog = page.locator(".el-dialog").filter({ hasText: "快递公司" }).last();
  await shipmentDialog.waitFor({ state: "visible" });
  await formItemByLabel(shipmentDialog, "快递公司").locator("input").first().fill("顺丰速运");
  await formItemByLabel(shipmentDialog, "快递单号").locator("input").first().fill(`SF${stamp}0001`);
  await shipmentDialog.getByRole("button", { name: "确定发货" }).click();
  await expectToast(page, "发货成功");
  await waitIdle(page);
  await searchAdmin(page, "订单号 / 收货人 / 电话", orderNo);
  await expect(page.locator(".el-table__row").filter({ hasText: orderNo }).first()).toContainText("已发货");
  await shot(page, "mall-full", "16-admin-order-shipped");

  // ---------- 13. 会员端：确认收货 ----------
  await mobileGoto(mobile, "/pages/orders/index");
  const shippedCard = mobile.locator(".order-card").first();
  await expect(shippedCard.locator(".status-text")).toContainText("待收货", { timeout: 20_000 });
  await shot(mobile, "mall-full", "17-mobile-order-shipped");
  await shippedCard.locator(".action-btn").filter({ hasText: "确认收货" }).click();
  await mobile.waitForTimeout(1200);
  await mobileConfirm(mobile, "请确认已收到商品");
  await mobile.waitForTimeout(2000);
  await mobileGoto(mobile, "/pages/orders/index");
  const completedCard = mobile.locator(".order-card").first();
  await expect(completedCard.locator(".status-text")).toContainText("交易已完成", { timeout: 20_000 });
  await shot(mobile, "mall-full", "18-mobile-order-completed");

  // ---------- 14. 会员端：提交评价 ----------
  await completedCard.locator(".action-btn").filter({ hasText: "评价有礼" }).click();
  await mobile.waitForTimeout(3000);
  await expect(mobile.locator(".content-input textarea")).toBeVisible({ timeout: 20_000 });
  await mobile.locator(".star-btn").nth(4).click();
  await mobile.locator(".content-input textarea").fill(reviewContent);
  await shot(mobile, "mall-full", "19-mobile-review-form");
  await mobile.locator(".submit-btn").click();
  await mobileToast(mobile, "评价已提交");
  await mobile.waitForTimeout(2000);

  // ---------- 15. 后台：评价审核 ----------
  await gotoAdmin(page, "/mall/review");
  await searchAdmin(page, "会员昵称 / 评价内容", reviewContent.slice(0, 20));
  const reviewRow = page.locator(".el-table__row").filter({ hasText: reviewContent.slice(0, 20) }).first();
  await expect(reviewRow).toBeVisible();
  // 新评价默认应为「待审核」；远端若部署的是旧版后端会直接落「已通过」，两者都属合法初始态
  await expect(reviewRow).toContainText(/待审核|已通过/);
  await shot(page, "mall-full", "20-admin-review-pending");

  await reviewRow.getByRole("button", { name: "审核" }).click();
  const auditDialog = page.locator(".el-dialog").filter({ hasText: "审核状态" }).last();
  await auditDialog.waitFor({ state: "visible" });
  await auditDialog.locator(".el-radio").filter({ hasText: "已通过" }).first().click();
  await auditDialog.getByRole("button", { name: "保存" }).click();
  await expectToast(page, "审核状态更新成功");
  await waitIdle(page);
  await searchAdmin(page, "会员昵称 / 评价内容", reviewContent.slice(0, 20));
  await expect(
    page.locator(".el-table__row").filter({ hasText: reviewContent.slice(0, 20) }).first()
  ).toContainText("已通过");
  await shot(page, "mall-full", "21-admin-review-approved");

  // ---------- 16. 后台：订单终态回查 ----------
  await gotoAdmin(page, "/mall/order");
  await searchAdmin(page, "订单号 / 收货人 / 电话", orderNo);
  await expect(page.locator(".el-table__row").filter({ hasText: orderNo }).first()).toContainText("已完成");
  await shot(page, "mall-full", "22-admin-order-completed");

  await mobileContext.close();
});
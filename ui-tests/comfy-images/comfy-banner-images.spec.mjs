import { test } from "@playwright/test";
import { formItemByLabel, login, shot, waitIdle } from "../crm-flow-helpers.mjs";
import {
  FORCE,
  LIMIT,
  buildBannerPrompt,
  confirmAndWaitSaved,
  expect,
  generateImage,
  waitComfyReady,
} from "./comfy-image-helpers.mjs";

const BANNER_ASPECT = "16:9 (Widescreen)";

// 位置 + 标题在同一门店内唯一，作为 Banner 行标识；改图不会改动这两个字段
const bannerKey = async row => {
  const position = (await row.locator("td").nth(1).innerText().catch(() => "")).trim();
  const title = (await row.locator("td").nth(2).innerText().catch(() => "")).trim();
  return `${position}|${title}`;
};

const findBannerRow = async (page, key) => {
  const rows = page.locator(".el-table__body-wrapper .el-table__row");
  const count = await rows.count();
  for (let index = 0; index < count; index++) {
    if ((await bannerKey(rows.nth(index))) === key) return rows.nth(index);
  }
  return null;
};

test("商城 Banner：本地 ComfyUI 横幅出图 → 后台逐条替换", async ({ page }) => {
  test.setTimeout(3_600_000);

  await waitComfyReady();
  await login(page);
  await page.goto("/#/mall/banner");
  await waitIdle(page);

  const rows = page.locator(".el-table__body-wrapper .el-table__row");
  await rows.first().waitFor({ state: "visible", timeout: 30_000 });
  const rowCount = await rows.count();
  if (rowCount === 0) throw new Error("Banner 列表为空，无法配图");

  const items = [];
  for (let index = 0; index < rowCount; index++) {
    const row = rows.nth(index);
    items.push({
      key: await bannerKey(row),
      src: await row.locator("img").first().getAttribute("src").catch(() => null),
    });
  }

  const total = Math.min(items.length, LIMIT);
  let processed = 0;

  for (let index = 0; index < total; index++) {
    const item = items[index];
    const row = await findBannerRow(page, item.key);
    if (!row) {
      console.log(`\n[${index + 1}] ${item.key} 在列表中已不存在，跳过`);
      continue;
    }
    if (!FORCE && item.src && !item.src.includes("placeholder")) {
      console.log(`\n[${index + 1}] ${item.key} 已有 Banner 图，跳过`);
      continue;
    }

    await row.getByRole("button", { name: "编辑" }).first().click();
    const dialog = page.locator(".el-dialog").filter({ hasText: "Banner图片" }).last();
    await dialog.waitFor({ state: "visible", timeout: 20_000 });
    await waitIdle(page);

    const position = await formItemByLabel(dialog, "展示位置")
      .locator(".el-select__wrapper")
      .first()
      .innerText()
      .catch(() => "");
    const title = await formItemByLabel(dialog, "Banner标题").locator("input").first().inputValue().catch(() => "");

    const prompt = buildBannerPrompt({ position, title });
    const { filePath } = await generateImage({
      prompt,
      name: title || position || "banner",
      tag: `banner${index + 1}`,
      aspectRatio: BANNER_ASPECT,
    });
    console.log(`\n[${index + 1}] ${position} / ${title}\n    prompt: ${prompt}\n    file: ${filePath}`);

    const imageItem = formItemByLabel(dialog, "Banner图片");
    const beforeSrc = await imageItem.locator(".upload-image").first().getAttribute("src").catch(() => null);
    await imageItem.locator('input[type="file"]').first().setInputFiles(filePath);
    await expect
      .poll(() => imageItem.locator(".upload-image").first().getAttribute("src"), { timeout: 180_000 })
      .not.toBe(beforeSrc);

    await confirmAndWaitSaved(page, dialog);

    processed++;
    console.log(`[${index + 1}] 已替换 Banner 图：${position} / ${title}`);
  }

  console.log(`\n完成：替换 ${processed} 张 Banner 图，共 ${items.length} 条`);
  await page.goto("/#/mall/banner");
  await waitIdle(page);
  await shot(page, "comfy-images", "banner-list");

  if (processed === 0) {
    expect(items.length, "Banner 列表为空，没有可配图数据").toBeGreaterThan(0);
  }
});
import { test } from "@playwright/test";
import { formItemByLabel, login, shot, waitIdle } from "../crm-flow-helpers.mjs";
import {
  FORCE,
  LIMIT,
  buildProductPrompt,
  confirmAndWaitSaved,
  ensureFreightTemplate,
  expect,
  generateImage,
  gotoProductList,
  parseCategory,
  pickNextRow,
  waitComfyReady,
} from "./comfy-image-helpers.mjs";

test("商城商品主图：本地 ComfyUI 出图 → 后台逐商品上传", async ({ page }) => {
  test.setTimeout(3_600_000);

  await waitComfyReady();
  await login(page);
  await gotoProductList(page);

  let processed = 0;
  const skippedCodes = new Set();
  const doneCodes = new Set();

  while (processed < LIMIT) {
    const rows = page.locator(".el-table__body-wrapper .el-table__row");
    if ((await rows.count()) === 0) break;

    // 保存后列表会刷新，FORCE 模式下若只取第一个满足条件的行会一直停在首行，这里按编码逐个推进
    const next = await pickNextRow(
      rows,
      doneCodes,
      skippedCodes,
      async row => FORCE || (await row.locator(".product-thumb").count()) === 0,
    );
    if (!next) break;

    doneCodes.add(next.code);
    await rows.nth(next.index).getByRole("button", { name: "编辑" }).first().click();

    const dialog = page.locator(".el-dialog").filter({ hasText: "商品编码" }).last();
    await dialog.waitFor({ state: "visible", timeout: 20_000 });
    await waitIdle(page);

    const name = await formItemByLabel(dialog, "商品名称").locator("input").first().inputValue();
    const categoryText = await formItemByLabel(dialog, "商品分类")
      .locator(".el-select__wrapper")
      .first()
      .innerText()
      .catch(() => "");
    const desc = await formItemByLabel(dialog, "商品描述")
      .locator("textarea")
      .first()
      .inputValue()
      .catch(() => "");

    const { catParent, catName } = parseCategory(categoryText);
    const prompt = buildProductPrompt(name, catParent, catName, desc);
    const { filePath } = await generateImage({ prompt, name });
    console.log(`\n[${processed + 1}] ${name} (${catParent}/${catName})\n    prompt: ${prompt}`);

    const mainImageItem = formItemByLabel(dialog, "商品主图");
    const beforeSrc = await mainImageItem.locator(".upload-image").first().getAttribute("src").catch(() => null);
    await mainImageItem.locator('input[type="file"]').first().setInputFiles(filePath);
    await expect
      .poll(() => mainImageItem.locator(".upload-image").first().getAttribute("src"), { timeout: 180_000 })
      .not.toBe(beforeSrc);

    await ensureFreightTemplate(page, dialog);
    await confirmAndWaitSaved(page, dialog);

    processed++;
    console.log(`[${processed}] 已上传主图：${name} ← ${filePath}`);
  }

  console.log(`\n完成：上传 ${processed} 个商品主图，跳过已配图 ${skippedCodes.size} 个`);
  await gotoProductList(page);
  await shot(page, "comfy-images", "product-list");

  // 幂等复跑：列表商品已全部配图时没有任何任务可做，属于正常跳过而非失败
  if (processed === 0) {
    expect(skippedCodes.size, "商品列表为空，既没有缺图商品也没有已配图商品").toBeGreaterThan(0);
  }
});
import { test } from "@playwright/test";
import { formItemByLabel, login, shot, waitIdle } from "../crm-flow-helpers.mjs";
import {
  FORCE,
  LIMIT,
  buildAlbumPrompt,
  confirmAndWaitSaved,
  ensureFreightTemplate,
  expect,
  generateImage,
  gotoProductList,
  pickNextRow,
  parseCategory,
  seedOf,
  waitAlbumSettled,
  waitAlbumUploaded,
  waitComfyReady,
} from "./comfy-image-helpers.mjs";

const ALBUM_TARGET = Number.parseInt(process.env.COMFY_ALBUM_COUNT || "3", 10);
const ALBUM_LIMIT = 9;

// 精确读取 Imgs.vue 组件的 _fileList.value 长度（排除 DOM transition 残留）
// 从 .upload-box 根元素找到 Imgs.vue 的 Vue 实例
const probeAlbumFileList = async page => {
  return await page.evaluate(() => {
    const boxes = document.querySelectorAll(".upload-box");
    const results = [];
    for (const box of boxes) {
      let inst = box.__vueParentComponent;
      while (inst) {
        const sp = inst.setupState;
        if (sp && sp._fileList !== undefined) {
          results.push({
            _fileList_len: sp._fileList?.value?.length ?? sp._fileList?.length ?? -1,
            props_fileList_len: (inst.props?.fileList ?? []).length,
            hasFileListProp: !!inst.props?.fileList,
            componentName: inst.type?.name || inst.type?.__name || "(anon)",
          });
          break;
        }
        inst = inst.parent;
      }
    }
    return results;
  }).catch(err => [{ error: String(err) }]);
};

const albumItemOf = dialog => formItemByLabel(dialog, "商品相册");

test("商城商品相册：本地 ComfyUI 多角度出图 → 后台批量上传", async ({ page }) => {
  test.setTimeout(3_600_000);

  await waitComfyReady();
  await login(page);
  await gotoProductList(page);

  const skippedCodes = new Set();
  const doneCodes = new Set();
  let processed = 0;

  // DEBUG
  const debugRows = page.locator(".el-table__body-wrapper .el-table__row");
  const debugCount = await debugRows.count();
  console.log(`[DEBUG] 初始表格行数: ${debugCount}`);
  const debugLoading = await page.locator(".el-table__loading").count();
  const debugEmpty = await page.locator(".el-table__empty-block").count();
  console.log(`[DEBUG] loading=${debugLoading} empty=${debugEmpty}`);

  while (processed < LIMIT) {
    const rows = page.locator(".el-table__body-wrapper .el-table__row");
    const rowCount = await rows.count();
    console.log(`[DEBUG] while循环内 rowCount=${rowCount}`);
    if (rowCount === 0) break;
    console.log(`[DEBUG] 即将调用 pickNextRow...`);
    const next = await pickNextRow(rows, doneCodes, skippedCodes, async () => true);
    console.log(`[DEBUG] pickNextRow 返回: ${JSON.stringify(next)}`);
    if (!next) break;

    // 弹窗是复用的：Object.assign 之前，输入框/相册列表里还是上一个商品的残留数据。
    // 若此刻读取相册张数，会把上一个商品的相册误算到当前商品上，导致本行被当成"相册已满"跳过
    // （表现为此前偶数行全部没有相册图）。因此用行内商品名称作为期望值，等待表单刷新到位。
    const rowName = (await rows.nth(next.index).locator("td").nth(2).innerText().catch(() => "")).trim();

    await rows.nth(next.index).getByRole("button", { name: "编辑" }).first().click();

    const dialog = page.locator(".el-dialog").filter({ hasText: "商品编码" }).last();
    await dialog.waitFor({ state: "visible", timeout: 20_000 });
    await waitIdle(page);

    // 等商品编码与当前行一致，确保 Object.assign(productForm, row) 已执行完
    await expect
      .poll(
        async () => (await formItemByLabel(dialog, "商品编码").locator("input").first().inputValue().catch(() => "")).trim() === next.code,
        { timeout: 10_000, message: `弹窗数据未刷新到当前商品 ${next.code}` },
      )
      .toBe(true);

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

    const albumItem = albumItemOf(dialog);
    // 等相册 DOM 刷新到当前商品（连续两次采样一致），避免读到复用弹窗里上一个商品的残留相册
    const domSuccess = await waitAlbumSettled(page, albumItem);
    const probe = await probeAlbumFileList(page);
    // 找 Imgs.vue 组件：componentName 为 UploadImgs 的那个
    const imgsProbe = probe.find(p => p.componentName === "UploadImgs") || probe[0];
    const existing = imgsProbe?._fileList_len ?? domSuccess;

    console.log(`\n    === ${name} (${next.code}) ===`);
    console.log(`    DOM is-success=${domSuccess} | Imgs.vue._fileList=${imgsProbe?._fileList_len} | props.fileList=${imgsProbe?.props_fileList_len} | 判定=${existing}`);

    if (!FORCE && existing >= ALBUM_TARGET) {
      skippedCodes.add(next.code);
      console.log(`    相册已满 (${existing}/${ALBUM_TARGET})，跳过：${name}`);
      await dialog.locator(".el-dialog__footer").getByRole("button", { name: "取消" }).click();
      await dialog.waitFor({ state: "hidden", timeout: 20_000 }).catch(() => {});
      await waitIdle(page);
      continue;
    }

    const startCount = existing;
    const need = Math.min(ALBUM_TARGET - startCount, ALBUM_LIMIT - startCount);
    if (need <= 0) {
      skippedCodes.add(next.code);
      console.log(`    无需补充相册 (existing=${existing})，跳过：${name}`);
      await dialog.locator(".el-dialog__footer").getByRole("button", { name: "取消" }).click();
      await dialog.waitFor({ state: "hidden", timeout: 20_000 }).catch(() => {});
      await waitIdle(page);
      continue;
    }

    const { catParent, catName } = parseCategory(categoryText);
    const files = [];
    // 每张图同时切换形态、包装与机位；种子由商品编码派生，保证各商品之间的组合也不相同
    const seed = seedOf(next.code);
    for (let i = 0; i < need; i++) {
      const prompt = buildAlbumPrompt(name, catParent, catName, desc, i, seed);
      const { filePath } = await generateImage({ prompt, name, tag: `album${i + 1}` });
      files.push(filePath);
      console.log(`\n[${processed + 1}] ${name} 相册图 ${i + 1}/${need} ← ${filePath}\n    prompt: ${prompt}`);
    }

    await albumItem.locator('input[type="file"]').first().setInputFiles(files);
    await waitAlbumUploaded(albumItem, startCount + need);
    await page.waitForTimeout(2000);

    await ensureFreightTemplate(page, dialog);
    await confirmAndWaitSaved(page, dialog);

    doneCodes.add(next.code);
    processed++;
    console.log(`[${processed}] 已上传相册 ${need} 张：${name}`);
  }

  console.log(`\n完成：为 ${processed} 个商品补齐相册，跳过已满 ${skippedCodes.size} 个`);
  await gotoProductList(page);
  await shot(page, "comfy-images", "product-album-list");

  if (processed === 0) {
    expect(skippedCodes.size, "商品列表为空，既没有待配相册商品也没有已满商品").toBeGreaterThan(0);
  }
});
import { existsSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { join } from "node:path";
import { expect } from "@playwright/test";
import { formItemByLabel, waitIdle } from "../crm-flow-helpers.mjs";

export const COMFY_BASE = process.env.COMFY_BASE_URL || "http://127.0.0.1:8188";
export const COMFY_WORKFLOW = process.env.COMFY_WORKFLOW || "E:/Code/QPS/tools/comfyui/comfy-api-workflow.json";
export const IMAGE_DIR = process.env.COMFY_IMAGE_DIR || "E:/Code/QPS/ui-tests/playwright-output/comfy-images";
export const LIMIT = Number.parseInt(process.env.COMFY_LIMIT || "999", 10);
export const FORCE = process.env.COMFY_FORCE === "1";
export const COMFY_TIMEOUT = Number.parseInt(process.env.COMFY_TIMEOUT_MS || "300000", 10);

// 出图必须保持无字：中文品名与描述会让模型尝试渲染汉字，产生乱码文字，因此正向提示词只保留英文描述
export const NEGATIVE =
  "blurry, distorted, low quality, deformed, cartoon, anime, " +
  "text, words, letters, characters, chinese characters, typography, calligraphy, " +
  "watermark, logo, brand name, label, packaging text, caption, subtitle, " +
  "signature, stamp, numbers, alphabet, ui elements, screenshot";

// 去掉中文字符与全角标点，只保留模型能稳定理解的英文/数字片段
export const toAsciiOnly = text =>
  (text || "")
    .replace(/[\u3000-\u303f\u4e00-\u9fa5\uff00-\uffef]/g, " ")
    .replace(/[^\x20-\x7e]+/g, " ")
    .replace(/\s+/g, " ")
    .trim();

export const CAT_PACK = {
  根茎类: "dried roots and rhizomes, earthy brown tones, woody texture, concentric rings",
  花叶类: "dried flowers and leaves, delicate texture, herbaceous, soft natural colors",
  果实类: "dried berries and peels, wrinkled surface, vibrant natural colors",
  动物类: "animal-derived traditional medicine, preserved organic matter",
  矿物类: "mineral crystals, crystalline structure, rough natural rock surface",
};

export const HERB_DICT = [
  { match: /人参|ginseng/i, en: "ginseng root, oval shape, light yellowish brown" },
  { match: /黄芪|astragalus/i, en: "astragalus slices, pale yellow circular disks" },
  { match: /当归|dang\s*gui/i, en: "angelica sinensis root slices, dark brown aromatic" },
  { match: /枸杞|goji|wolfberry/i, en: "goji berries, bright crimson red, wrinkled surface" },
  { match: /陈皮|chen\s*pi/i, en: "dried tangerine peel, curled orange-brown pieces" },
  { match: /阿胶|ejiao/i, en: "donkey-hide gelatin, dark brown glossy cubes" },
  { match: /玫瑰|rose/i, en: "dried rose petals and buds, pink and red" },
  { match: /菊花|chrysanthemum/i, en: "dried chrysanthemum flowers, white and yellow petals" },
  { match: /金银花|honeysuckle/i, en: "dried honeysuckle buds, pale yellow-green" },
  { match: /鹿茸|antler/i, en: "dried deer antler slices, creamy brown porous texture" },
  { match: /石膏|gypsum/i, en: "gypsum mineral crystals, white translucent" },
  { match: /白术|atractylodes/i, en: "atractylodes macrocephala rhizome slices" },
  { match: /甘草|licorice/i, en: "licorice root slices, sweet yellow fibrous" },
  { match: /薄荷|mint/i, en: "dried mint leaves, green aromatic" },
  { match: /桑叶|mulberry/i, en: "dried mulberry leaves" },
  { match: /山楂|hawthorn/i, en: "dried hawthorn berries, deep red" },
  { match: /五味子|schisandra/i, en: "dried schisandra berries, dark red wrinkled" },
  { match: /全蝎|scorpion/i, en: "dried scorpion, preserved" },
  { match: /蜈蚣|centipede/i, en: "dried centipede" },
  { match: /朱砂|cinnabar/i, en: "cinnabar mineral, bright red crystalline" },
  { match: /龙骨|dragon bone/i, en: "fossilized bone fragments" },
  { match: /磁石|magnetite/i, en: "magnetite mineral, black metallic" },
];

export const BG_VARIANTS = [
  "rustic wooden board, natural linen cloth base",
  "slate stone surface, dark textured backdrop",
  "clean white marble surface, minimal studio setup",
  "weathered terracotta plate, warm earthy backdrop",
  "bamboo mat surface, soft natural backdrop",
  "dark walnut wood table, moody warm lighting",
  "ceramic white plate on light wood, bright airy setup",
  "jute burlap fabric base, neutral natural tone",
];

export const LIGHT_VARIANTS = [
  "soft studio lighting from 45 degree angle",
  "natural window light, gentle shadows",
  "warm golden hour side lighting",
  "diffused overhead studio light, minimal shadow",
  "Rembrandt lighting, dramatic yet natural",
  "bright even front lighting, commercial product style",
];

export const ANGLE_VARIANTS = [
  "eye-level centered composition",
  "slightly angled top-down view, 15 degree tilt",
  "straight on front view",
  "low angle hero shot",
  "flat lay overhead composition",
  "45 degree angle product photography",
];

// 相册图只描述机位与构图，不再包含容器，避免与「形态 / 包装」维度互相冲突
export const ALBUM_VARIANTS = [
  "close-up macro detail of the surface texture",
  "top-down flat lay on a textured surface with scattered pieces around",
  "side view showing the depth and thickness of the pile",
  "hero shot with a soft shadow falling behind the subject",
  "three-quarter angled view with shallow depth of field",
  "editorial composition with generous negative space",
];

// 形态维度：同一商品在不同张里变换呈现形态，整颗 / 切片 / 粉 / 冲泡 / 分级
// 只写形态本身，不写容器，避免与「包装」维度同时出现两个互相矛盾的载体；
// cats 限定适用品类，避免果实类被写成「切片环纹」这类不合理的形态
export const FORM_VARIANTS = [
  {
    en: "whole dried pieces kept intact in their natural original shape",
    cats: ["根茎类", "花叶类", "果实类", "动物类", "矿物类"],
  },
  { en: "thinly sliced pieces with the cross-section rings clearly visible", cats: ["根茎类", "动物类"] },
  {
    en: "coarsely ground into a loose powder with a fine even texture",
    cats: ["根茎类", "花叶类", "果实类", "动物类", "矿物类"],
  },
  { en: "steeped as a tea, amber infusion visible with raw pieces beside it", cats: ["根茎类", "花叶类", "果实类"] },
  {
    en: "carefully graded pieces sorted by size into neat rows",
    cats: ["根茎类", "花叶类", "果实类", "动物类", "矿物类"],
  },
  {
    en: "a generous handful lifted up, loose pieces falling back into the pile",
    cats: ["根茎类", "花叶类", "果实类", "矿物类"],
  },
];

// 礼盒类商品不出现散装手抓等形态，只保留盒装能装得下的呈现方式
export const GIFT_FORMS = [
  {
    en: "whole dried pieces kept intact in their natural original shape",
    cats: ["根茎类", "花叶类", "果实类", "动物类", "矿物类"],
  },
  { en: "thinly sliced pieces with the cross-section rings clearly visible", cats: ["根茎类", "动物类"] },
  {
    en: "pieces neatly arranged in even rows filling the compartments",
    cats: ["根茎类", "花叶类", "果实类", "动物类", "矿物类"],
  },
  {
    en: "pieces sealed in small individual inner pouches inside the box",
    cats: ["根茎类", "花叶类", "果实类", "动物类", "矿物类"],
  },
];

// 包装维度：散装堆、纸袋、棉麻袋、玻璃罐、真空袋、马口铁罐（礼盒只用于礼盒类商品）
export const PACK_VARIANTS = [
  "loose bulk pile with no container, plain and unbranded",
  "packed in a plain kraft paper bag with the top folded open",
  "packed in a plain linen drawstring pouch, loosely closed",
  "stored in a clear glass jar with a cork lid, plain unbranded",
  "sealed in a flat transparent vacuum pouch, plain unbranded",
  "packed in a plain tin canister with a simple lid",
];

export const pick = arr => arr[Math.floor(Math.random() * arr.length)];

const NO_TEXT_TAIL = "clean blank background, no text, no labels, no lettering, 4k, high resolution";

const herbOf = name => HERB_DICT.find(h => h.match.test(name));

export const buildProductPrompt = (name, catParent, catName, desc) => {
  const pack = CAT_PACK[catParent] || CAT_PACK[catName] || "natural herbal product photography";
  const herb = herbOf(name);
  const isGift = /礼盒|精装|木盒|高档|精品/i.test(name);
  const packing = isGift
    ? "luxury wooden gift box packaging, plain unbranded surface, elegant presentation"
    : pick(BG_VARIANTS);
  // 品名与描述只用于本地匹配品类/药材，不进入提示词，避免模型渲染汉字
  return [
    "Professional e-commerce product photo of dried Chinese herbal medicine",
    herb ? herb.en : "",
    pack,
    toAsciiOnly(desc),
    packing,
    pick(LIGHT_VARIANTS),
    pick(ANGLE_VARIANTS),
    NO_TEXT_TAIL,
  ]
    .filter(Boolean)
    .join(", ");
};

const GIFT_PACKS = [
  "presented in a plain wooden gift box with compartments",
  "presented in an elegant plain paper gift box with a silk lining",
  "presented in a plain rigid gift box with the lid slightly open",
  "presented in a plain wooden gift box with a fabric ribbon closure",
];

const GIFT_SCENES = [
  "hero shot with a soft shadow falling behind the subject",
  "three-quarter angled view with shallow depth of field",
  "close-up macro detail of the surface texture",
  "editorial composition with generous negative space",
];

// 用商品编码派生稳定种子：不同商品的组合序列从不同起点开始，避免每个商品都撞成同一套画面
export const seedOf = code => {
  let hash = 0;
  for (const ch of String(code || "")) hash = (hash * 31 + ch.charCodeAt(0)) % 99991;
  return hash;
};

// 按种子轮转取变体；步长与池大小互质，保证同一商品连续多张不会重复取到同一项
const rotate = (pool, seed, index, step, offset = 0) =>
  pool[(((seed + offset + index * step) % pool.length) + pool.length) % pool.length];

export const buildAlbumPrompt = (name, catParent, catName, desc, index = 0, seed = 0) => {
  const pack = CAT_PACK[catParent] || CAT_PACK[catName] || "natural herbal product photography";
  const herb = herbOf(name);
  const isGift = /礼盒|精装|木盒|高档|精品/i.test(name);
  const catKey = CAT_PACK[catParent] ? catParent : CAT_PACK[catName] ? catName : "";
  const formPool = isGift ? GIFT_FORMS : FORM_VARIANTS;
  // 形态按品类过滤，过滤后为空时退回全量，避免小品类无形态可用
  const forms = formPool.filter(item => !catKey || item.cats.includes(catKey)).map(item => item.en);
  const usableForms = forms.length ? forms : formPool.map(item => item.en);
  // 形态、包装、机位、背景、光线各用不同的偏移与步长，同一商品的每张图在所有维度上都不同
  const form = rotate(usableForms, seed, index, 1, 0);
  const wrap = isGift ? rotate(GIFT_PACKS, seed, index, 1, 0) : rotate(PACK_VARIANTS, seed, index, 1, 2);
  const scene = rotate(isGift ? GIFT_SCENES : ALBUM_VARIANTS, seed, index, 1, 1);
  const bg = rotate(BG_VARIANTS, seed, index, 3);
  const light = rotate(LIGHT_VARIANTS, seed, index, 5);
  return [
    "Professional e-commerce product photo of dried Chinese herbal medicine",
    herb ? herb.en : "",
    pack,
    toAsciiOnly(desc),
    form,
    wrap,
    scene,
    bg,
    light,
    NO_TEXT_TAIL,
  ]
    .filter(Boolean)
    .join(", ");
};

export const buildBannerPrompt = ({ position = "", title = "", subject = "" } = {}) => {
  const herb = herbOf(`${title} ${subject}`);
  const scene = /首页|home/i.test(position)
    ? "wide horizontal banner composition, generous empty negative space on the left for overlay"
    : "wide horizontal banner composition, centered subject with soft vignette";
  return [
    "Traditional Chinese medicine aesthetic wide banner photograph",
    // 未命中具体药材时用通用药材陈列，避免随机品类包带出与主题无关的画面
    herb ? herb.en : "assorted dried herbs, roots and berries in shallow wooden bowls",
    "arranged on a dark wooden table with dried herbs, mortar and pestle, bamboo mat",
    scene,
    pick(LIGHT_VARIANTS),
    "cinematic depth of field, rich natural tones",
    NO_TEXT_TAIL,
  ]
    .filter(Boolean)
    .join(", ");
};

export const injectPrompt = (workflow, positive, { aspectRatio = "1:1 (Square)", megapixels = 1 } = {}) => {
  const copy = JSON.parse(JSON.stringify(workflow));
  for (const node of Object.values(copy)) {
    if (!node.inputs) continue;
    if (node.class_type === "ResolutionSelector") {
      node.inputs.aspect_ratio = aspectRatio;
      node.inputs.megapixels = megapixels;
    }
    if (node.class_type === "KSampler") {
      node.inputs.steps = 25;
      node.inputs.seed = Math.floor(Math.random() * 99999999999999);
    }
    if (node.class_type !== "CLIPTextEncode") continue;
    const title = (node._meta?.title || "").toLowerCase();
    node.inputs.text = title.includes("negative") ? NEGATIVE : positive;
  }
  return copy;
};

export const waitComfyReady = async () => {
  for (let i = 0; i < 10; i++) {
    try {
      const res = await fetch(`${COMFY_BASE}/system_stats`);
      if (res.ok) return;
    } catch {
      // ComfyUI 尚未启动，继续重试
    }
    await new Promise(resolve => setTimeout(resolve, 1500));
  }
  throw new Error(`ComfyUI 未启动：${COMFY_BASE}（请先启动 ComfyUI Desktop，端口 8188）`);
};

export const waitComfyResult = async promptId => {
  const start = Date.now();
  while (Date.now() - start < COMFY_TIMEOUT) {
    const res = await fetch(`${COMFY_BASE}/history/${promptId}`);
    if (res.ok) {
      const payload = await res.json();
      const entry = payload[promptId];
      if (entry?.status?.status_str === "success") return entry.outputs;
      if (entry?.status?.status_str === "error") {
        throw new Error(`ComfyUI 生成失败：${JSON.stringify(entry.status.messages)}`);
      }
    }
    await new Promise(resolve => setTimeout(resolve, 3000));
  }
  throw new Error(`ComfyUI 生成超时（${COMFY_TIMEOUT}ms）：${promptId}`);
};

export const generateImage = async ({ prompt, name, tag = "", aspectRatio = "1:1 (Square)", megapixels = 1 }) => {
  if (!existsSync(COMFY_WORKFLOW)) throw new Error(`ComfyUI 工作流不存在：${COMFY_WORKFLOW}`);

  const workflow = injectPrompt(JSON.parse(readFileSync(COMFY_WORKFLOW, "utf-8")), prompt, {
    aspectRatio,
    megapixels,
  });

  const submitRes = await fetch(`${COMFY_BASE}/prompt`, {
    method: "POST",
    headers: { "content-type": "application/json" },
    body: JSON.stringify({ prompt: workflow, client_id: "qps-ui-test" }),
  });
  if (!submitRes.ok) throw new Error(`ComfyUI 提交失败：${submitRes.status} ${(await submitRes.text()).slice(0, 200)}`);
  const { prompt_id: promptId } = await submitRes.json();

  const outputs = await waitComfyResult(promptId);
  const outputNode = Object.values(outputs).find(node => node.images?.length);
  if (!outputNode) throw new Error("ComfyUI 未返回图片");
  const image = outputNode.images[0];

  const query = new URLSearchParams({
    filename: image.filename,
    subfolder: image.subfolder || "",
    type: image.type || "output",
  });
  const viewRes = await fetch(`${COMFY_BASE}/view?${query.toString()}`);
  if (!viewRes.ok) throw new Error(`ComfyUI 图片下载失败：${viewRes.status}`);

  mkdirSync(IMAGE_DIR, { recursive: true });
  const safe = name.replace(/[^\w\u4e00-\u9fa5-]/g, "_").slice(0, 40);
  const suffix = tag ? `-${tag}` : "";
  const filePath = join(IMAGE_DIR, `${safe}${suffix}-${promptId.slice(0, 8)}.png`);
  writeFileSync(filePath, Buffer.from(await viewRes.arrayBuffer()));
  return { filePath, prompt };
};

export const parseCategory = text => {
  const cleaned = (text || "").replace(/\s+/g, " ").trim();
  const parts = cleaned.split("/").map(part => part.trim()).filter(Boolean);
  return parts.length > 1
    ? { catParent: parts[0], catName: parts[1] }
    : { catParent: parts[0] || "", catName: parts[0] || "" };
};

export const gotoProductList = async page => {
  await page.goto("/#/mall/product");
  await page.locator(".el-table__body-wrapper .el-table__row").first().waitFor({ state: "visible", timeout: 30_000 });
  await waitIdle(page);
  const sizeSelect = page.locator(".el-pagination .el-select").first();
  if ((await sizeSelect.count()) > 0) {
    await sizeSelect.click();
    await page.locator(".el-select-dropdown__item:visible").filter({ hasText: /^100/ }).first().click().catch(() => {});
    await page.locator(".el-table__body-wrapper .el-table__row").first().waitFor({ state: "visible", timeout: 30_000 });
    await waitIdle(page);
  }
};

// 部分种子商品没有绑定运费模板，直接保存会被表单校验拦截；这里自动补选默认模板
export const ensureFreightTemplate = async (page, dialog) => {
  const item = formItemByLabel(dialog, "运费模板");
  const shown = (await item.locator(".el-select__wrapper").first().innerText().catch(() => "")).trim();
  console.log(`    运费模板当前值：${JSON.stringify(shown)}`);
  if (shown && !shown.includes("请选择")) return;

  await item.locator(".el-select__wrapper").first().click();
  const dropdown = page.locator(".el-select-dropdown:visible").last();
  await dropdown.waitFor({ state: "visible", timeout: 10_000 });

  const defaultOption = dropdown.locator(".el-select-dropdown__item").filter({ hasText: "默认" }).first();
  const option = (await defaultOption.count()) > 0 ? defaultOption : dropdown.locator(".el-select-dropdown__item").first();
  const label = (await option.innerText().catch(() => "")).trim();
  await option.click();
  console.log(`    运费模板已选：${label}，弹窗可见=${await dialog.isVisible().catch(() => false)}`);
  await waitIdle(page);
};

// 弹窗是复用的：打开新商品时 Object.assign 同步刷新表单（商品编码立刻正确），
// 但 el-upload 的相册列表是异步重渲染的，会先保留上一个商品的图片约 500ms 再刷新。
// 若此刻读取相册张数，会把残留的图算到当前商品上，误判"相册已满"而跳过
// （表现为此前偶数行全部没有相册图）。
// 这里要求相册张数在连续 5 次、每次间隔 200ms 的采样中都一致（覆盖残留清除窗口），
// 确保读到的是当前商品的真实数据。
export const waitAlbumSettled = async (page, albumItem, timeout = 15_000) => {
  const SAMPLES = 5;
  const INTERVAL = 200;
  const deadline = Date.now() + timeout;
  let prev = -1;
  let streak = 0;
  while (Date.now() < deadline) {
    const cur = await albumItem.locator(".el-upload-list__item.is-success").count();
    streak = cur === prev ? streak + 1 : 1;
    prev = cur;
    if (streak >= SAMPLES) return prev;
    await page.waitForTimeout(INTERVAL);
  }
  throw new Error("相册列表数据未稳定（弹窗残留未清除）");
};

// el-upload 在上传开始时就渲染列表项，此时 url 还没回填；必须等每一项都成功且图片地址就绪，
// 否则父级表单里的相册数组仍是空的，保存后相册会被丢掉
export const waitAlbumUploaded = async (albumItem, expectedCount, timeout = 180_000) => {
  await expect
    .poll(
      async () => {
        const done = albumItem.locator(".el-upload-list__item.is-success");
        if ((await done.count()) < expectedCount) return false;
        const srcs = await done.locator("img").evaluateAll(nodes => nodes.map(node => node.getAttribute("src") || ""));
        return srcs.length >= expectedCount && srcs.every(Boolean);
      },
      { timeout, message: "相册图片未全部上传成功" },
    )
    .toBe(true);
};

// 保存成功会立即关闭弹窗并销毁页脚按钮，Playwright 会在点击动作中途发现元素消失而重试到超时；
// 因此这里把「弹窗已关闭」视为提交已生效，并用弹窗最终是否关闭来判断保存是否真正失败。
export const confirmAndWaitSaved = async (page, dialog) => {
  const confirm = dialog.locator(".el-dialog__footer").getByRole("button", { name: "确定" });
  try {
    await confirm.click({ timeout: 20_000 });
  } catch (error) {
    if (await dialog.isVisible().catch(() => false)) throw error;
    console.log("    弹窗已关闭，提交已生效");
  }

  await dialog.waitFor({ state: "hidden", timeout: 30_000 }).catch(() => {});
  if (await dialog.isVisible().catch(() => false)) throw new Error("保存未完成，弹窗仍然打开");
  await waitIdle(page);
};

// 商品编码列是表格第 2 列，用作商品唯一标识；编码缺失时退化为整行文本，避免所有行被判为同一商品
export const rowCode = async row => {
  const code = (await row.locator("td").nth(1).innerText().catch(() => "")).trim();
  if (code) return code;
  const fallback = (await row.innerText().catch(() => "")).replace(/\s+/g, " ").trim();
  return fallback || "unknown-row";
};

// 按商品编码逐个推进：保存后列表会刷新，若只取第一个满足条件的行会一直停在首行
export const pickNextRow = async (rows, doneCodes, skippedCodes, predicate) => {
  const rowCount = await rows.count();
  console.log(`[DEBUG pickNextRow] rowCount=${rowCount} doneCodes.size=${doneCodes.size} skippedCodes.size=${skippedCodes.size}`);
  for (let index = 0; index < rowCount; index++) {
    const row = rows.nth(index);
    const code = await rowCode(row);
    console.log(`[DEBUG pickNextRow] index=${index} code=${JSON.stringify(code)}`);
    if (doneCodes.has(code) || skippedCodes.has(code)) continue;
    if (await predicate(row)) return { index, code };
    skippedCodes.add(code);
  }
  return null;
};

export { expect };
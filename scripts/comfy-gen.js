const fs = require("fs");
const path = require("path");
const ax = require("axios");
const crypto = require("crypto");
const sql = require("mssql");

const LOG = "e:/Code/QPS/comfy-flow-log.txt";
fs.writeFileSync(LOG, "");
function log(m) { fs.appendFileSync(LOG, `[${new Date().toISOString().slice(11,19)}] ${m}\n`); console.log(m); }

const COMFY = "http://127.0.0.1:8188";
const MONTH = new Date().toISOString().slice(0, 7).replace("-", "");
const API_ROOT = path.resolve("e:/Code/QPS/web-api/src/QPS.Host/wwwroot");
const WF_PATH = "e:/Code/QPS/comfy-api-workflow.json";

const NEGATIVE = "blurry, distorted, low quality, text, watermark, logo, cartoon, anime, deformed";

async function waitForComfy() {
  for (let i = 0; i < 10; i++) {
    try {
      await ax.get(COMFY + "/system_stats", { timeout: 5000 });
      return true;
    } catch(e) { log("  retry " + (i+1) + ": " + e.message.slice(0,80)); }
    await new Promise(r => setTimeout(r, 1000));
  }
  return false;
}

async function submitPrompt(workflow) {
  const r = await ax.post(COMFY + "/prompt", { prompt: workflow, client_id: "qps-bot" }, { timeout: 15000 });
  return r.data.prompt_id;
}

async function waitResult(promptId, timeoutMs = 300000) {
  const start = Date.now();
  while (Date.now() - start < timeoutMs) {
    const r = await ax.get(COMFY + "/history/" + promptId, { timeout: 5000 });
    const entry = r.data[promptId];
    if (entry && entry.status && entry.status.status_str === "success") {
      return entry.outputs;
    }
    if (entry && entry.status && entry.status.status_str === "error") {
      throw new Error("Comfy error: " + JSON.stringify(entry.status.messages));
    }
    await new Promise(r => setTimeout(r, 3000));
  }
  throw new Error("Comfy timeout");
}

async function getImage(filename, subfolder, type) {
  const r = await ax.get(COMFY + "/view", { params: { filename, subfolder, type }, responseType: "arraybuffer", timeout: 30000 });
  return Buffer.from(r.data);
}

function buildProductPrompt(name, catParent, catName, desc) {
  const CAT_PACK = {
    "根茎类": "dried roots and rhizomes, earthy brown tones, woody texture, concentric rings",
    "花叶类": "dried flowers and leaves, delicate texture, herbaceous, soft natural colors",
    "果实类": "dried berries and peels, wrinkled surface, vibrant natural colors",
    "动物类": "animal-derived traditional medicine, preserved organic matter",
    "矿物类": "mineral crystals, crystalline structure, rough natural rock surface",
  };
  const PACK = CAT_PACK[catParent] || CAT_PACK[catName] || "natural herbal product photography";
  const HERB = HERB_DICT.find(h => h.match.test(name));
  const isGift = /礼盒|精装|木盒|高档|精品/i.test(name);
  const packing = isGift ? "luxury wooden gift box packaging, elegant presentation" : "rustic wooden board, natural linen cloth base";
  return `Professional e-commerce product photo of dried Chinese herbal medicine ${name}, ${desc || ""}, ${PACK}, ${HERB ? HERB.en + ", " : ""}${packing}, studio lighting, clean background, 4k, soft shadow, centered composition, high resolution`;
}

const HERB_DICT = [
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

function buildCategoryPrompt(name) {
  return `Single large icon filling the entire frame edge to edge, Chinese herbal medicine category "${name}", close-up, flat vector silhouette, no text no letter no character, transparent background`;
}

function buildBannerPrompt(name) {
  return `Wide banner for Chinese herbal medicine e-commerce, "${name}" theme, marketing atmosphere, warm natural lighting, traditional Chinese medicine aesthetic, promotional design, eye-catching, 16:9 composition`;
}

function buildActivityPrompt(name) {
  return `Promotional cover image for activity "${name}", Chinese herbal medicine theme, festive atmosphere, marketing design, vibrant colors, eye-catching`;
}

const ENTITY_SIZE = {
  product_main: { aspect_ratio: "1:1 (Square)",              megapixels: 1,    steps: 25 },
  category:     { aspect_ratio: "1:1 (Square)",              megapixels: 0.56, steps: 20 },
  banner:       { aspect_ratio: "16:9 (Widescreen)",        megapixels: 1,    steps: 25 },
  activity:     { aspect_ratio: "3:4 (Portrait Standard)",  megapixels: 1,    steps: 25 },
};

function injectPrompt(workflow, positive, negative, entity) {
  const wf = JSON.parse(JSON.stringify(workflow));
  const size = ENTITY_SIZE[entity] || ENTITY_SIZE.product_main;
  for (const [id, node] of Object.entries(wf)) {
    if (node.class_type === "ResolutionSelector" && node.inputs) {
      node.inputs.aspect_ratio = size.aspect_ratio;
      node.inputs.megapixels = size.megapixels;
      log(`  [${id}] 尺寸 <- ${size.aspect_ratio} ${size.megapixels}MP`);
    }
    if (node.class_type === "KSampler" && node.inputs && size.steps) {
      node.inputs.steps = size.steps;
      log(`  [${id}] steps <- ${size.steps}`);
    }
    if (node.class_type !== "CLIPTextEncode" || !node.inputs) continue;
    const title = node._meta?.title || "";
    if (title.includes("Negative") || title.includes("negative")) {
      node.inputs.text = negative || NEGATIVE;
      log(`  [${id}] negative prompt <- ${(negative || NEGATIVE).slice(0, 40)}...`);
    } else {
      node.inputs.text = positive;
      log(`  [${id}] positive prompt <- ${positive.slice(0, 60)}...`);
    }
  }
  return wf;
}

let FORCE_OVERWRITE = false;
const processedIds = new Set();

async function getNextJob(entity) {
  const conn = await sql.connect({ server: "localhost", database: "QPS_CRM", user: "sa", password: "123456", options: { encrypt: false, trustServerCertificate: true } });
  try {
    const meta = {
      product_main: { table: "MallProducts",  field: "MainImageUrl", scene: "product",  nameField: "Name" },
      category:     { table: "MallCategories", field: "Icon",        scene: "category", nameField: "Name" },
      banner:       { table: "MallBanners",    field: "ImageUrl",   scene: "banner",   nameField: "Title" },
      activity:     { table: "MallActivities", field: "CoverImage",  scene: "activity", nameField: "Title" },
    }[entity];
    if (!meta) throw new Error("unknown entity: " + entity);
    const whereParts = ["IsDeleted = 0"];
    if (!FORCE_OVERWRITE) {
      whereParts.push(`(${meta.field} IS NULL OR ${meta.field} = '' OR ${meta.field} NOT LIKE '/uploads/%')`);
    }
    if (processedIds.size > 0) {
      const excluded = Array.from(processedIds).map(id => `'${id}'`).join(",");
      whereParts.push(`Id NOT IN (${excluded})`);
    }
    const r = await new sql.Request().query(
      `SELECT TOP 1 * FROM ${meta.table} WHERE ${whereParts.join(" AND ")} ORDER BY SortOrder`
    );
    const row = r.recordset[0];
    if (!row) return null;
    processedIds.add(row.Id);

    let catName = "", catParent = "";
    if (entity === "product_main" && row.CategoryId) {
      const cat = await new sql.Request().input("cid", sql.UniqueIdentifier, row.CategoryId).query(
        "SELECT c.Name, p.Name as ParentName FROM MallCategories c LEFT JOIN MallCategories p ON c.ParentId = p.Id WHERE c.Id = @cid"
      );
      catName = cat.recordset[0]?.Name || "";
      catParent = cat.recordset[0]?.ParentName || "";
    }
    return { row, meta, catName, catParent };
  } finally { await sql.close(); }
}

async function saveAndUpdateDb(buf, scene, entity, row) {
  const folder = path.join(API_ROOT, "uploads", scene, MONTH);
  fs.mkdirSync(folder, { recursive: true });
  const fname = crypto.randomBytes(16).toString("hex") + ".png";
  const fullPath = path.join(folder, fname);
  fs.writeFileSync(fullPath, buf);
  const dbPath = `/uploads/${scene}/${MONTH}/${fname}`;

  const conn = await sql.connect({ server: "localhost", database: "QPS_CRM", user: "sa", password: "123456", options: { encrypt: false, trustServerCertificate: true } });
  try {
    const tableMap = { product_main: "MallProducts", category: "MallCategories", banner: "MallBanners", activity: "MallActivities" };
    const fieldMap = { product_main: "MainImageUrl", category: "Icon", banner: "ImageUrl", activity: "CoverImage" };
    await new sql.Request()
      .input("p", sql.NVarChar, dbPath)
      .input("id", sql.UniqueIdentifier, row.Id)
      .query(`UPDATE ${tableMap[entity]} SET ${fieldMap[entity]} = @p WHERE Id = @id`);
  } finally { await sql.close(); }
  return dbPath;
}

async function main() {
  const args = process.argv.slice(2);
  FORCE_OVERWRITE = args.includes("--force");
  const cleanArgs = args.filter(a => a !== "--force");
  const entity = cleanArgs[0] || "product_main";
  const count = parseInt(cleanArgs[1] || "1");

  log("==== QPS ComfyUI 批量出图 ====");
  log(`entity=${entity} count=${count} force=${FORCE_OVERWRITE}`);

  if (!fs.existsSync(WF_PATH)) { log("❌ workflow 模板不存在: " + WF_PATH); process.exit(1); }
  const baseWf = JSON.parse(fs.readFileSync(WF_PATH, "utf-8"));
  log("✅ workflow 模板已加载, 节点: " + Object.keys(baseWf).join(", "));

  log("等待 ComfyUI...");
  if (!(await waitForComfy())) { log("❌ ComfyUI 没启动"); process.exit(1); }
  log("✅ ComfyUI 已就绪");

  let success = 0, fail = 0;
  for (let i = 0; i < count; i++) {
    log(`\n--- [${i+1}/${count}] 取任务 ---`);
    let job;
    try { job = await getNextJob(entity); } catch(e) { log("❌ 取任务失败: " + e.message); break; }
    if (!job) { log("✅ 全部完成，无剩余缺图记录"); break; }

    const { row, meta, catName, catParent } = job;
    const name = row.Name || row.Title || "product";
    let positive, negative;

    if (entity === "product_main") {
      positive = buildProductPrompt(name, catParent, catName, row.Description || "");
      negative = NEGATIVE;
    } else if (entity === "category") {
      positive = buildCategoryPrompt(name);
      negative = "realistic, 3d render, photograph, text, watermark, logo, blurry";
    } else if (entity === "banner") {
      positive = buildBannerPrompt(name);
      negative = "blurry, low quality, text";
    } else if (entity === "activity") {
      positive = buildActivityPrompt(name);
      negative = "blurry, low quality";
    }

    log(`📝 [${row.Id.slice(0,8)}] ${name}  scene=${meta.scene}`);
    log(`prompt: ${positive.slice(0, 100)}...`);

    const wf = injectPrompt(baseWf, positive, negative, entity);

    let pid;
    try { pid = await submitPrompt(wf); } catch(e) { log("❌ 提交失败: " + e.message); fail++; continue; }
    log(`🛫 已提交 prompt_id=${pid}`);

    let outputs;
    try { outputs = await waitResult(pid); } catch(e) { log("❌ 生成失败: " + e.message); fail++; continue; }

    let outNode;
    for (const [nid, out] of Object.entries(outputs)) {
      if (out.images && out.images.length) { outNode = out; break; }
    }
    if (!outNode) { log("❌ 没有图片输出"); fail++; continue; }

    const img = outNode.images[0];
    log(`下载: ${img.filename} sub=${img.subfolder || "-"}`);
    let buf;
    try { buf = await getImage(img.filename, img.subfolder || "", img.type || "output"); } catch(e) { log("❌ 下载失败: " + e.message); fail++; continue; }

    const dbPath = await saveAndUpdateDb(buf, meta.scene, entity, row);
    log(`💾 已存盘 + 回写 DB: ${dbPath}  (${buf.length} bytes)`);
    success++;
  }
  log(`\n==== 完成: 成功 ${success}, 失败 ${fail} ====`);
}

main().catch(e => { log("FATAL: " + e.message); process.exit(1); });
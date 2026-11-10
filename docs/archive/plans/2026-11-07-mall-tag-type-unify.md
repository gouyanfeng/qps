# MallTags 增加类型字段，统一商城标签库

## 一、概述

给 `MallTags` 增加类型字段（字符串，内置 `Product` / `Review` 但不做枚举限制），让商品标签与评价标签共用一张标签字典：

1. 商品标签只允许选择 `Type = Product` 的标签。
2. 评价标签改为从标签字典读取（`Type = Review`），替换移动端写死的 6 个预设标签。
3. 管理端新增「标签管理」页面，可按类型筛选并维护所有标签。
4. 编码/名称唯一性改为「同类型内唯一」，不同类型可同名。
5. 标签颜色改用颜色选择器（取色器）录入，编码不固定前缀、自由输入。
6. 类型同样不做固定枚举：`Product` / `Review` 只作为内置常用值，后台可自由输入新类型。

已确认决策：统一标签库、新增标签管理页；类型与编码都不固定枚举/格式，均为自由输入；颜色用取色器。

## 二、现状分析

- 实体 [MallTag.cs](file:///e:/Code/QPS/web-api/src/QPS.Mall/Domain/Entities/MallTag.cs)：`Code / Name / Color / SortOrder / IsActive`，无类型。
- 配置 [MallModelConfiguration.cs](file:///e:/Code/QPS/web-api/src/QPS.Persistence/Configurations/Mall/MallModelConfiguration.cs#L48-L60)：`Code`、`Name` 为全局唯一过滤索引。
- 后端只有后台接口 [MallTagController.cs](file:///e:/Code/QPS/web-api/src/QPS.Mall/Controllers/Admin/MallTagController.cs)，`GET api/admin/mall/tags` 不支持类型过滤；无移动端标签接口。
- 后台标签 CRUD 目前内嵌在 [商品管理页](file:///e:/Code/QPS/admin/src/views/mall/product/index.vue#L199-L255) 的「管理标签」弹窗里，没有独立菜单页。
- 移动端 [review-create/index.vue](file:///e:/Code/QPS/mobile/src/pages/review-create/index.vue#L118) 写死 `PRESET_TAGS` 6 个；评价落库仍是逗号分隔的标签名称字符串（`MallProductReview.Tags`），无需改表。
- 商品保存时通过 [MallProductRules.GetActiveTagsAsync](file:///e:/Code/QPS/web-api/src/QPS.Mall/Application/Features/MallProducts/MallProductRules.cs#L314) 校验标签启用状态，需补类型过滤。
- 菜单为静态配置 [authMenuList.json](file:///e:/Code/QPS/admin/src/assets/json/authMenuList.json#L153-L180)，商城菜单按顺序排布。

## 三、改动清单

### 3.1 后端实体与常量

**`web-api/src/QPS.Mall/Domain/Entities/MallTag.cs`**
- 新增常量类型类（可放在同文件）：

```csharp
public static class MallTagType
{
    public const string Product = "Product";
    public const string Review = "Review";
    public static readonly string[] Common = { Product, Review };
}
```

- `MallTagType` 仅作为内置常用值（默认类型、前端下拉建议），**不做枚举白名单校验**，后台可自由输入其他类型。
- `MallTag` 新增 `public string Type { get; private set; }`。
- `Create(string code, string name, string color, int sortOrder, bool isActive, string type = MallTagType.Product)`：参数放末尾，`Type = Trim()`，空值默认 `Product`，长度 ≤20，超长抛 `BusinessException(400, "标签类型不能超过 20 个字符")`。
- `Update(...)` **不修改 Type**（类型创建后不可变，避免已关联商品/评价的标签被改类），保持原有 Type。
- `ChangeStatus` 不变。

**`web-api/src/QPS.Persistence/Configurations/Mall/MallModelConfiguration.cs`**
- `Type`：`HasMaxLength(20).IsRequired().HasDefaultValue(MallTagType.Product)`。
- 删除 `Code`、`Name` 单列唯一索引，改为：

```csharp
entity.HasIndex(tag => new { tag.Type, tag.Code }).IsUnique().HasFilter("[IsDeleted] = 0");
entity.HasIndex(tag => new { tag.Type, tag.Name }).IsUnique().HasFilter("[IsDeleted] = 0");
```

### 3.2 迁移

新增迁移 `AddMallTagType`：
- `AddColumn<string>("Type", "MallTags", "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Product")`。
- 删除旧索引 `IX_MallTags_Code`、`IX_MallTags_Name`，创建 `IX_MallTags_Type_Code`、`IX_MallTags_Type_Name`（唯一 + `[IsDeleted] = 0` 过滤）。
- 用 `migrationBuilder.Sql` 插入 6 条评价标签（固定 GUID、`Type = 'Review'`、`Code` 用 `REVIEW-TAG-01`…`06`、`IsActive = 1`、`SortOrder` 1–6、`Color = '#2E7D32'`（十六进制，与取色器格式一致），`Name` 为药材道地/包装精美/物流快/效果显著/客服专业/性价比高，`CreatedAt/UpdatedAt` 取 `GETDATE()`，`CreatedBy/UpdatedBy` 填 `'system'`，显式给出表全字段）。
- `Down` 对称：删除 6 条种子数据、删除新索引、恢复旧索引、删除 `Type` 列。
- 生成命令（先停 5000 后端进程避免 dll 占用）：

```powershell
dotnet ef migrations add AddMallTagType --project src/QPS.Persistence --startup-project src/QPS.Host
```

### 3.3 后端 DTO 与处理器

**`web-api/src/QPS.Mall/Contracts/DTOs/MallTagDtos.cs`**
- `MallTagDto` 增加 `string Type`。
- `MallTagRequest` 增加 `string Type { get; set; } = "Product"`。

**`web-api/src/QPS.Mall/Application/Features/MallTags/MallTagHandlers.cs`**
- `GetMallTagsQuery` 增加 `string? Type`；查询非空时 `Where(tag => tag.Type == request.Type)`，排序保持 `SortOrder, Name`。
- `Handle(CreateMallTagCommand)`：`Normalize` 返回值增加 `Type`，`EnsureUniqueAsync` 增加 `type` 参数，唯一判断限定同类型。
- `Handle(UpdateMallTagCommand)`：保留原 `tag.Type`，唯一校验使用原类型；请求里的 `Type` 忽略。
- `EnsureUniqueAsync` 签名改为 `(string type, string code, string name, Guid? excludeId, ...)`，条件加 `tag.Type == type`；错误文案改为「同类型下标签编码或名称已存在」。
- `Normalize` 的类型处理：`request.Type.Trim()`，空值默认 `Product`，**不校验白名单**，仅限制 ≤20 字符。
- `Normalize` 的编码改为**自由输入**：仅 `Trim()`，不再 `ToUpperInvariant()`、不加固定前缀、不自动生成；仍要求非空且 ≤50 字符。
- `Normalize` 的颜色校验放宽：允许 `#RGB` / `#RRGGBB` 十六进制颜色，也兼容历史值（如 `success` 等 Element 标签色名），长度 ≤20；空值仍默认 `success`。
- 404 文案统一改为「标签不存在」。

**`web-api/src/QPS.Mall/Controllers/Admin/MallTagController.cs`**
- `GetTags` 增加 `[FromQuery] string? type`，透传 `Type`。

**新增 `web-api/src/QPS.Mall/Controllers/Mobile/MallTagController.cs`**
- `[Route("api/mall/tags")]`，无鉴权。
- `GET`：参数 `[FromQuery] string? type`（默认 `Review`）、`[FromQuery] bool? isActive`（默认 `true`）；类型为任意字符串，仅按值过滤；返回 `MallTagDto` 列表。

**`web-api/src/QPS.Mall/Application/Features/MallProducts/MallProductRules.cs`**
- `GetActiveTagsAsync` 查询条件增加 `tag.Type == MallTagType.Product`，避免评价标签被挂到商品。
- 商品详情/列表返回的 `tags` 无需额外过滤（关联表里只可能是商品标签，历史数据默认也是 Product）。

**`web-api/src/QPS.Persistence/Seeds/MallMockDataSeeder.cs`**
- `MallTag.Create`、`tag.Update` 调用补 `MallTagType.Product`（`SyncTagsAsync` 内两处）。
- 评价标签由迁移种子数据提供，seeder 不重复插入。

### 3.4 管理端

**新建 `admin/src/views/mall/tag/index.vue`（标签管理页）**
- 不使用 `QueryPage`（标签量小且接口返回完整数组），采用普通 `el-table` + 本地筛选：
  - 顶部筛选：类型下拉（「全部」+ 从现有标签类型动态去重生成，含商品/评价及自定义类型）、关键字、状态下拉（全部/启用/停用）。
  - 表格列：类型（`el-tag`，商品=primary、评价=warning）、编码、名称、颜色（`#RRGGBB` 显示色块+色值，历史非十六进制值如 `success` 退化为文本）、排序、状态（`el-switch`，调 `changeTagStatus`）、操作（编辑、删除）。
  - 「新增标签」按钮；新增/编辑弹窗字段：
    - 类型：`el-select`（`filterable` + `allow-create`），预置「Product / Review」选项，也允许直接输入新类型；**编辑时禁用**（类型不可变）。
    - 编码：普通输入框，自由输入（不限制前缀/格式，仅必填、≤50 字），不做自动生成。
    - 名称：普通输入框，必填、≤50 字。
    - 颜色：`el-color-picker`（`color-format="hex"`，`predefine` 提供一组主题色，`show-alpha` 关闭），默认 `#2E7D32`；历史非十六进制值打开弹窗时显示为空。
    - 排序：`el-input-number`（0–9999）；是否启用：`el-switch`。
  - 删除前 `ElMessageBox.confirm`；删除失败（仍关联商品）直接展示后端文案。
  - 数据加载 `mallTagApi.getTags()` 一次，操作成功后重新拉取。

**`admin/src/api/modules/mallTag.ts`**
- 现有 `getTags(params)` 已支持传参，无需新增方法；`addTag/updateTag` 透传 `type` 字段即可。

**`admin/src/assets/json/authMenuList.json`**
- 在「商品管理」节点后插入：

```json
{
  "path": "/mall/tag",
  "name": "mallTag",
  "component": "/mall/tag/index",
  "meta": {
    "icon": "CollectionTag",
    "title": "标签管理",
    "isLink": "",
    "isHide": false,
    "isFull": false,
    "isAffix": false,
    "isKeepAlive": true
  }
}
```

**`admin/src/views/mall/product/index.vue`**
- 加载标签时改为 `mallTagApi.getTags({ type: 'Product' })`，`activeTagOptions` 只保留商品标签。
- 内嵌「管理标签」弹窗的新增/编辑 payload 固定 `type: 'Product'`，弹窗内列表仅展示商品类型；弹窗标题改「商品标签管理」。
- 内嵌弹窗的「颜色」输入框同步改为 `el-color-picker`（hex），编码仍自由输入，与标签管理页保持一致。
- 保留弹窗（不删除既有能力），新增菜单页负责全类型维护。

### 3.5 移动端

**`mobile/src/api/types.ts`**
- 新增：

```ts
export interface MallTagItem {
  id: string
  code: string
  name: string
  color: string
  sortOrder: number
}
```

**`mobile/src/api/index.ts`**
- 新增 `getReviewTags(): Promise<MallTagItem[]>`：`GET /api/mall/tags?type=Review&isActive=true`，无 token 也可请求；失败返回空数组由调用方提示。

**`mobile/src/pages/review-create/index.vue`**
- 删除 `PRESET_TAGS` 常量。
- 新增 `const tagOptions = ref<MallTagItem[]>([])`，`presetTags` 改为 `computed(() => tagOptions.value.map(tag => tag.name))`（模板 `v-for` 无需改动）。
- `onLoad` 中与订单详情并行请求 `getReviewTags()`；标签接口失败不阻断评价流程（仅空列表，展示「暂无可选标签」占位文本）。
- `toggleTag`、提交逻辑保持不变（仍提交标签名称数组）。
- 无标签时不渲染标签区，或展示灰色提示「暂无可选标签」。

### 3.6 不改动项

- 评价表 `Tags` 字段仍存名称字符串，不做关联表改造。
- 评价发布不做「标签必须存在于字典」的强校验（避免标签停用导致提交失败），保留现有数量/长度校验。
- 不改 `components`、`layout`、请求拦截器、路由守卫。

## 四、验证步骤

1. 后端：停 5000 进程 → `dotnet build QPS.sln -c Release --no-restore` 0 错误 → `dotnet ef database update` → 重启 5000。
2. 数据核对：`MallTags` 中历史标签 `Type = Product`；新增 6 条 `Type = Review`。
3. 接口：
   - `GET api/admin/mall/tags?type=Product` 仅商品标签；`?type=Review` 返回 6 条。
   - 新增同名标签：同类型 400、不同类型成功。
   - 编辑接口传 `type` 不生效（类型不变）。
   - 删除仍关联商品的标签返回 400。
   - `GET api/mall/tags?type=Review` 返回启用中的评价标签。
4. 商品校验：用评价标签 ID 创建/更新商品 → 400（`GetActiveTagsAsync` 仅商品类型）。
5. 前端构建：`admin npm run build`、`mobile npm run type-check`。
6. Playwright（管理端 5173）：
   - 「标签管理」菜单可进入；按类型筛选正确；新增评价标签 → 切到「评价」筛选可见；编辑名称、停用、删除正常。
   - 编码自由输入（如 `review-fast`、中文编码）可保存并唯一；同类型重复报错、不同类型同编码可保存。
   - 类型可自由输入新值（如 `Gift`），保存后出现在类型筛选下拉中，可按该类型查询。
   - 颜色用取色器选择 `#C2410C` 等 hex 值保存后，列表色块与编辑回显一致；历史 `success` 标签打开弹窗不报错。
   - 商品编辑的标签下拉不含评价标签；内嵌标签弹窗新增的标签类型为商品，颜色同样用取色器。
7. Playwright（移动端 5174）：
   - 发表评价页标签来自接口（管理端新增的评价标签刷新后可见）；提交后评价带标签。
   - 评价详情/全部评价页标签筛选不影响（仍按评价数据统计）。

## 五、假设与边界

- 标签类型为自由字符串，系统只约定 `Product` / `Review` 两个内置值；新增业务类型无需改后端，直接用新类型值即可。
- 商品编辑与商品保存校验固定只认 `Product` 类型；评价发布页固定只取 `Review` 类型。
- 类型创建后不可修改；如需改类型，先删除再新建。
- 迁移中的 6 条评价标签使用固定 GUID，重复执行迁移不会重复插入（迁移只执行一次）。
- 历史商品标签默认归为 `Product`，历史评价标签字符串保持不变、不迁移到关联表。

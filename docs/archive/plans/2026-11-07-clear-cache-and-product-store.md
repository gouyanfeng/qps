# 清除图片缓存 + 商品门店关联 + 促销类型丰富 实施计划

## 一、目标与范围

1. **移动端清除图片缓存**：把已实现但无入口的 `clearLocalImageCache()` 接到「我的」页。
2. **商品门店关联补齐**：后端 `MallProduct.StoreId` 全链路已存在（必填校验、查询过滤、StoreName 返回、admin 门店管理页均有），但 **admin 商品表单不传 storeId，当前新增/编辑商品会被后端 400「请选择商品所属门店」直接拒绝**。本计划补齐 admin 表单门店选择、列表展示与筛选，并在移动端详情页展示所属门店。
3. **促销类型丰富**（用户反馈「赠送、换购之类」）：管理端促销表单目前只有 3 种类型（满减/折扣/买赠），本计划扩为 7 种：满减、折扣、买赠、**满赠、满额包邮、第二件半价、首单特惠**。促销引擎是 Trigger × Action 正交策略架构，满赠/满额包邮为已有策略组合（后端仅放开校验），第二件半价/首单特惠各加一个轻量策略类。移动端零改动（促销标签读 `RuleText`，结算自动生效）。
   - 完整「换购」（结算页展示换购商品、用户勾选加价购买）涉及移动端结算改造与订单换购项模型，改动大，**本次不做**；第二件半价 + 满赠已覆盖主要营销诉求。

## 二、现状分析（已核实）

- `mobile/src/utils/imageCache.ts` L138：`clearLocalImageCache()` 已实现（删本地文件 + 清映射），**零引用**；整模块 `// #ifndef H5` 包裹，H5 构建时被裁掉。
- `mobile/src/pages/profile/index.vue`：「我的」tab；`clearLocalImageCache` 未接入。
- `MallProductRules.cs` L289-297：`EnsureStoreValidAsync` 对 `Guid.Empty` 抛 400「请选择商品所属门店」→ admin 表单目前提交必失败。
- `admin/src/api/modules/mallStore.ts`：已有 `getStoreList`（GET /admin/mall/stores 分页）。
- `admin/src/views/mall/product/index.vue`：商品弹窗表单（L112-173）无门店字段；`validateProductForm`（L1083）与 `submitProduct` payload（L1108-1118）均无 storeId。
- `mobile/src/api/index.ts` `mapProduct`（L212）未映射 storeName；`api/types.ts` `Product` 无 storeName。

### 促销链路（已核实）

- 策略架构：`ITriggerStrategy`（`FullAmount`/`BuyProduct`）× `IActionStrategy`（`Gift`/`DiscountAmount`/`DiscountRate`/`FreightFree`），注册于 `web-api/src/QPS.Mall/DependencyInjection.cs` L22-31。
- `OrderContext.IsFirstOrder` 字段**已存在**（`Actions.cs` L55，下单时按用户订单数计算），但没有对应 Trigger。
- `MallPromotionEngine.cs`：`DiscountActions={DiscountRate}`（同 SKU 择优）、`StackableActions={DiscountAmount,FreightFree,Gift}`；新增 Action 需加入可叠加组。
- `MallPromotionRules.cs` L17-25：`ValidTriggerTypes`/`ValidActionTypes` 白名单，新增类型必须在此放开；L85-143 `ValidateConfiguration` 按 triggerType/actionType 校验 config JSON。
- `MallProductPromotionEnricher.cs` L175-205 `BuildRuleText`：按 triggerType/actionType 生成移动端促销标签文本，新类型需补分支。
- admin `promotion/index.vue`：`promotionType` 0/1/2 绑死在表单下拉（L79-83）、类型名（L281-286）、规则文本（L288-302）、回显（L358-359）、payload 构造（L401-411）五处。

## 三、改动清单

### 3.1 移动端清除图片缓存

**`mobile/src/utils/imageCache.ts`**
- `clearLocalImageCache()` 返回值改为 `number`（清除的缓存文件条数），供 toast 展示。

**`mobile/src/pages/profile/index.vue`**
- 在底部品质认证卡之前插入「设置操作卡」：一行 cell「清除图片缓存」，左侧图标 + 文案，右侧箭头。
- 点击逻辑条件编译：
  - `// #ifndef H5`：调用 `clearLocalImageCache()`，toast「已清除 N 张图片缓存」（N 为 0 时提示「暂无缓存」）。
  - `// #ifdef H5`：toast「H5 端缓存由浏览器自动管理」。
- import 也用 `// #ifndef H5` 包住（H5 构建下模块为空，避免运行时 undefined）。
- 样式沿用页面现有卡片风格（白卡、圆角、`q-icon`）。

### 3.2 admin 商品门店关联

**`admin/src/views/mall/product/index.vue`**（唯一改动文件）
- 商品弹窗表单「商品分类」下方新增 `el-form-item label="所属门店" required`：
  - `el-select` 绑定 `productForm.storeId`，选项来自 `mallStoreApi.getStoreList({ page: 1, pageSize: 100, isActive: true })`。
- `validateProductForm`：`!productForm.storeId` 时 warning「请选择商品所属门店」。
- `submitProduct` payload 增加 `storeId: productForm.storeId`。
- `openProductDialog` 编辑回显：`productForm.storeId = row.storeId || ''`；门店列表为空时 warning「请先在门店管理中创建门店」。
- 列表「分类」列后新增「所属门店」列（`row.storeName || '-'`）。
- 搜索区新增「门店」下拉筛选，选中后随查询参数传 `storeId`（后端已支持）。

### 3.3 移动端展示所属门店（轻量）

- `mobile/src/api/types.ts`：`Product` 增加 `storeName: string`。
- `mobile/src/api/index.ts` `mapProduct`：映射 `storeName: p.storeName || ''`。
- `mobile/src/pages/product-detail/index.vue`：价格卡商品名下方，`product.storeName` 非空时显示一行小字「门店：×××」。

### 3.4 促销类型丰富

#### 后端（5 个文件）

**`web-api/src/QPS.Mall/Domain/Services/Triggers.cs`**
- 新增 `FirstOrderTrigger`：`TriggerType => "FirstOrder"`，`IsMatch` 返回 `ctx.IsFirstOrder`。

**`web-api/src/QPS.Mall/Domain/Services/Actions.cs`**
- 新增 `QuantityTierDiscountAction`：`ActionType => "QuantityTier"`，config `{ "discountRate": 0.5 }`（第 2 件按此比例价）。对每个受影响 item：优惠件数 = `floor(quantity / 2)`，行优惠额 = `CurrentPrice * (1 - discountRate) * floor(qty/2)`，折算回单价更新 `CurrentPrice`，置 `IsDiscounted = true`、`AppliedPromotionId`（分摊方式与 `DiscountAmountAction` 同风格）。

**`web-api/src/QPS.Mall/DependencyInjection.cs`**
- 注册 `ITriggerStrategy, FirstOrderTrigger`、`IActionStrategy, QuantityTierDiscountAction`。

**`web-api/src/QPS.Mall/Application/Features/MallPromotions/MallPromotionRules.cs`**
- `ValidTriggerTypes` 加 `"FirstOrder"`；`ValidActionTypes` 加 `"QuantityTier"`。
- `ValidateConfiguration` 补：
  - `"QuantityTier"`：`discountRate` 必须存在且 `0 < rate < 1`（否则 400「第二件折扣比例必须在 0 到 1 之间」）。
  - `"FirstOrder"`：无 config 要求。

**`web-api/src/QPS.Mall/Domain/Services/MallPromotionEngine.cs`**
- `StackableActions` 加 `"QuantityTier"`。

**`web-api/src/QPS.Mall/Application/Features/MallProducts/MallProductPromotionEnricher.cs`**（`BuildRuleText`）
- condition 补 `"FirstOrder" => "首单"`。
- benefit 补 `"QuantityTier" => $"第 2 件 {rate * 10:0.#} 折"`。

#### 管理端（`admin/src/views/mall/promotion/index.vue` 单文件）

- 促销类型下拉扩为 7 项：满减(0)、折扣(1)、买赠(2)、**满赠(3)、满额包邮(4)、第二件半价(5)、首单特惠(6)**。
- 表单规则区按类型渲染：
  - 3 满赠：满额门槛（`thresholdAmount`）+ 赠品规格（`giftSkuId`）+ 赠送数量（`giftQuantity`），复用买赠的赠品控件。
  - 4 满额包邮：满额门槛。
  - 5 第二件半价：半价比例 slider（`tierDiscountRate`，默认 0.5，可选 0.3~0.9）。
  - 6 首单特惠：折扣比例（复用 `discountPercent` slider）。
- `submitForm` payload 映射（promotionType → triggerType/actionType/config）：
  - 0：`FullAmount{minAmount}` + `DiscountAmount{amount}`
  - 1：`FullAmount{minAmount:0.01}` + `DiscountRate{rate}`
  - 2：`BuyProduct{minQuantity}` + `Gift{skuId,quantity}`
  - 3：`FullAmount{minAmount:thresholdAmount}` + `Gift{skuId,quantity}`
  - 4：`FullAmount{minAmount:thresholdAmount}` + `FreightFree{}`
  - 5：`FullAmount{minAmount:0.01}` + `QuantityTier{discountRate:tierDiscountRate}`
  - 6：`FirstOrder{}` + `DiscountRate{rate}`
- 回显 `openDialog`：按 `triggerType + actionType` 组合反推 promotionType（`FullAmount+Gift`→3、`FullAmount+FreightFree`→4、`FullAmount+QuantityTier`→5、`FirstOrder+DiscountRate`→6，原有 3 种不变；`actionConfigJson` 里读 `discountRate` 回显 `tierDiscountRate`）。
- `getPromotionTypeName` / `getPromotionRule` 补 4 种新类型的名称与规则文本。
- 表单校验：满赠需选赠品规格；满赠/满额包邮需 `thresholdAmount > 0`。
- 适用范围：满赠/满额包邮/第二件半价均沿用现有 scope 逻辑（`FullAmount` 触发时引擎按范围过滤商品行）。

#### 移动端

- 零改动：促销标签读后端 `RuleText`，第二件半价/首单折扣在结算时由引擎自动改价，满赠自动加赠品行。

## 四、不做的事

- 不做完整「换购」（结算页选换购商品、订单换购项），涉及三端大改，另立项。
- 不新增移动端促销 UI。
- 不改商品分类体系与商品数量（已确认用户所指为促销类型）。
- 不动 src/components、layout、请求拦截器。

## 五、验证步骤

1. 后端：`dotnet build E:\Code\QPS\web-api\QPS.sln --no-restore` 通过。
2. `mobile` `npm run type-check` 通过；`admin` `npm run build` 通过。
3. 管理端（5173，手工操作）：
   - 新增 4 种促销各一条：满赠（满 100 送黄芪 1 件）、满额包邮（满 99 包邮）、第二件半价、首单特惠 9 折，均保存成功。
   - 编辑回查：各类型正确回显配置；旧促销（满减/折扣/买赠）仍可正常编辑。
4. 移动端（5174）：
   - 商品详情页显示新促销标签（如「满 ¥100，赠 1 件」「首单，9 折」「第 2 件 5 折」「满 ¥99，免运费」）。
   - 结算验证：买 2 件参与第二件半价的商品，订单金额正确；首单用户享受首单特惠；满额订单运费为 0、满赠订单含赠品行。
   - 商品详情页显示「门店：×××」。
   - 「我的」页「清除图片缓存」入口，H5 提示浏览器自动管理。
5. 控制台无新增报错。

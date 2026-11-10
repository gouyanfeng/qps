# 促销两分支模型 + 换购（TradeIn）修复 实施计划

## 一、目标与背景

用户指出：现有促销规则的「适用范围」都落在**商品**上（Global/Category/Product/Sku，决定改价作用于哪些商品行），没有落在**订单价格**上的分支，换购场景无法满足。并明确要求模型分**两个分支**：

1. **适用源 = 商品**：现有行为（选商品范围，促销作用于命中商品行）。
2. **适用源 = 订单价格**：以**整个订单金额**为条件（满 X 元），动作作用于订单级（满减、满赠、包邮、换购），不关联具体商品范围。

### 现状关键发现（已核实）

- `TradeInAction`（换购）**后端已存在**且已注册（`Actions.cs` L67-86、`DependencyInjection.cs` L29、`MallPromotionRules.cs` L24/L131 白名单与校验、Enricher L200-204 规则文本），但它把换购品塞进 `GiftSkus` 通道——下单时与赠品同路：**unitPrice=0、isGift=true、自动加入订单**（`CreateMallOrderCommand.cs` L355-401），即换购品被 0 元白送，且用户无法选择、不按 `tradeInPrice` 收费。这是本次要修复的核心错误。
- `GiftSkuRow`（`PromotionTypes.cs` L89-95）无价格字段。
- `MallPromotionEngine.Match`：scope 过滤后以命中行金额构造 trigger 上下文；`TradeIn` 在 `StackableActions`（L9）中被自动叠加。
- `MallOrderItem.Create` 已支持 `isGift/promotionId/promotionName/originalUnitPrice`（`MallOrderItem.cs` L115-128）——换购项可直接用 `isGift:false + unitPrice=tradeInPrice + originalUnitPrice=sku 原价`。
- `CreateMallOrderRequest`（`MallOrderDtos.cs` L158-185）无换购项字段；Mobile 无 `MallPromotionController`。
- `MallProductPromotionEnricher.EnrichAsync`（L29-67）把所有启用促销按 scope 挂到商品/SKU 详情——订单级促销也会被错误挂上。
- 移动端 checkout 页：`subtotal`（L251）、`finalPrice`（L255）、`submitOrder` 传 `{skuId,quantity}`（L337-346）。
- 管理端 `promotion/index.vue`：promotionType 0/1/2（满减/折扣/买赠），类型名 L281、规则文本 L288、回显 L358、payload L401-411。

## 二、方案设计：ScopeType 新增 "Order" 值

不新增数据库字段（ScopeType 为字符串，无约束、无需迁移），以 `ScopeType == "Order"` 表达「适用源=订单价格」分支：

| 分支 | ScopeType | 门槛统计 | 动作作用点 |
|---|---|---|---|
| 商品 | Global/Category/Product/Sku（现状） | 命中商品行金额 | 商品行改价/赠品 |
| 订单价格 | **Order（新增）** | **全订单原价合计** | 订单级：整单减、满赠、包邮、换购资格 |

## 三、改动清单

### 3.1 后端

**`web-api/src/QPS.Mall/Domain/Services/MallPromotionEngine.cs`**
- `FilterByScope` 加 `"Order" => items.ToList()`（门槛按全订单金额统计，与 Global 同路径）。
- `Match` 中跳过 `ActionType=="TradeIn"` 的促销（换购改为用户选择式，不再自动进 matches / 不自动加换购品）。

**`web-api/src/QPS.Mall/Domain/Services/Actions.cs`**
- `TradeInAction.Execute` 改为空操作（防御性：引擎已跳过；保留注册避免 `_actions.Get` 抛异常）。注释说明换购走下单 `ExchangeItems` 链路。

**`web-api/src/QPS.Mall/Application/Features/MallPromotions/MallPromotionRules.cs`**
- `ValidScopeTypes`（如有白名单）加 `"Order"`；`TriggerType=="FullAmount"` 时允许 `ScopeType=="Order"`。
- `TradeIn` config 校验补强：`tradeInPrice > 0`（现有 L131 校验基础上补价格正数校验）。

**`web-api/src/QPS.Mall/Application/Features/MallProducts/MallProductPromotionEnricher.cs`**
- `AppliesToSku` 对 `promotion.ScopeType == "Order"` 返回 `false`——订单级促销不挂商品详情（避免商品页出现与单品无关的促销标签）。

**新建 `web-api/src/QPS.Mall/Controllers/Mobile/MallPromotionController.cs`**
- `GET api/mall/promotions/exchange?amount={goodsAmount}`（JWT 会员身份，风格照现有 Mobile controller）。
- 新 Query `GetMallExchangeOffersQuery`（`Application/Features/MallPromotions/`）：
  - 查启用、未删、时间窗内、`ActionType=="TradeIn"`、`ScopeType=="Order"`、`TriggerType=="FullAmount"` 且 `minAmount <= amount` 的促销（`maxUsagePerUser` 校验当前用户 usage）；
  - 联表 `MallSkus/MallProducts`（显式 join，遵循项目约定）返回：
    `MallExchangeOfferDto { PromotionId, PromotionName, MinAmount, SkuId, ProductName, SpecName, ProductImage, OriginalPrice, TradeInPrice, MaxQuantity }`。

**`web-api/src/QPS.Mall/Contracts/DTOs/MallOrderDtos.cs`**
- 新增 `CreateMallOrderExchangeItemRequest { Guid PromotionId; Guid SkuId; int Quantity; }`；
- `CreateMallOrderRequest` 加 `List<CreateMallOrderExchangeItemRequest> ExchangeItems = new();`

**`web-api/src/QPS.Mall/Application/Features/MallOrders/CreateMallOrderCommand.cs`**（核心新增段）
- 促销引擎处理后、生成订单项前，处理 `request.ExchangeItems`：
  1. 空列表直接跳过（兼容旧调用）；同一 PromotionId 重复 → 400。
  2. 逐项校验：促销存在/启用/未删/时间窗/`ActionType=="TradeIn"`/`ScopeType=="Order"`；config 解析 `skuId/tradeInPrice/quantity`；请求 skuId 与 config 一致；`1 <= quantity <= config.quantity`；换购 SKU 存在、启用、库存足够。
  3. 门槛：正常商品原价合计（不含换购金额）≥ `minAmount`，否则 400「订单金额未达到换购门槛」。
  4. `maxUsagePerUser` 校验。
- 订单金额：`goodsAmount += tradeInPrice × quantity`；换购 SKU 计入运费计重（LineAmount 用换购价）。
- 订单项：`MallOrderItem.Create(..., unitPrice: tradeInPrice, quantity, weightGram, isGift: false, promotionId, promotionName, originalUnitPrice: sku.UnitPrice)`；扣库存 + `MallStockLog(OrderDeduct)`。
- usage：换购促销并入现有 usage 写入段（L403-421 模式）。

### 3.2 管理端（`admin/src/views/mall/promotion/index.vue` 单文件）

- 表单顶部新增「适用源」radio：**商品** / **订单价格**（默认商品）。
- 商品分支：现有满减/折扣/买赠 + 范围选择，逻辑不变。
- 订单分支（`scopeType='Order'`）：隐藏范围选择；类型下拉：满减(3)、满额赠品(4)、满额包邮(5)、**换购(6)**：
  - 满减：满额门槛；满额赠品：门槛 + 赠品规格/数量；包邮：门槛；换购：门槛 + 换购规格（`skuOptions`）+ 换购价（`tradeInPrice`，>0）+ 限购数量（`quantity`，默认 1）。
- payload 映射（订单分支统一 `scopeType:'Order'`、`scopes:[]`、`triggerType:'FullAmount'`）：
  - 3：`DiscountAmount{amount}`；4：`Gift{skuId,quantity}`；5：`FreightFree{}`；6：`TradeIn{skuId,tradeInPrice,quantity}`。
- 回显 `openDialog`：`scopeType==='Order'` → 适用源=订单价格，按 actionType 反推类型（DiscountAmount→3、Gift→4、FreightFree→5、TradeIn→6）。
- `getPromotionTypeName` / `getPromotionRule` 补 4 种订单级类型（换购规则文本：`满 ¥X 可 ¥Y 换购指定商品（限 Z 件）`）。
- 校验：订单分支门槛 > 0；换购需选规格、换购价 > 0、限购 ≥ 1。

### 3.3 移动端

**`mobile/src/api/`（types.ts + index.ts）**
- `ExchangeOffer { promotionId, promotionName, minAmount, skuId, productName, specName, productImage, originalPrice, tradeInPrice, maxQuantity }`；
- `getExchangeOffers(amount: number)`：`GET /api/mall/promotions/exchange?amount=`；
- `createOrder` 请求体加可选 `exchangeItems: { promotionId, skuId, quantity }[]`。

**`mobile/src/pages/checkout/index.vue`**
- `onShow` 在购物车金额就绪后调 `getExchangeOffers(subtotal)`（失败静默为空）。
- 优惠券区下方新增「优惠换购」卡片（offers 非空显示）：商品图 + 名称/规格 + 原价划线 + 换购价（主题色）+ 数量步进（0 ~ maxQuantity）。
- `exchangeAmount` computed = Σ(数量 × tradeInPrice)；`finalPrice` = subtotal + exchangeAmount - 券；确认弹窗文案含换购金额。
- `submitOrder`：换购数量 > 0 时传 `exchangeItems`。

## 四、不做的事

- 不改满减/折扣/买赠在「商品」分支下的现有行为；不做换购金额计入门槛的配置（固定不计入，防套娃）。
- 订单详情页换购项原价划线展示（`originalUnitPrice` 已入库）另做优化。
- 不动 `src/components`、请求拦截器等共享层；无数据库迁移。

## 五、验证步骤

1. 后端：`dotnet build E:\Code\QPS\web-api\QPS.sln --no-restore` 通过；`dotnet test ... --no-build` 通过。
2. `mobile` `npm run type-check`、`admin` `npm run build` 通过。
3. 管理端（5173）手工操作：
   - 适用源=订单价格，新增换购促销（满 100 元，9.9 元换购黄芪 100g，限 1 件）保存成功，编辑回查正确；
   - 新增订单满减/满额包邮各一条；旧商品分支促销（满减/折扣/买赠）编辑回显不受影响。
4. 移动端（5174）：
   - 购物车金额 ≥ 100 进入结算页出现「优惠换购」卡片；选 1 件后总价 = 商品金额 + 9.9 - 券；提交成功，订单详情含换购行（换购价、促销名，非 0 元赠品）；
   - 金额不足门槛不展示换购卡片；超额数量提交被 400 拦截；
   - 商品详情页不再出现订单级促销标签。
5. 控制台无新增报错。

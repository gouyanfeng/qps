# 促销价格展示改造（三项待办）实施计划

## Context

移动端商品展示已完成后端驱动的折后价/满减角标，但仍有三个问题需要处理：

1. **价格明细只有“促销优惠”一个合计**，用户无法区分“折后优惠（DiscountRate）”和“满减优惠（DiscountAmount）”。
2. **购物车与订单页商品信息没有满减角标、没有折后划线价**：购物车接口从未返回角标/折后价字段，前端 `item.badge` 恒为空；订单接口只有下单快照，需要区分历史成交价与实时促销。
3. **分类页点击任一二级类目后其他二级消失**：筛选胶囊的名称是从当前已加载商品反推的，点二级后商品只剩当前分类，导致其余胶囊名称取不到而被过滤。

本计划按顺序落地：A 优惠分项 → B 购物车/订单页展示 → C 分类页 bug。

---

## A. 优惠金额按类型拆分

### 后端

- [PromotionTypes.cs](file:///e:/Code/QPS/web-api/src/QPS.Mall/Domain/Services/PromotionTypes.cs)
  - `ApplyResult` 增加三个只读汇总（从 `Items.SelectMany(r => r.AppliedPromotions)` 按 `ActionType` 分组求和）：
    - `RateDiscountAmount`（DiscountRate 折后优惠）
    - `AmountDiscountAmount`（DiscountAmount 满减优惠）
    - `PromotionDiscountAmount = Rate + Amount`（合计，供兼容）
  - `Gift` / `FreightFree` 不产生金额，不进入任何分项。
- [MallPromotionPreviewService.cs](file:///e:/Code/QPS/web-api/src/QPS.Mall/Application/Features/MallPromotions/MallPromotionPreviewService.cs)
  - `MallPromotionPreview` 增加 `RateDiscountAmount`、`AmountDiscountAmount`；`DiscountAmount` 改为两者之和（与现有 `baseAmount - totalAfterAmount` 对账，若有 1 分差异以记录汇总为准并补断言）。
  - `Empty()` 同步返回 0/0。
- [MallCartDtos.cs](file:///e:/Code/QPS/web-api/src/QPS.Mall/Contracts/DTOs/MallCartDtos.cs)
  - `MallCartDto` 增加 `RateDiscountAmount`、`AmountDiscountAmount`，保留 `PromotionDiscountAmount`。
- [MallOrderDtos.cs](file:///e:/Code/QPS/web-api/src/QPS.Mall/Contracts/DTOs/MallOrderDtos.cs)
  - `MallOrderPreviewDto` 增加同名字段；`PayableAmount` 注释同步。
- [GetMallCartQuery.cs](file:///e:/Code/QPS/web-api/src/QPS.Mall/Application/Features/MallCarts/GetMallCartQuery.cs) L152、[PreviewMallOrderQuery.cs](file:///e:/Code/QPS/web-api/src/QPS.Mall/Application/Features/MallOrders/PreviewMallOrderQuery.cs) L104 映射两个分项。
- 订单侧（`GetMallOrdersQuery`、`MallOrderDetailLoader`、`MallOrderItem`）**不改**：订单只有下单合计，无分项列。

### 前端

- [api/index.ts](file:///e:/Code/QPS/mobile/src/api/index.ts)：`CartSummary`、`OrderPreview`、`MallCartResponse` 增加 `rateDiscountAmount`、`amountDiscountAmount`；`mapCartSummary` 透传。
- [q-checkout-bar.vue](file:///e:/Code/QPS/mobile/src/components/q-checkout-bar/q-checkout-bar.vue)：新增可选 props `rateDiscount`、`amountDiscount`（默认 0）；明细抽屉把“促销优惠”一行拆为“折后优惠 / 满减优惠”两行，金额为 0 的行不显示；“已省”合计仍按 `promotionDiscount + couponDiscount`。
  - ⚠️ 属共享组件，仅 cart/checkout 两处使用，新增 props 向后兼容；需确认后修改。
- [cart/index.vue](file:///e:/Code/QPS/mobile/src/pages/cart/index.vue)：传入两个新 props。
- [checkout/index.vue](file:///e:/Code/QPS/mobile/src/pages/checkout/index.vue)：页内“促销活动”行拆为“折后优惠 / 满减优惠”两行（0 不显示），逐条促销明细保留；`q-checkout-bar` 传新 props。

### 测试

新建 `web-api/tests/QPS.UnitTests/Application/Features/MallOrders/MallOrderPreviewDiscountSplitTests.cs`，复用 `TestSqliteDbContextFactory` + `PreviewMallOrderHandler`（参考 `MallOrderPreviewFreightTests.cs` 的装配方式），覆盖：

1. 只有折扣：rate > 0、amount = 0。
2. 只有满减：amount > 0、rate = 0。
3. 折扣 + 满减 + 买赠：分项之和 == `PromotionDiscountAmount`，赠品不产生金额。
4. 折扣 + 满减 + 券：分项之和 == `PromotionDiscountAmount`，`PayableAmount == GoodsAmount - 促销合计 - 券 + 运费`，与抽屉两行金额吻合。

另在 `MallPromotionEngineTests` 补 1–2 条 `ApplyResult` 分项断言（同一行同时命中折扣与满减）。

---

## B. 购物车角标/折后价；订单页只展示历史快照

### 事实结论

| 接口 | 角标字段 | 折后价字段 | 说明 |
|---|---|---|---|
| GET /mall/products | `promotionBadgeText/Color` | `skus[].discountedPrice/discountLabel` | 已具备 |
| GET /mall/carts | 无 | 无 | 只有 `price` 原价 |
| 订单列表/详情 | `badge` 列存在但恒空（无快照） | `originalUnitPrice`、`promotionName` 已投影 | 只能展示历史成交价 |

### 后端

- [MallCartDtos.cs](file:///e:/Code/QPS/web-api/src/QPS.Mall/Contracts/DTOs/MallCartDtos.cs)：`MallCartItemDto` 增加 `PromotionBadgeText`、`PromotionBadgeColor`、`DiscountedPrice`、`DiscountLabel`。
- [GetMallCartQuery.cs](file:///e:/Code/QPS/web-api/src/QPS.Mall/Application/Features/MallCarts/GetMallCartQuery.cs)：
  - 注入 `MallProductPromotionEnricher`；
  - 对购物车涉及的商品构造 `MallProductDto`（含对应 `MallSkuDto.Price`），调用 `EnrichAsync`，再把商品角标与 SKU 折后价/标签映射到 `MallCartItemDto`；
  - 判定与商品列表完全同一条链路（`MallPromotionEngine.AppliesToSku` + 最优折扣率），不新增第三套口径。
- 订单接口不改；`Badge` 保持空，不用实时促销覆盖历史价。

### 前端

- [api/index.ts](file:///e:/Code/QPS/mobile/src/api/index.ts) / [types.ts](file:///e:/Code/QPS/mobile/src/api/types.ts)：`CartItem` 增加 `promotionBadgeText/Color`、`discountedPrice`、`discountLabel`；`mapCartItems` 映射。
- [cart/index.vue](file:///e:/Code/QPS/mobile/src/pages/cart/index.vue)：
  - 图片角标改用 `item.promotionBadgeText`（与分类页同一 `.promo-corner` 样式）；
  - 价格显示 `discountedPrice ?? price`，有折扣时旁边划线显示 `price`；小计按显示单价 × 数量。
- [orders/index.vue](file:///e:/Code/QPS/mobile/src/pages/orders/index.vue) L92-95：成交价 `unitPrice`，有 `originalUnitPrice > unitPrice` 时划线；`promotionName` 作为小号促销文案。
- [order-detail/index.vue](file:///e:/Code/QPS/mobile/src/pages/order-detail/index.vue) L138-151：同上，展示下单时促销名称与划线原价；不加实时满减角标。

---

## C. 分类页二级类目消失

**根因**：[category/index.vue](file:///e:/Code/QPS/mobile/src/pages/category/index.vue) L216-221 `filterTabs` 用 `products.value` 的 `categoryName` 反推二级名称；点击二级后 `loadProducts(true)` 只返回该分类商品，其他二级名称取不到，被 `.filter(item => item.name)` 过滤掉。

**修法**：

- `SideNavItem` 增加 `childTabs: FilterTab[]`，挂载时直接由 `category.children` 的 `id/name` 生成（不再从商品推导）。
- `filterTabs` 直接返回 `[{ all }, ...activeNav.childTabs]`，与商品加载解耦。
- `selectSide` / `selectFilter` 的请求逻辑保持不变；切换一级仍会重置到“全部”并重建商品列表。
- 不重载分类树。

---

## 验证

1. 后端：`dotnet build "E:\Code\QPS\web-api\QPS.sln" --no-restore` 0 错误；`dotnet test` 全绿。
2. 移动端：`npm run type-check` 0 错误。
3. 真实页面（5174 + 5000）用 Playwright/Edge：
   - 结算页与购物车明细抽屉：折扣与满减两行、0 行不显示、已省不变 → 截图。
   - 购物车：有满减的商品图有角标、折扣商品显示折后价 + 划线 → 截图；订单详情展示历史价与促销文案 → 截图。
   - 分类页轮流点三个二级类目：其他二级始终可见、商品随点击变化 → 3 张截图。
   - 截图统一存 `E:\Code\QPS\ui-tests\screenshots\`。
4. 不 commit，保留 5173/5174/5000 服务进程。

## 影响与确认

- 唯一共享层改动是 `q-checkout-bar.vue` 新增两个可选 props（仅新增渲染行，不改变现有 props 语义），cart/checkout 两处消费者同步传值。
- 其余改动均在业务页面与 Mall 业务模块内。

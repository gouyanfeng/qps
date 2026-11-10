# 促销现状事实稿（代码为准）

> 状态：草稿，仅用于批次 3 合并前裁决；只读核对，未改任何代码与 `docs/`。
> 核对日期：2026-11-07。对应代码：`web-api/src/QPS.Mall`。

## 0. 三句话结论

1. 当前只能创建 **3 种促销**（满减 / 折扣 / 买赠）、**4 级作用域**（Global / Category / Product / Sku）。优先级、可叠加标记、触发器和次数限制都已在迁移中删除，属于历史设计。
2. 门槛口径**不统一**：满减按“折后、且只算作用域内商品”的金额复检；折扣与买赠按“原价、且只算作用域内商品”的合计判断。
3. 三份文档里，只有 `docs/domain/商城促销规则.md` 能当底稿（但有 4 处硬错误）；`promotion-price-display.md` 是已落地的展示变更记录；`promotion-exchange.md` 和 `clear-cache-and-product-store.md` 的促销部分是未落地的旧方案。

## 1. 代码事实

### 1.1 类型与实现

| type / actionType | 后台可创建 | 引擎实现 | 事实 |
|---|---|---|---|
| 满减 `DiscountAmount` | 是 | 是 | 主流类型，按作用域内商品分摊减免 |
| 折扣 `DiscountRate` | 是 | 是 | 按 SKU 单价打折，一个 SKU 只取优惠最大的一条 |
| 买赠 `Gift` | 是 | 是 | 达门槛后追加 0 元赠品行 |
| 免运费 `FreightFree` | **否** | 是 | 死代码：`MallPromotionRules` 白名单不含、`MallPromotionType` 枚举没有；只有引擎、Action 和运费计算保留 |
| 换购 / 首单 / 第 N 件 / 阶梯 / 满件 | 否 | 否 | 无实体字段、无 Action、无迁移 |

`promotionType` 只是展示分类，引擎只认 `actionType`；后台提交时按类型映射 actionType，但没有跨字段一致性校验。

### 1.2 作用域与命中

- 四种 `scopeType`：`Global` 全店、`Category` 分类（含子分类）、`Product` 单商品、`Sku` 单规格。
- 作用域由 `MallPromotionScopes` 目标集合决定；`Global` 不需要目标行。
- 一条促销没有目标行且不是 `Global` 时，命中为空。
- 匹配只用“作用域内商品的原价合计 ≥ `thresholdAmount`”。

### 1.3 生效时间

- 条件：`IsActive = true`，`StartDate <= 当前时间`，`EndDate` 为空或 `当前时间 < EndDate`（**右开区间**）。
- 没有“每人/每单限用次数”字段，也没有核销表；`20261106123000_RemoveMallPromotionUsageResidue` 已把残留下的字段删掉。

### 1.4 应用顺序、叠加与互斥

1. 先应用所有命中的 **折扣**：逐 SKU 选优惠金额最大的一条；金额相同按作用域更具体、`CreatedAt` 更早、`Id` 更小。
2. 再应用 **满减**：按“作用域更具体优先、优惠金额更大优先”排序；逐条试算，若其覆盖的任一 SKU 已被上一条满减改过价，则整条跳过；不冲突的满减可以多条同时生效。
3. **折扣与满减可叠加**：折扣先改单价，满减在折后金额上再减。
4. **满减执行时会再校验一次门槛**，基准是“当前（折后）作用域内小计”，不是匹配时的原价。
5. 满减减免额取 `min(配置金额, 当前作用域小计)`，不会把商品价减成负数，也不会减到运费。
6. 同一 SKU 不会同时吃两条满减；但同一订单可以出现多条作用于不同商品的满减。
7. 没有订单级 Scope，因此“全店满减”本质是 `Global` 作用域，门槛按整单原价，执行时按整单折后金额复检。

### 1.5 门槛口径（关键差异）

| 类型 | 匹配时判断 | 执行时判断 |
|---|---|---|
| 满减 | 作用域内**原价**合计 | 作用域内**折后**小计（不满足则整条不生效） |
| 折扣 | 作用域内**原价**合计 | 不再判断 |
| 买赠 | 作用域内**原价**合计 | 不再判断 |

补充事实：

- `thresholdAmount` 在后台是必填且 `> 0`（最小 0.01），所以“无门槛折扣/买赠”只能填 0.01 近似。
- 分类作用域只算该分类（含子分类）商品，不算同单其他商品。
- 运费不参与任何门槛。

### 1.6 金额计算

- 折扣：`新单价 = round(原单价 × rate, 2)`，`rate` 取值 `0.1–1`（后台滑杆，0.8 即 8 折）。
- 满减：按行金额占比分摊到分，最后一行吃掉尾差，保证 `Σ行优惠 = 订单优惠`。
- 订单金额链路：`商品原价合计 → 促销优惠 → 优惠券（按促销后金额校验门槛并抵扣）→ 运费（命中免运费则清零）→ 应付`。
- 订单快照记录：`goodsAmount`、`promotionDiscountAmount`、`couponDiscountAmount`、`couponName`、`freightAmount`；明细行记录 `originalUnitPrice`、成交单价、`promotionName`、`promotionSnapshot`、`promotionDiscountAmount`。

### 1.7 买赠与履约

- 赠品按配置数量生成 **0 元订单行**（`IsGift = true`），不计入商品金额与优惠金额。
- 一条买赠促销在一次下单里只发 **一次** 配置数量，不按“满几个门槛发几份”叠加。
- 赠品库存用条件更新 `Stock >= quantity` 扣减：
  - 扣减成功 → 写赠品行 + 库存流水；
  - 库存不足 / 赠品下架或删除 → **不发放该赠品**，把原因写进订单 `GiftShortageMessage`，**不阻断下单**；
  - 主商品库存不足 → 抛业务异常，整单回滚。
- 赠品不参与运费计算（按商品行金额算运费，赠品 0 元；重量/体积也未计入运费行）。

### 1.8 展示口径

| 位置 | 显示什么 | 是否按门槛 |
|---|---|---|
| 商品列表 / 首页 / 分类 / 活动页 | 角标 + `discountedPrice`（仅折扣生效时） | **忽略折扣门槛**，让用户先看到折扣价 |
| 商品详情 | 折后价 + 角标 | 同上；不展示规则文案 |
| 购物车 | 行角标 + 行折后价 + 底部“折扣/满减”分项 + 赠品清单 | 按真实门槛（选中商品） |
| 结算预览 | 商品金额、促销分项、促销后金额、券、运费、应付 + 赠品 | 按真实门槛（提交的商品） |
| 订单 / 订单详情 | 下单快照（成交价、原价、促销名、赠品行） | 不重算，不跟随促销变更 |

细节：

- 角标只覆盖“满减”和“折扣”；**买赠没有角标**。
- 后端商品 DTO 会返回 `promotions`（含 `ruleText`），但移动端没有任何页面渲染这份规则列表，只用了 `discountedPrice` 和角标。
- 列表页的“满减角标”是单 SKU 试算结果：单件不满足门槛时通常不显示，命中才显示。
- 购物车合计只减促销，不减券、不加运费；券和运费在结算页才算。

### 1.9 与活动专题的关系

- `MallCampaign`（活动专题）与 `MallPromotion` 是两套独立数据，没有外键关联。
- 活动页只负责聚合商品并复用商品列表的促销展示；活动本身不产生额外优惠。
- 迁移 `20261105051401_RemoveMallActivityPromotion` 已删除旧的“活动挂促销”字段。

### 1.10 代码地图（核对入口）

| 关注点 | 文件 |
|---|---|
| 匹配与应用 | `Domain/Services/MallPromotionEngine.cs` |
| Action 实现 | `Domain/Services/Actions.cs` |
| 实体与枚举 | `Domain/Entities/MallPromotion.cs`、`MallPromotionScope.cs` |
| 创建/更新校验 | `Application/Features/MallPromotions/MallPromotionRules.cs` |
| 生效查询 | `Application/Features/MallPromotions/MallPromotionEffectiveQuery.cs` |
| 上下文与作用域 | `Application/Features/MallPromotions/MallPromotionContextBuilder.cs` |
| 试算 | `Application/Features/MallPromotions/MallPromotionPreviewService.cs` |
| 商品展示补全 | `Application/Features/MallProducts/MallProductPromotionEnricher.cs` |
| 购物车 | `Application/Features/MallCarts/GetMallCartQuery.cs` |
| 下单 | `Application/Features/MallOrders/CreateMallOrderCommand.cs`、`OrderSnapshotBuilder.cs` |
| 优惠券 | `Application/Features/MallCoupons/MallCouponService.cs` |

## 2. 三份文档逐条对比

### 2.1 `docs/domain/商城促销规则.md`

| 文档条目 | 代码事实 | 判定 |
|---|---|---|
| 促销 = 门槛 + 优惠，3 种类型 | 一致 | ✅ 是 |
| 4 级作用域 Global/Category/Product/Sku | 一致，分类含子分类 | ✅ 是 |
| 满减门槛“按参与商品原价合计，不含运费” | 匹配按原价，但执行按**折后**小计复检 | ⚠️ 半对，必须改 |
| 折扣按单品单价，不作用运费和赠品 | 一致 | ✅ 是 |
| 买赠达门槛送 `giftQuantity` 件 | 一致，但一次下单只送一次 | ⚠️ 需补“不按倍数叠加” |
| 计算前先排除“超出每人/每单使用次数”的活动 | 无此字段、无核销表，迁移已删除 | ❌ 不符（历史遗留） |
| 同一商品命中多条取优惠最大 | 折扣按 SKU 取最优；满减按覆盖冲突与金额排序，不冲突可叠加 | ⚠️ 需改写为规则 |
| 未提折扣 + 满减叠加 | 实际可叠加，且满减在折后金额上算 | ❌ 缺关键规则 |
| 未提赠品库存不足 / 0 元行 / 快照 | 已有明确实现 | ❌ 缺关键规则 |
| 未提展示口径、券与运费顺序 | 已实现 | ❌ 缺章节 |
| 未提 FreightFree 死代码 | 引擎有实现但不可创建 | ❌ 缺“现状说明” |

**结论**：结构可用，事实层有 4 处必须改：门槛口径、使用次数、叠加/互斥、赠品履约。

### 2.2 `.trae/documents/promotion-price-display.md`

| 文档条目 | 代码事实 | 判定 |
|---|---|---|
| 购物车只有总优惠、商品行无角标 | 已修复：行角标 + 行折后价 + 分项 | ✅ 已落地（文档描述的是修复前） |
| 结算页只有“优惠”一行 | 已修复：折扣 / 满减分项 + 促销明细 | ✅ 已落地 |
| 后端补 `RateDiscountAmount` / `AmountDiscountAmount` | 购物车、试算、结算 DTO 均有 | ✅ 已落地 |
| 列表页忽略折扣门槛、购物车按门槛 | 一致 | ✅ 是 |
| 分类页二级分类 tab 修复 | 已落地 | ✅ 已落地 |
| 促销类型仍为 3 种 | 与代码一致 | ✅ 是 |

**结论**：这是一份“已完成的展示修复记录”，不是规则文档。事实基本准确，但描述对象是修复前状态；合并时应抽取“展示口径”一节，其余作为变更记录归档。

### 2.3 `.trae/documents/promotion-exchange.md`

| 文档条目 | 代码事实 | 判定 |
|---|---|---|
| 引擎是 Trigger × Action 正交架构 | 当前是 v2 直连策略，`Triggers.cs`、`ITriggerStrategy` 已不存在 | ❌ 架构已废弃 |
| 订单级 Scope（`Order`） | 只有 4 种 Scope，无 Order | ❌ 未实现 |
| 换购 `TradeIn` Action | 白名单与枚举均无 | ❌ 未实现 |
| 首单 / 第 N 件 / 满件触发 | 无字段、无代码 | ❌ 未实现 |
| `StackableActions`、可叠加标记 | 迁移 `20261031031325_RemovePromotionPriorityCombinable` 已删 | ❌ 未实现 |
| 使用次数限制 | 迁移 `20261106123000_RemoveMallPromotionUsageResidue` 已删 | ❌ 未实现 |
| 免运费 `FreightFree` | 引擎有、不可创建 | ⚠️ 半实现 / 死代码 |
| `ApplyResult.AppliedPromotionId` 等字段 | 当前字段为 `RateDiscountAmount` / `AmountDiscountAmount` / `GiftSkus` / `IsFreightFree` | ❌ 与现码不符 |

**结论**：整份是旧版促销扩展方案，与当前代码不对应。不能作为合并素材，只能作为历史方案归档或丢弃。

### 2.4 `.trae/documents/clear-cache-and-product-store.md`

| 文档条目 | 代码事实 | 判定 |
|---|---|---|
| 促销扩展为 7 种类型 | 只有 3 种可创建 | ❌ 未落地 |
| 促销引擎改为 Trigger × Action | 已回退为 v2 直连策略 | ❌ 方案废弃 |
| `OrderContext.IsFirstOrder`、`QuantityTier` 等 | 无代码 | ❌ 未落地 |
| `FreightFree` 作为可配置权益 | 死代码，后台不可创建 | ❌ 未落地 |
| 图片缓存清理入口 | 已落地（profile 页 + `clearLocalImageCache`） | ✅ 已落地 |
| 商品 `storeId` | 已落地（商品页与实体） | ✅ 已落地 |

**结论**：三块内容里两块已落地、促销块整体未落地且前提已失效。促销部分不得进入 `domain`，其余两块各自进变更记录。

## 3. 裁决

**底稿：`docs/domain/商城促销规则.md`。** 它是唯一以“规则”为对象的文档，结构（类型 / 作用域 / 计算 / 字段）与当前代码骨架一致；需要按第 1 节重写事实，而不是换底稿。

**合并时的取舍**

- 保留并修正：3 种类型、4 级作用域、满减分摊、折扣按单价、买赠 0 元行、活动与促销独立。
- 删除：使用次数 / 限购、`MaxUsagePerUser` / `MaxUsagePerOrder` 字段、任何“触发器”描述。
- 新增：叠加与互斥（折扣按 SKU 最优、满减冲突跳过、折扣 + 满减可叠加）、门槛双口径、赠品不足不阻断、展示口径、券与运费顺序、FreightFree 死代码说明。
- 不采纳：`promotion-exchange.md` 的订单级 Scope / 换购 / 首单 / 第 N 件 / Trigger 架构；`clear-cache-and-product-store.md` 的 7 类型与 Trigger 架构。
- 归档：两份旧方案进 `docs/archive/`（标注“未落地、前提已失效”）；`promotion-price-display.md` 的落地部分进 `archive/changelogs/promotion-price-display.md`，展示口径抽入新文档。

## 4. 合并后文档结构（`docs/domain/mall-rules.md`）

| 章 | 一句话 |
|---|---|
| 1. 术语与适用范围 | 定义促销、门槛、作用域、优惠，说明本文只描述当前 v2 引擎。 |
| 2. 促销类型 | 只列满减、折扣、买赠三种，给出各自配置字段。 |
| 3. 作用域与命中 | 说明 4 级作用域、分类含子分类、无目标行即空命中。 |
| 4. 门槛口径 | 明确满减按折后作用域小计复检，折扣 / 买赠按原价作用域合计。 |
| 5. 优惠计算与金额恒等式 | 给出折扣取整、满减分摊、尾差处理与订单金额链路。 |
| 6. 叠加、互斥与优先级 | 折扣按 SKU 最优、满减冲突跳过、不冲突可叠加、无显式 priority 字段。 |
| 7. 买赠与履约 | 0 元赠品行、一次一发、库存不足记录不阻断、赠品不计运费。 |
| 8. 时间窗与限购 | 右开时间窗；明确当前无限购 / 次数限制。 |
| 9. 优惠券与运费 | 券按促销后金额校验并抵扣，运费最后算，免运费标记的处理。 |
| 10. 各端展示口径 | 列表忽略门槛、购物车与结算按门槛、订单只读快照、买赠无角标。 |
| 11. 活动专题与促销 | 两套独立数据，活动页只聚合商品，不产生额外优惠。 |
| 12. 未实现与已废弃 | 列出 FreightFree 死代码、Trigger 架构、换购 / 首单 / 第 N 件、次数限制。 |
| 13. 代码地图 | 指向引擎、Action、试算、下单、券、展示的核对入口。 |

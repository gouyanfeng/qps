# 商城促销与售后规则

> 状态：有效 | 最后核对：2026-11-10 | 对应代码：`web-api/src/QPS.Mall/Domain/Services/MallPromotionEngine.cs`（促销引擎）、`Application/Features/MallOrders/CreateMallOrderCommand.cs`（下单校验）、`web-api/src/QPS.Mall/Domain/MallAfterSaleRules.cs`（售后约束）、`Application/Features/MallRefunds`、`Application/Features/MallReturns`（退款/退货流程）、`admin/src/views/mall/promotion/index.vue`（管理端促销配置）。

## 1. 统一模型

每条促销由两个固定维度加一个动作构成：

```
促销 = 商品范围（Scope） + 订单门槛（ThresholdAmount） + 动作（Action）
```

- **商品范围**（`ScopeType`）：`Global` 全场 / `Category` 指定分类 / `Product` 指定商品 / `Sku` 指定规格。分类范围按祖先链匹配，选中父分类即覆盖其下子分类商品。
- **订单门槛**（`ThresholdAmount`，独立列）：范围内商品金额累计达到该值时促销生效，所有动作类型统一使用此门槛。
- **门槛金额的计算口径**：折扣、买赠、免运费按范围内商品**原价**合计判断；满减延后到应用阶段，按范围内**行级折扣后**的金额判断（见 3.1）。
- **促销类型**（`PromotionType`，int 列）：`0` 满减 / `1` 折扣 / `2` 买赠，仅用于列表筛选；真正参与计算的是 `ActionType`。

## 2. 动作类型（Action）

| 类型 | actionType | 动作配置 | 层级 | 规则文本示例 |
|---|---|---|---|---|
| 满减 | `DiscountAmount` | `{ amount }` 减免金额 | 订单级 | 满 ¥100 减 ¥20 |
| 折扣 | `DiscountRate` | `{ rate }` 折扣比例（0.1~1） | 行级 | 满 ¥100 打 8.0 折 |
| 买赠 | `Gift` | `{ skuId, quantity }` 赠品规格与数量 | 行级 | 满 ¥100 赠 1 件赠品 |
| 免运费 | `FreightFree` | `{}` 无参数 | 订单级 | 满 ¥100 免运费 |

`FreightFree` 已在引擎、动作策略与 DI 注册中落地（`Actions.cs`、`DependencyInjection.cs`），下单与预览会据 `IsFreightFree` 把运费置 0。但 `MallPromotionType` 枚举只有 0/1/2 三个值，管理端促销表单也只提供满减、折扣、买赠三项，因此当前无法从管理端创建免运费促销；新增入口时必须同步枚举、表单与校验。

## 3. 生效方式

### 3.1 引擎自动应用

促销独立生效，不依赖任何活动关联。`MallPromotionEngine` 分「匹配」与「应用」两个阶段：

**匹配（`Match`）**：按 `CreatedAt → Id` 固定顺序遍历，逐条过滤。

1. 促销启用（`IsActive`）、未删除；
2. 时间窗生效：`StartDate` 为空或 ≤ 当前时间，且 `EndDate` 为空或 > 当前时间（左闭右开）；
3. 范围匹配：筛选订单中属于促销范围的订单项，无命中行直接跳过；
4. 门槛判断：非满减动作按范围内原价合计 ≥ `ThresholdAmount` 才算命中；满减留到应用阶段判断。

**应用（`Apply`）**：顺序固定，不可调整。

1. **行级折扣**：逐行只取一条，优惠额最大者胜出；同额按作用范围具体度 `Sku > Product > Category > Global`，再按 `CreatedAt`、`Id` 稳定排序。
2. **买赠**：直接生效，不参与比价，生成 0 元赠品行（带 `IsGift` 标记与促销关联），可与折扣并存。
3. **整单满减**：门槛按各自覆盖范围内行级折扣后的金额判断，抵扣额取 `min(amount, 范围内金额)`；覆盖商品有交集的两条满减互斥，只取实际抵扣最大的一条；完全不重叠则各自生效。
4. **免运费**：直接生效，置 `IsFreightFree`。

满减与折扣/赠品可叠加；有交集的满减之间、同一行的折扣之间互斥。促销不做使用次数限制。

### 3.2 与活动的关系

活动仅承担内容展示（封面、说明、富文本详情、时间窗口），**不再关联促销**，也不影响促销是否生效、不限制活动页商品范围。

## 4. 通用控制字段

| 字段 | 说明 |
|---|---|
| `ThresholdAmount` | 订单门槛金额，范围内商品金额达到该值时生效 |
| `ScopeType` | 适用范围：`Global` / `Category` / `Product` / `Sku` |
| `StartDate` / `EndDate` | 有效期，可空 = 长期有效；左闭右开 |
| `IsActive` | 启用状态 |
| `PromotionTagId` | 可选促销标签，关联 `MallTags` 中 `Type=Promotion` 的标签，商品展示时带出 |
| `ActionType` / `ActionConfigJson` | 动作类型与动作参数 JSON |

促销不设每人/每单使用次数限制；历史字段 `MaxUsagePerUser`、`MaxUsagePerOrder` 已随迁移删除，不得重新引入。

## 5. 售后规则

售后分「整单退款」与「退货退款」两条链路，都只按整单实付金额执行，不支持部分退款、部分退货。公共约束集中在 `MallAfterSaleRules`，退款/退货 Handler 不得各自实现校验。

### 5.1 申请约束

| 约束 | 规则 | 实现位置 |
|---|---|---|
| 退款范围 | 仅整单退款；请求传入金额只做一致性校验，必须等于 `PaidAmount`（含运费） | `CreateMallRefundCommand` |
| 退货范围 | 仅整单退货；退货单金额必须等于 `PaidAmount` | `CreateMallReturnCommand`、`ConfirmMallReturnReceiptCommand` |
| 申请时效 | 已签收订单须在 `CompletedAt` 之后 7 天内申请；待发货订单的仅退款不受时效限制 | `MallAfterSaleRules.EnsureWithinAfterSaleWindow` |
| 原因 | 必填，去除首尾空白后不能为空，最长 200 字符 | `MallAfterSaleRules.RequireReason` |
| 补充说明 | 选填，空值统一存 `null`，最长 500 字符 | `MallAfterSaleRules.NormalizeDescription` |
| 凭证图片 | 最多 6 张，入库为去重后逗号分隔的相对地址 | `MallAfterSaleRules.NormalizeEvidence` |
| 退款资格 | 仅 `Paid` / `Shipped` / `Completed` 可进入退款；支付单必须存在成功记录 | `MallOrder.BeginRefund` |
| 退货资格 | 仅 `Shipped` / `Completed`，或「已退款但库存未回补」的订单可申请退货 | `MallOrder.BeginReturn` |
| 退货唯一性 | 同一订单只允许存在一条非 `Rejected` 退货单 | `CreateMallReturnCommand` |

原因在后端是自由文本（≤200 字符），不做枚举校验；前端统一提供 `不想要了`、`拍错/多拍`、`商品破损/变质`、`少件/漏发`、`质量问题`、`其他` 六个固定选项，选择「其他」时用补充说明展开。

### 5.2 状态流转

| 对象 | 流转 | 触发 |
|---|---|---|
| `MallOrder`（退款） | `Paid`/`Shipped`/`Completed` → `Refunding` → `Refunded`；失败回退到退款前状态 | `BeginRefund` → `CompleteRefund` / `FailRefund(previousStatus)` |
| `MallOrder`（退货） | `Shipped`/`Completed`/`Refunded` → `Returning` → `Returned`；拒绝回退到退货前状态 | `BeginReturn` → `CompleteReturn` / `FailReturn(previousStatus)` |
| `MallRefund` | `Pending` → `Success` / `Failed` | `MarkSuccess` / `MarkFailed` |
| `MallReturn` | `Pending` → `Approved` / `Rejected`；`Approved` 期间可多次填写寄回运单 → `Received` → `Completed` | `Approve` / `Reject` / `SubmitTracking` / `MarkReceived` / `MarkCompleted` |

退款失败时的回退状态由 `MallOrderResolver.StatusBeforeRefund` 唯一判定：已签收回 `Completed`、已发货回 `Shipped`、否则回 `Paid`；退货拒绝由 `StatusBeforeReturn` 判定，已退款订单回 `Refunded`。状态只允许由领域方法写入，Handler 不得直接赋值。

### 5.3 库存回补与优惠券

| 场景 | 库存 | 优惠券 |
|---|---|---|
| 未发货退款成功 | 立即回补，流水类型 `RefundRestore` | 释放 |
| 已发货/已签收退款成功 | 不回补，等退货入库确认时回补 | 释放 |
| 退货收货确认 | 回补一次，流水类型 `ReturnRestore` | 释放 |
| 已退款待入库订单再走退货 | 收货确认时回补，不重复建退款单 | 已释放，不再处理 |

- 回补以 `MallStockLogs` 中 `RelatedOrderNo` + `RefundRestore`/`ReturnRestore` 流水为幂等标记，由 `MallOrderResolver.HasRestoredStockAsync` / `RestoreStockOnceAsync` 判定，不新增表字段。
- 赠品行随订单明细一并回补。
- 释放调用 `MallCouponService.ReleaseByOrderNoAsync`，把该订单已使用的券条件更新回 `claimed`，并清空 `UsedAt`、`UsedOrderNo`。
- 退款/退货成功后，该订单所有 `Success` 支付单统一转为 `Refunded`。
- 状态变更、库存回补、流水写入、优惠券释放在同一事务内由 MediatR 事务行为包裹，失败整体回滚。
- 模拟退款接口仅用于非生产环境，生产环境直接返回 403。
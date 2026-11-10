# 商城促销规则

> 代码位置：`web-api/src/QPS.Mall/Domain/Services/MallPromotionEngine.cs`（引擎）、`Application/Features/MallOrders/CreateMallOrderCommand.cs`（下单校验）、`admin/src/views/mall/promotion/index.vue`（管理端配置）。

## 1. 统一模型

每条促销由两个固定维度加一个动作构成：

```
促销 = 商品范围（Scope） + 订单门槛（ThresholdAmount） + 动作（Action）
```

- **商品范围**（`ScopeType`）：`Global` 全场 / `Category` 指定分类 / `Product` 指定商品 / `Sku` 指定规格。
- **订单门槛**（`ThresholdAmount`，独立列）：范围内商品金额累计达到该值时促销生效，三种促销类型统一使用此门槛。
- **门槛金额的计算口径**：按范围内商品的订单金额累计；范围选 `Global` 时按全订单金额计算。

## 2. 三种促销类型（Action）

| 类型 | actionType | 动作配置 | 规则文本示例 |
|---|---|---|---|
| 满减 | `DiscountAmount` | `{ amount }` 减免金额 | 满 ¥100 减 ¥20 |
| 折扣 | `DiscountRate` | `{ rate }` 折扣比例（0.1~1） | 满 ¥100 打 8.0 折 |
| 买赠 | `Gift` | `{ skuId, quantity }` 赠品规格与数量 | 满 ¥100 赠 1 件赠品 |

## 3. 生效方式

### 3.1 引擎自动应用

促销独立生效，不依赖任何活动关联。`MallPromotionEngine` 在计算订单价格时自动匹配并应用：

1. 促销启用、未删除、在促销自身时间范围内、未超出每人/每单使用次数限制；
2. 范围匹配：筛选订单中属于促销范围的订单项；
3. 门槛判断：范围内商品金额合计 ≥ `ThresholdAmount`；
4. 满减直接减金额，折扣按比例折算，买赠生成 0 元赠品订单项（带 `IsGift` 标记与促销关联）。

### 3.2 与活动的关系

活动仅承担内容展示（封面、说明、富文本详情、时间窗口），**不再关联促销**，也不影响促销是否生效、不限制活动页商品范围。

## 4. 通用控制字段

| 字段 | 说明 |
|---|---|
| `ThresholdAmount` | 订单门槛金额，范围内商品金额达到该值时生效 |
| `MaxUsagePerUser` | 每人限用次数（0 = 不限） |
| `MaxUsagePerOrder` | 每单限用次数（0 = 不限） |
| `StartDate` / `EndDate` | 有效期（可空 = 长期有效） |
| `IsActive` | 启用状态 |
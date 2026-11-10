# 实体说明

> 基于 2026-11-07 的 `web-api/src` 代码整理。实体定义位于各模块 `Domain/Entities`，表映射位于 `QPS.Persistence/Configurations`，迁移位于 `QPS.Persistence/Migrations`。

## 1. 阅读约定

所有领域实体继承 `QPS.Shared/Common/BaseEntity.cs`：

| 字段 | 含义 |
|---|---|
| `Id` | 主键，Guid |
| `CreatedAt` / `CreatedBy` | 创建时间与创建人 |
| `UpdatedAt` / `UpdatedBy` | 最近更新时间与更新人 |
| `IsDeleted` | 软删除标记 |

- 软删除由 `AppDbContext.SaveChanges` 统一把 `Deleted` 转为 `IsDeleted = true`，查询由全局过滤器过滤。
- `SystemOperationLog` 与 `SystemErrorLog` 不参与软删除。
- Mall 表名在 `MallModelConfiguration` 中显式 `ToTable`；System/Crm 未显式指定的表名与 `AppDbContext` 的 `DbSet` 名称一致。

## 2. 项目职责与实体归属

| 项目 | 是否拥有实体 | 职责 | 实体/对象数量 |
|---|---|---|---|
| `QPS.Host` | 否 | 组合根、HTTP 管道、上传接口、响应包装、异常中间件 | 0 个实体；2 个上传 Controller |
| `QPS.Shared` | 否 | `BaseEntity`、响应/分页模型、JWT 配置、Pipeline、模块契约 | 0 个实体；1 个实体基类 |
| `QPS.Persistence` | 否 | `AppDbContext`、三套模型配置、迁移、种子、Identity 服务 | 0 个实体；承载全部表映射 |
| `QPS.System` | 是 | 用户、角色、权限、字典、行政区、日志 | 9 个实体 |
| `QPS.Crm` | 是 | 基地主体/基地/供应、联系人、跟进、流转、厂商、采购需求、撮合机会、业务活动 | 13 个实体 + 2 个常量类 + 1 个评分输入对象 + 1 个评分策略 |
| `QPS.Mall` | 是 | 商品、购物车、订单履约、营销、会员、内容 | 32 个实体 |

实体数据统一由 `AppDbContext` 管理，它同时实现 `ISystemDbContext`、`ICrmDbContext`、`IMallDbContext`、`IUnitOfWork`。

## 3. System 模块（9 个实体）

| 实体 | 表名 | 关键字段 | 说明 |
|---|---|---|---|
| `SystemUser` | `SystemUsers` | `RoleId`、`Username`、`PasswordHash`、`RealName`、`IsActive` | 后台账号。当前鉴权以 `RoleId → SystemRolePermissions` 为准 |
| `SystemRole` | `SystemRoles` | `Name`、`Code` | 角色 |
| `SystemPermission` | `SystemPermissions` | `Name`、`Code`、`ParentId`、`PermissionType` | 权限码树；`PermissionType` 区分菜单/按钮等类型 |
| `SystemRolePermission` | `SystemRolePermissions` | `RoleId`、`PermissionId` | 角色-权限关联，鉴权实际读取该表 |
| `SystemUserRole` | `SystemUserRoles` | `UserId`、`RoleId` | 多角色关联表，当前权限检查器未读取，属预留结构 |
| `SystemDataDictionary` | `SystemDataDictionaries` | `ParentId`、`Code`、`Name`、`Value`、`SortOrder`、`IsActive` | 通用数据字典，自关联树 |
| `SystemChinaRegion` | `SystemChinaRegions` | `Code`、`Name`、`FullName`、`Level`、`ParentCode`、`ProvinceCode`、`CityCode` | 省市区行政区划 |
| `SystemErrorLog` | `SystemErrorLogs` | `ErrorType`、`ErrorMessage`、`StackTrace`、`RequestUrl`、`UserId`、`HttpStatusCode` | 异常日志，不软删除 |
| `SystemOperationLog` | `SystemOperationLogs` | `ActionType`、`EntityType`、`EntityId`、`OperatorName`、`ChangeJson` | 操作日志，由 `AppDbContext.SaveChanges` 自动写入 |

## 4. CRM 模块（13 个实体）

| 实体 | 表名 | 关键字段 | 说明 |
|---|---|---|---|
| `CrmHerbBaseSubject` | `CrmHerbBaseSubjects` | `SubjectName`、`SubjectType`、`OwnerUserId`、`Status`、`Grade`、`Score`、`Scale`、`PrimaryContactName`、`PrimaryContactPhone`、`LastFollowAt`、`NextFollowAt` | 药材基地主体，聚合多个基地；评分与跟进摘要落在主体上 |
| `CrmHerbBase` | `CrmHerbBases` | `HerbBaseSubjectId`、`BaseName`、`SubjectName`、`Grade`、`Score`、`Scale`、`Province`、`City`、`Area`、`Address`、`Status`、`OwnerUserId`、`PrimaryContactName`、`PrimaryContactPhone` | 具体基地，归属主体 |
| `CrmHerbBaseSupply` | `CrmHerbBaseSupplies` | `HerbBaseId`、`HerbBaseSubjectId`、`ProductName`、`AvailableQuantity`、`QuantityUnit`、`Specification`、`QualityRequirement`、`ExpectedPrice`、`ValidUntil`、`Status` | 基地可供货信息 |
| `CrmContact` | `CrmContacts` | `EntityType`、`EntityId`、`ContactName`、`Phone`、`PhoneType`、`Wechat`、`RoleName`、`IsPrimary`、`Status` | 通用联系人，按 `EntityType + EntityId` 挂到主体/基地/厂商 |
| `CrmFollowRecord` | `CrmFollowRecords` | `EntityType`、`EntityId`、`ContactId`、`FollowType`、`FollowResult`、`IntentLevel`、`Content`、`NextFollowAt`、`OperatorUserId` | 通用跟进记录，由跟进任务工作台复用 |
| `CrmBusinessEntityAttribute` | `CrmBusinessEntityAttributes` | `EntityType`、`EntityId`、`AttributeCode`、`AttributeValue`、`SortOrder` | 业务实体扩展属性 |
| `CrmTransferRecord` | `CrmTransferRecords` | `ActionType`、`EntityType`、`EntityId`、`FromOwnerUserId`、`ToOwnerUserId`、`OperatorUserId`、`Remark` | 归属流转流水 |
| `CrmVendor` | `CrmVendors` | `VendorName`、`NormalizedVendorName`、`PriorityLevel`、`LatestPurchaseTime`、`OwnerUserId`、`LastFollowAt`、`LastFollowResult`、`NextFollowAt` | 厂商档案 |
| `CrmVendorDemand` | `CrmVendorDemands` | `VendorId`、`ContactId`、`DemandNo`、`DemandName`、`DemandAt`、`Status`、`SourceType`、`ExpectedDeliveryAt`、`ReceivingAddress` | 厂商采购需求 |
| `CrmVendorDemandItem` | `CrmVendorDemandItems` | `VendorDemandId`、`ProductName`、`Quantity`、`QuantityUnit`、`Specification`、`QualityRequirement`、`TargetPrice`、`ExpectedDeliveryAt` | 采购需求明细 |
| `CrmMatchOpportunity` | `CrmMatchOpportunities` | `DemandItemId`、`DemandId`、`VendorId`、`DemandProductName`、`DemandQuantity`、`OwnerUserId`、`Status`、`Stage`、`NextAction`、`NextActionAt`、`CloseResult` | 人工供需撮合机会 |
| `CrmMatchCandidate` | `CrmMatchCandidates` | `MatchOpportunityId`、`HerbBaseSupplyId`、`HerbBaseId`、`HerbBaseSubjectId`、`ProductName`、`AvailableQuantity`、`ExpectedPrice`、`Status`、`EliminationReason`、`SortOrder` | 撮合候选供应 |
| `CrmBusinessActivity` | `CrmBusinessActivities` | `EntityType`、`EntityId`、`ActionType`、`Content` | 业务动态时间线 |

非实体辅助类型：

- `CrmTransferActionType`：流转动作常量（入库/分配/转交/退回待分配池）。
- `CrmTransferEntityType`：流转实体类型常量。
- `CrmHerbBaseSubjectScoreInput`：主体评分输入值对象，不映射数据库表。
- `CrmHerbBaseSubjectScorePolicy`：主体评分静态策略，不映射数据库表。

## 5. Mall 模块（32 个实体）

### 5.1 商品与购物车

| 实体 | 表名 | 关键字段 | 说明 |
|---|---|---|---|
| `MallStore` | `MallStores` | `Code`、`Name`、`Address`、`IsActive`、`DefaultFreightTemplateId` | 门店 |
| `MallCategory` | `MallCategories` | `Code`、`Name`、`ParentId`、`Icon`、`SortOrder`、`IsActive` | 商品分类树 |
| `MallTag` | `MallTags` | `Type`、`Code`、`Name`、`Color`、`SortOrder`、`IsActive` | 统一标签库；`Type` 区分商品/评价等标签 |
| `MallProduct` | `MallProducts` | `StoreId`、`Code`、`Name`、`CategoryId`、`MainImageUrl`、`AlbumImages`、`DetailHtml`、`IsHot`、`Sales`、`FreightTemplateId` | 商品主表，关联门店 |
| `MallProductTag` | `MallProductTags` | `ProductId`、`TagId` | 商品-标签关联 |
| `MallSku` | `MallSkus` | `ProductId`、`SkuCode`、`SpecName`、`Price`、`MarketPrice`、`WeightGram`、`Stock`、`WarnStock` | 商品规格与库存 |
| `MallProductReview` | `MallProductReviews` | `ProductId`、`MemberId`、`OrderId`、`OrderItemId`、`Rating`、`Content`、`Tags`、`Images`、`IsFeatured`、`AuditStatus`、`ReplyContent` | 商品评价，支持图片、精选与审核 |
| `MallCartItem` | `MallCartItems` | `UserId`、`SkuId`、`ProductId`、`Quantity` | 购物车明细；**无购物车车头** |

### 5.2 订单与履约

| 实体 | 表名 | 关键字段 | 说明 |
|---|---|---|---|
| `MallOrder` | `MallOrders` | `OrderNo`、`MemberId`、`Status`、`ReceiverName/Phone/Address`、`GoodsAmount`、`PromotionDiscountAmount`、`CouponDiscountAmount`、`FreightAmount`、`PayableAmount`、`PaidAmount`、`RefundedAmount`、`GiftShortageMessage` | 订单主表，金额有恒等校验 |
| `MallOrderItem` | `MallOrderItems` | `OrderId`、`SkuId`、`ProductId`、`ProductName`、`SkuCode`、`SpecName`、`ProductImage`、`UnitPrice`、`Quantity`、`WeightGram`、`ItemAmount`、`IsGift`、`PromotionId`、`PromotionName`、`OriginalUnitPrice`、`PromotionDiscountAmount`、`PromotionSnapshot` | 订单明细，保存下单时的商品与促销快照 |
| `MallPayment` | `MallPayments` | `PaymentNo`、`OrderId`、`Amount`、`Method`、`Status`、`TransactionNo`、`FailureReason`、`PaidAt` | 支付单 |
| `MallRefund` | `MallRefunds` | `RefundNo`、`OrderId`、`PaymentId`、`Amount`、`Reason`、`EvidenceImages`、`Status`、`RefundedAt` | 退款单 |
| `MallReturn` | `MallReturns` | `ReturnNo`、`OrderId`、`PaymentId`、`RefundId`、`Amount`、`Reason`、`Status`、`ExpressCompany`、`TrackingNo`、`ApprovedAt`、`ReceivedAt` | 退货单 |
| `MallShipment` | `MallShipments` | `ShipmentNo`、`OrderId`、`ExpressCompany`、`TrackingNo`、`Status`、`ShippedAt`、`DeliveredAt`、`IsVoided`、`VoidedAt`、`VoidReason` | 运单，支持作废 |
| `MallStockLog` | `MallStockLogs` | `SkuId`、`SkuCode`、`SpecName`、`ChangeType`、`ChangeQuantity`、`BeforeStock`、`AfterStock`、`RelatedOrderNo` | 库存变更流水 |
| `MallAddress` | `MallAddresses` | `UserId`、`ReceiverName`、`ReceiverPhone`、`ProvinceName`、`CityName`、`DistrictName`、`DetailAddress`、`IsDefault` | 会员收货地址 |

### 5.3 营销

| 实体 | 表名 | 关键字段 | 说明 |
|---|---|---|---|
| `MallPromotion` | `MallPromotions` | `Name`、`PromotionType`、`ThresholdAmount`、`ScopeType`、`StartDate`、`EndDate`、`ActionType`、`ActionConfigJson`、`PromotionTagId` | 促销；`PromotionType` 0 满减 / 1 折扣 / 2 买赠，`ActionType` 另有 `FreightFree` |
| `MallPromotionScope` | `MallPromotionScopes` | `PromotionId`、`ScopeTargetType`、`ScopeTargetId` | 促销适用范围（商品/分类/标签等） |
| `MallCoupon` | `MallCoupons` | `Code`、`Name`、`CouponType`、`ThresholdAmount`、`DiscountAmount`、`MaxDiscount`、`TotalCount`、`RemainCount`、`PerLimit`、`ValidDays`、`ScopeType`、`IsActive` | 优惠券模板 |
| `MallMemberCoupon` | `MallMemberCoupons` | `CouponId`、`MemberId`、`CouponCode`、`Status`、`ExpireAt`、`ClaimedAt`、`UsedAt`、`UsedOrderNo` | 会员持有的券 |
| `MallCampaign` | `MallCampaigns` | `Type`、`Name`、`CoverImageUrl`、`ContentHtml`、`StartAt`、`EndAt`、`IsActive`、`SortOrder` | 活动专题；`Type` 0 商品推荐 / 1 报名（预留）/ 2 领券 |
| `MallCampaignProduct` | `MallCampaignProducts` | `CampaignId`、`ProductId`、`GroupName`、`SortOrder` | 活动推荐商品分组 |
| `MallCampaignCoupon` | `MallCampaignCoupons` | `CampaignId`、`CouponId`、`SortOrder` | 活动关联券 |

### 5.4 会员

| 实体 | 表名 | 关键字段 | 说明 |
|---|---|---|---|
| `MallMember` | `MallMembers` | `Phone`、`PasswordHash`、`LevelId`、`Nickname`、`Avatar`、`TotalPoints`、`AvailablePoints`、`TotalSpent`、`TotalOrders`、`LastConsumeAt`、`IsActive` | 商城会员 |
| `MallMemberLevel` | `MallMemberLevels` | `Name`、`LevelCode`、`DiscountRate`、`MinSpent`、`MaxSpent`、`UpgradePoints`、`SortOrder`、`IsDefault` | 会员等级 |
| `MallMemberPoint` | `MallMemberPoints` | `MemberId`、`ChangeAmount`、`Balance`、`ChangeType`、`RefNo`、`Remark` | 积分流水 |

### 5.5 内容

| 实体 | 表名 | 关键字段 | 说明 |
|---|---|---|---|
| `MallBanner` | `MallBanners` | `Position`、`ImageUrl`、`LinkType`、`LinkValue`、`Title`、`SortOrder`、`StartDate`、`EndDate` | 轮播图 |
| `MallNotice` | `MallNotices` | `Title`、`Content`、`NoticeType`、`IsTop`、`SortOrder`、`StartDate`、`EndDate` | 公告 |
| `MallArticle` | `MallArticles` | `Title`、`Summary`、`ContentHtml`、`ArticleType`、`Author`、`ViewCount`、`SeoTitle`、`IsTop`、`PublishedAt` | 文章 |
| `MallPage` | `MallPages` | `PageCode`、`PageName`、`Title`、`ContentHtml`、`SeoTitle`、`SeoKeywords`、`SeoDescription` | 单页内容 |

### 5.6 运费

| 实体 | 表名 | 关键字段 | 说明 |
|---|---|---|---|
| `MallFreightTemplate` | `MallFreightTemplates` | `Name`、`BillingType`、`IsDefault`、`IsActive` | 运费模板 |
| `MallFreightRegion` | `MallFreightRegions` | `TemplateId`、`RegionName`、`ProvinceNames`、`CityNames`、`FirstValue`、`FirstFee`、`StepValue`、`StepFee`、`FreeShippingAmount`、`FreeShippingQuantity`、`IsDefaultRegion` | 模板的区域计费规则 |

### 5.7 `MallCartItem` 结构（合表后）

购物车已合表：不再保留车头，只保留明细行；**没有任何明细即视为空购物车**。

| 字段 | 类型 | 说明 |
|---|---|---|
| `UserId` | Guid | 会员 ID，购物车按会员归属 |
| `SkuId` | Guid | SKU ID |
| `ProductId` | Guid | 商品 ID，冗余保存便于按商品聚合 |
| `Quantity` | int | 数量，必须大于 0 |

约束与行为：

- 唯一索引 `(UserId, SkuId)`，过滤条件 `[IsDeleted] = 0`。
- `Create(userId, skuId, productId, quantity)` 创建明细。
- `SetQuantity(quantity)` 覆盖数量，`quantity <= 0` 抛 `BusinessException(400)`。
- `AddQuantity(delta)` 累加数量，`delta <= 0` 抛 `BusinessException(400)`。
- 对应迁移：`20261106130000_FixMallCartUniqueIndexFilters`、`20261106140000_MergeMallCartHeaderIntoItems`。

### 5.8 `MallOrder` / `MallOrderItem` 快照

`MallOrder` 的金额恒等式：

```
应付金额 = max(0, 商品原价 - 促销优惠 - 券抵扣) + 运费
```

- `EnsureAmountIdentity(...)` 在落库前校验，误差超过 0.01 抛 `BusinessException`。
- 状态常量集中在 `QPS.Mall/Domain/MallStatuses.cs`：`PendingPayment`、`Paid`、`Shipped`、`Completed`、`Cancelled`、`Refunding`、`Refunded`、`Returning`、`Returned`。
- 订单明细保存下单瞬间的商品快照：`ProductName`、`SkuCode`、`SpecName`、`ProductImage`、`UnitPrice`、`OriginalUnitPrice`、`WeightGram`。
- 促销快照：`PromotionId`、`PromotionName`、`PromotionDiscountAmount`、`PromotionSnapshot`（JSON），商品改价或促销下线不影响历史订单。
- 赠品通过 `IsGift` 标记，赠品缺货原因写入订单 `GiftShortageMessage`。

## 6. 状态枚举

| 枚举/常量类 | 取值 |
|---|---|
| `MallPromotionType` | `DiscountAmount=0`、`DiscountRate=1`、`Gift=2` |
| `MallCampaignType` | `ProductShowcase=0`、`Signup=1`（预留）、`Coupon=2` |
| `MallOrderStatus` | 待支付/已支付/已发货/已完成/已取消/退款中/已退款/退货中/已退货 |
| `MallPaymentStatus` | `Pending`、`Success`、`Failed`、`Refunded`、`Cancelled` |
| `MallRefundStatus` | `Pending`、`Success`、`Failed` |
| `MallReturnStatus` | `Pending`、`Approved`、`Received`、`Completed`、`Rejected` |
| `MallShipmentStatus` | `Shipped`、`Delivered` |
| `MallStockChangeType` | `OrderDeduct`、`OrderCancel`、`RefundRestore`、`ReturnRestore`、`ManualAdjust` |

# QPS 数据库废表 / 废字段只读审计报告

- 审计对象：`QPS_CRM`（`web-api/src/QPS.Host/appsettings.json` 中 `DefaultConnection` 指向的库）
- 审计时间：2026-11-07
- 审计性质：**只读**。未执行任何 DDL / DML，未修改表结构、数据或代码。
- 数据库规模：55 张业务表（另有 `__EFMigrationsHistory`），846 个字段，已应用 EF 迁移 47 个。

## 0. 方法与限制（先说明证据口径）

1. **数据库侧**：按要求先调用 `mcp_MSSQL_*`，但默认连接账号 `qps_db` 登录失败（`用户 'qps_db' 登录失败。`），且没有可用的命名连接。为完成审计，改用本机只读 `sqlcmd` 连同一实例的 `QPS_CRM`，账号与 `appsettings.json` 一致。所有查询均为 `SELECT` / `sys.*` 元数据查询。
2. **代码侧**：`web-api/src` 的实体、`Configurations`、`AppDbContextModelSnapshot`、47 个迁移文件，以及 admin / mobile 前端引用；对表名和字段名做了全仓 `Grep` 计数。
3. **软删除**：本报告同时统计“总行数”和 `IsDeleted=0` 的“有效行数”。`CrmHerbBases` 11675 行中有效仅 2433 行、`CrmHerbBaseSubjects` 2793 行中有效仅 458 行，**不能把软删除行当作无人使用**。
4. **重要限制：当前库是 seed/test 数据环境**（大量数据由 `seed-test-data` 写入）。因此：
   - “0 行”只能说明**本环境没跑过该业务**，不能单独证明功能废弃；
   - 索引使用统计只覆盖本次 SQL Server 启动后（`2026-11-06 10:24:55` 起），“0 读取”也不是长期结论。
   - 本报告把“代码零引用 + 模型游离 + 无对象依赖”作为强证据，把“0 行但代码仍在读写”的单列为不确定项，不硬判。

---

## 1. 总览

| 指标 | 数量 | 说明 |
|---|---:|---|
| 业务表 | 55 | 不含 `__EFMigrationsHistory` |
| 字段 | 846 | `sys.columns` 统计 |
| EF 模型已映射表 | 54 | 与数据库仅差 1 张表 |
| **疑似废弃表（强证据）** | **2** | `ImportTempSrc`、`SystemUserRoles` |
| **疑似废弃字段（强证据）** | **1** | `CrmHerbBases.CoordinateText` |
| 快照冗余字段（建议二期下线，非垃圾） | 2 | `MallOrderItems.PromotionId`、`PromotionName` |
| 零行但代码仍在使用的表 | 9 | 全部为 CRM 供应商 / 需求 / 撮合 / 流转模块 |
| UI 未闭环导致恒空的字段 | 2 组 | `MallBanners.StartDate/EndDate`、`MallFreightRegions.FreeShippingQuantity` |
| 已被迁移删除的历史表 | 0 残留 | `MallCartHeaders`、`MallActivities`、`MallPromotionUsage` 等均已清理干净 |

结论先说：**这库整体很干净，只有 1 张“完全游离于代码之外”的导入临时表、1 张被单角色模型取代的关联表、1 个手工加的坐标文本列。** 其它“0 行/0 读取”的表，基本都是因为本环境没有走过对应业务流程，不能删。

---

## 2. 疑似废弃表

### 2.1 `ImportTempSrc` —— 一次性导入临时表（强证据，建议删除）

| 项目 | 证据 |
|---|---|
| 行数 | 14 行（无 `IsDeleted` 列，全部有效） |
| 代码引用 | **0**：全仓 `Grep "ImportTempSrc"`（含 .cs/.ts/.vue/.js/.json/.sql/.md）无命中 |
| EF 模型 | 不在 `AppDbContextModelSnapshot.cs` 中；数据库 55 张业务表中唯一没有对应实体/DbSet 的表 |
| 数据库对象依赖 | `sys.objects` 中无存储过程、视图、触发器、函数引用它；`sys.foreign_keys` 无任何外键 |
| 索引使用 | 从 2026-11-06 10:24 服务器启动至审计时：seeks=0 / scans=0 / lookups=0 |
| 创建时间 | `sys.tables.create_date = 2026-10-31 16:08:42` |
| 数据血缘 | 14/14 行的 `BaseName` 都能在 `CrmHerbBases` 找到同名记录；`CrmHerbBases` 的有效数据写入时间为 `2026-10-31 16:30:40`，晚于本表创建 |
| 字段填充 | 14 行中：SourceId / BaseName / SubjectName / Province / MainProduct / Remark / NormSubject / IsGenericSubject 各 14 个非空；City 8、Area 11、Address 1、Scale 2；**Lat / Lng / CoordinateText / ContactPerson / Telephone 全为 NULL** |

判断：这是 2026-10-31 为导入 `CrmHerbBases` 建的**一次性中转表**，导入完成后就再没有任何代码、DB 对象或索引访问。属于明确的历史残留。

建议：先把 14 行导出留档（`E:\Code\QPS\agent-temp` 下，不进源码库），确认没有外部定时任务/SQL 脚本依赖后删除整表。风险：**低**。

最后可能的使用点：`2026-10-31 16:08:42` 建表并写入，随后 16:30 数据进入 `CrmHerbBases`，此后无访问。

### 2.2 `SystemUserRoles` —— 被单角色模型取代的关联表（强证据，建议删除）

| 项目 | 证据 |
|---|---|
| 行数 | 2 行，写入时间 `2026-10-31 16:03:25`，CreatedBy/UpdatedBy 均为 `seed-test-data` |
| 代码引用 | 后端仅 **2 处**：实体声明 [SystemUserRole.cs](file:///E:/Code/QPS/web-api/src/QPS.System/Domain/Entities/SystemUserRole.cs) 与 DbSet 接口 [ISystemDbContext.cs](file:///E:/Code/QPS/web-api/src/QPS.System/Contracts/ISystemDbContext.cs#L11)、[AppDbContext.cs](file:///E:/Code/QPS/web-api/src/QPS.Persistence/Database/AppDbContext.cs#L25)。**没有任何业务查询/写入** |
| 权限实际来源 | 用户只有单角色：`SystemUsers.RoleId`（[SystemUser.cs](file:///E:/Code/QPS/web-api/src/QPS.System/Domain/Entities/SystemUser.cs#L7)）；登录按 `user.RoleId` 查角色（[LoginCommand.cs](file:///E:/Code/QPS/web-api/src/QPS.System/Application/Features/Auth/LoginCommand.cs#L60-L77)）；取权限同样按 `user.RoleId`（[GetUserPermissionsQuery.cs](file:///E:/Code/QPS/web-api/src/QPS.System/Application/Features/Auth/GetUserPermissionsQuery.cs#L36-L42)）；新建/编辑用户直接写 `RoleId`（[CreateUserCommand.cs](file:///E:/Code/QPS/web-api/src/QPS.System/Application/Features/Users/CreateUserCommand.cs#L51)、[UpdateUserCommand.cs](file:///E:/Code/QPS/web-api/src/QPS.System/Application/Features/Users/UpdateUserCommand.cs#L60)） |
| 约束/索引 | 无外键；仅 1 个主键索引 `PK_SystemUserRoles`，没有 `(UserId, RoleId)` 唯一索引，不符合正常多对多关联表设计 |
| 索引使用 | seeks=0 / scans=0 / lookups=0 |
| 迁移历史 | 所有迁移的 `Up` 都没有创建它（首个 CRM 迁移 `20260828072947_CrmSchemaBaseline.cs` 为上行为空的基线）；它只出现在 Designer 快照里，说明是 EF 接管前由外部初始脚本建的历史表 |

判断：历史多角色关联表的残留，现行业务已完全使用 `SystemUsers.RoleId` 单角色模型；2 行数据是 seed 写入的孤儿数据。

建议：与产品确认**没有“一人多角色”的规划**后，删除 2 行数据、移除实体与 DbSet，并新增迁移 `DropTable("SystemUserRoles")`。风险：**低**。

最后可能的使用点：`2026-10-31 16:03:25` seed 写入后再无任何读取。

### 2.3 已被迁移删除的历史表（确认无残留，不需要再处理）

以下“废表”在早期就被迁移清掉了，现在的 55 张表里已经找不到，不需要重复清理：

- 购物车表头 `MallCartHeaders` → 合并进 `MallCartItems`（[20261106140000_MergeMallCartHeaderIntoItems.cs](file:///E:/Code/QPS/web-api/src/QPS.Persistence/Migrations/20261106140000_MergeMallCartHeaderIntoItems.cs#L55)）；
- 旧活动 `MallActivities` → 专题活动 `MallCampaigns`（[20261106095501_RemoveMallActivities.cs](file:///E:/Code/QPS/web-api/src/QPS.Persistence/Migrations/20261106095501_RemoveMallActivities.cs#L14)、[20261106083629_AddMallCampaigns.cs](file:///E:/Code/QPS/web-api/src/QPS.Persistence/Migrations/20261106083629_AddMallCampaigns.cs#L97-L100)）；
- 活动促销 `MallActivityPromotions`（[20261105051401_RemoveMallActivityPromotion.cs](file:///E:/Code/QPS/web-api/src/QPS.Persistence/Migrations/20261105051401_RemoveMallActivityPromotion.cs#L14)）；
- 促销核销残留 `MallPromotionUsages` 及 `MaxUsagePerOrder/PerUser` 列（[20261106123000_RemoveMallPromotionUsageResidue.cs](file:///E:/Code/QPS/web-api/src/QPS.Persistence/Migrations/20261106123000_RemoveMallPromotionUsageResidue.cs)）；
- CRM 旧“采购需求”已重命名为 `CrmVendorDemands`（[20260904110000_RenameCrmPurchaseDemandsToVendorDemands.cs](file:///E:/Code/QPS/web-api/src/QPS.Persistence/Migrations/20260904110000_RenameCrmPurchaseDemandsToVendorDemands.cs)）。

---

## 3. 疑似废弃字段

### 3.1 `CrmHerbBases.CoordinateText` —— 模型外的手工坐标文本列（强证据）

| 项目 | 证据 |
|---|---|
| 类型 | `nvarchar(200)`，nullable |
| 行数 / 非空 | 全表 11675 行，非空 836 行；其中**有效行（IsDeleted=0）403 行** |
| 代码引用 | **0**：全仓 `Grep "CoordinateText"` 无命中；`web-api/src` 无任何读写 |
| EF 模型 | **不在** `AppDbContextModelSnapshot.cs` 中，是数据库里唯一“有列、无实体属性”的字段（通过快照字段差集核对） |
| 迁移历史 | 47 个迁移文件中**没有任何** `CoordinateText` 字样，说明该列不是 EF 迁移创建的，而是外部 SQL / 手工加列 |
| 数据特征 | 有值样例：`516152-5229812`，形如“纬度-经度”拼接文本；与实体中的 `Lat/Lng` 两列同源 |
| 相关列现状 | `Lat/Lng` 全表仅 424 行非空，且**全部落在软删除行**；有效 2433 行里 0 个非空，说明坐标没有回填到 `Lat/Lng` |
| 最后写入 | 有效行最后更新时间 `2026-10-31 16:30:40`（与 `CrmHerbBases` 导入时间一致） |

判断：导入阶段为保留原始坐标文本而加的外部列，代码从未读取，EF 也不管理；但它目前是 403 条有效主体的**唯一坐标来源**（`Lat/Lng` 为空），所以不能直接当垃圾删。

建议：先把 403 条有效行的 `Id + BaseName + CoordinateText` 导出留档，并确认是否需要解析回填 `Lat/Lng`；确认不再需要后再通过迁移删列。风险：**中**（删早了会丢失可回填的坐标源数据）。

### 3.2 `MallOrderItems.PromotionId` / `PromotionName` —— 已被快照字段 100% 取代（建议二期下线，暂保留）

| 表.列 | 类型 | 非空/总行数 | 代码引用 | 判断依据 | 建议 |
|---|---|---:|---|---|---|
| `MallOrderItems.PromotionId` | `uniqueidentifier` | 56 / 56 | 5 处：写入 [OrderSnapshotBuilder.cs](file:///E:/Code/QPS/web-api/src/QPS.Mall/Application/Features/MallOrders/OrderSnapshotBuilder.cs#L74-L75)、[OrderSnapshotBuilder.cs](file:///E:/Code/QPS/web-api/src/QPS.Mall/Application/Features/MallOrders/OrderSnapshotBuilder.cs#L108-L109)；读出 [GetMallOrdersQuery.cs](file:///E:/Code/QPS/web-api/src/QPS.Mall/Application/Features/MallOrders/GetMallOrdersQuery.cs#L178-L179)、[MallOrderDetailLoader.cs](file:///E:/Code/QPS/web-api/src/QPS.Mall/Application/Features/MallOrders/MallOrderDetailLoader.cs#L77-L78) | `PromotionSnapshot` 已有 56/56 行；`PromotionId` 无任何订单业务逻辑使用，仅做 DTO 透传 | 保留观察，二期随 API 契约一起删 |
| `MallOrderItems.PromotionName` | `nvarchar(100)` | 56 / 56 | 同上（写入 2 处、读出 2 处），mobile 端仍有 `promotionName` 兜底展示 | 快照已全量回填：`PromotionSnapshot` 非空 56 行，且“只有 PromotionName、没有 Snapshot”的订单数为 **0** | 保留观察，待前端去掉兜底后再删 |

判断：快照化完成后，这两列是**冗余但仍在写**的兼容字段，不是脏数据；历史订单展示、发票/售后等场景若回滚旧备份仍可能读取，所以不适合现在硬删。

建议：现阶段只打“快照冗余”标记；二期确认 admin/mobile 不再使用 `promotionName` 兜底后，再一并删除两列与 API 字段。风险：**低-中**。

---

## 4. 不确定项（有交叉引用或数据非空，单列观察，不硬判）

### 4.1 9 张“0 行但代码仍在读写”的 CRM 表：**建议保留**

这些表命中“无数据 + 本次统计无索引读取”，但代码、admin 页面、API 都在，属于功能已实现、本环境未跑过业务流程，**不能判废**：

| 表 | 行数 | 后端引用次数 | 最后读取（索引统计） | 结论 |
|---|---:|---:|---|---|
| `CrmVendors` | 0 | 81 | 2026-11-07 18:29:41（seek+scan） | 供应商模块有完整增删改查，保留 |
| `CrmVendorDemands` | 0 | 35 | 2026-11-07 18:29:41 | 需求模块，保留 |
| `CrmVendorDemandItems` | 0 | 12 | 2026-11-07 18:29:41 | 需求明细，保留 |
| `CrmMatchOpportunities` | 0 | 35 | 2026-11-07 16:35 / 17:48 | 撮合模块，保留 |
| `CrmMatchCandidates` | 0 | 20 | 2026-11-07 17:48:54 | 撮合候选，保留 |
| `CrmHerbBaseSupplies` | 0 | 34 | 2026-11-07 18:29:47 | 供应关系，保留 |
| `CrmFollowRecords` | 0 | 32 | 2026-11-07 18:29:41（scan 213 次） | 跟进记录被列表查询真实读取，保留 |
| `CrmTransferRecords` | 0 | 15 | 无读取（无变更所有者操作） | 交接记录仅由“改负责人”写入，保留 |
| `CrmBusinessActivities` | 0 | 10 | 无读取 | 撮合机会的日志表，仅在产生撮合时写入，保留 |

补充证据：这些模块在 admin 端都有独立页面（`admin/src/views/crm/vendor`、`purchaseDemand`、`matchOpportunity`、`herbBaseSupply`、`followTask` 等），且有真实索引 seek/scan（见上表），只是本环境没有产生业务数据。

### 4.2 `MallBanners.StartDate` / `EndDate`：接口已支持、前端未闭环

- 6 张有效 Banner，两列非空均为 **0**。
- 后端完整使用：列表/详情映射、按时间窗口过滤、新建/更新都传（[MallBannerHandlers.cs](file:///E:/Code/QPS/web-api/src/QPS.Mall/Application/Features/MallBanners/MallBannerHandlers.cs#L81-L151)，过滤在 [同文件 L105-L106](file:///E:/Code/QPS/web-api/src/QPS.Mall/Application/Features/MallBanners/MallBannerHandlers.cs#L105-L106)）。
- 但 admin 只**展示**“时间窗口”（[banner/index.vue](file:///E:/Code/QPS/admin/src/views/mall/banner/index.vue#L45-L51)），编辑弹窗（[同文件 L74-L125](file:///E:/Code/QPS/admin/src/views/mall/banner/index.vue#L74-L125)）没有开始/结束日期输入控件，所以数据永远为空，页面永远显示“不限 ~ 永久”。
- 结论：**不是废字段，是功能未闭环**。建议二选一：补日期选择器（启用定时上下架），或删除字段 + 过滤逻辑。风险：低。

### 4.3 `MallFreightRegions.FreeShippingQuantity`：按件包邮分支未接入 UI

- 3 个区域，3 行有效，该列非空 **0**。
- 后端有完整链路：实体、DTO、创建/更新命令、校验、计算器分支（[MallFreightCalculator.cs](file:///E:/Code/QPS/web-api/src/QPS.Mall/Application/Features/MallFreightRules/MallFreightCalculator.cs#L248)）。
- admin 运费区域表单只提供 `freeShippingAmount`（金额包邮，[freight/index.vue](file:///E:/Code/QPS/admin/src/views/mall/freight/index.vue#L131)），没有 `freeShippingQuantity` 输入。
- 结论：可能是“按件包邮”预留能力。若产品确认不做按件包邮，可删列 + DTO + 计算分支；否则保留。风险：低。

### 4.4 `CrmHerbBases.Lat` / `Lng`：有代码、无有效数据

- 全表 424 行有值，但**全部是软删除行**；2433 条有效主体 0 条有经纬度。
- 后端 DTO/命令/查询都在用（不是死代码），是导入时未回填。
- 与 3.1 的 `CoordinateText` 联动：建议先决定是否用 `CoordinateText` 回填，再谈删列。**建议保留**。

### 4.5 `CrmBusinessEntityAttributes`：EAV 结构只剩一个属性，且有重复行

- 3075 行（有效 783 行），`EntityType` 只有 `CRM_HERB_BASE_SUBJECT`，`AttributeCode` 只有 `CRM_MAIN_PRODUCT`，`SortOrder` 恒为 1。
- 有效 458 个主体对应 783 行属性 → 存在重复（最多一个主体 23 行）。
- API/命令/查询仍在使用（[GetCrmBusinessEntityAttributesQuery.cs](file:///E:/Code/QPS/web-api/src/QPS.Crm/Application/Features/CrmBusinessEntityAttributes/GetCrmBusinessEntityAttributesQuery.cs#L62)、[SaveCrmBusinessEntityAttributesCommand.cs](file:///E:/Code/QPS/web-api/src/QPS.Crm/Application/Features/CrmBusinessEntityAttributes/SaveCrmBusinessEntityAttributesCommand.cs#L29-L65)）。
- 结论：表**不废**，但“通用 EAV”设计已退化为单属性存储，且 seed 产生重复数据。建议先清理重复行（业务确认保留哪一行），不要删表。

### 4.6 有数据但本次统计 0 读取的 CMS / 会员表：低活跃，不建议动

`MallArticles`（6 行）、`MallNotices`（4 行）、`MallPages`（3 行）、`MallMemberLevels`（3 行）、`MallMemberPoints`（2 行）在统计窗口内索引读取为 0，但都有数据、有 admin 页面/后端 API。判断为“本环境没打开这些页面”，**不是废表**。

### 4.7 `MallBanners` 之外的“全空列”说明

审计已逐列核对“有效行全 NULL”的字段，除 4.2/4.3 外均能在代码里找到真实写入路径，不建议按空值删列：
- `CrmHerbBases.OwnerUserId/LastFollowAt/NextFollowAt`、`CrmHerbBaseSubjects.OwnerUserId/LastFollowAt/LastFollowResult/NextFollowAt/Remark`：负责人分配、跟进列表筛选、评分逻辑都在读写（如 [GetCrmHerbBasesQuery.cs](file:///E:/Code/QPS/web-api/src/QPS.Crm/Application/Features/CrmHerbBases/GetCrmHerbBasesQuery.cs#L93-L144)）。
- `MallRefunds.Description`、`MallReturns.Description/RejectReason`：售后创建/审核接口均会写入（[CreateMallRefundCommand.cs](file:///E:/Code/QPS/web-api/src/QPS.Mall/Application/Features/MallRefunds/CreateMallRefundCommand.cs#L69)、[CreateMallReturnCommand.cs](file:///E:/Code/QPS/web-api/src/QPS.Mall/Application/Features/MallReturns/CreateMallReturnCommand.cs#L24)）；当前为空是本环境售后没填描述，不是字段废弃。
- `MallProducts.FreightTemplateId`：117 行中仅 1 行有值，但运费计算有“商品模板 → 门店默认模板”的显式回退，属于正常可选配置。
- `MallCoupons.MaxDiscount`：仅 1 行有值，是百分比券的封顶金额，admin 表单有输入（[coupon/index.vue](file:///E:/Code/QPS/admin/src/views/mall/coupon/index.vue#L163)）。

---

## 5. 建议的清理顺序与注意事项

### 建议顺序

1. **`ImportTempSrc`（立即，低风险）**
   导出 14 行 CSV 留档 → 确认外部没有定时导入任务 → `DROP TABLE`。这张表不在 EF 模型，删除不需要改代码。
2. **`SystemUserRoles`（先确认，低风险）**
   确认产品不做“一人多角色” → 删 2 行 seed 数据 → 移除实体/DbSet → 新增 EF 迁移 `DropTable`。
3. **`CrmHerbBases.CoordinateText`（先归档，中风险）**
   导出 403 条有效行的坐标文本 → 与业务确认是否回填 `Lat/Lng` → 不需要则迁移删列；需要则先回填再删。
4. **`MallOrderItems.PromotionId/PromotionName`（二期，低-中风险）**
   先让 admin/mobile 去掉 `promotionName` 兜底，再改 API 契约、迁移删列。历史订单展示必须验证一次。
5. **其余“0 行 / 0 读取”表**：只做观察，不做删除；等对应 CRM 模块真实跑过一轮再评估。

### 必须保留（高风险，别动）

- **订单/支付/退款/退货/发货**：`MallOrders`、`MallOrderItems`、`MallPayments`、`MallRefunds`、`MallReturns`、`MallShipments`。订单快照字段（`ProductName`、`ProductImage`、`PromotionSnapshot`、`CouponName`、`CouponDiscountAmount`、`PromotionDiscountAmount`、`GiftShortageMessage`）是历史订单 / 财务对账的唯一依据。
- **审计与操作日志**：`SystemOperationLogs`（5227 行）、`SystemErrorLogs`（170 行），涉及审计追溯。
- **软删除历史数据**：`CrmHerbBases` 的 9242 行软删记录、`MallProductTags` 的 1685 行软删记录等，是历史轨迹，不是“没人用的数据”，不得批量物理删除。
- **权限/角色**：`SystemRoles`、`SystemRolePermissions`、`SystemPermissions`。
- **`CrmBusinessEntityAttributes`**：虽然 EAV 设计退化，但主营品种仍依赖它，先清重复行、后谈结构改造。

### 操作注意事项

- 任何删表/删列必须走 **EF 迁移 + 备份**，不要在生产库直接执行本报告的 DDL 建议；本报告基于 seed 环境，不能直接作为生产清理脚本。
- 删除前对目标表/列做一次 `SELECT INTO` 或 CSV 导出，保留至少一个发布周期。
- `MallBanners` / `MallFreightRegions` 两个“UI 未闭环”字段，优先修 UI 或明确下线，不建议在没有产品结论时默默删列。
- 本环境索引统计从 `2026-11-06 10:24:55` 开始，所有“0 读取”结论都只代表该窗口，不能作为唯一判定依据；本报告已用“代码引用 + EF 模型 + 迁移 + 数据血缘”交叉验证。

---

## 附录 A：全表清单与证据（总行数 / 有效行数 / 引用 / 读取）

> 有效行数 = `IsDeleted=0`；读取次数 = seeks + scans + lookups（自 2026-11-06 10:24:55 起）。

| 表 | 总行数 | 有效行数 | 后端引用 | 迁移引用 | 索引读取 | 最后 Seek | 最后 Scan |
|---|---:|---:|---:|---:|---:|---|---|
| CrmBusinessActivities | 0 | 0 | 10 | 42 | 0 | - | - |
| CrmBusinessEntityAttributes | 3075 | 783 | 27 | 64 | 0 | - | - |
| CrmContacts | 2433 | 2433 | 72 | 48 | 28 | 2026-11-07 16:35:39 | - |
| CrmFollowRecords | 0 | 0 | 32 | 45 | 213 | - | 2026-11-07 18:29:41 |
| CrmHerbBaseSubjects | 2793 | 458 | 56 | 45 | 360 | 2026-11-07 18:29:41 | 2026-11-07 18:29:47 |
| CrmHerbBaseSupplies | 0 | 0 | 34 | 56 | 128 | - | 2026-11-07 18:29:47 |
| CrmHerbBases | 11675 | 2433 | 54 | 47 | 293 | 2026-11-07 18:29:47 | 2026-11-07 18:29:41 |
| CrmMatchCandidates | 0 | 0 | 20 | 45 | 8 | - | 2026-11-07 17:48:54 |
| CrmMatchOpportunities | 0 | 0 | 35 | 66 | 11 | 2026-11-07 16:35:35 | 2026-11-07 17:48:54 |
| CrmTransferRecords | 0 | 0 | 15 | 52 | 0 | - | - |
| CrmVendorDemandItems | 0 | 0 | 12 | 54 | 107 | - | 2026-11-07 18:29:41 |
| CrmVendorDemands | 0 | 0 | 35 | 64 | 217 | 2026-11-07 18:29:41 | 2026-11-07 18:29:41 |
| CrmVendors | 0 | 0 | 81 | 60 | 330 | 2026-11-07 18:29:41 | 2026-11-07 18:29:41 |
| ImportTempSrc | 14 | 14 | 0 | 0 | 0 | - | - |
| MallAddresses | 12 | 6 | 35 | 47 | 247 | 2026-11-07 16:20:53 | 2026-11-07 16:25:33 |
| MallArticles | 6 | 6 | 25 | 43 | 0 | - | - |
| MallBanners | 6 | 6 | 25 | 43 | 483 | 2026-11-07 16:17:27 | 2026-11-07 16:17:18 |
| MallCampaignCoupons | 5 | 5 | 8 | 26 | 69 | - | 2026-11-07 16:13:01 |
| MallCampaignProducts | 15 | 14 | 16 | 24 | 152 | 2026-11-07 15:38:53 | 2026-11-07 16:16:21 |
| MallCampaigns | 8 | 8 | 40 | 29 | 277 | 2026-11-07 16:13:01 | 2026-11-07 16:16:21 |
| MallCartItems | 32 | 4 | 20 | 60 | 513 | 2026-11-07 16:18:03 | 2026-11-07 16:25:33 |
| MallCategories | 27 | 27 | 54 | 45 | 4049 | 2026-11-07 16:25:33 | 2026-11-07 16:17:27 |
| MallCoupons | 6 | 5 | 58 | 56 | 355 | 2026-11-07 16:18:19 | 2026-11-07 16:15:23 |
| MallFreightRegions | 3 | 3 | 13 | 38 | 189 | - | 2026-11-07 16:18:24 |
| MallFreightTemplates | 2 | 2 | 30 | 52 | 244 | 2026-11-07 16:18:24 | 2026-11-07 16:18:24 |
| MallMemberCoupons | 9 | 9 | 29 | 41 | 361 | 2026-11-07 13:22:52 | 2026-11-07 16:18:19 |
| MallMemberLevels | 3 | 3 | 21 | 53 | 0 | - | - |
| MallMemberPoints | 2 | 2 | 9 | 35 | 0 | - | - |
| MallMembers | 11 | 11 | 42 | 46 | 528 | 2026-11-07 16:17:27 | 2026-11-07 16:18:13 |
| MallNotices | 4 | 4 | 23 | 43 | 0 | - | - |
| MallOrderItems | 56 | 56 | 14 | 64 | 532 | 2026-11-07 13:23:46 | 2026-11-07 16:18:13 |
| MallOrders | 35 | 35 | 74 | 119 | 1212 | 2026-11-07 15:43:01 | 2026-11-07 16:18:13 |
| MallPages | 3 | 3 | 18 | 43 | 0 | - | - |
| MallPayments | 23 | 23 | 30 | 42 | 204 | 2026-11-07 13:29:56 | 2026-11-07 15:43:01 |
| MallProductReviews | 17 | 17 | 25 | 48 | 1105 | 2026-11-07 15:38:29 | 2026-11-07 16:18:13 |
| MallProductTags | 1711 | 26 | 15 | 40 | 1155 | 2026-11-07 16:17:57 | 2026-11-07 16:17:27 |
| MallProducts | 117 | 116 | 76 | 85 | 2564 | 2026-11-07 16:25:33 | 2026-11-07 16:17:27 |
| MallPromotionScopes | 27 | 4 | 21 | 36 | 2660 | 2026-11-07 10:12:44 | 2026-11-07 16:25:33 |
| MallPromotions | 27 | 6 | 52 | 53 | 1429 | 2026-11-06 23:27:27 | 2026-11-07 16:25:33 |
| MallRefunds | 10 | 10 | 34 | 45 | 153 | 2026-11-07 13:30:31 | 2026-11-07 15:43:01 |
| MallReturns | 5 | 5 | 27 | 45 | 150 | 2026-11-07 13:39:01 | 2026-11-07 15:43:01 |
| MallShipments | 8 | 8 | 23 | 64 | 340 | 2026-11-07 14:47:54 | 2026-11-07 16:18:13 |
| MallSkus | 129 | 129 | 66 | 44 | 1819 | 2026-11-07 16:25:33 | 2026-11-07 16:25:33 |
| MallStockLogs | 77 | 77 | 24 | 36 | 24 | - | 2026-11-07 16:11:04 |
| MallStores | 1 | 1 | 36 | 63 | 916 | 2026-11-07 16:17:57 | 2026-11-07 16:17:18 |
| MallTags | 19 | 17 | 41 | 59 | 1717 | 2026-11-07 16:17:57 | 2026-11-07 16:25:33 |
| SystemChinaRegions | 124 | 124 | 3 | 44 | 66 | - | 2026-11-07 17:04:13 |
| SystemDataDictionaries | 27 | 27 | 22 | 53 | 60 | - | 2026-11-07 18:29:47 |
| SystemErrorLogs | 170 | 170 | 4 | 44 | 6 | - | 2026-11-07 16:25:21 |
| SystemOperationLogs | 5227 | 5227 | 4 | 44 | 6 | - | 2026-11-07 16:25:22 |
| SystemPermissions | 44 | 44 | 10 | 79 | 378 | 2026-11-07 17:49:48 | 2026-11-07 18:29:50 |
| SystemRolePermissions | 138 | 45 | 10 | 59 | 400 | 2026-11-07 18:29:50 | 2026-11-07 18:29:50 |
| SystemRoles | 2 | 2 | 12 | 49 | 387 | 2026-11-07 18:29:50 | 2026-11-07 18:29:50 |
| SystemUserRoles | 2 | 2 | 2 | 44 | 0 | - | - |
| SystemUsers | 2 | 2 | 20 | 44 | 2486 | 2026-11-07 18:29:50 | 2026-11-07 18:29:47 |

> `后端引用`为表名在 `web-api/src`（不含 Migrations）的字符串出现次数，含实体/DbSet/DTO/注释；仅作相对强弱参考，最终判断以文中“EF 模型 / 迁移 / 数据血缘 / 读写点”证据为准。

## 附录 B：关键取证 SQL（只读）

```sql
-- 表清单与行数
SELECT t.name, SUM(p.rows) AS [Rows]
FROM sys.tables t
JOIN sys.partitions p ON p.object_id = t.object_id AND p.index_id IN (0,1)
GROUP BY t.name ORDER BY t.name;

-- 列定义
SELECT t.name, c.column_id, c.name, ty.name, c.max_length, c.is_nullable, dc.definition
FROM sys.tables t
JOIN sys.columns c ON c.object_id = t.object_id
JOIN sys.types ty ON ty.user_type_id = c.user_type_id
LEFT JOIN sys.default_constraints dc ON dc.parent_object_id = t.object_id AND dc.parent_column_id = c.column_id;

-- 非空/有效行统计（示例：CrmHerbBases.CoordinateText）
SELECT COUNT(*) AS Total,
       SUM(CASE WHEN IsDeleted = 0 THEN 1 ELSE 0 END) AS Active,
       SUM(CASE WHEN IsDeleted = 0 AND CoordinateText IS NOT NULL THEN 1 ELSE 0 END) AS ActiveNonNull
FROM CrmHerbBases;

-- 索引使用（统计窗口自服务器启动）
SELECT OBJECT_NAME(i.object_id) AS TableName, i.name, us.user_seeks, us.user_scans,
       us.user_lookups, us.last_user_seek, us.last_user_scan
FROM sys.indexes i
LEFT JOIN sys.dm_db_index_usage_stats us
  ON us.object_id = i.object_id AND us.index_id = i.index_id AND us.database_id = DB_ID()
WHERE i.type > 0;

-- 依赖检查：是否有存储过程/视图/触发器引用 ImportTempSrc
SELECT o.name, o.type_desc
FROM sys.objects o
WHERE o.type IN ('P','V','TR','FN','IF','TF')
  AND OBJECT_DEFINITION(o.object_id) LIKE '%ImportTempSrc%';
```

审计结论以本报告第 2、3 节的证据为准；第 4 节只作为观察清单，不建议据此直接做删除。

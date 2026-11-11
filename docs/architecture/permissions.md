# 权限设计方案

> 状态：有效 | 最后核对：2026-11-10 | 对应代码：`web-api/src/QPS.System/Application/Services/PermissionChecker.cs`、`QPS.System/Application/Features/Permissions`、`web-api/src/QPS.Shared/Contracts/PermissionAttribute.cs`、`web-api/src/QPS.Shared/Pipeline/PermissionBehaviour.cs`、`admin/src/components/Permission`、`admin/src/assets/json/authMenuList.json`

本文档用于让后续开发者或 AI 快速理解 QPS 项目的权限设计。当前方案追求简单、直观、少封装。

## 1. 核心原则

权限码是唯一判断依据。

后端 `SystemPermissions.Code` 是权限来源，前端不再维护按钮权限映射表，也不再使用 `BUTTONS.xxx` 这类别名。

页面上需要控制权限的地方，直接写真实权限码：

```vue
<Permission code="CRM_HERB_BASE_CONTACT_PRIMARY">
  <el-button>设为主</el-button>
</Permission>
```

这样读页面时可以直接看到按钮受哪个权限控制。

## 2. 校验链路

`PermissionAttribute` 标在 Command / Query 类型上，由 `PermissionBehaviour` 在 MediatR 管道里统一校验，Controller 与路由中间件不参与权限码映射。`PermissionBehaviour.Handle` 的实际顺序：

1. 请求类型实现 `IAllowAnonymousRequest`，或端点元数据带 `IAllowAnonymous` → 直接放行。
2. 已认证且角色为 `member`、端点上声明了 `MallMember` 策略 → 直接放行（移动端商城接口走这条）。
3. `ICurrentUserService.UserId` 不是有效 GUID → 抛 `BusinessException(401, "登录状态无效")`。
4. `PermissionChecker.IsUserActiveAsync` 返回不可用 → 抛 `BusinessException(401, "当前用户不可用")`。
5. 请求类型上取到权限码且 `PermissionChecker.HasPermissionAsync` 不通过 → 抛 `BusinessException(403, "无权访问该功能")`。
6. 以上都通过才进入 Handler。

权限码从类型上读取后缓存在 `PermissionMetadataCache`（按 `Type` 缓存 `GetCustomAttribute<PermissionAttribute>()?.PermissionCode`），因此同一个请求类型只在首次解析反射，不要在运行期修改特性。

登录态与角色策略在 `QPS.Host/Registration/ServiceRegistrationExtensions.cs` 里配置：默认策略要求已认证用户，另有 `AdminOnly`（`RequireRole("admin")`）、`MallMember`（`RequireRole("member")`）、`AnyUser`（仅要求已认证）三条策略。管理端 Controller 统一使用 `AdminOnly`，功能权限在此基础上由 `PermissionBehaviour` 二次校验。

## 3. 当前权限清单

权限分页面入口（`PermissionType = Menu`）和页面内操作（`PermissionType = Button`）两类，都存放在 `SystemPermissions` 中。下表按「模块 → 页面 → 按钮」列出当前代码与前端页面中实际引用的权限码。

### 3.1 模块与页面（Menu）

| 模块 | 页面权限码 | 引用位置 |
|---|---|---|
| `HOME` | `HOME` | 首页（`/home/index`），顶层菜单即页面 |
| `CRM` | `CRM` | CRM 管理分组 |
| `CRM` | `CRM_FOLLOW_TASK` | 我的待办（`/crm/follow-tasks`） |
| `CRM` | `CRM_HERB_BASE` | 基地管理（`/crm/herb-base`） |
| `CRM` | `CRM_HERB_BASE_SUPPLY` | 供应列表（`/crm/herb-base-supply`），与基地管理独立授权 |
| `CRM` | `CRM_VENDOR` | 厂商管理（`/crm/vendor`） |
| `CRM` | `CRM_PURCHASE_DEMAND_VIEW` | 采购需求（`/crm/purchase-demand`） |
| `CRM` | `CRM_MATCH_OPPORTUNITY_VIEW` | 撮合机会（`/crm/match-opportunity`），同时用于详情按钮 |
| `MALL` | `MALL` | 商城管理分组 |
| `MALL` | `MALL_PRODUCT` | 商品管理（`/mall/product`） |
| `MALL` | `MALL_CATEGORY` | 商品分类（`/mall/category`） |
| `MALL` | `MALL_TAG` | 标签管理（`/mall/tag`） |
| `MALL` | `MALL_REVIEW` | 评价管理（`/mall/review`） |
| `MALL` | `MALL_STOCK` | 库存管理（`/mall/stock`） |
| `MALL` | `MALL_PROMOTION` | 促销活动（`/mall/promotion`） |
| `MALL` | `MALL_CAMPAIGN` | 专题活动（`/mall/campaign`） |
| `MALL` | `MALL_COUPON` | 优惠券管理（`/mall/coupon`） |
| `MALL` | `MALL_BANNER` | Banner 管理（`/mall/banner`） |
| `MALL` | `MALL_ORDER` | 订单管理（`/mall/order`） |
| `MALL` | `MALL_SHIPMENT` | 发货管理（`/mall/shipment`） |
| `MALL` | `MALL_RETURN_ORDER` | 退货管理（`/mall/returnOrder`） |
| `MALL` | `MALL_STORE` | 商城门店（`/mall/store`） |
| `MALL` | `MALL_FREIGHT` | 运费规则（`/mall/freight`） |
| `SYSTEM` | `SYSTEM` | 系统设置分组 |
| `SYSTEM` | `SYSTEM_ROLE` | 角色设置 |
| `SYSTEM` | `SYSTEM_PERMISSION` | 权限设置 |
| `SYSTEM` | `SYSTEM_USER` | 用户管理 |
| `SYSTEM` | `SYSTEM_DATA_DICTIONARY` | 数据字典 |
| `SYSTEM` | `SYSTEM_REGION` | 地址区域维护 |
| `SYSTEM` | `SYSTEM_OPERATION_LOG` | 操作日志 |
| `SYSTEM` | `SYSTEM_ERROR_LOG` | 错误日志 |

菜单数据来自 `admin/src/assets/json/authMenuList.json` 的 `meta.permissionCode`；`ROOT` 是权限树根，不在菜单与矩阵中展示。**每个菜单页面都必须声明 `permissionCode`**，否则权限矩阵里没有该行、无法授权；分组码（`CRM`、`MALL`、`SYSTEM`）既是菜单节点也是权限树中的祖先节点，勾选子页面时会自动补齐。权限矩阵按菜单顺序展示，按钮按「新增 → 编辑 → 删除」排列，删除类固定最后。

### 3.2 页面内操作（Button）

| 归属页面 | 权限码 | 说明 |
|---|---|---|
| 基地管理 | `CRM_HERB_BASE_ADD` / `CRM_HERB_BASE_EDIT` / `CRM_HERB_BASE_DELETE` / `CRM_HERB_BASE_STATUS` | 新增、编辑、删除、启停 |
| 基地管理 | `CRM_HERB_BASE_CONTACT_ADD` / `CRM_HERB_BASE_CONTACT_EDIT` / `CRM_HERB_BASE_CONTACT_PRIMARY` | 联系人新增、编辑、设为主 |
| 供应列表 | `CRM_HERB_BASE_SUPPLY_MANAGE` | 供应信息维护（新增、编辑、删除、状态变更合并为一个权限） |
| 基地管理 | `CRM_HERB_BASE_SUPPLY_VIEW` | 供应信息查看；由迁移创建，当前前后端均无引用，视为存量待清理 |
| 厂商管理 | `CRM_VENDOR_ADD` / `CRM_VENDOR_EDIT` | 新增、编辑 |
| 采购需求 | `CRM_PURCHASE_DEMAND_MANAGE` | 维护采购需求 |
| 撮合机会 | `CRM_MATCH_OPPORTUNITY_MANAGE` | 维护撮合机会 |
| 通用跟进 | `CRM_FOLLOW` | 新增跟进记录 |
| 通用流转 | `CRM_TRANSFER` | 基地主体与厂商的负责人流转，`TransferMode` 区分 assign / transfer / return |
| 用户管理 | `SYSTEM_USER_ADD` / `SYSTEM_USER_EDIT` | 新增、编辑用户 |
| 角色设置 | `SYSTEM_ROLE_ADD` / `SYSTEM_ROLE_EDIT` / `SYSTEM_ROLE_DELETE` | 新增、编辑、删除角色 |
| 权限设置 | `SYSTEM_PERMISSION_EDIT` | 保存角色权限矩阵 |
| 数据字典 | `SYSTEM_DATA_DICTIONARY_ADD` / `SYSTEM_DATA_DICTIONARY_EDIT` / `SYSTEM_DATA_DICTIONARY_DELETE` | 新增、编辑、删除字典项 |
| 商品管理 | `MALL_PRODUCT_ADD` / `MALL_PRODUCT_EDIT` / `MALL_PRODUCT_DELETE` | 新增、编辑、删除商品（含标签维护） |
| 商品分类 | `MALL_CATEGORY_ADD` / `MALL_CATEGORY_EDIT` / `MALL_CATEGORY_DELETE` | 新增、编辑、删除分类 |
| 标签管理 | `MALL_TAG_ADD` / `MALL_TAG_EDIT` / `MALL_TAG_DELETE` | 新增、编辑、删除标签 |
| 评价管理 | `MALL_REVIEW_EDIT` / `MALL_REVIEW_DELETE` | 审核、回复、删除评价 |
| 库存管理 | `MALL_STOCK_EDIT` | 调整库存（含批量调整） |
| 促销活动 | `MALL_PROMOTION_ADD` / `MALL_PROMOTION_EDIT` / `MALL_PROMOTION_DELETE` | 新增、编辑、删除促销 |
| 专题活动 | `MALL_CAMPAIGN_ADD` / `MALL_CAMPAIGN_EDIT` / `MALL_CAMPAIGN_DELETE` | 新增、编辑、删除专题 |
| 优惠券管理 | `MALL_COUPON_ADD` / `MALL_COUPON_EDIT` / `MALL_COUPON_DELETE` | 新增、编辑、删除优惠券 |
| Banner 管理 | `MALL_BANNER_ADD` / `MALL_BANNER_EDIT` / `MALL_BANNER_DELETE` | 新增、编辑、删除 Banner |
| 订单管理 | `MALL_ORDER_EDIT` | 发货、退款、收货、取消订单等状态操作 |
| 发货管理 | `MALL_SHIPMENT_EDIT` | 发货、作废发货单 |
| 退货管理 | `MALL_RETURN_ORDER_EDIT` | 通过、拒绝、确认收货并退款 |
| 商城门店 | `MALL_STORE_ADD` / `MALL_STORE_EDIT` / `MALL_STORE_DELETE` | 新增、编辑、删除门店 |
| 运费规则 | `MALL_FREIGHT_ADD` / `MALL_FREIGHT_EDIT` / `MALL_FREIGHT_DELETE` | 新增、编辑、删除运费模板 |

已废弃、不得重新引入的权限码：

- `CRM_HERB_BASE_ASSIGN`、`CRM_VENDOR_ASSIGN`：负责人分配已统一收敛到 `CRM_TRANSFER`，前后端与迁移中均无残留引用。
- `CRM_VENDOR_DELETE`：厂商页面不提供删除操作，代码中无该权限码。
- `CRM_HERB_BASE_CONTACT_INVALID`：联系人作废功能已移除，只允许出现在历史数据清理名单中。
- `CRM_VENDOR_PURCHASE_PLAN`：这是实体类型而非权限码。厂商采购计划已迁移为采购需求，实体类型改为 `CRM_PURCHASE_DEMAND`，只在历史迁移 `20260831013718_MoveVendorProductsToPurchasePlans` 的属性筛选索引里保留。

### 3.3 后端校验位置

`[Permission]` 特性只用于管理端接口，当前实际标注点：

| 权限码 | 标注位置 |
|---|---|
| `SYSTEM_PERMISSION` | `GetPermissionTreeQuery`、`GetRolePermissionsQuery` |
| `SYSTEM_PERMISSION_EDIT` | `UpdateRolePermissionsCommand` |
| `CRM_MATCH_OPPORTUNITY_VIEW` | 撮合机会列表与详情查询 |
| `CRM_MATCH_OPPORTUNITY_MANAGE` | 撮合机会创建、更新、进度选项查询 |
| `CRM_TRANSFER` | `ChangeCrmOwnerCommand` 内的 `TransferPermissionCode` 常量 |

重要事实：**除上述端点外，CRM 与 Mall 的管理端端点目前都没有 `[Permission]`，全部落在 `AdminPermissionBaseline.txt` 基线里**。也就是说 `CRM_HERB_BASE_ADD`、`CRM_VENDOR_EDIT`、`CRM_FOLLOW`、`CRM_HERB_BASE_SUPPLY_MANAGE` 等按钮权限当前只有前端生效，后端没有拦截。基线 205 条按项目分布为 `QPS.System` 16 条、`QPS.Crm` 60 条、`QPS.Mall` 129 条（含 `MallFileController.UploadImage`），这就是缺口的具体清单；不要把「权限码存在于 `SystemPermissions`」误认为「后端已校验」。

现状与规范差距（`web-api/AGENTS.md` 要求 CRM 权限码必须收敛到 `CrmPermissionCodes`，常量名与权限码字符串完全一致）：

- 只有撮合机会的两个权限码已收敛；`CRM_TRANSFER` 仍是 `ChangeCrmOwnerCommand` 内的私有常量，其余 CRM 权限码在业务代码里根本没有出现（因为对应端点尚未声明 `[Permission]`）。
- System 模块的 `SYSTEM_PERMISSION`、`SYSTEM_PERMISSION_EDIT` 直接写在 `[Permission("...")]` 上，System 侧尚无对应常量类。
- 新增或改动权限码时必须收敛进常量类并同步替换字面量；不要沿用存量写法继续扩散字面量。

`QPS.ArchitectureTests/AdminEndpointPermissionTests` 会校验每个管理端端点是否声明了 `[Permission]` 或落在 `AdminPermissionBaseline.txt` 基线内，新增端点必须二选一，否则测试失败。基线当前 205 条，只允许缩小：补齐权限后必须从基线删除对应行，改大需在提交说明里给出理由。给基线内的端点补 `[Permission]` 时，权限码必须与前端页面实际使用的字符串一致，否则前端放行的按钮会被后端 403。


## 4. 命名规则

按钮权限使用：

```text
页面权限码_操作
```

例如：

```text
CRM_HERB_BASE
CRM_HERB_BASE_ADD
CRM_HERB_BASE_EDIT
CRM_HERB_BASE_CONTACT_PRIMARY
```

不要在前端把 `contactPrimary` 转换成 `CONTACT_PRIMARY`。权限码应显式写出来。

## 5. 前端实现

前端使用 `Permission` 组件控制插槽显示：

```text
admin/src/components/Permission/index.vue
```

组件职责只有一个：判断当前用户权限列表是否包含传入的 `code`，实现即 `authStore.userPermissions.includes(props.code)`，不含角色判断、不含菜单判断。

页面用法：

```vue
<Permission code="SYSTEM_USER_ADD">
  <el-button type="primary">新增用户</el-button>
</Permission>
```

如果还需要业务条件，业务条件写在插槽内部：

```vue
<Permission code="CRM_HERB_BASE_CONTACT_PRIMARY">
  <el-button v-if="!row.isPrimary" type="primary" link>设为主</el-button>
</Permission>
```

不要恢复或新增 `useAuthButtons` 这类映射封装。

模板里需要先算业务条件、再决定是否渲染整块 UI（例如按 `CRM_TRANSFER` 控制详情抽屉里的流转入口）时，用 store 上的 `hasPermissionGet(code)`，它同样只做「权限列表是否包含该 code」的判断：

```ts
const canManageTransfer = computed(() => authStore.hasPermissionGet("CRM_TRANSFER"));
```

两种写法都直接写真实权限码，区别只是 `Permission` 组件用于模板插槽，`hasPermissionGet` 用于脚本中的计算属性。

## 6. 后端职责

后端负责维护权限数据和登录后的权限返回。

主要职责：

```text
1. 在迁移中增量维护标准权限（写入 SystemPermissions 并挂父节点）。
2. 维护权限父子关系。
3. 按角色分配权限（SystemRolePermissions）。
4. 清理废弃权限。
5. 登录后返回当前用户拥有的权限码列表。
```

第 5 项由 `GetUserPermissionsQuery` 实现：从 `SystemUsers.RoleId` 出发关联 `SystemRolePermissions` 与 `SystemPermissions`，返回该用户角色可用的权限码数组，供前端 `Permission` 组件与 `hasPermissionGet` 判断。

废弃权限只应放在退休权限清理名单里，不应再次创建，完整名单见上文「当前权限清单」的「已废弃、不得重新引入的权限码」。

## 7. 角色策略

基础数据（`ROOT` 权限、`HOME` 等菜单权限、`admin` / `user` 角色、初始管理员账号）不在仓库里：`20260828072947_CrmSchemaBaseline` 是空迁移（`Up` / `Down` 都无内容），`QPS.Persistence/Seeds` 只有 `MallMockDataSeeder`（商城演示数据）。这些权限与角色数据来自既有数据库本身，`QPS.Persistence/Migrations` 只做增量：新增权限码、给 `admin` 角色补授权。因此迁移里插入新权限时，必须假设 `ROOT` 与 `admin` 已存在，写法上先 `SELECT` 取父节点 ID、取不到就跳过或抛错，不要硬编码 GUID。

`admin` 角色拥有完整权限。普通 `user` 角色默认只有 `HOME` 入口，因此默认不能进入基地或厂商管理；需要业务人员维护卖方和买方时，应由管理员在权限设置页授予 `CRM_HERB_BASE`、`CRM_VENDOR` 及对应操作权限。

新增权限码时，迁移里必须同时完成三件事：写入 `SystemPermissions`、挂到正确的父节点（页面码或分组码，不要直接挂 `ROOT` 绕过层级）、给 `admin` 角色插入 `SystemRolePermissions` 记录（幂等写法参考 `20260902063400_AddCrmHerbBaseSupplies` 的 `NOT EXISTS` 判断）。

## 8. 新增权限流程

新增一个权限码时按以下流程，任一步缺失都视为未完成：

1. 后端常量类里加常量（CRM 放 `CrmPermissionCodes`），常量名与权限码字符串完全一致。
2. 写 EF 迁移：插入 `SystemPermissions`（指定正确父节点）、给 `admin` 角色授权，`Down` 里对称删除。
3. 页面入口权限同步写入 `admin/src/assets/json/authMenuList.json` 的 `meta.permissionCode`。
4. 页面用 `Permission` 组件或 `hasPermissionGet` 包住受控 UI。
5. 若端点尚无 `[Permission]`，同时补上；补完从 `AdminPermissionBaseline.txt` 删除对应行。
6. 跑 `npm run build` 与 `dotnet test` 验证。

示例：

```vue
<Permission code="CRM_HERB_BASE_EXPORT">
  <el-button>导出</el-button>
</Permission>
```

不要新增前端权限映射，不要使用隐式拼接规则。
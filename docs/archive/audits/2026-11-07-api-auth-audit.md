# 接口鉴权审计报告

- 审计对象：`web-api/src/**/Controllers`（QPS.Mall / QPS.Crm / QPS.System / QPS.Host）
- 审计方式：静态盘点（只读，未改代码、未动 git）
- 审计时间：2026-11-07

## 1. 总览

| 模块 | 控制器 | 端点 | 匿名 | 仅登录（无角色） | AdminOnly | MallMember |
|---|---|---|---|---|---|---|
| Mall/Admin | 19 | 128 | 0 | 0 | 128 | 0 |
| Mall/Mobile | 18 | 52 | 18 | 0 | 0 | 34 |
| Crm | 7 | 66 | 0 | 66 | 0 | 0 |
| System | 8 | 25 | 2 | 23 | 0 | 0 |
| Host | 2 | 2 | 0 | 1 | 0 | 1 |
| **合计** | **54** | **273** | **20** | **90** | **128** | **35** |

> 统计口径：按端点（HTTP 动作）计；有效鉴权取「动作级 AllowAnonymous > 动作级 Authorize > 类级策略 > FallbackPolicy」。因此少数挂在 AdminOnly 控制器上的公开读接口计入「匿名」。

## 2. 鉴权现状

### 2.1 身份与令牌

| 项 | 现状 | 位置 |
|---|---|---|
| 管理端登录 | `POST /api/admin/auth/login`（`[AllowAnonymous]`），校验 SystemUsers，角色取 SystemRoles.Code，取不到时兜底 `"admin"` | [AuthController.cs](file:///e:/Code/QPS/web-api/src/QPS.System/Controllers/AuthController.cs#L21-L28)、[LoginCommand.cs](file:///e:/Code/QPS/web-api/src/QPS.System/Application/Features/Auth/LoginCommand.cs#L58-L67) |
| 会员登录 | `POST /api/mall/auth/login`、`/register`，角色硬编码 `"member"` | [MallMemberHandlers.cs](file:///e:/Code/QPS/web-api/src/QPS.Mall/Application/Features/MallMembers/MallMemberHandlers.cs#L207) |
| 令牌内容 | 两种令牌都由同一个 `JwtGenerator` 签发，claim 只有 `userId` / `username` / `role`，issuer、audience、secret 完全相同 | [JwtGenerator.cs](file:///e:/Code/QPS/web-api/src/QPS.Persistence/Identity/JwtGenerator.cs#L32-L53) |
| 会员与后台的可区分点 | `role`：`member` / 后台角色码（默认 `admin`）；无 audience 或签名隔离 | 同上 |
| `FallbackPolicy` | `RequireAuthenticatedUser()`：未显式标注的端点，任何已登录令牌（含会员令牌）都能通过 HTTP 鉴权 | [ServiceRegistrationExtensions.cs](file:///e:/Code/QPS/web-api/src/QPS.Host/Registration/ServiceRegistrationExtensions.cs#L149-L157) |
| `AdminOnly` | `RequireRole("admin")`；`RoleClaimType = "role"` | 同上，L144、L154 |
| `MallMember` | `RequireRole("member")` | 同上，L155 |
| 应用层兜底 | MediatR `PermissionBehaviour`：非匿名请求要求 userId 是有效 SystemUser，随后按 `PermissionAttribute` 校验权限码；仅当端点策略为 `MallMember` 时对会员放行 | [PermissionBehaviour.cs](file:///e:/Code/QPS/web-api/src/QPS.Shared/Pipeline/PermissionBehaviour.cs#L31-L53) |

### 2.2 关键结论

1. **Admin 控制器已全部标注 `[Authorize(Policy = "AdminOnly")]`**：Mall 的 19 个 Admin 控制器、128 个端点全部带策略，会员令牌访问会得到 403。原任务前提「全仓零引用」已不成立，该修复已在上一次提交中落地。
2. **仍有 90 个端点只验登录、不验角色**：CRM 66 个、QPS.System 23 个、Host 上传 1 个。会员令牌在 HTTP 层可通过这些端点，但进入 MediatR 后 `PermissionBehaviour` 会因 userId 不是有效 SystemUser 返回 401，所以**目前未形成实际越权**；但这是应用层偶然兜住，不是设计保证，且非 admin 的后台角色账号可正常越权。
3. **会员令牌与后台令牌仅靠 `role` 字段区分**，签名与 issuer/audience 相同；任何漏加策略的端点都会直接成为会员越权入口。

## 3. 风险分级

### 3.1 未鉴权的敏感接口（最高优先）

| 等级 | 接口 | 问题 |
|---|---|---|
| 高 | `GET /api/mall/addresses` | 不传 `userId` 时返回**全量会员收货地址**（姓名、手机号、详细地址）；handler 无当前用户过滤 |
| 高 | `GET /api/mall/addresses/{addressId}` | 仅按 addressId 查询，无归属校验（IDOR） |
| 高 | `PUT /api/mall/addresses/{addressId}` | 可修改他人地址；仅按 addressId 定位 |
| 高 | `DELETE /api/mall/addresses/{addressId}` | 可删除他人地址 |
| 中 | `POST /api/mall/addresses` | 直接信任 body 里的 `userId`，可替他人建地址；默认地址逻辑也按该 userId 处理 |
| 中 | `POST /api/admin/file/upload/img` | 控制器无 `[Authorize]`，只有 FallbackPolicy → **会员令牌可成功上传**（该接口不走 MediatR，无兜底） |

> 地址链路位于 [MallAddressController.cs](file:///e:/Code/QPS/web-api/src/QPS.Mall/Controllers/Mobile/MallAddressController.cs#L21-L51)，handler 分别位于 [GetMallAddressesQuery.cs](file:///e:/Code/QPS/web-api/src/QPS.Mall/Application/Features/MallAddresses/GetMallAddressesQuery.cs#L23-L41)、[GetMallAddressQuery.cs](file:///e:/Code/QPS/web-api/src/QPS.Mall/Application/Features/MallAddresses/GetMallAddressQuery.cs#L23-L51)、[UpdateMallAddressCommand.cs](file:///e:/Code/QPS/web-api/src/QPS.Mall/Application/Features/MallAddresses/UpdateMallAddressCommand.cs#L26-L61)。控制器既没有注入 `ICurrentUserService`，handler 也没有归属校验。

### 3.2 只验登录、不验角色的后台接口

| 等级 | 模块 | 端点 | 说明 |
|---|---|---|---|
| 高 | QPS.System 用户/角色/权限 | 12 | `UserController`(4)、`RoleController`(4)、`PermissionController`(4) 均无角色策略，仅 `UpdateRolePermissionsCommand` 带 PermissionAttribute；任何有效后台账号可建用户、改角色、改权限配置 |
| 中 | QPS.System 字典/日志 | 8 | 数据字典增删改、操作日志、错误日志只要求登录 |
| 中 | QPS.Crm 全部 | 66 | 7 个控制器均无 `[Authorize(Policy)]`；只有撮合机会的 5 个命令带 PermissionAttribute，其余基地/厂商/需求/跟进/仪表盘无权限码校验 |
| 中 | QPS.Host 上传 | 1 | 见 3.1 |
| 低 | QPS.System Auth | 3 | `logout`、`user-permissions`、`change-password` 针对当前令牌用户，风险有限；建议一并收敛到 admin 侧策略 |

### 3.3 正常的匿名只读接口（20 个，无需处理）

| 类型 | 接口 |
|---|---|
| 移动端公开目录 | `GET /api/mall/products`、`/products/{id}`、`/products/{id}/reviews`、`/categories`、`/categories/tree`、`/articles`、`/articles/{id}`、`/banners`、`/campaigns`、`/campaigns/{id}`、`/campaigns/{id}/products`、`/notices`、`/pages/{pageCode}`、`/tags`、`/store/default` |
| 认证入口 | `POST /api/mall/auth/login`、`/register`、`POST /api/admin/auth/login` |
| 基础数据 | `GET /api/admin/china-regions`（行政区数据，前后台共用） |
| 试算 | `POST /api/mall/freight-preview`（只读运费试算，不落库、不返回他人数据） |

## 4. 会员标识来源与归属校验

| 控制器 | 会员标识来源 | 归属校验 | 结论 |
|---|---|---|---|
| MallCartController | 写操作强制覆盖为令牌 userId；路径 userId 仅兼容旧前端 | `EnsurePathUser` 校验路径与令牌一致（空 Guid 放行） | ✅ |
| MallOrderController | 全部取令牌 userId；查询/详情/取消/支付/退款都附带 MemberId | handler 按 MemberId 过滤 | ✅ |
| MallReturnExternalController | 令牌 userId 传入命令 | `MallOrderResolver.EnsureOwnership`（管理端代客时 MemberId=null 跳过） | ✅ |
| MallMemberController | 令牌 userId | — | ✅ |
| MallCouponController | 令牌 userId（可用券接口按登录态区分公共券/已领券） | — | ✅ |
| MallAddressController | **query 的 `userId` 或 body 的 `userId`** | **无任何归属校验** | ❌ 见 3.1 |
| MallProductReviewController | 令牌 userId | handler 校验已完成订单归属 | ✅ |

## 5. 建议改法（按最小改动）

1. **Admin/Mall**：保持现状（19 个 Admin 控制器已统一 `AdminOnly`）。新增 Admin 控制器时沿用该约定，无需另造策略。
2. **QPS.System**：给 `UserController`、`RoleController`、`PermissionController`、`DataDictionaryController`、`OperationLogController`、`ErrorLogController` 类上加 `[Authorize(Policy = "AdminOnly")]`；`AuthController` 保留 `[Authorize]`，登录动作继续 `[AllowAnonymous]`。
3. **QPS.Crm**：7 个控制器类上加 `[Authorize(Policy = "AdminOnly")]`；撮合机会已有 PermissionAttribute，无需改动。
4. **QPS.Host**：`FileUploadController` 类上加 `[Authorize(Policy = "AdminOnly")]`（会员上传已有独立的 `MemberFileUploadController`）。
5. **收货地址**：控制器注入 `ICurrentUserService`，列表/详情/修改/删除按当前会员归属过滤，创建时覆盖 body `userId`；只动 Mall 模块内部，不改共享层。
6. **长期**：会员令牌与后台令牌使用不同 audience 或密钥，可从签发层彻底隔离；本期不必做。

## 6. 附：Admin 控制器清单（QPS.Mall/Controllers/Admin）

| 控制器（文件） | 路由前缀 | 鉴权 | 端点（方法 · 路径 · 作用） |
|---|---|---|---|
| MallArticleController<br>`MallArticleController.cs` | （动作级绝对路径） | AdminOnly | GET /api/admin/mall/articles · 查询列表<br>GET /api/admin/mall/articles/{id:guid} · 查询详情<br>POST /api/admin/mall/articles · 新增<br>PUT /api/admin/mall/articles/{id:guid} · 修改<br>DELETE /api/admin/mall/articles/{id:guid} · 删除<br>PUT /api/admin/mall/articles/{id:guid}/publish · 发布<br>PUT /api/admin/mall/articles/{id:guid}/unpublish · 下架<br>PUT /api/admin/mall/articles/{id:guid}/top · 设置默认/置顶/主联系人 |
| MallBannerController<br>`MallBannerController.cs` | （动作级绝对路径） | AdminOnly | GET /api/admin/mall/banners · 查询列表<br>GET /api/admin/mall/banners/{id:guid} · 查询详情<br>POST /api/admin/mall/banners · 新增<br>PUT /api/admin/mall/banners/{id:guid} · 修改<br>DELETE /api/admin/mall/banners/{id:guid} · 删除<br>PUT /api/admin/mall/banners/{id:guid}/status · 改状态/属性<br>POST /api/admin/mall/banners/batch-sort · 批量排序 |
| MallCampaignController<br>`MallCampaignController.cs` | /api/admin/mall | AdminOnly | GET /api/admin/mall/campaigns · 查询列表<br>GET /api/admin/mall/campaigns/coupons · 查询列表<br>GET /api/admin/mall/campaigns/{id:guid} · 查询详情<br>POST /api/admin/mall/campaigns · 新增<br>PUT /api/admin/mall/campaigns/{id:guid} · 修改<br>PATCH /api/admin/mall/campaigns/{id:guid}/status · 改状态/属性<br>DELETE /api/admin/mall/campaigns/{id:guid} · 删除 |
| MallCategoryController<br>`MallCategoryController.cs` | /api/admin/mall/categories | AdminOnly | GET /api/admin/mall/categories · 查询列表<br>GET /api/admin/mall/categories/{id:guid} · 查询详情<br>POST /api/admin/mall/categories · 新增<br>PUT /api/admin/mall/categories/{id:guid} · 修改<br>PATCH /api/admin/mall/categories/{id:guid}/status · 改状态/属性<br>DELETE /api/admin/mall/categories/{id:guid} · 删除 |
| MallController<br>`MallController.cs` | /api/admin/mall | AdminOnly | GET /api/admin/mall/ping · 健康检查<br>GET /api/admin/mall/stores · 查询列表<br>GET /api/admin/mall/stores/{id:guid} · 查询详情<br>POST /api/admin/mall/stores · 新增<br>PUT /api/admin/mall/stores/{id:guid} · 修改<br>PATCH /api/admin/mall/stores/{id:guid}/status · 改状态/属性<br>DELETE /api/admin/mall/stores/{id:guid} · 删除 |
| MallCouponController<br>`MallCouponController.cs` | /api/admin/mall/coupons | AdminOnly | GET /api/admin/mall/coupons · 查询列表<br>GET /api/admin/mall/coupons/{id} · 查询详情<br>POST /api/admin/mall/coupons · 新增<br>PUT /api/admin/mall/coupons/{id} · 修改<br>DELETE /api/admin/mall/coupons/{id} · 删除<br>PUT /api/admin/mall/coupons/{id}/status · 改状态/属性<br>PUT /api/admin/mall/coupons/{id}/replenish · 补发券<br>GET /api/admin/mall/coupons · 查询列表<br>POST /api/admin/mall/coupons/claim · 领取<br>PUT /api/admin/mall/coupons/{id}/use · 核销 |
| MallFreightTemplateController<br>`MallFreightTemplateController.cs` | /api/admin/mall | AdminOnly | GET /api/admin/mall/freight-templates · 查询列表<br>GET /api/admin/mall/freight-templates/{templateId:guid} · 查询详情<br>POST /api/admin/mall/freight-templates · 新增<br>PUT /api/admin/mall/freight-templates/{templateId:guid} · 修改<br>DELETE /api/admin/mall/freight-templates/{templateId:guid} · 删除 |
| MallMemberController<br>`MallMemberController.cs` | /api/admin/mall/members | AdminOnly | GET /api/admin/mall/members · 查询列表<br>GET /api/admin/mall/members/{id} · 查询详情<br>GET /api/admin/mall/members/by-phone/{phone} · 查询详情<br>POST /api/admin/mall/members · 新增<br>PUT /api/admin/mall/members/{id} · 修改<br>DELETE /api/admin/mall/members/{id} · 删除<br>PUT /api/admin/mall/members/{id}/status · 改状态/属性<br>PUT /api/admin/mall/members/{id}/adjust-points · 批量/库存/积分调整 |
| MallMemberLevelController<br>`MallMemberLevelController.cs` | /api/admin/mall/member-levels | AdminOnly | GET /api/admin/mall/member-levels · 查询列表<br>GET /api/admin/mall/member-levels/{id} · 查询详情<br>POST /api/admin/mall/member-levels · 新增<br>PUT /api/admin/mall/member-levels/{id} · 修改<br>DELETE /api/admin/mall/member-levels/{id} · 删除<br>PUT /api/admin/mall/member-levels/{id}/status · 改状态/属性<br>PUT /api/admin/mall/member-levels/{id}/set-default · 设置默认/置顶/主联系人 |
| MallNoticeController<br>`MallNoticeController.cs` | （动作级绝对路径） | AdminOnly | GET /api/admin/mall/notices · 查询列表<br>GET /api/admin/mall/notices/{id:guid} · 查询详情<br>POST /api/admin/mall/notices · 新增<br>PUT /api/admin/mall/notices/{id:guid} · 修改<br>DELETE /api/admin/mall/notices/{id:guid} · 删除<br>PUT /api/admin/mall/notices/{id:guid}/status · 改状态/属性<br>PUT /api/admin/mall/notices/{id:guid}/top · 设置默认/置顶/主联系人 |
| MallOrderController<br>`MallOrderController.cs` | /api/admin/mall | AdminOnly | GET /api/admin/mall/orders · 查询列表<br>GET /api/admin/mall/orders/{id:guid} · 查询详情<br>POST /api/admin/mall/orders/{id:guid}/cancel · 取消<br>POST /api/admin/mall/orders/{id:guid}/shipments · 新增<br>POST /api/admin/mall/orders/{id:guid}/confirm-receipt · 审批/确认/模拟<br>POST /api/admin/mall/orders/{id:guid}/refunds · 新增<br>GET /api/admin/mall/payments · 查询列表<br>GET /api/admin/mall/refunds · 查询列表<br>POST /api/admin/mall/payments/{id:guid}/simulate · 审批/确认/模拟<br>POST /api/admin/mall/refunds/{id:guid}/simulate · 审批/确认/模拟 |
| MallPageController<br>`MallPageController.cs` | （动作级绝对路径） | AdminOnly | GET /api/admin/mall/pages · 查询列表<br>GET /api/admin/mall/pages/{id:guid} · 查询详情<br>POST /api/admin/mall/pages · 新增<br>PUT /api/admin/mall/pages/{id:guid} · 修改<br>DELETE /api/admin/mall/pages/{id:guid} · 删除<br>PUT /api/admin/mall/pages/{id:guid}/status · 改状态/属性 |
| MallProductController<br>`MallProductController.cs` | /api/admin/mall | AdminOnly | GET /api/admin/mall/products · 查询列表<br>GET /api/admin/mall/products/{id:guid} · 查询详情<br>POST /api/admin/mall/products · 新增<br>PUT /api/admin/mall/products/{id:guid} · 修改<br>PATCH /api/admin/mall/products/{id:guid}/status · 改状态/属性<br>PATCH /api/admin/mall/products/{id:guid}/hot · 改状态/属性<br>DELETE /api/admin/mall/products/{id:guid} · 删除<br>POST /api/admin/mall/products/{id:guid}/copy · 复制<br>POST /api/admin/mall/products/{productId:guid}/skus · 新增<br>PUT /api/admin/mall/skus/{id:guid} · 修改<br>PATCH /api/admin/mall/skus/{id:guid}/status · 改状态/属性<br>DELETE /api/admin/mall/skus/{id:guid} · 删除 |
| MallProductReviewController<br>`MallProductReviewController.cs` | /api/admin/mall/reviews | AdminOnly | GET /api/admin/mall/reviews · 查询列表<br>PATCH /api/admin/mall/reviews/{id:guid}/status · 改状态/属性<br>PATCH /api/admin/mall/reviews/{id:guid}/featured · 改状态/属性<br>PUT /api/admin/mall/reviews/{id:guid}/reply · 回复<br>DELETE /api/admin/mall/reviews/{id:guid} · 删除 |
| MallPromotionController<br>`MallPromotionController.cs` | /api/admin/mall | AdminOnly | GET /api/admin/mall/promotions · 查询列表<br>GET /api/admin/mall/promotions/{promotionId:guid} · 查询详情<br>POST /api/admin/mall/promotions · 新增<br>PUT /api/admin/mall/promotions/{promotionId:guid} · 修改<br>PATCH /api/admin/mall/promotions/{promotionId:guid}/status · 改状态/属性<br>DELETE /api/admin/mall/promotions/{promotionId:guid} · 删除 |
| MallReturnAdminController<br>`MallReturnAdminController.cs` | /api/admin/mall | AdminOnly | GET /api/admin/mall/returns · 查询列表<br>GET /api/admin/mall/returns/{id:guid} · 查询详情<br>POST /api/admin/mall/returns/{returnNo}/approve · 审批/确认/模拟<br>POST /api/admin/mall/returns/{returnNo}/confirm-receipt · 审批/确认/模拟 |
| MallShipmentController<br>`MallShipmentController.cs` | /api/admin/mall | AdminOnly | GET /api/admin/mall/shipments · 查询列表<br>GET /api/admin/mall/shipments/{id:guid} · 查询详情<br>POST /api/admin/mall/shipments/{id:guid}/void · 作废 |
| MallStockController<br>`MallStockController.cs` | /api/admin/mall | AdminOnly | GET /api/admin/mall/skus · 查询列表<br>POST /api/admin/mall/skus/{id:guid}/adjust-stock · 批量/库存/积分调整<br>POST /api/admin/mall/skus/batch-adjust · 批量/库存/积分调整<br>GET /api/admin/mall/stock-logs · 查询列表 |
| MallTagController<br>`MallTagController.cs` | /api/admin/mall/tags | AdminOnly | GET /api/admin/mall/tags · 查询列表<br>GET /api/admin/mall/tags/page · 查询列表<br>POST /api/admin/mall/tags · 新增<br>PUT /api/admin/mall/tags/{id:guid} · 修改<br>PATCH /api/admin/mall/tags/{id:guid}/status · 改状态/属性<br>DELETE /api/admin/mall/tags/{id:guid} · 删除 |


## 7. 附：Mobile 控制器清单（QPS.Mall/Controllers/Mobile）

| 控制器（文件） | 路由前缀 | 鉴权 | 端点（方法 · 路径 · 作用） |
|---|---|---|---|
| MallAddressController<br>`MallAddressController.cs` | /api/mall | MallMember | GET /api/mall/addresses · 查询列表<br>GET /api/mall/addresses/{addressId:guid} · 查询详情<br>POST /api/mall/addresses · 新增<br>PUT /api/mall/addresses/{addressId:guid} · 修改<br>DELETE /api/mall/addresses/{addressId:guid} · 删除 |
| MallArticleController<br>`MallArticleController.cs` | /api/mall | 匿名 | GET /api/mall/articles · 查询列表<br>GET /api/mall/articles/{id:guid} · 查询详情 |
| MallAuthController<br>`MallAuthController.cs` | /api/mall/auth | 匿名 | POST /api/mall/auth/register · 注册<br>POST /api/mall/auth/login · 登录 |
| MallBannerController<br>`MallBannerController.cs` | /api/mall | 匿名 | GET /api/mall/banners · 查询列表 |
| MallCampaignPublicController<br>`MallCampaignController.cs` | /api/mall | 匿名 | GET /api/mall/campaigns · 查询列表<br>GET /api/mall/campaigns/{id:guid} · 查询详情<br>GET /api/mall/campaigns/{id:guid}/products · 查询详情 |
| MallCartController<br>`MallCartController.cs` | /api/mall | MallMember | GET /api/mall/carts/{userId:guid} · 查询详情<br>POST /api/mall/carts/add · 新增<br>PUT /api/mall/carts/update-item · 修改<br>DELETE /api/mall/carts/{userId:guid}/items/{skuId:guid} · 删除<br>POST /api/mall/carts/remove-items · 删除<br>DELETE /api/mall/carts/{userId:guid} · 删除 |
| MallCategoryController<br>`MallCategoryController.cs` | /api/mall | 匿名 | GET /api/mall/categories/tree · 查询列表<br>GET /api/mall/categories · 查询列表 |
| MallCouponController<br>`MallCouponController.cs` | /api/mall | MallMember | GET /api/mall/coupons/available · 查询列表<br>POST /api/mall/coupons/for-order · 查询列表 |
| MallFreightPreviewController<br>`MallFreightPreviewController.cs` | /api/mall | 匿名 | POST /api/mall/freight-preview · 试算/可用查询 |
| MallMemberController<br>`MallMemberController.cs` | /api/mall | MallMember | GET /api/mall/member/me · 查询列表<br>PUT /api/mall/member/me · 修改<br>PUT /api/mall/member/me/password · 修改密码<br>GET /api/mall/member/me/points · 查询列表<br>GET /api/mall/member/me/order-badge · 查询列表<br>GET /api/mall/member/me/coupons · 查询列表<br>GET /api/mall/member/me/coupon-count · 查询列表<br>POST /api/mall/member/me/coupons/{couponId}/claim · 领取 |
| MallNoticeController<br>`MallNoticeController.cs` | /api/mall | 匿名 | GET /api/mall/notices · 查询列表 |
| MallOrderController<br>`MallOrderController.cs` | /api/mall | MallMember | POST /api/mall/orders · 新增<br>POST /api/mall/orders/preview · 试算/可用查询<br>GET /api/mall/orders · 查询列表<br>GET /api/mall/orders/{orderNo} · 查询详情<br>POST /api/mall/orders/{orderNo}/cancel · 取消<br>POST /api/mall/orders/{orderNo}/payments · 新增<br>POST /api/mall/payments/{paymentNo}/simulate · 审批/确认/模拟<br>POST /api/mall/orders/{orderNo}/confirm-receipt · 审批/确认/模拟<br>POST /api/mall/orders/{orderNo}/refunds · 新增<br>POST /api/mall/refunds/{refundNo}/simulate · 审批/确认/模拟 |
| MallPageController<br>`MallPageController.cs` | /api/mall | 匿名 | GET /api/mall/pages/{pageCode} · 查询详情 |
| MallProductController<br>`MallProductController.cs` | /api/mall | 匿名 | GET /api/mall/products · 查询列表<br>GET /api/mall/products/{id:guid} · 查询详情<br>GET /api/mall/products/{id:guid}/reviews · 查询详情 |
| MallProductReviewController<br>`MallProductReviewController.cs` | /api/mall | MallMember | POST /api/mall/products/{productId:guid}/reviews · 新增 |
| MallReturnExternalController<br>`MallReturnExternalController.cs` | /api/mall | MallMember | POST /api/mall/orders/{orderNo}/returns · 新增<br>POST /api/mall/returns/{returnNo}/ship · 发货 |
| MallStoreController<br>`MallStoreController.cs` | /api/mall | 匿名 | GET /api/mall/store/default · 查询列表 |
| MallTagController<br>`MallTagController.cs` | /api/mall/tags | 匿名 | GET /api/mall/tags · 查询列表 |


## 8. 附：QPS.Crm 控制器清单

| 控制器（文件） | 路由前缀 | 鉴权 | 端点（方法 · 路径 · 作用） |
|---|---|---|---|
| CrmBusinessEntityAttributeController<br>`CrmBusinessEntityAttributeController.cs` | /api/admin/crm/business-entity-attributes | 仅登录（FallbackPolicy） | GET /api/admin/crm/business-entity-attributes · 查询列表<br>PUT /api/admin/crm/business-entity-attributes · 保存扩展属性 |
| CrmFollowTaskController<br>`CrmFollowTaskController.cs` | /api/admin/crm/follow-tasks | 仅登录（FallbackPolicy） | GET /api/admin/crm/follow-tasks · 查询列表<br>GET /api/admin/crm/follow-tasks/overview · 查询列表 |
| CrmHerbBaseController<br>`CrmHerbBaseController.cs` | /api/admin/crm/herb-bases | 仅登录（FallbackPolicy） | GET /api/admin/crm/herb-base-subjects · 查询列表<br>GET /api/admin/crm/herb-base-subjects/{id:guid} · 查询详情<br>PATCH /api/admin/crm/herb-base-subjects/{subjectId:guid}/owner · 改状态/属性<br>GET /api/admin/crm/herb-base-subjects/{subjectId:guid}/contacts · 查询详情<br>POST /api/admin/crm/herb-base-subjects/{subjectId:guid}/contacts · 新增<br>PUT /api/admin/crm/herb-base-subjects/{subjectId:guid}/contacts/{contactId:guid} · 修改<br>PATCH /api/admin/crm/herb-base-subjects/{subjectId:guid}/contacts/{contactId:guid}/primary · 设置默认/置顶/主联系人<br>PATCH /api/admin/crm/herb-base-subjects/{subjectId:guid}/contacts/{contactId:guid}/status · 修改<br>GET /api/admin/crm/herb-base-subjects/{subjectId:guid}/follow-records · 查询详情<br>POST /api/admin/crm/herb-base-subjects/{subjectId:guid}/follow-records · 新增<br>GET /api/admin/crm/herb-base-subjects/{subjectId:guid}/transfer-records · 查询详情<br>PUT /api/admin/crm/herb-base-subjects/{id:guid} · 修改<br>GET /api/admin/crm/herb-bases · 查询列表<br>GET /api/admin/crm/herb-bases/{id} · 查询详情<br>POST /api/admin/crm/herb-bases · 新增<br>PUT /api/admin/crm/herb-bases/{id} · 修改<br>DELETE /api/admin/crm/herb-bases/{id} · 删除<br>GET /api/admin/crm/herb-bases/{herbBaseId:guid}/supplies · 查询详情<br>GET /api/admin/crm/herb-base-supplies · 查询列表<br>POST /api/admin/crm/herb-bases/{herbBaseId:guid}/supplies · 新增<br>PUT /api/admin/crm/herb-base-supplies/{id:guid} · 修改<br>DELETE /api/admin/crm/herb-base-supplies/{id:guid} · 删除<br>PATCH /api/admin/crm/herb-base-supplies/{id:guid}/status · 改状态/属性 |
| CrmMatchOpportunityController<br>`CrmMatchOpportunityController.cs` | /api/admin/crm | 仅登录（FallbackPolicy） | GET /api/admin/crm/match-opportunities · 查询列表<br>GET /api/admin/crm/match-opportunity-progress-options · 查询列表<br>GET /api/admin/crm/match-opportunities/{id:guid} · 查询详情<br>GET /api/admin/crm/vendor-demand-items/{demandItemId:guid}/match-candidates · 查询详情<br>POST /api/admin/crm/vendor-demand-items/{demandItemId:guid}/match-opportunities · 新增<br>POST /api/admin/crm/match-opportunities/{id:guid}/candidates · 新增<br>PATCH /api/admin/crm/match-opportunities/{id:guid}/candidates/{candidateId:guid}/status · 修改<br>PATCH /api/admin/crm/match-opportunities/{id:guid}/progress · Advance<br>PATCH /api/admin/crm/match-opportunities/{id:guid}/status · 修改<br>GET /api/admin/crm/match-opportunities/{id:guid}/activities · 查询详情 |
| CrmVendorController<br>`CrmVendorController.cs` | /api/admin/crm/vendors | 仅登录（FallbackPolicy） | GET /api/admin/crm/vendors · 查询列表<br>GET /api/admin/crm/vendors/{id} · 查询详情<br>POST /api/admin/crm/vendors · 新增<br>PUT /api/admin/crm/vendors/{id} · 修改<br>PATCH /api/admin/crm/vendors/{id:guid}/owner · 改负责人<br>GET /api/admin/crm/vendors/herb-product-options · 查询列表<br>GET /api/admin/crm/vendors/{id:guid}/contacts · 查询详情<br>POST /api/admin/crm/vendors/{id:guid}/contacts · 新增<br>PUT /api/admin/crm/vendors/{id:guid}/contacts/{contactId:guid} · 修改<br>PATCH /api/admin/crm/vendors/{id:guid}/contacts/{contactId:guid}/primary · 设置默认/置顶/主联系人<br>PATCH /api/admin/crm/vendors/{id:guid}/contacts/{contactId:guid}/status · 修改<br>GET /api/admin/crm/vendors/{id:guid}/follow-records · 查询详情<br>POST /api/admin/crm/vendors/{id:guid}/follow-records · 新增<br>GET /api/admin/crm/vendors/{id:guid}/transfer-records · 查询详情<br>GET /api/admin/crm/vendors/{id}/purchase-demands · 查询详情 |
| CrmVendorDemandController<br>`CrmVendorDemandController.cs` | /api/admin/crm/purchase-demands | 仅登录（FallbackPolicy） | GET /api/admin/crm/purchase-demands · ?<br>GET /api/admin/crm/purchase-demands/{id:guid} · ?<br>POST /api/admin/crm/purchase-demands · ?<br>PUT /api/admin/crm/purchase-demands/{id:guid} · ?<br>DELETE /api/admin/crm/purchase-demands/{id:guid} · ?<br>PATCH /api/admin/crm/purchase-demands/{id:guid}/status · ? |
| DashboardController<br>`DashboardController.cs` | /api/admin/dashboard/crm | 仅登录（FallbackPolicy） | GET /api/admin/dashboard/crm/follow-funnel · 查询列表<br>GET /api/admin/dashboard/crm/supply-product-distribution · 查询列表<br>GET /api/admin/dashboard/crm/follow-trend · 查询列表<br>GET /api/admin/dashboard/crm/new-base-trend · 查询列表<br>GET /api/admin/dashboard/crm/vendor-priority-distribution · 查询列表<br>GET /api/admin/dashboard/crm/vendor-follow-trend · 查询列表<br>GET /api/admin/dashboard/crm/new-purchase-demand-trend · 查询列表<br>GET /api/admin/dashboard/crm/vendor-purchase-product-distribution · 查询列表 |


## 9. 附：QPS.System 控制器清单

| 控制器（文件） | 路由前缀 | 鉴权 | 端点（方法 · 路径 · 作用） |
|---|---|---|---|
| AuthController<br>`AuthController.cs` | /api/admin/auth | 匿名 / 仅登录（[Authorize]） | POST /api/admin/auth/login · 登录<br>POST /api/admin/auth/logout · 登出<br>GET /api/admin/auth/user-permissions · 查询列表<br>POST /api/admin/auth/change-password · 修改密码 |
| ChinaRegionController<br>`ChinaRegionController.cs` | /api/admin/china-regions | 匿名 | GET /api/admin/china-regions · 查询列表 |
| DataDictionaryController<br>`DataDictionaryController.cs` | /api/admin/data-dictionaries | 仅登录（[Authorize]） | GET /api/admin/data-dictionaries · 查询列表<br>GET /api/admin/data-dictionaries/tree · 查询列表<br>GET /api/admin/data-dictionaries/{id} · 查询详情<br>POST /api/admin/data-dictionaries · 新增<br>PUT /api/admin/data-dictionaries/{id} · 修改<br>DELETE /api/admin/data-dictionaries/{id} · 删除 |
| ErrorLogController<br>`ErrorLogController.cs` | /api/admin/error-logs | 仅登录（[Authorize]） | GET /api/admin/error-logs · 查询列表 |
| OperationLogController<br>`OperationLogController.cs` | /api/admin/operation-logs | 仅登录（[Authorize]） | GET /api/admin/operation-logs · 查询列表 |
| PermissionController<br>`PermissionController.cs` | /api/admin/permissions | 仅登录（FallbackPolicy） | GET /api/admin/permissions/tree · 查询列表<br>GET /api/admin/permissions · 查询列表<br>GET /api/admin/permissions/{roleCode} · 查询详情<br>PUT /api/admin/permissions · 修改 |
| RoleController<br>`RoleController.cs` | /api/admin/roles | 仅登录（FallbackPolicy） | GET /api/admin/roles · 查询列表<br>GET /api/admin/roles/{id} · 查询详情<br>POST /api/admin/roles · 新增<br>PUT /api/admin/roles/{id} · 修改 |
| UserController<br>`UserController.cs` | /api/admin/users | 仅登录（[Authorize]） | GET /api/admin/users · 查询列表<br>GET /api/admin/users/{id} · 查询详情<br>POST /api/admin/users · 新增<br>PUT /api/admin/users/{id} · 修改 |


## 10. 附：QPS.Host 控制器清单

| 控制器（文件） | 路由前缀 | 鉴权 | 端点（方法 · 路径 · 作用） |
|---|---|---|---|
| FileUploadController<br>`FileUploadController.cs` | /api/admin/file | 仅登录（FallbackPolicy） | POST /api/admin/file/upload/img · 上传图片 |
| MemberFileUploadController<br>`MemberFileUploadController.cs` | /api/mall/file | MallMember | POST /api/mall/file/upload/img · 上传图片 |


## 11. 附：全部匿名端点（20）

| 方法 | 路径 | 控制器 |
|---|---|---|
| GET | `/api/mall/articles` | MallArticleController |
| GET | `/api/mall/articles/{id:guid}` | MallArticleController |
| POST | `/api/mall/auth/register` | MallAuthController |
| POST | `/api/mall/auth/login` | MallAuthController |
| GET | `/api/mall/banners` | MallBannerController |
| GET | `/api/mall/campaigns` | MallCampaignPublicController |
| GET | `/api/mall/campaigns/{id:guid}` | MallCampaignPublicController |
| GET | `/api/mall/campaigns/{id:guid}/products` | MallCampaignPublicController |
| GET | `/api/mall/categories/tree` | MallCategoryController |
| GET | `/api/mall/categories` | MallCategoryController |
| POST | `/api/mall/freight-preview` | MallFreightPreviewController |
| GET | `/api/mall/notices` | MallNoticeController |
| GET | `/api/mall/pages/{pageCode}` | MallPageController |
| GET | `/api/mall/products` | MallProductController |
| GET | `/api/mall/products/{id:guid}` | MallProductController |
| GET | `/api/mall/products/{id:guid}/reviews` | MallProductController |
| GET | `/api/mall/store/default` | MallStoreController |
| GET | `/api/mall/tags` | MallTagController |
| POST | `/api/admin/auth/login` | AuthController |
| GET | `/api/admin/china-regions` | ChinaRegionController |

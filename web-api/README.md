# QPS Backend

## 项目定位

QPS Backend 是通用框架的后端基座：模块化单体 + MediatR + EF Core，提供鉴权、权限、数据字典、操作日志以及统一的响应与分页契约。`QPS.Crm`、`QPS.Mall` 是示例业务模块，按需注册或整体替换。

框架整体定位见 [根目录 README](../README.md)，示例业务背景见 [项目背景与业务链路](../docs/product/business-context.md)。

QPS Backend is an ASP.NET Core 8 service that provides CRM and system
administration capabilities, including authentication, users, roles,
permissions, CRM business data, data dictionaries, and error logging.

## Tech Stack

| Category | Technology |
| --- | --- |
| Framework | ASP.NET Core 8 |
| Language | C# |
| ORM | Entity Framework Core 8 |
| Database | SQL Server |
| CQRS | MediatR |
| Auth | JWT |
| Validation | FluentValidation |
| API Docs | Swagger |

## Project Layout

```text
src/
  QPS.Shared/         公共契约、异常、分页、Pipeline 行为与领域事件
  QPS.Api/            对外 API 契约与通用约定
  QPS.System/         系统模块：认证、用户、角色、权限、字典与日志
  QPS.Crm/            示例模块：CRM 基地、厂商、采购需求、撮合机会、跟进任务与首页图表
  QPS.Mall/           示例模块：商城商品、分类、订单、支付、退款与优惠券
  QPS.Persistence/    单一 AppDbContext、实体配置、迁移、当前用户与 JWT
  QPS.Host/           组合根：启动、鉴权、Swagger、过滤器与中间件
tests/
  QPS.UnitTests/          单元测试
  QPS.ArchitectureTests/  架构约束测试
```

## 架构

后端采用模块化单体：按业务模块划分项目，由 `QPS.Host` 组合根决定启用哪些模块。

- 模块注册即启用：调用 `AddQpsCrm()` / `AddQpsMall()` 才会加载对应模块的 Handler 与 Controller，不调用则完全不参与运行；没有 appsettings 开关，也没有动态 DLL 加载。
- 依赖方向为 `Shared ← System ← Crm ← Persistence ← Host`，`QPS.Mall` 仅依赖 `Shared`；模块之间禁止反向依赖和循环引用。
- 校验、权限、事务三个公共 Pipeline 行为只在 `Shared` 中实现；业务模块不得重复定义。
- 全系统只保留一个 `AppDbContext`，同时满足 `ISystemDbContext`、`ICrmDbContext`、`IMallDbContext` 与 `IUnitOfWork`。

完整说明见 [模块化单体架构](../docs/architecture/modular-monolith.md)。

## API Areas

| Controller | Route | Purpose |
| --- | --- | --- |
| AuthController | /api/auth | Login/logout |
| RoleController | /api/admin/roles | Role management |
| UserController | /api/admin/users | User management |
| PermissionController | /api/admin/permissions | Permission tree and role permissions |
| DataDictionaryController | /api/admin/data-dictionaries | Data dictionary management |
| CrmHerbBaseController | /api/admin/crm/herb-base-subjects, /api/admin/crm/herb-bases | 基地主体、基地及供应信息 |
| CrmVendorController | /api/admin/crm/vendors | 厂商、联系人、跟进和厂商采购需求 |
| CrmVendorDemandController | /api/admin/crm/purchase-demands | 采购需求维护与状态流转 |
| CrmMatchOpportunityController | /api/admin/crm/match-opportunities | 候选供应筛选、撮合机会、候选处置与阶段推进 |
| CrmFollowTaskController | /api/admin/crm/follow-tasks | 当前负责人跟进任务工作台 |
| CrmBusinessEntityAttributeController | /api/admin/crm/business-entity-attributes | CRM 业务实体扩展属性维护 |
| DashboardController | /api/admin/dashboard/crm/* | CRM 首页图表 |
| Mall（示例模块） | /api/admin/mall/*、/api/mall/* | 商品、分类、订单、支付、退款、优惠券、会员、Banner、活动、库存、运费模板等 |
| MallFileController | /api/admin/file | 管理端图片上传（`QPS.Mall/Controllers/Admin`） |
| MallMemberFileController | /api/mall/file | 会员端评价/售后图片上传（`QPS.Mall/Controllers/Mobile`） |

## 文件存储

上传通过 `IFileStorage` 抽象落地：契约与校验在 `QPS.Shared/Storage`，实现（`LocalFileStorage` / `OssFileStorage`）在 `QPS.Host/Storage`，由 `Storage:Provider` 决定。两种模式返回同一套 `/uploads/...` 相对地址，前端无需区分。

| Provider | 落点 | 访问方式 |
| --- | --- | --- |
| `Local`（默认） | `ContentRoot/wwwroot/uploads/{scene}/{yyyyMM}/`，容器内挂 `qps-uploads` 数据卷 | api 静态文件直出，网关 `/uploads/` 或其自身对外 |
| `Oss` | 阿里云 OSS 对象 `uploads/{scene}/{yyyyMM}/` | 前端拼 `VITE_FILE_BASE_URL` 访问 Bucket 域名 |

- 场景白名单：管理端 `product/category/banner/activity/avatar/common`，会员端 `review/aftersale`
- 限制：单图 ≤5MB，仅 jpg/jpeg/png/gif/webp，文件名统一为 GUID
- 默认走 `Local`；切 OSS 时把凭据写进 `appsettings.Local.json`（与 `appsettings.json` 同目录，**不入库**，加载优先级最高），本地 `dotnet run` 立即生效
- 容器部署：根目录 `.env` 只放占位符，真实值写服务器本地 `.env.local`（不入库），启动命令
  `docker compose --env-file .env --env-file .env.local up -d`，compose 以 `Storage__*` 注入 api
- 图片可访问性：上传后按 `Storage:Oss:ObjectAcl`（默认 `public-read`）给**单个对象**授权，Bucket 保持私有也能读；若账号开了「阻止公共访问」，该授权会被 OSS 拒绝（`Put public object acl is not allowed`），此时需关闭该开关或改走 CDN/签名 URL
- 前端 `VITE_FILE_BASE_URL` 必须与 Bucket 访问域名一致（当前 `https://qps-bucket.oss-cn-hangzhou.aliyuncs.com`）
- `Provider=Oss` 但关键参数缺失时启动即抛错（fail fast），不会等到上传才报 500

## 运行

本地启动、构建验证和 IIS 发布统一见 [本地启动与发布](../docs/operations/getting-started.md)。

Swagger is available at `/swagger` when the API is running.
# QPS Backend

## 项目定位

本项目服务中药材基地卖方与药厂买方，通过供给信息、采购需求和业务跟进管理，支持中药材供需撮合与交易推进。

完整的项目背景和业务链路见 [项目背景与业务链路](../docs/product/项目背景与业务链路.md)。

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
  1.QPS.Domain/          Domain entities and shared primitives
  2.QPS.Application/     Application commands, queries, DTOs, and interfaces
  3.QPS.Infrastructure/  EF Core, identity, and service implementations
  4.QPS.WebAPI/          API controllers, filters, and startup
tests/
  QPS.UnitTests/
  QPS.IntegrationTests/
```

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
| CrmFollowTaskController | /api/admin/crm/follow-tasks | 当前负责人跟进任务工作台 |
| DashboardController | /api/admin/dashboard/crm/* | CRM 首页图表 |

## 运行

本地启动、构建验证和 IIS 发布统一见 [本地启动与发布](../docs/operations/本地启动与发布.md)。

Swagger is available at `/swagger` when the API is running.

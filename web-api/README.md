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
  QPS.Shared/         公共契约、异常、分页、Pipeline 行为与领域事件
  QPS.System/         系统模块：认证、用户、角色、权限、字典与日志
  QPS.Crm/            CRM 模块：基地、厂商、采购需求、撮合机会、跟进任务与首页图表
  QPS.Mall/           商场模块：商场门店
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

完整说明见 [模块化单体架构](../docs/architecture/模块化单体架构.md)。

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
| MallController | /api/admin/mall/* | 商场门店分页查询、详情、新增、编辑、启用/停用与删除 |

## 运行

本地启动、构建验证和 IIS 发布统一见 [本地启动与发布](../docs/operations/本地启动与发布.md)。

Swagger is available at `/swagger` when the API is running.

# QPS 通用项目框架

QPS 是一套**模块化单体后端 + 管理后台 + 多端移动应用**的通用工程框架，用于快速搭建同时需要后台管理与移动端的业务系统（如商城、CRM、运营中台等）。

仓库当前内置 `CRM` 与 `Mall` 两个示例业务模块，用来演示框架的分层方式、鉴权流程、多端复用和工程规范。新项目使用时**保留框架层，替换示例模块**即可：后端按模块增删项目，管理端在 `views/` 下新增目录，移动端在 `pages/` 下新增页面。

## 技术栈

| 层 | 技术 | 职责 |
| --- | --- | --- |
| 后端宿主 | ASP.NET Core 8 | 启动、全局过滤器、依赖注册、权限管道 |
| 后端模块 | MediatR + EF Core 8 + SQL Server | 按业务模块拆分，各自持有 Controllers / Application / Domain / Contracts |
| 管理端 | Vue 3 + Element Plus + Pinia + Vue Router + ECharts | 后台管理、权限控制、数据可视化 |
| 移动端 | uni-app（Vue 3 + TypeScript + Vite） | 一套代码输出 H5 / 微信小程序 / App |

## 框架已内置的能力

- **全局鉴权**：`FallbackPolicy` 默认要求登录，公开接口用 `[AllowAnonymous]` 显式放行；业务模块可挂权限校验管道。
- **统一契约**：全局响应包装、统一异常处理、统一分页返回（`items / totalCount / hasMore`）。
- **管理端基座**：axios 拦截器、路由守卫、动态权限树、按模块拆分的 `views/<module>`。
- **移动端基座**：`request()` 自动携带 token、401 统一跳登录、easycom 组件（`q-icon`、`q-page-header-back`、`q-scroll-list` 等）。
- **跨端样式**：CSS 变量统一主题、`rpx` 响应式、内置 tabBar，`calc(100vh - var(--window-bottom))` 适配多端安全区。
- **工程规范**：根 `AGENTS.md` 约束、`agent-temp` 临时目录、`ui-tests` Playwright 隔离端口。

## 目录结构

```text
web-api/                    ASP.NET Core 后端（模块化单体）
  src/QPS.Host/             宿主：启动、全局过滤器、注册
  src/QPS.Shared/           跨模块共享能力
  src/QPS.System/           基础模块（菜单、权限、区域、日志）
  src/QPS.Crm/              CRM 示例模块
  src/QPS.Mall/             商城示例模块
  src/QPS.Persistence/      EF Core 上下文、迁移、种子数据
admin/                      Vue 3 管理端（views 按模块划分）
mobile/                    uni-app 移动端（pages 按业务划分）
docs/                      架构、领域、运维与需求文档
agent-temp/                普通临时产物（已忽略）
ui-tests/                  Playwright UI 测试、截图和报告（已忽略）
```

## 示例业务模块

- `QPS.System`：菜单、权限、行政区域、操作日志等基础能力，通常直接复用。
- `QPS.Crm`：中药材供需撮合（厂商、基地、采购需求、跟进、撮合机会），可整体替换。
- `QPS.Mall`：商城（商品、购物车、订单、支付、优惠券），含管理端页面与移动端 H5。

新增业务模块的典型动作：

1. 后端新建 `web-api/src/QPS.<Module>`，复用 `QPS.Shared` 与 `QPS.Persistence`；
2. 管理端新增 `admin/src/views/<module>`、`admin/src/api/modules/<module>.ts`；
3. 移动端新增 `mobile/src/pages/<module>` 并在 `pages.json` 注册。

## 文档

- 架构：[模块化单体架构](docs/architecture/模块化单体架构.md)、[权限设计](docs/architecture/权限设计.md)
- 领域：[实体说明](docs/domain/实体说明.md)
- 运维：[本地启动与发布](docs/operations/本地启动与发布.md)
- 示例业务背景：[项目背景与业务链路](docs/product/项目背景与业务链路.md)

# QPS 文档索引

本文件是 `E:\Code\QPS` 唯一的文档索引入口，只做导航，不写正文。规则、约束、命令、表结构一律落在被索引的文件里。

## 索引

| 文档 | 内容 | 标签 |
|---|---|---|
| [docs/operations/getting-started.md](docs/operations/getting-started.md) | 端口与启动、`dotnet build -m:1`、数据库迁移、IIS 发布、产物目录（`agent-temp` / `ui-tests`）、UTF-8 与 PowerShell 编码、常见故障排查 | **必读** |
| [docs/architecture/modular-monolith.md](docs/architecture/modular-monolith.md) | 六项目分层与依赖方向、模块启用方式、Pipeline 顺序、Handler 事务边界、共享层边界、协作约定（最小改动 / 不回退他人改动） | **必读** |
| [docs/architecture/permissions.md](docs/architecture/permissions.md) | 权限码唯一判断依据、Menu/Button 权限清单、`[Permission]` 校验链路、前端 `Permission` 用法、`AdminEndpointPermissionTests` 护栏 | **必读** |
| [docs/domain/entities.md](docs/domain/entities.md) | 实体归属、软删除、唯一索引需带 `IsDeleted`、订单读快照 | **必读** |
| [docs/domain/mall-rules.md](docs/domain/mall-rules.md) | 促销动作类型与应用顺序、门槛与时间窗、优惠互斥、金额口径；售后申请约束、退款/退货状态流转、库存回补与优惠券释放 | **必读** |
| [docs/architecture/sqlite-migration-analysis.md](docs/architecture/sqlite-migration-analysis.md) | SQL Server 与 SQLite 双 provider 可行性结论与风险 | 选读 |
| [docs/product/business-context.md](docs/product/business-context.md) | CRM 供需撮合业务背景、角色、业务链路 | 选读 |
| [docs/contracts/mall-campaign-api.md](docs/contracts/mall-campaign-api.md) | 活动专题接口契约：URL、字段、错误码、真实响应示例 | 选读 |
| [docs/design/admin-permissions-spec.md](docs/design/admin-permissions-spec.md) | 权限设置页两栏矩阵规格 | 选读 |
| [docs/design/member-health-spec.md](docs/design/member-health-spec.md) | 会员健康档案设计与建表方案（待实施） | 选读 |
| [docs/decisions/adr-0001-doc-structure.md](docs/decisions/adr-0001-doc-structure.md) | 文档结构取舍：为什么根目录只做索引 | 选读 |
| [docs/README.md](docs/README.md) | 各子目录边界、命名规则、写作约定、阅读器用法 | 选读 |
| [docs/archive/README.md](docs/archive/README.md) | 归档规则与历史产物存放约定 | 选读 |
| [admin/AGENTS.md](admin/AGENTS.md) | 管理端封装组件优先、共享层禁改、列表页模式、语义重命名、UI 测试端口隔离 | **必读** |
| [web-api/AGENTS.md](web-api/AGENTS.md) | 后端业务约束、权限码、校验分工、代码风格、构建与端口 | **必读** |
| [mobile/AGENTS.md](mobile/AGENTS.md) | 移动端技术栈、布局与图标规范、图片组件、验收脚本 | **必读** |

## 维护规则

- 只索引已被 git 跟踪的文件；不得出现 `agent-temp/`、`ui-tests/`、`.trae/` 或任何未跟踪路径。
- 新增文档必须与索引行在同一个提交里出现；删除文档时同提交删掉对应索引行。
- 新行按「必读在前、选读在后」插入，说明列写关键词，便于不打开文件也能判断相关性。
- 历史与已失效内容一律放 `docs/archive/`，不进入本表逐条列举。
# QPS 后端开发指南

本文件只约束 `qps-ht/` 内的后端开发工作；本地启动与 IIS 发布流程见 [`../docs/operations/本地启动与发布.md`](../docs/operations/本地启动与发布.md)。

## 运行与验证

- 后端调试端口为 `5000`，本地 IIS 验证端口为 `20004`；启动调试服务前先确认没有遗留的后端进程占用 `5000`。
- 后端改动至少执行：

```powershell
dotnet build "E:\Code\QPS\qps-ht\QPS.sln" --no-restore
dotnet test "E:\Code\QPS\qps-ht\QPS.sln" --no-build --logger "console;verbosity=minimal"
```

## 业务与数据约定

- 继承 `BaseEntity` 的实体默认走 EF 全局软删除过滤；常规查询不要重复写 `!IsDeleted`，只有必须查全量时才使用 `IgnoreQueryFilters()`。
- 不要为了调试清空数据库；优先增量补齐或使用初始化逻辑。
- CRM 客户按药材基地模型处理，实体类型统一使用 `CRM_HERB_BASE`，不要新增 `CRM_CUSTOMER` 业务编码。
- CRM 相关硬编码收敛到 `QPS.Application.Features.Crm.CrmCodes`，避免在 Handler 中散落业务编码和状态值。
- 客户来源字段使用 `SourceId`，不要使用 `SourceLeadId`。
- 主营品类、采购品类等扩展属性统一使用 `CrmBusinessEntityAttributes`，不要为单项属性新增独立表。
- `CrmBusinessEntityAttributes.EntityType` 使用实体编码，`AttributeCode` 使用属性编码。
- 字典和值、权限编码统一使用全大写加下划线；前端展示名称，接口和入库使用编码值。
- 客户负责人使用 `OwnerUserId` 关联 `SystemUsers`；查询层负责补齐负责人名称。

## 语义重命名

- 重命名实体、数据库表/列、实体编码、属性编码、权限码或接口路径前，必须先在实施计划或变更说明中列出“语义重命名影响清单”，不能只依赖源码全局替换。
- 清单至少逐项检查并决定“迁移、保留历史、删除”策略：数据库表和列、索引/筛选条件/约束、EF 模型快照、迁移 `Up/Down` SQL、`CrmBusinessEntityAttributes.EntityType/AttributeCode`、`CrmFollowRecord.EntityType`、`CrmTransferRecord.EntityType`、`SystemOperationLog.EntityType`、`SystemPermissions.Code`、`SystemDataDictionaries.Code/Value`。
- 迁移前后必须对受影响数据执行数量和唯一性对账；有属性表关联时，还必须检查 `EntityId` 指向的新实体记录是否存在。任何旧编码残留都必须明确标注为 EF 历史迁移或历史数据，不能默认为可忽略。
- 新增或调整属性表筛选索引时，索引名和 `HasFilter` 中的 `EntityType/AttributeCode` 必须与迁移的数据更新同步修改；更新后的运行时代码只允许写入新编码。

## 代码风格

- Application 层按业务功能分目录：`Features/{领域}/{功能}`；文件命名空间必须与其目录一致，例如 `Features.Crm.CrmTransfers`。
- Contracts 与功能目录平行组织：仅被某个功能使用的 DTO 放入 `Contracts/{领域}/{功能}`；跨 CRM 功能共用的 DTO 才保留在 `Contracts/Crm` 根目录。
- 一个文件的主类型名必须与文件名一致；包含同一功能的一组 DTO 时使用复数文件名（如 `CrmDashboardChartDtos.cs`）。
- Command、Query 使用 `动词 + Crm + 业务对象 + Command/Query` 命名；静态查询辅助类以单数业务对象加 `Query` 结尾（如 `CrmTransferRecordQuery`），规则类用 `Rules`，映射类用 `Mapper`。不要使用复数集合名作为辅助类名。
- 仅被多个 CRM 功能共同使用的常量、字典校验等组件可以放在 `Features/Crm` 根目录；功能专属组件必须归入所属功能目录。
- `Handle` 只负责用例编排；复杂校验、查询、映射和状态同步下沉到私有方法或领域方法。
- 构造函数、`Handle` 和复杂私有方法添加简短 XML 注释，说明业务意图。
- 长条件和长参数列表换行；方法之间保留一个空行。
- 领域不变量放在 Domain 实体或领域方法，Application 层负责跨仓储、跨服务的协作。
- 不为统一风格批量修改无关 System 文件；改动应直接降低重复或风险。

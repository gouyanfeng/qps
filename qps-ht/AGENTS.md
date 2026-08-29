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

## 代码风格

- `Handle` 只负责用例编排；复杂校验、查询、映射和状态同步下沉到私有方法或领域方法。
- 构造函数、`Handle` 和复杂私有方法添加简短 XML 注释，说明业务意图。
- 长条件和长参数列表换行；方法之间保留一个空行。
- 领域不变量放在 Domain 实体或领域方法，Application 层负责跨仓储、跨服务的协作。
- 不为统一风格批量修改无关 System 文件；改动应直接降低重复或风险。

## 临时文件

- 临时测试、Playwright 临时 spec、调试脚本和分析产物放在 `E:\Code\QPS\qps-ui-tests`，不要放入源码目录或提交仓库。

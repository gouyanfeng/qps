# 模块化单体架构

> 基于 2026-11-07 的 `web-api` 代码整理。后端为 .NET 8 模块化单体：一个进程、一个数据库，按业务模块拆分项目，模块通过 `AddQpsXxx()` 显式启用。

## 1. 项目与职责

| 项目 | 职责 | 关键内容 |
|---|---|---|
| `QPS.Shared` | 与业务无关的公共契约和横切能力 | `BaseEntity`、`ApiResponse`、分页、JWT 配置、`IUnitOfWork`、`IPermissionChecker`、`ICurrentUserService`、`PermissionAttribute`、领域事件契约、三个 Pipeline Behaviour |
| `QPS.System` | 系统能力 | 用户、角色、权限、数据字典、行政区、操作日志、错误日志；登录、权限检查器 |
| `QPS.Crm` | CRM 业务 | 药材基地主体/基地/供应、联系人、跟进、流转、厂商、采购需求、撮合机会、业务活动 |
| `QPS.Mall` | 商城业务 | 商品/SKU/分类/标签/评价、购物车、订单/支付/退款/退货/发货、促销/优惠券/活动、会员/地址/积分、内容页 |
| `QPS.Persistence` | 持久化基础设施 | 单一 `AppDbContext`、三套模型配置、迁移、种子数据、`CurrentUserService`、`JwtGenerator` |
| `QPS.Host` | 组合根与 HTTP 基础设施 | `Program.cs`、上传控制器、响应包装过滤器、异常中间件、MVC/JWT/CORS/Swagger 注册 |

## 2. 依赖方向

```
QPS.Host ──> Shared, System, Crm, Mall, Persistence
QPS.Persistence ──> Shared, System, Crm, Mall
QPS.Crm ──> Shared, System
QPS.Mall ──> Shared
QPS.System ──> Shared
QPS.Shared ──> 无业务模块依赖
```

约束由 `web-api/tests/QPS.ArchitectureTests/ProjectReferenceTests.cs` 和 `ModuleBoundaryTests.cs` 固化：

- `QPS.Shared` 不引用任何业务模块或 Host。
- `QPS.System` 不引用 Crm、Mall、Persistence、Host。
- `QPS.Crm` 不引用 Mall、Persistence、Host。
- `QPS.Mall` 不引用其他业务模块、Persistence、Host。
- `QPS.Persistence` 不引用 Host。
- Mall 的 Feature 不得直接引用其他 Feature 的 Handler。

## 3. 模块内分层

业务模块（System / Crm / Mall）保持四层目录：

```
QPS.Xxx/
  Domain/          实体、值对象、领域服务、领域事件
  Application/     Features/<业务>/*Command|*Query|*Handler|*Validator
  Contracts/       模块对外的 DbContext 接口与 DTO
  Controllers/     HTTP 入口，只调用 IMediator
```

- **Domain**：只依赖 `QPS.Shared`，不依赖 MediatR、EF Core 或 ASP.NET Core。
- **Application**：按 Feature 垂直切分，一个用例一个 Handler；查询命名 `XxxQuery`，命令命名 `XxxCommand`。
- **Contracts**：暴露 `ISystemDbContext` / `ICrmDbContext` / `IMallDbContext` 及 DTO，供 Persistence 实现、供 Controller 使用。
- **Controllers**：不写业务逻辑，只把请求转成 Command/Query 并 `Send`。

`QPS.Shared` 分层：

```
Common/      BaseEntity、ApiResponse、PaginationRequest/Response、JwtOptions
Contracts/   IUnitOfWork、ICurrentUserService、IPermissionChecker、PermissionAttribute、
             IAllowAnonymousRequest、IDomainEvent、IDomainEventDispatcher、IJwtGenerator
Pipeline/    PermissionBehaviour、ValidationBehaviour、TransactionBehaviour、领域事件派发
Exceptions/  BusinessException、ValidationException 处理约定
Extensions/  通用扩展
```

`QPS.Persistence` 分层：

```
Database/        AppDbContext（同时实现 ISystemDbContext、ICrmDbContext、IMallDbContext、IUnitOfWork）
Configurations/  SystemModelConfiguration、CrmModelConfiguration、MallModelConfiguration
Identity/        CurrentUserService、JwtGenerator
Migrations/      EF Core 迁移
Seeds/           MallMockDataSeeder（仅 Development）
```

`QPS.Host` 分层：

```
Controllers/   FileUploadController、MemberFileUploadController
Filters/       ResponseWrapperFilter
Middleware/    ExceptionHandlerMiddleware
Registration/  ServiceRegistrationExtensions（MVC、CORS、Swagger、JWT）
Properties/    launchSettings.json（5000/5001）
```

## 4. 模块启用机制

`Program.cs` 中的注册顺序：

1. `AddQpsMvc()` + `ConfigureQpsApplicationParts(typeof(Program).Assembly)`
2. `AddQpsCors()`、`AddQpsSwagger()`
3. 读取并绑定 `JwtOptions`，`AddQpsJwtAuthentication()`
4. `AddQpsShared()`
5. `AddQpsPersistence(configuration)`
6. `AddQpsSystem(mvcBuilder)`、`AddQpsCrm(mvcBuilder)`、`AddQpsMall(mvcBuilder)`

每个 `AddQpsXxx()` 做两件事：

- 用 `AddMediatR(cfg => cfg.RegisterServicesFromAssembly(...))` 注册本模块 Handler。
- 把本模块程序集加入 MVC 的 `ApplicationParts`，暴露本模块 Controller。

`ConfigureQpsApplicationParts` 会先清空默认发现范围，再加入 Host 自身。因此**未调用 `AddQpsXxx()` 的模块既不注册 Handler，也不暴露 Controller**，`ModuleRegistrationTests` 对此有覆盖。

Mall 额外注册了促销策略与购物车处理器：

- `DiscountAmountAction`、`DiscountRateAction`、`GiftAction`、`FreightFreeAction`
- `IActionStrategyRegistry`、`MallPromotionEngine`、`MallPromotionContextBuilder`、`MallPromotionPreviewService`
- `MallCouponService`、`MallProductPromotionEnricher`、`GetMallCartHandler`

## 5. Pipeline Behaviour 的真实约束

三个 Behaviour 在 `QPS.Shared/DependencyInjection.cs` 中的注册顺序就是执行顺序，**Permission 在最外层**：

```
PermissionBehaviour  →  ValidationBehaviour  →  TransactionBehaviour  →  Handler
```

### 5.1 PermissionBehaviour

- 请求实现 `IAllowAnonymousRequest`，或 Endpoint 标记 `[AllowAnonymous]`：直接放行。
- 已登录 `member` 角色且 Endpoint 使用 `MallMember` 策略：放行（移动端会员接口）。
- 否则要求 `ICurrentUserService.UserId` 是有效 Guid，且用户 `IsActive`，否则抛出 401 `BusinessException`。
- 从请求类型的 `[Permission("...")]` 读取权限码，缓存于 `PermissionMetadataCache`；无权限抛 403。
- 权限判定链路：`SystemUser.RoleId → SystemRolePermissions → SystemPermissions.Code`。

**约束**：新增需要权限的接口时，把 `[Permission]` 标在 Command/Query 上，不要写在 Controller；Controller 只负责路由与 `[Authorize]` 策略。

### 5.2 ValidationBehaviour

- 收集当前请求类型的所有 `IValidator<TRequest>`，执行 FluentValidation。
- 有错误时抛出 `ValidationException`，由 Host 的异常中间件统一转为业务错误响应。

**约束**：输入校验放 Validator，不在 Handler 里堆手写 if 判断；跨实体业务规则放 Domain。

### 5.3 TransactionBehaviour

- 请求类型名以 `Query` 结尾：直接 `next()`，**查询不开启写事务**。
- 其余请求（Command）：`BeginTransactionAsync` → Handler → `CommitAsync`；异常时 `RollbackAsync` 后重抛。
- `AppDbContext.BeginTransactionAsync` 内部有 `Database.CurrentTransaction == null` 判断，允许安全嵌套。

**约束**：

- Handler **不得**手写 `BeginTransactionAsync` / `CommitAsync` / `RollbackAsync`；需要事务的写操作必须命名为 `XxxCommand`。
- Handler 可以按需调用 `SaveChangesAsync`；多步写入依赖外层事务保证原子性。
- 只读逻辑命名为 `XxxQuery`，避免无意义的事务开销。

`PipelineTests` 强制约束：业务模块不得自己实现 `IPipelineBehavior`，三个通用 Behaviour 只能放在 `QPS.Shared`。

## 6. 持久化模型

- 全库只有 `AppDbContext`，同时实现三套模块契约与 `IUnitOfWork`。
- `OnModelCreating` 依次应用 `SystemModelConfiguration`、`CrmModelConfiguration`、`MallModelConfiguration`，再挂软删除查询过滤器。
- `SaveChanges` / `SaveChangesAsync` 统一处理：软删除、审计字段（`CreatedAt/By`、`UpdatedAt/By`）、操作日志。
- `BaseEntity` 提供 `Id`、审计字段与 `IsDeleted`；业务代码不做物理删除，统一置 `IsDeleted`。
- 除 `SystemOperationLog`、`SystemErrorLog` 外，删除均转为软删除。

## 7. HTTP 基础设施

`Program.cs` 请求管道：

```
Swagger → CORS(AllowAll) → StaticFiles(wwwroot, /uploads 长缓存)
        → ExceptionHandlerMiddleware → Authentication → Authorization → MapControllers
```

- `ResponseWrapperFilter` 把所有 Controller 返回值包装为 `ApiResponse`。
- DataAnnotations 校验失败返回 HTTP 200 + `{ code: 400, msg }`，与异常中间件格式一致。
- JSON 使用 camelCase，中文不转义。
- JWT 默认 `FallbackPolicy` 要求已认证；预置策略 `AdminOnly`（admin）、`MallMember`（member）、`AnyUser`。
- 上传目录 `wwwroot/uploads` 启动时自动创建；`/uploads` 响应带一年 `immutable` 缓存头。

## 8. 新增模块/功能的检查清单

1. 新模块项目只引用 `QPS.Shared`（确需系统能力才引用 `QPS.System`），不得引用 Host/Persistence。
2. 在 `QPS.Host/Program.cs` 调用 `AddQpsXxx(mvcBuilder)`，否则模块不会生效。
3. 在 `QPS.Persistence` 增加 `IXxxDbContext` 实现与模型配置，并让 `AppDbContext` 实现该契约。
4. 写操作命名 `XxxCommand` 并依赖全局事务；读操作命名 `XxxQuery`。
5. 需要权限的请求加 `[Permission]`；公开接口实现 `IAllowAnonymousRequest`。
6. 新增跨模块能力时通过 Contracts 接口，不直接引用其他模块内部实现。

## 9. 协作与共享层约定（三端通用）
- 所有回答使用中文。
- 修改范围保持最小，只改完成当前任务所必需的源码或配置；不得回退其他未提交改动。
- **已有封装组件优先使用**：各端项目中已有的封装组件（业务组件、布局组件、图表组件等），开发新功能时必须优先使用，不得绕过已有封装自行编写功能重复的代码。
- **禁止直接修改基础组件与公共模块**：各端的 `components`、`layout`、`utils`、`api` 拦截器、路由守卫、权限模块等共享基础设施，**严禁未经确认直接修改**。如需扩展或修复，必须先向负责人说明原因、影响范围和拟议方案，**得到明确确认后才能执行**。未经确认的改动一律退回。
- 未获确认的改动仅限在业务页面/模块内部完成，不得扩散到共享层。
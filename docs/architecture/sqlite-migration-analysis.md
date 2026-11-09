# QPS 从 SQL Server 迁移到 SQLite 的可行性分析

> 基于 2026-11-07 的 `web-api` 实际代码与迁移文件整理。EF Core 8.0.8，`net8.0`，单一 `AppDbContext`，49 个迁移，54 张表。本文只做分析，未改任何代码、库或迁移。

## 0. 结论

**推荐只做双 provider 支持，不推荐把生产库整体迁到 SQLite。**

SQLite 适合开发、单元测试、单机演示和离线试用；生产环境继续使用 SQL Server。若强行整体替换，金额语义、大小写语义、迁移体系和并发写入这四块都需要实质性改造，且部分风险无法靠 SQLite 本身消除。

---

## 1. 现状实测数据

| 项目 | 实测值 | 出处 |
|---|---|---|
| Provider | `UseSqlServer` | `QPS.Persistence/DependencyInjection.cs:24` |
| 连接串 | SQL Server `DefaultConnection` | `QPS.Host/appsettings.json:3` |
| EF 包 | `Microsoft.EntityFrameworkCore.SqlServer 8.0.8` | `QPS.Persistence/QPS.Persistence.csproj` |
| 测试包 | `Microsoft.EntityFrameworkCore.Sqlite 8.0.8`、`InMemory 8.0.8` | `QPS.UnitTests.csproj` |
| DbSet / 表 | 54 | `QPS.Persistence/Database/AppDbContext.cs` |
| 迁移 | 49 个（不含 Designer / ModelSnapshot） | `QPS.Persistence/Migrations` |
| 迁移内 raw SQL | 52 处 `migrationBuilder.Sql` | 同上 |
| 迁移内 SQL Server 专有 token | `nvarchar` 279、`datetime2` 137、`decimal(18,2)` 48、`GETDATE()` 23、`NEWID()` 19、`TOP (` 13、`IF EXISTS` 1、`N'…'` 1 | 同上 |
| 未出现的专有项 | `GETUTCDATE`、`DATETIMEOFFSET`、`rowversion`、`HasDefaultValueSql`、`GO`、`sp_`、`MERGE` 均为 0 | 同上 |
| 过滤唯一索引 | 29 处 `HasFilter`，全部为 `IsUnique()` 索引 | `QPS.Persistence/Configurations/*` |
| decimal 字段 | 迁移中出现 48 处 `decimal(18,2)` 列定义；含 DTO 共 174 处 `decimal` 声明 | `src/**`、`Migrations/**` |
| `ExecuteUpdateAsync` | 5 处 | 订单创建 2、券领取 2、订单超时关闭 1 |
| `EF.Functions.Like` | 少量，均为关键词搜索 | `src/**` |
| `AsSplitQuery` / `FromSqlRaw` / `SqlBulkCopy` | 0 | — |
| 事务 | 所有 Command 由 `TransactionBehaviour` 包一层事务 | `QPS.Shared/Pipeline/TransactionBehaviour.cs:24-33` |
| Design-time factory | 无（0 处 `IDesignTimeDbContextFactory`） | — |
| SQLite 测试工厂 | 已有，见第 8 节 | `QPS.UnitTests/Common/TestSqliteDbContextFactory.cs` |

---

## 2. Provider 接入：要动哪些文件

### 2.1 必须改

| 文件 | 改动 | 风险 |
|---|---|---|
| `QPS.Persistence/QPS.Persistence.csproj` | 增加 `Microsoft.EntityFrameworkCore.Sqlite 8.0.8` | 无影响 |
| `QPS.Persistence/DependencyInjection.cs` | 第 21-24 行改为按配置选择 provider：SQL Server 走现有连接串，SQLite 走 `UseSqlite` + 文件路径 | 需改造 |
| `QPS.Host/appsettings.json` | 增加 `Database:Provider` 与 `ConnectionStrings:SqliteConnection`；`DefaultConnection` 保留 | 需改造 |
| `QPS.Host/Program.cs` | 第 64-93 行的启动逻辑：SQLite 下不能直接 `MigrateAsync()` 跑 SQL Server 迁移，需按 provider 分支 | 阻断（不改则启动失败） |
| `QPS.Persistence/Migrations/*` | SQLite 需要独立的一套迁移，或改用 `EnsureCreated`（见第 4 节） | 阻断 |
| 5 处 `HasColumnType("nvarchar(max)")` | `MallModelConfiguration.cs:73/74/182/334/351`；SQLite 不需要类型名，建议按 provider 条件设置或直接移除 | 需改造 |
| 29 处 `HasFilter` | SQLite 支持部分索引，但其中 `CrmModelConfiguration.cs:148` 的 `[Status] IN (N'推进中', N'暂停')` 需去掉 `N` 前缀；其余 `[IsDeleted] = 0` 的方括号 SQLite 可接受 | 需改造 |
| `Dockerfile` / 本地启动 | SQLite 文件与上传目录一样需要可写卷；连接串改为文件路径 | 需改造 |

### 2.2 双 provider 并行的可行性

可以并行，但**不能让两套迁移共存在同一个程序集**：EF 的 `ModelSnapshot` 与迁移历史只认一个 provider，混放会导致 snapshot 互相覆盖。两种可行结构：

1. **生产 SQL Server 保留迁移；SQLite 仅用 `EnsureCreated`**（推荐，成本最低）
   - SQL Server：`MigrateAsync()` + 现有 49 个迁移。
   - SQLite：`Database.EnsureCreatedAsync()`，不生成迁移；开发/测试库可从空库直接建表。
   - 代价：SQLite 无法享受增量迁移，每次模型变化要删库重建；仅适合开发/测试。
2. **按 provider 拆迁移程序集**
   - 新建 `QPS.Persistence.Sqlite`（或把迁移拆到 `Migrations/SqlServer` 与 `Migrations/Sqlite` 两个程序集），通过 `MigrationsAssembly` 指定。
   - 代价：项目结构变复杂；两套迁移要同步维护；EF CLI 需要额外参数。

当前没有 `IDesignTimeDbContextFactory`，`dotnet ef` 依赖 Host 启动来获得 DbContext。改成配置驱动后，CLI 也必须带上 provider 配置，否则生成迁移时会拿到错误的 provider。

---

## 3. 金额与 decimal（本次最关键的阻断点之一）

### 3.1 EF + SQLite 实际怎么存

SQLite 没有 `decimal` 类型。EF Core 的 SQLite provider 默认把 `decimal` 映射为 **TEXT**，写入时按 invariant 字符串保存（如 `"1234.56"`）。迁移里的 `decimal(18,2)` 声明不会被 SQLite 执行，重新生成迁移后也不会保留精度语义。

后果分两类：

- **C# 侧读写是精确的**：decimal → TEXT → decimal 往返不丢精度，领域层 `MallOrder.CalculatePayableAmount`、退款金额校验这些 C# 计算不受影响。
- **SQL 侧比较 / 排序 / 聚合是错的**：
  - `ORDER BY` 按文本字典序：`"9.9" > "10.0"`，金额排序会乱。
  - `WHERE amount <= x` 按文本比较，筛选结果错误。
  - `SUM()` 会把 TEXT 隐式转 REAL，产生浮点误差；`MAX()` 按文本比较。
  - TEXT 列上的索引对数值范围查询无效。

### 3.2 代码里已经存在的 SQL 侧 decimal 用法（实测）

| 位置 | 用法 | SQLite 后果 |
|---|---|---|
| `MallCouponMobileHandler.cs:42` | `.OrderByDescending(c => c.DiscountAmount)` | 券按面额排序错误（9.9 排在 10 前面） |
| `GetCrmMatchSupplyCandidatesQuery.cs:83` | `.Where(item => item.ExpectedPrice <= request.MaxExpectedPrice)` | 价格上限筛选错误 |
| `CreateCrmHerbBaseCommand.cs:154` | `.SumAsync(herbBase => herbBase.Scale ?? 0)` | 规模汇总走 REAL，出现小数漂移 |
| `DeleteCrmHerbBaseCommand.cs:74` | 同上 | 同上 |
| `UpdateCrmHerbBaseCommand.cs:131` | 同上 | 同上 |
| `CreateCrmMatchOpportunityCommand.cs:116` | `.MaxAsync(...)` | 文本 MAX，结果可能取到错误的最大值 |

`GetMallShipmentDetailQuery.cs:65` 的 `SumAsync(Quantity)` 是 int，不受影响。其余 `Sum` 基本都在内存中对已加载集合计算，不受 provider 影响。

### 3.3 可选方案与代价

| 方案 | 做法 | 优点 | 代价 | 适用 |
|---|---|---|---|---|
| A. 最小单位整数（long / scaled integer） | 全局 value converter，把 decimal 按业务精度转成 long（金额 2 位、规模 2 位、坐标 6 位等） | SQL 比较/排序/SUM 全部正确；索引有效 | 48 个列逐一确认精度；不同业务量纲不同，不能一刀切；SQL 里直接读会看到整数，报表/运维需换算；DTO 与前端仍是 decimal，需要转换层 | 生产 SQLite |
| B. TEXT + 自定义转换器 | 固定宽度、符号对齐的字符串 | 存储精确、比较可用 | 实现复杂（负数、不同小数位、补零规则），可读性差，SQL 聚合仍不可用 | 不推荐 |
| C. REAL / double | 直接映射 double | 改动最少 | 精度丢失，金额恒等式与对账会漂移，等于放弃 decimal 语义 | 禁止 |
| D. 保留 decimal，禁止 SQL 侧比较/排序/聚合 | 把第 3.2 节的 6 处改为内存计算或先取回再筛 | 改动最小，C# 语义不变 | 需要审计所有 IQueryable 上的 decimal 用法，并加护栏测试防止回退；数据量大时内存排序/筛选不划算 | 开发/测试 SQLite 可接受 |

**建议**：若只做开发/测试库，选 D，并把第 3.2 节 6 处收敛成“先取数再计算”或加架构测试禁止 SQL 侧 decimal 比较/排序；若真要把生产迁到 SQLite，必须选 A。

---

## 4. 迁移：改现有还是重新生成

### 4.1 现有迁移不能直接复用

49 个迁移全部由 SQL Server provider 生成，包含：

- 列类型 `nvarchar(...)` 279 处、`datetime2` 137 处、`decimal(18,2)` 48 处
- 裸 SQL 52 处，其中 `GETDATE()` 23、`NEWID()` 19、`TOP (` 13、`IF EXISTS` 1
- 索引过滤条件 `[IsDeleted] = 0`，以及 1 处 `N'推进中'`
- Designer 文件里的 SQL Server 模型注解

SQLite 没有 `GETDATE()` / `NEWID()` / `TOP`；`N'…'` 不是合法字符串前缀；`decimal(18,2)` 与 `datetime2` 没有对应语义。直接 `MigrateAsync()` 会在第一个迁移就失败。

### 4.2 两种做法

| 做法 | 说明 | 代价 | 结论 |
|---|---|---|---|
| 改现有 49 个迁移 | 逐条替换类型、raw SQL、过滤条件 | 极高：49 个迁移 × 手改，snapshot 与 Designer 也要改；改完后 SQL Server 侧不可用；历史迁移已执行过的库无法对齐 | 不推荐 |
| 重新生成 provider 专属迁移 | 用 SQLite provider 从当前模型生成一套新迁移（可压成 1 个 initial 或按模块拆分） | 中：需要把 52 处 raw SQL 按 provider 重写（`CURRENT_TIMESTAMP`、参数化 GUID、`LIMIT 1`、`INSERT ... WHERE NOT EXISTS`）；两套迁移要同步；迁移历史表分开 | 双 provider 时可选；只做开发/测试时可用 `EnsureCreated` 完全绕开 |

**建议**：只做开发/测试 → 不生成 SQLite 迁移，用 `EnsureCreated`；若要长期双 provider → 为 SQLite 单独生成一套迁移程序集。

---

## 5. SQLite 不支持 / 有坑的写法逐项核对

| 能力 | 代码现状 | SQLite 可用性 | 坑与结论 |
|---|---|---|---|
| `ExecuteUpdateAsync` / `ExecuteDeleteAsync` | 5 处 `ExecuteUpdateAsync`（`CreateMallOrderCommand.cs:420/488`、`MallCouponService.cs:182/203`、`MallOrderResolver.cs:158`）；无 `ExecuteDeleteAsync` | 可用（EF Core 7+ SQLite provider 支持） | 生成单条 `UPDATE`，靠受影响行数判断条件更新是否命中；语义与 SQL Server 一致。但事务内仍是单写锁，长事务会放大锁等待 |
| 过滤唯一索引 `HasFilter("[IsDeleted] = 0")` | 29 处；1 处 `CrmModelConfiguration.cs:148` 带 `N'推进中'` | 可用（SQLite 支持部分索引） | 方括号引用 SQLite 接受；`N'…'` 必须去掉；建议改为 provider 条件生成，不要直接复用 SQL Server 字符串。注意：测试工厂会删掉所有过滤条件，单测覆盖不到 |
| 自引用外键 | CRM 流转、商城分类等存在自引用 | 可用 | EF Core 的 SQLite provider 默认开启 `PRAGMA foreign_keys=ON`；级联删除行为需逐项确认，SQL Server 与 SQLite 的删除限制语义接近 |
| 复合唯一索引 | 多处（如 `MallModelConfiguration.cs:58/98`） | 可用 | 无差异；索引列顺序需与查询一致 |
| `Include` / `AsSplitQuery` | 大量 `Include`；`AsSplitQuery` 为 0 | 可用 | `Include` 无差异；若未来引入 `AsSplitQuery`，SQLite 也支持，只是拆成多次查询 |
| `EF.Functions.Like` | 少量关键词搜索 | 可用 | SQLite `LIKE` 对 ASCII 默认不区分大小写，与 SQL Server CI 接近；但 `Contains` 会被翻译成 `instr()`，**区分大小写**，与 SQL Server 不一致（见下） |
| 大小写敏感比较 | 登录 `LoginCommand.cs:92` 用 `u.Username == username`；大量编码唯一索引 | **行为变化** | SQL Server 默认 CI：`Admin` 能登录 `admin`；SQLite `=` 区分大小写，登录会失败。唯一索引在 SQLite 下也变成区分大小写，`ABC` 与 `abc` 可同时存在。需对用户名/权限码/商品码/券码等加 `COLLATE NOCASE` 或统一小写归一化 |
| `DateTime` / `DateTimeOffset` | `DateTime` 大量；`DateTimeOffset` 为 0 | 可用 | `DateTime` 映射 TEXT（固定宽度 ISO），排序与范围比较正常；`DateTimeOffset` 未使用，不构成风险 |
| 并发令牌 | 无 `rowversion` / `IsConcurrencyToken`（0 处） | 无差异 | 本来就没有乐观并发保护；SQLite 下同样没有。不要误以为单写锁等于并发安全，它只保证串行化 |
| 批量插入 | 无 `SqlBulkCopy`；`AddRange` + `SaveChanges` | 可用但更慢 | SQLite provider 的批处理能力弱于 SQL Server，大量 `AddRange`（种子、订单明细）会产生更多往返；需实测种子耗时 |
| 裸 SQL（迁移/种子） | 52 处迁移 raw SQL | **不可用** | `GETDATE()`→`CURRENT_TIMESTAMP`、`NEWID()`→参数化 GUID、`TOP (1)`→`LIMIT 1`、`IF EXISTS`→`WHERE NOT EXISTS`；迁移需重新生成 |

---

## 6. 并发与事务

### 6.1 现状

- 所有 Command 经 `TransactionBehaviour` 自动 `BeginTransaction / Commit / Rollback`（`TransactionBehaviour.cs:24-33`）。
- 关键写路径：
  - 下单：`CreateMallOrderCommand` 在一个事务里做库存条件扣减（2 次 `ExecuteUpdateAsync`）、写订单/明细/流水。
  - 领券：`MallCouponService` 两次 `ExecuteUpdateAsync`（领券计数 + 会员券）。
  - 支付/退款/超时关单：`MallOrderResolver` 条件更新 + 库存回补，均在同一事务内。

### 6.2 SQLite 行为

- 单写锁：同一时刻只允许一个写事务；WAL 模式下读可以并发，写仍串行。
- 事务从首次写入一直持锁到提交；上面的下单/退款事务包含多次写，锁持有时间会被放大。
- `Microsoft.Data.Sqlite` 默认有 busy timeout（约 30 秒），但高并发下仍会出现 `database is locked`；开启 WAL 只能缓解读，不解决写排队。
- 没有重试策略：`TransactionBehaviour` 没有 `ExecutionStrategy` 重试，SQLite 锁冲突会直接冒到接口。

### 6.3 判断

| 场景 | 判断 |
|---|---|
| 单实例、开发/测试、并发写 < 5/s、数据量 < 几十万行 | 可接受（建议 `journal_mode=WAL`、`busy_timeout=5000`、缩短事务） |
| 生产多用户下单、秒杀、支付回调峰值、多实例部署 | **不可接受**：写入串行化会直接成为瓶颈，且 `database is locked` 无法通过扩容解决 |
| 支付回调幂等 | 单写锁反而降低并发竞争，但没有乐观并发/唯一幂等键时，仍可能重复更新；与 provider 无关 |

---

## 7. 软删除、审计、操作日志

- 全局查询过滤器 `HasQueryFilter(e => !e.IsDeleted)` 在 `AppDbContext.OnModelCreating` 中统一注册，属于 EF 模型层，SQLite 下行为一致。
- 软删除、审计字段填充依赖 `SaveChanges` 拦截器/领域事件，均在 C# 侧执行，与 provider 无关。
- 操作日志：系统在 `SaveChanges` 时为变更实体批量写日志。SQLite 下这些写入与业务写入在同一事务里，日志量越大事务越长，锁持有越久；表本身无上限增长，SQLite 文件会持续变大且难以在线收缩。
- 差异点只有一个：**过滤唯一索引**依赖 `HasFilter`，SQLite 下需去掉 `N'…'` 并确认方括号写法；其余无行为差异。

---

## 8. 测试基础设施可复用程度

### 8.1 已有

`QPS.UnitTests/Common/TestSqliteDbContextFactory.cs` 已经能在 SQLite 上建库并跑测试：

- 使用 `EnsureCreated`，不跑迁移。
- 建库前做两件“去 SQL Server 化”的事：移除所有 `HasColumnType(...)` 里的类型名（如 `nvarchar(max)`），以及移除所有索引过滤条件。
- 已被 5 个测试类使用：`MallAddressOwnershipTests`、`MallOrderPreviewDiscountSplitTests`、`MallOrderPreviewFreightTests`、`MallProductPromotionEnricherTests`、`MallRefundCommandTests`。

### 8.2 缺口

- 没有覆盖下单库存扣减（`CreateMallOrderCommand` 的 `ExecuteUpdateAsync`）。
- 没有覆盖领券并发/条件更新。
- 没有覆盖支付、退款回补、超时关单的事务路径。
- 因为工厂删掉了 `HasFilter`，过滤唯一索引在 SQLite 下的真实行为完全没被测到。
- 没有 decimal 排序/筛选/聚合的回归测试（第 3.2 节的 6 处都没有保护）。
- 没有针对 SQLite 的迁移冒烟测试；也没有种子数据在 SQLite 上的测试。
- 没有大小写敏感场景（登录、唯一码）的测试。

结论：工厂可以复用，但只覆盖了“纯领域/内存计算”的一部分，离“证明 SQLite 可用”还差得远。

---

## 9. 种子与启动逻辑

| 位置 | 行为 | SQLite 下 |
|---|---|---|
| `Program.cs:64-69` | Development 环境执行 `MigrateAsync()` | **阻断**：现有迁移是 SQL Server 的，必须改为 provider 分支或 `EnsureCreatedAsync` |
| `Program.cs:79-88` | 读取工作区 `mobile/src/mock`，调用 `MallMockDataSeeder.SeedAsync` | 可用：种子走 EF API + JSON 解析，provider 无关；但目录在容器/部署环境不存在时只会告警跳过 |
| `Program.cs:93` | 非开发环境仅 `CanConnectAsync()` | 可用；SQLite 下会创建空文件但不建表，需确保发布流程负责建库 |
| `MallMockDataSeeder` | EF `AddRange` + 事务 + JSON 导入 | 可用；大量插入会变慢，且事务内长时间持写锁 |
| `mobile/src/mock` JSON | decimal 字段按 JSON number 反序列化 | 可用；写入 TEXT 后读回精度一致 |

---

## 10. 不该迁的部分

| 对象 | 原因 |
|---|---|
| 订单/支付/退款/库存等金额表 | 需要真正 decimal 语义；SQLite 只能 TEXT 或整数分，改造成本高 |
| 下单、领券、支付回调、退款回补 | 高并发写 + 单写锁，生产不可接受 |
| 系统操作日志、库存流水、审计类大表 | 持续追加、文件膨胀、单写锁下事务变长 |
| 多实例/水平扩容部署 | SQLite 是单机文件库，无法共享写 |
| 报表/统计类 SUM、MAX、排序 | SQL 侧 decimal 聚合在 SQLite 下不可靠 |
| 关键词搜索/全文检索 | 当前只有 `LIKE`/`Contains`，没有全文索引；数据量大时 SQLite 全表扫描更慢，且大小写语义与 SQL Server 不一致 |

**折中方案可行性：高。** 保留 SQL Server 作为生产库，SQLite 仅用于开发、单元测试、演示；需要解决的是双 provider 配置、decimal 查询审计、大小写语义、启动建库分支，以及测试覆盖补齐。

---

## 11. 改动清单（按文件/层）

### 阻断（不改无法启动或数据错误）

| 文件 / 层 | 问题 |
|---|---|
| `QPS.Persistence/Migrations/*`（49 个） | 全部为 SQL Server 迁移，SQLite 无法执行 |
| `QPS.Host/Program.cs:64-69` | `MigrateAsync()` 直接跑 SQL Server 迁移 |
| decimal SQL 侧用法（第 3.2 节 6 处） | 排序/筛选/聚合在 SQLite 下结果错误 |
| 登录与唯一码的大小写语义（`LoginCommand.cs:92` 等） | 登录与唯一性校验行为变化 |

### 需改造

| 文件 / 层 | 改动 |
|---|---|
| `QPS.Persistence/DependencyInjection.cs:21-24` | provider 分支 |
| `QPS.Persistence/QPS.Persistence.csproj` | 增加 SQLite 包 |
| `QPS.Host/appsettings.json` | provider 配置 + SQLite 连接串 |
| `QPS.Persistence/Configurations/MallModelConfiguration.cs:73/74/182/334/351` | 去掉/条件化 `nvarchar(max)` |
| `QPS.Persistence/Configurations` 全部 `HasFilter` | 去掉 `N'…'`，按 provider 生成过滤条件 |
| 大小写敏感列 | `COLLATE NOCASE` 或写入前归一化 |
| `Dockerfile` / 本地启动 | SQLite 文件与上传目录的持久化卷 |
| 测试 | 扩充 SQLite 场景覆盖（第 8.2 节） |
| EF CLI / 迁移程序集 | 双 provider 时指定 `MigrationsAssembly` |

### 无影响

| 层 | 说明 |
|---|---|
| Domain 实体与领域服务 | 纯 C# 计算，provider 无关 |
| Application Handler（除 decimal SQL 用法外） | EF API 抽象，provider 无关 |
| Controllers / 中间件 / 权限 / JWT | 与数据库无关 |
| 全局软删除过滤器、审计拦截器 | EF 模型层，行为一致 |
| 管理端 / 移动端前端 | 不感知数据库 |

---

## 12. 建议的落地路径（若接受双 provider）

1. `DependencyInjection` 增加 `Database:Provider` 配置，SQL Server 为默认，SQLite 为可选。
2. SQLite 下启动走 `EnsureCreatedAsync()`，不生成迁移；SQL Server 保持 `MigrateAsync()`。
3. 把第 3.2 节 6 处 decimal SQL 用法改为内存计算，并加架构测试禁止 SQL 侧 decimal 比较/排序/聚合回退。
4. 在测试工厂中补上过滤索引（不要一律删除）并增加下单/领券/退款/大小写回归。
5. 容器/本地为 SQLite 文件与上传目录挂持久化卷。
6. 生产继续 SQL Server；SQLite 只出现在开发与测试配置中。

---

## 附：文档生成器状态

本次按要求尝试运行 `node tools/build-docs-viewer.mjs`，但 **`tools/build-docs-viewer.mjs` 不存在**（`tools/` 目录下没有该文件）。仓库里只有一个未跟踪的临时脚本 `agent-temp/build-docs-viewer.mjs`，不属于入库工具，因此没有运行、也没有临时新建替代脚本。`docs/docs-viewer.html` 尚未包含本文；如需更新阅读器，请先把生成器正式放到 `tools/build-docs-viewer.mjs`，我再执行。

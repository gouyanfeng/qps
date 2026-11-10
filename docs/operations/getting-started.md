# 本地启动与发布

> 基于 2026-11-07 的仓库结构与 `QPS.Host` 配置整理。所有命令均以 `E:\Code\QPS` 为根目录。

## 1. 环境要求

| 依赖 | 要求 |
|---|---|
| .NET SDK | 8.0（`QPS.Host.csproj` 目标框架 `net8.0`） |
| SQL Server | 本地实例；默认连接串指向 `localhost` 的 `QPS_CRM` 库 |
| Node.js / npm | 管理端与移动端均使用 `package-lock.json`（npm） |
| EF Core CLI | 仅生产手动迁移需要，版本 8.x |

## 2. 端口与地址

| 服务 | 地址 | 说明 |
|---|---|---|
| 后端 HTTP | `http://localhost:5000` | `launchSettings.json` 默认绑定 |
| 后端 HTTPS | `https://localhost:5001` | 同上 |
| Swagger | `http://localhost:5000/swagger` | Development 下可直接访问 |
| 管理端 | `http://127.0.0.1:5173` | Vite `strictPort`，被占用时直接报错，不会自动换端口 |
| 移动端 H5 | `http://127.0.0.1:5174` | 同上 |

后端 `launchSettings.json` 已设置 `ASPNETCORE_ENVIRONMENT=Development`，因此本地 `dotnet run` 默认进入开发模式。

## 3. 数据库

### 3.1 连接串

`web-api/src/QPS.Host/appsettings.json`：

```
Server=localhost;Database=QPS_CRM;User Id=sa;Password=123456;TrustServerCertificate=True;Encrypt=False;
```

生产环境不要改仓库内文件，使用环境变量覆盖：

```
ConnectionStrings__DefaultConnection=Server=...;Database=...;User Id=...;Password=...;
```

### 3.2 Development：自动迁移 + 示例数据

`QPS.Host/Program.cs` 在 `IsDevelopment()` 时：

1. 用 `CreateExecutionStrategy()` 包裹 `Database.MigrateAsync()`，启动即把数据库迁到最新版本。
2. 从 ContentRoot 向上查找 `mobile/src/mock` 目录，执行 `MallMockDataSeeder.SeedAsync` 导入商城示例数据；目录不存在时只记录警告。

因此本地首次启动前只需保证 SQL Server 可连接，不需要手动建表。

### 3.3 Production：手动迁移

非 Development 分支只执行 `Database.CanConnectAsync()`，**不会自动建库或迁移**。发布前必须手动执行：

```powershell
cd E:\Code\QPS\web-api
dotnet ef database update `
  --project src/QPS.Persistence `
  --startup-project src/QPS.Host
```

需要交给 DBA 执行时，生成 SQL 脚本：

```powershell
dotnet ef migrations script `
  --project src/QPS.Persistence `
  --startup-project src/QPS.Host `
  -o migrations.sql
```

新增迁移：

```powershell
dotnet ef migrations add <MigrationName> `
  --project src/QPS.Persistence `
  --startup-project src/QPS.Host
```

首次使用 EF 工具：

```powershell
dotnet tool install --global dotnet-ef --version 8.*
```

## 4. 后端启动

```powershell
cd E:\Code\QPS\web-api
dotnet restore QPS.sln
dotnet build QPS.sln -m:1
dotnet run --project src/QPS.Host
```

- `-m:1` 限制 MSBuild 并行度，避免多项目并发编译占满内存；机器内存充足时可去掉，但本仓库默认建议保留。
- 启动入口是 `src/QPS.Host`，不是旧的 `src/4.QPS.WebAPI`。
- 控制器按模块注册：`AddQpsSystem` / `AddQpsCrm` / `AddQpsMall` 未调用时，对应模块的 Handler 与 Controller 都不生效。

### 测试

```powershell
dotnet test tests/QPS.UnitTests/QPS.UnitTests.csproj -m:1
dotnet test tests/QPS.ArchitectureTests/QPS.ArchitectureTests.csproj -m:1
```

`QPS.ArchitectureTests` 校验项目依赖方向、模块启用、Pipeline 归属与控制器策略，改动模块边界后必须跑。

## 5. 管理端

```powershell
cd E:\Code\QPS\admin
npm ci
npm run dev
```

- 开发地址 `http://127.0.0.1:5173`。
- `.env` 中 `VITE_API_URL=http://localhost:5000/api`，`admin/src/api/index.ts` 以此为 axios `baseURL`；未配置时回退 `http://localhost:5000/api`。
- 生产构建：

```powershell
npm run build
```

`build` 先执行 `vue-tsc -b` 类型检查，再执行 `vite build`；类型错误会阻断构建。

## 6. 移动端 H5

```powershell
cd E:\Code\QPS\mobile
npm ci
npm run dev:h5
```

- 开发地址 `http://127.0.0.1:5174`。
- H5 代码读取 `VITE_API_BASE_URL`（`src/utils/http.ts`、`src/api/index.ts`），未设置时回退 `http://localhost:5000`。
- 当前 `.env` 写的是 `VITE_API_URL`，该变量不会被 H5 的 HTTP 层读取；需要指定后端时，在 `.env.local` 中设置 `VITE_API_BASE_URL=http://localhost:5000`。
- 生产构建：

```powershell
npm run build:h5
```

`package.json` 还保留微信/支付宝等小程序构建脚本，当前主要使用 H5。

## 7. 生产发布

### 7.1 后端

```powershell
cd E:\Code\QPS\web-api
dotnet publish src/QPS.Host -c Release -o ./publish
```

发布前/后检查：

1. 目标环境设置 `ASPNETCORE_ENVIRONMENT=Production`。
2. 通过环境变量提供 `ConnectionStrings__DefaultConnection` 与 `Jwt__SecretKey` / `Jwt__Issuer` / `Jwt__Audience`。
3. 先执行 3.3 的 `dotnet ef database update`，再启动进程。
4. 确保进程对 `wwwroot/uploads` 有读写权限；目录在启动时自动创建。
5. Production 不会导入 `mobile/src/mock` 示例数据。

启动：

```powershell
$env:ASPNETCORE_ENVIRONMENT="Production"
dotnet .\publish\QPS.Host.dll
```

或由 IIS / 反向代理承载 `publish` 目录，按实际承载方式配置进程模型与端口。

### 7.2 前端

```powershell
cd E:\Code\QPS\admin
npm run build
# 产物在 admin/dist

cd E:\Code\QPS\mobile
npm run build:h5
# 产物在 mobile/dist/build/h5
```

部署前确认构建时的 `VITE_API_URL` / `VITE_API_BASE_URL` 指向生产 API，而不是 `localhost:5000`。

## 8. 常见问题

| 现象 | 排查 |
|---|---|
| `dotnet build` 内存占用高或失败 | 使用 `-m:1`，关闭并行构建与其他占用内存的进程 |
| 5000/5173/5174 启动失败 | 端口被占用；前端 `strictPort` 不会自动换端口，先确认占用进程 |
| 启动时报数据库连接失败 | 检查 SQL Server 实例、`DefaultConnection`、账号密码与 `TrustServerCertificate` |
| 生产启动后表结构缺失 | Production 只做 `CanConnectAsync`，需先手动 `database update` |
| 登录返回 401 | 检查 `Jwt` 三个配置项是否完整，以及请求是否携带 Bearer Token |
| 移动端请求打到错误地址 | 检查 `VITE_API_BASE_URL`；`.env` 中的 `VITE_API_URL` 对 H5 不生效 |
| 本地没有商城示例数据 | 确认 `mobile/src/mock` 存在，且环境为 Development |
| 接口 403 | 请求类型上是否有正确的 `[Permission]`，当前用户角色是否拥有该权限码 |

## 9. 产物目录
- 普通临时产物（草稿、分析结果、实验脚本、调试输出、构建日志）统一放在 `E:\Code\QPS\agent-temp`；不得放入源码目录或提交。
- Playwright UI 测试的 spec、调试脚本、截图和报告统一放在 `E:\Code\QPS\ui-tests`；不得混入 `agent-temp` 或源码目录。
- 不得使用 `C:\Users\Dust\AppData\Local\Temp\QPS`、`CodexTemp` 或其他系统临时目录保存项目产物。

## 10. 编码与终端
- Windows 下读取 `.md`、`.txt`、`.json`、`.xml`、`.yml`、`.yaml`、`.cs`、`.js`、`.ts`、`.vue`、`.html`、`.css` 等文本文件时，优先按 UTF-8 编码处理。
- PowerShell 读取文件使用 `Get-Content -LiteralPath "路径" -Encoding UTF8`；除非项目已有其他明确要求，文本修改保持 UTF-8。
- 遇到 `鈥?`、`鈫?`、`鈮?`、`锟斤拷` 等乱码时，先判断控制台或输出链路编码，不能直接认定源文件已损坏。
- Windows PowerShell 5.1 运行含中文的临时 `.ps1` 时使用 UTF-8 with BOM，或改用 PowerShell 7。
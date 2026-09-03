# QPS 中药材供需撮合平台

QPS 面向中药材基地卖方与药厂买方，统一管理供给信息、采购需求、业务跟进和供需撮合过程。

完整的产品定位和业务链路见 [docs/product/项目背景与业务链路.md](docs/product/项目背景与业务链路.md)，本地启动与发布见 [docs/operations/本地启动与发布.md](docs/operations/本地启动与发布.md)。

## 目录

```text
qps-ht/         ASP.NET Core 后端
qps-web-admin/  Vue 3 管理端
codex-temp/     普通临时产物（已忽略）
qps-ui-tests/   Playwright UI 测试、截图和报告（已忽略）
docs/           共享产品与研发文档
```

## 本地启动

以下命令启动本地后端和前端。前端固定使用 `http://127.0.0.1:5173`，并通过 `http://localhost:5000/api` 连接本地后端；UI 测试也使用这组端口。

```powershell
dotnet run --project .\qps-ht\src\4.QPS.WebAPI\QPS.WebAPI.csproj -- --urls http://localhost:5000

Set-Location .\qps-web-admin
npm run dev
```

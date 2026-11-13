# 部署与 CI

> 状态：有效 | 最后核对：2026-11-13 | 对应代码：`docker-compose.yml`、`.github/workflows/docker-image.yml`、`.github/workflows/sync-deploy.yml`、`.env.example`、`admin/Dockerfile`、`mobile/Dockerfile`

本地启动与源码发布见 [getting-started.md](getting-started.md)，本文件只写 Docker 化部署与 CI 链路。

## 1. 部署形态

无统一网关，三个服务直接暴露端口（2026-11-12 确认）：

| 服务 | 端口 | 说明 |
|---|---|---|
| admin | `5173:80` | 容器内 nginx 只做静态托管 + SPA 兜底（`try_files … /index.html`），**无任何反代** |
| mobile | `5174:80` | 同上 |
| qps-api | `5000:5000` | ASP.NET Core，CORS 策略 `AllowAll`（前端直连，`withCredentials: false`，JWT 走 `Authorization` 头） |

前端请求后端是**跨域直连** `VITE_API_BASE_URL`，不经过前端容器的 nginx。

## 2. 环境变量与配置文件

| 文件 | 入库 | 用途 |
|---|---|---|
| `.env.example` | 是 | 脱敏占位模板；CI 用 `--env-file` 引用；服务器 `cp .env.example .env` 后填真实值 |
| `.env` | 否（gitignore） | 服务器/本地实际生效值 |
| `.env.dev` | 否（gitignore） | 本地容器模式专用真实凭据（`DB_HOST=host.docker.internal`） |
| `appsettings.Local.json` | 否（gitignore/dockerignore） | 后端本机私有配置，`Program.cs` 以 optional 加载、优先级最高 |

两类变量，传递机制完全不同：

- **构建期**：`VITE_API_BASE_URL`、`VITE_FILE_BASE_URL` 由 compose `build.args` 传入 Dockerfile `ARG`，vite 在打包时**内联成字面量**，镜像里没有环境变量可查，改值必须重建镜像。
- **运行期**：`ConnectionStrings__DefaultConnection`（compose 拼接 `DB_*`）、`Jwt__SecretKey`、`Storage__Oss__*` 经 `environment:` 注入容器，`__` 映射为 .NET 配置层级分隔符；改值只需改服务器 `.env` 后 `up -d`，**无需重新拉镜像**。

CI 中的注入优先级：shell 环境变量 > `--env-file` 文件 > compose `${VAR:-default}`。注意 `${VAR:-x}` 在变量为**空字符串**时也回落默认值，所以 CI 里对 `VITE_API_BASE_URL` 先判空再 export。

## 3. 镜像构建（CI）

`docker-image.yml`：push / PR 到 `master` 触发。

- **PR 到 master**：只 build 不 push，作编译验证门禁（push 步骤带 `if: github.event_name == 'push'`）。
- **合并进 master**：`docker compose --env-file .env.example build` 构建 `qps-api` / `qps-admin` / `qps-mobile` 三个镜像（compose 只构建带 `build:` 段的服务），以 `github.sha` 与 `latest` 双 tag 推到 `ghcr.io/<owner小写>`。
- `VITE_API_BASE_URL` 来自仓库 Variables（Settings → Secrets and variables → Variables）；`DB_PASSWORD` / `JWT_SECRET_KEY` 在 CI 里注入 `ci-placeholder` 只为满足 compose `:?` 非空校验，构建阶段不连数据库。

镜像 tag 策略：`:latest` 供服务器日常 pull；`:<sha>` 不可变，用于精确回滚（`IMAGE_TAG=<sha> docker compose up -d`）。

## 4. 分支与发布流

| 分支 | 角色 |
|---|---|
| `dev` | 开发主分支，所有日常提交与推送都落这里 |
| `master` | 受保护（必须 PR Approve），合并即触发镜像构建 |
| `deploy` | 仅存 `docker-compose.yml` + `.env.example`，由 `sync-deploy.yml` 在这两个文件变更时自动同步 |

发布链路：dev 提交 → auto-pr 开 PR 到 master → 人工 Approve + Merge（网页合并以 web-flow 身份触发 CI；bot 用 GITHUB_TOKEN 合并不触发工作流）→ CI 构建推镜像 → 服务器 `docker compose pull && docker compose up -d --remove-orphans`。

## 5. 服务器更新操作

```bash
git pull        # deploy 分支拿到最新 compose 与 .env.example
cp .env.example .env   # 仅首次；已有 .env 则保留真实值
nano .env       # 核对 DB_*、JWT_SECRET_KEY、OSS_*、VITE_API_BASE_URL
docker compose pull
docker compose up -d --remove-orphans
```

- `--remove-orphans`：清理早期网关容器残留，首次升级必带。
- 数据库升级不自动执行：Production 只做 `CanConnectAsync`，迁移命令见 [getting-started.md](getting-started.md) 第 3.3 节。
- 验证前端地址是否生效：`docker run --rm --entrypoint sh ghcr.io/<owner>/qps-admin:latest -c "grep -o '.\{0,60\}VITE\|http://[^\"']*5000' /usr/share/nginx/html/assets/*.js"`，产物里应是服务器地址而非 `localhost:5000`。

## 6. 常见问题

| 现象 | 原因与处理 |
|---|---|
| 镜像里 API 地址仍是 `localhost:5000` | CI 变量为空串触发 compose `:-` 回落；确认仓库 Variables 已配置，且工作流用条件 export |
| CI 报 `couldn't find env file: .../.env` | 工作流漏了 `--env-file .env.example`；`.env` 已退出仓库跟踪 |
| push 镜像 403 | job 级 `permissions: packages: write` 缺失，或镜像 owner 含大写（GHCR 强制小写） |
| 服务器前端白屏 / 请求打不上 | 核对 `.env` 的 `VITE_API_BASE_URL` 是否为服务器地址；该值是构建期变量，改后必须重建镜像 |
| 改了 `.env` 前端不生效 | `VITE_*` 是构建期内联值；运行期只有后端 `DB_*` / `JWT` / `Storage__*` 这类变量改 `.env` 即可生效 |

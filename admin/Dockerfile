# 管理端镜像：Node 构建 + Nginx 托管静态产物。
# 基础镜像与本地 Node 22.22.2 保持同一大版本。
FROM node:22-alpine AS build

WORKDIR /app

# 只复制依赖清单，充分利用 Docker 层缓存；npm ci 严格按 lockfile 安装。
COPY package.json package-lock.json ./
RUN npm ci

COPY . .

# 接口地址默认与 .env.production 一致（/api）。
# CI 需要指向其他地址时才用 --build-arg VITE_API_URL=... 覆盖；
# 构建参数优先级高于 .env.production，两者都没有时才会落到代码里的 localhost 兜底。
ARG VITE_API_URL=/api
RUN VITE_API_URL="$VITE_API_URL" npm run build

FROM nginx:alpine-slim AS runtime

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80

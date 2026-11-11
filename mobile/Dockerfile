# 移动端 H5 镜像：Node 构建 + Nginx 托管静态产物。
# 基础镜像与本地 Node 22.22.2 保持同一大版本。
FROM node:22-alpine AS build

WORKDIR /app

# 只复制依赖清单，充分利用 Docker 层缓存；npm ci 严格按 lockfile 安装。
COPY package.json package-lock.json ./
RUN npm ci

COPY . .

# 部署在网关 /h5/ 子路径下（vite.config.ts 固定 base=/h5/）。
# API 走同源相对路径（空串），由 gateway 把 /api 转发给 qps-api；
# 指向其他地址时用 --build-arg VITE_API_BASE_URL=... 覆盖。
ARG VITE_API_BASE_URL=
# 图片等静态文件访问域名；缺省由代码兜底为阿里云 OSS 域名（见 mobile/src/utils/http.ts）。
ARG VITE_FILE_BASE_URL=
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL \
    VITE_FILE_BASE_URL=$VITE_FILE_BASE_URL
RUN npm run build:h5

FROM nginx:alpine-slim AS runtime

COPY nginx.conf /etc/nginx/conf.d/default.conf
# 网关转发 /h5/* 时已剥掉前缀，容器内按根路径托管即可。
COPY --from=build /app/dist/build/h5 /usr/share/nginx/html

EXPOSE 80

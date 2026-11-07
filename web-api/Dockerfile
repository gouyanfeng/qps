# --- 构建阶段 ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# 1) 先只拷贝解决方案、NuGet 配置和各项目的 .csproj，用于分层缓存 restore。
#    这里必须包含 QPS.Host 引用的全部模块项目，否则 restore 会因缺少项目文件失败。
COPY QPS.sln ./
COPY nuget.config ./
COPY src/QPS.Shared/QPS.Shared.csproj src/QPS.Shared/
COPY src/QPS.System/QPS.System.csproj src/QPS.System/
COPY src/QPS.Crm/QPS.Crm.csproj src/QPS.Crm/
COPY src/QPS.Mall/QPS.Mall.csproj src/QPS.Mall/
COPY src/QPS.Persistence/QPS.Persistence.csproj src/QPS.Persistence/
COPY src/QPS.Host/QPS.Host.csproj src/QPS.Host/
RUN dotnet restore src/QPS.Host/QPS.Host.csproj

# 2) 再拷贝剩余源码。publish 自带 build，不再单独 build，避免重复编译。
COPY . .
RUN dotnet publish src/QPS.Host --configuration Release --output /app/publish --no-restore

# --- 运行阶段 ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Alpine 运行时缺少 ICU 与时区库：ICU 影响中文/本地化，tzdata 让 TZ 生效。
RUN apk add --no-cache icu-libs tzdata
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    TZ=Asia/Shanghai

# 上传文件写入 ContentRoot/wwwroot（见 Program.cs 与 FileUploadController）。
# 预建目录并授权给镜像内置的非 root 用户 app，同时声明数据卷，便于把上传内容挂载到宿主机。
RUN mkdir -p /app/wwwroot && chown -R app:app /app
VOLUME ["/app/wwwroot"]
USER app

# 对外端口与本地约定保持一致：5000。
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000
ENTRYPOINT ["dotnet", "QPS.Host.dll"]

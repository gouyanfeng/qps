# --- 构建阶段 ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# 1) 先只拷贝解决方案与各项目的 .csproj，用于分层缓存 restore
COPY QPS.sln ./
COPY nuget.config ./
COPY src/1.QPS.Domain/QPS.Domain.csproj src/1.QPS.Domain/
COPY src/2.QPS.Application/QPS.Application.csproj src/2.QPS.Application/
COPY src/3.QPS.Infrastructure/QPS.Infrastructure.csproj src/3.QPS.Infrastructure/
COPY src/4.QPS.WebAPI/QPS.WebAPI.csproj src/4.QPS.WebAPI/
RUN dotnet restore

# 2) 再拷贝全部源码，只有源码变动才会触发后续 build/publish
COPY . .
RUN dotnet build --configuration Release --no-restore
RUN dotnet publish src/4.QPS.WebAPI --configuration Release --output /app/publish --no-build

# --- 运行阶段：换成 Alpine ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Alpine 镜像默认不包含 ICU (国际化库)，如果你的程序涉及特殊的时间格式或货币转换，需要加上这行：
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

EXPOSE 80
ENV ASPNETCORE_URLS=http://+:80
ENTRYPOINT ["dotnet", "QPS.WebAPI.dll"]

using MediatR;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using QPS.Shared.Contracts;
using QPS.System.Application.Services;
using QPS.System.Contracts;

namespace QPS.System;

public static class DependencyInjection
{
    /// <summary>
    /// 注册 System 模块。调用即启用，不调用则其 Handler 与 Controller 均不参与运行。
    /// </summary>
    public static IServiceCollection AddQpsSystem(
        this IServiceCollection services,
        IMvcBuilder mvcBuilder)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.AddScoped<IPermissionChecker, PermissionChecker>();
        services.AddScoped<IErrorLogService, ErrorLogService>();

        mvcBuilder.ConfigureApplicationPartManager(manager =>
            manager.ApplicationParts.Add(new AssemblyPart(typeof(DependencyInjection).Assembly)));

        return services;
    }
}

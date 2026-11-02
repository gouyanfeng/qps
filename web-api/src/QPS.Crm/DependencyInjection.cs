using MediatR;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

namespace QPS.Crm;

public static class DependencyInjection
{
    /// <summary>
    /// 注册 CRM 模块。调用即启用，不调用则其 Handler 与 Controller 均不参与运行。
    /// </summary>
    public static IServiceCollection AddQpsCrm(
        this IServiceCollection services,
        IMvcBuilder mvcBuilder)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        mvcBuilder.ConfigureApplicationPartManager(manager =>
            manager.ApplicationParts.Add(new AssemblyPart(typeof(DependencyInjection).Assembly)));

        return services;
    }
}

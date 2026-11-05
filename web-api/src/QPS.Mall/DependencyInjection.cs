using MediatR;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using QPS.Mall.Application.Features.MallCarts;
using QPS.Mall.Application.Features.MallProducts;
using QPS.Mall.Domain.Services;

namespace QPS.Mall;

public static class DependencyInjection
{
    /// <summary>
    /// 注册商城模块。调用即启用，不调用则其 Handler 与 Controller 均不参与运行。
    /// </summary>
    public static IServiceCollection AddQpsMall(
        this IServiceCollection services,
        IMvcBuilder mvcBuilder)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddTransient<GetMallCartHandler>();

        services.AddScoped<IActionStrategy, DiscountAmountAction>();
        services.AddScoped<IActionStrategy, DiscountRateAction>();
        services.AddScoped<IActionStrategy, GiftAction>();
        services.AddScoped<IActionStrategy, FreightFreeAction>();

        services.AddScoped<IActionStrategyRegistry, ActionStrategyRegistry>();
        services.AddScoped<MallPromotionEngine>();
        services.AddScoped<MallProductPromotionEnricher>();

        mvcBuilder.ConfigureApplicationPartManager(manager =>
            manager.ApplicationParts.Add(new AssemblyPart(typeof(DependencyInjection).Assembly)));

        return services;
    }
}
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QPS.Crm;
using QPS.Mall;
using QPS.Persistence;
using QPS.Shared;
using QPS.Shared.Common;
using QPS.System;
using Xunit;

namespace QPS.ArchitectureTests;

/// <summary>
/// 校验“注册即启用”：不调用模块注册方法时，该模块的 Handler、服务与 Controller 都不参与运行。
/// </summary>
public class ModuleRegistrationTests
{
    private static readonly global::System.Reflection.Assembly CrmAssembly = typeof(QPS.Crm.DependencyInjection).Assembly;
    private static readonly global::System.Reflection.Assembly SystemAssembly = typeof(QPS.System.DependencyInjection).Assembly;
    private static readonly global::System.Reflection.Assembly MallAssembly = typeof(QPS.Mall.DependencyInjection).Assembly;

    [Fact]
    public void WithoutCrm_ShouldNotRegisterCrmTypes()
    {
        var services = CreateServices(registerCrm: false);
        using var provider = services.BuildServiceProvider();

        var crmDescriptors = services
            .Where(descriptor => descriptor.ImplementationType?.Assembly == CrmAssembly)
            .ToList();
        Assert.Empty(crmDescriptors);

        Assert.Contains(services, descriptor => descriptor.ImplementationType?.Assembly == SystemAssembly);
    }

    [Fact]
    public void WithoutCrm_ShouldNotExposeCrmControllers()
    {
        var services = CreateServices(registerCrm: false);
        using var provider = services.BuildServiceProvider();

        var controllers = DiscoverControllers(provider);

        Assert.NotEmpty(controllers);
        Assert.DoesNotContain(controllers, controller => controller.AsType().Assembly == CrmAssembly);
        Assert.Contains(controllers, controller => controller.AsType().Assembly == SystemAssembly);
    }

    [Fact]
    public void WithCrm_ShouldRegisterCrmHandlersAndExposeCrmControllers()
    {
        var services = CreateServices(registerCrm: true);
        using var provider = services.BuildServiceProvider();

        Assert.Contains(services, descriptor => descriptor.ImplementationType?.Assembly == CrmAssembly);

        var controllers = DiscoverControllers(provider);
        Assert.Contains(controllers, controller => controller.AsType().Assembly == CrmAssembly);
    }

    [Fact]
    public void WithoutMall_ShouldNotRegisterMallTypesOrControllers()
    {
        var services = CreateServices(registerCrm: true, registerMall: false);
        using var provider = services.BuildServiceProvider();

        Assert.DoesNotContain(services, descriptor => descriptor.ImplementationType?.Assembly == MallAssembly);

        var controllers = DiscoverControllers(provider);
        Assert.DoesNotContain(controllers, controller => controller.AsType().Assembly == MallAssembly);
        Assert.Contains(controllers, controller => controller.AsType().Assembly == CrmAssembly);
    }

    [Fact]
    public void WithMall_ShouldRegisterMallHandlersAndExposeMallControllers()
    {
        var services = CreateServices(registerCrm: true, registerMall: true);
        using var provider = services.BuildServiceProvider();

        Assert.Contains(services, descriptor => descriptor.ImplementationType?.Assembly == MallAssembly);

        var controllers = DiscoverControllers(provider);
        Assert.Contains(controllers, controller => controller.AsType().Assembly == MallAssembly);
    }

    [Fact]
    public void RequiredModules_ShouldResolveKeyServices()
    {
        var services = CreateServices(registerCrm: true);
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true
        });

        using var scope = provider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        Assert.NotNull(scopedProvider.GetRequiredService<MediatR.IMediator>());
        Assert.NotNull(scopedProvider.GetRequiredService<QPS.Shared.Contracts.ICurrentUserService>());
        Assert.NotNull(scopedProvider.GetRequiredService<QPS.Shared.Contracts.IUnitOfWork>());
        Assert.NotNull(scopedProvider.GetRequiredService<QPS.Shared.Contracts.IPermissionChecker>());
    }

    [Fact]
    public void RegisteredHandlers_ShouldBeResolvableFromContainer()
    {
        using var provider = CreateServices(registerCrm: true).BuildServiceProvider();
        using var scope = provider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        Assert.NotNull(scopedProvider.GetService(
            typeof(MediatR.IRequestHandler<QPS.System.Application.Features.Auth.LoginCommand, QPS.System.Contracts.DTOs.Auth.LoginResponse>)));
        Assert.NotNull(scopedProvider.GetService(
            typeof(MediatR.IRequestHandler<QPS.Crm.Application.Features.GetCrmHerbProductOptionsQuery, List<QPS.Crm.Contracts.DTOs.AttributeOptionDto>>)));
        Assert.NotNull(scopedProvider.GetService(
            typeof(MediatR.IRequestHandler<
                QPS.Mall.Application.Features.MallOrders.GetMallOrdersQuery,
                QPS.Shared.Common.PaginationResponse<QPS.Mall.Contracts.DTOs.MallOrderDto>>)));
    }

    private static ServiceCollection CreateServices(bool registerCrm, bool registerMall = true)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=QPS_ArchitectureTests;Trusted_Connection=True;"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton(new JwtOptions
        {
            SecretKey = "ArchitectureTestsSecretKeyThatIsLongEnough"
        });
        services.AddLogging();
        services.AddHttpContextAccessor();

        var mvcBuilder = services.AddControllers();
        mvcBuilder.ConfigureApplicationPartManager(manager => manager.ApplicationParts.Clear());

        services.AddQpsShared();
        services.AddQpsPersistence(configuration);
        services.AddQpsSystem(mvcBuilder);

        if (registerCrm)
        {
            services.AddQpsCrm(mvcBuilder);
        }

        if (registerMall)
        {
            services.AddQpsMall(mvcBuilder);
        }

        return services;
    }

    private static IList<TypeInfo> DiscoverControllers(IServiceProvider provider)
    {
        var partManager = provider.GetRequiredService<ApplicationPartManager>();
        var feature = new ControllerFeature();
        partManager.PopulateFeature(feature);
        return feature.Controllers;
    }
}

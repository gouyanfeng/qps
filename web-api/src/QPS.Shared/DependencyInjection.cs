using MediatR;
using Microsoft.Extensions.DependencyInjection;
using QPS.Shared.Contracts;
using QPS.Shared.Pipeline;

namespace QPS.Shared;

public static class DependencyInjection
{
    public static IServiceCollection AddQpsShared(this IServiceCollection services)
    {
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(PermissionBehaviour<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionBehaviour<,>));
        return services;
    }
}

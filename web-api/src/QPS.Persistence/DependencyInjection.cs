using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QPS.Crm.Contracts;
using QPS.Mall.Contracts;
using QPS.Persistence.Database;
using QPS.Persistence.Identity;
using QPS.Shared.Common;
using QPS.Shared.Contracts;
using QPS.System.Contracts;

namespace QPS.Persistence;

public static class DependencyInjection
{
    /// <summary>
    /// 注册持久化基础设施：单一 AppDbContext、当前用户、JWT、工作单元。
    /// </summary>
    public static IServiceCollection AddQpsPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=localhost;Database=QPS_CRM;User Id=sa;Password=123456;TrustServerCertificate=True;Encrypt=False;";

        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<ISystemDbContext>(provider => provider.GetRequiredService<AppDbContext>());
        services.AddScoped<ICrmDbContext>(provider => provider.GetRequiredService<AppDbContext>());
        services.AddScoped<IMallDbContext>(provider => provider.GetRequiredService<AppDbContext>());
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<AppDbContext>());

        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddScoped<IJwtGenerator>(provider =>
            new JwtGenerator(provider.GetRequiredService<JwtOptions>()));

        return services;
    }
}

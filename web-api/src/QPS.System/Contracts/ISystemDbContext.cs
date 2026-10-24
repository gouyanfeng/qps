using Microsoft.EntityFrameworkCore;
using QPS.System.Domain.Entities;

namespace QPS.System.Contracts;

public interface ISystemDbContext
{
    DbSet<SystemUser> SystemUsers { get; }
    DbSet<SystemRole> SystemRoles { get; }
    DbSet<SystemPermission> SystemPermissions { get; }
    DbSet<SystemUserRole> SystemUserRoles { get; }
    DbSet<SystemRolePermission> SystemRolePermissions { get; }
    DbSet<SystemDataDictionary> SystemDataDictionaries { get; }
    DbSet<SystemChinaRegion> SystemChinaRegions { get; }
    DbSet<SystemErrorLog> SystemErrorLogs { get; }
    DbSet<SystemOperationLog> SystemOperationLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

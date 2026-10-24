using Microsoft.EntityFrameworkCore;
using QPS.Shared.Contracts;
using QPS.System.Contracts;

namespace QPS.System.Application.Services;

public sealed class PermissionChecker(ISystemDbContext dbContext) : IPermissionChecker
{
    public Task<bool> IsUserActiveAsync(Guid userId, CancellationToken cancellationToken = default) =>
        dbContext.SystemUsers.AnyAsync(user => user.Id == userId && user.IsActive, cancellationToken);

    public Task<bool> HasPermissionAsync(
        Guid userId,
        string permissionCode,
        CancellationToken cancellationToken = default) =>
        (from user in dbContext.SystemUsers
         join rolePermission in dbContext.SystemRolePermissions on user.RoleId equals rolePermission.RoleId
         join permission in dbContext.SystemPermissions on rolePermission.PermissionId equals permission.Id
         where user.Id == userId && permission.Code == permissionCode
         select permission.Id)
        .AnyAsync(cancellationToken);
}

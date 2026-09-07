using System.Collections.Concurrent;
using System.Reflection;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Attributes;
using QPS.Application.Interfaces;
using QPS.Domain.Exceptions;

namespace QPS.Application.Behaviours;

public class PermissionBehaviour<TRequest, TResponse>(
    IDbContext dbContext,
    ICurrentUserService currentUserService) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var permissionCode = PermissionMetadataCache.GetPermissionCode(typeof(TRequest));
        if (permissionCode == null)
        {
            return await next(cancellationToken);
        }

        if (!Guid.TryParse(currentUserService.UserId, out var userId) || userId == Guid.Empty)
        {
            throw new BusinessException(401, "登录状态无效");
        }

        var user = await dbContext.SystemUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == userId && item.IsActive, cancellationToken);
        if (user == null)
        {
            throw new BusinessException(401, "当前用户不可用");
        }

        var hasPermission = await (
            from rolePermission in dbContext.SystemRolePermissions
            join permission in dbContext.SystemPermissions on rolePermission.PermissionId equals permission.Id
            where rolePermission.RoleId == user.RoleId && permission.Code == permissionCode
            select permission.Id)
            .AnyAsync(cancellationToken);
        if (!hasPermission)
        {
            throw new BusinessException(403, "无权访问该功能");
        }

        return await next(cancellationToken);
    }
}

internal static class PermissionMetadataCache
{
    private static readonly ConcurrentDictionary<Type, string?> PermissionCodes = new();

    public static string? GetPermissionCode(Type requestType) =>
        PermissionCodes.GetOrAdd(
            requestType,
            type => type.GetCustomAttribute<PermissionAttribute>()?.PermissionCode);
}

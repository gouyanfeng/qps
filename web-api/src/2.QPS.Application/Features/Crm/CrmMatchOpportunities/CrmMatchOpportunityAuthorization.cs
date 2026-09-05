using Microsoft.EntityFrameworkCore;
using QPS.Application.Interfaces;
using QPS.Domain.Exceptions;

namespace QPS.Application.Features.Crm.CrmMatchOpportunities;

internal static class CrmMatchOpportunityAuthorization
{
    private const string ViewPermissionCode = "CRM_MATCH_OPPORTUNITY_VIEW";
    private const string ManagePermissionCode = "CRM_MATCH_OPPORTUNITY_MANAGE";

    public static Task EnsureViewAsync(IDbContext dbContext, ICurrentUserService currentUserService, CancellationToken cancellationToken) =>
        EnsurePermissionAsync(dbContext, currentUserService, ViewPermissionCode, cancellationToken);

    public static Task EnsureManageAsync(IDbContext dbContext, ICurrentUserService currentUserService, CancellationToken cancellationToken) =>
        EnsurePermissionAsync(dbContext, currentUserService, ManagePermissionCode, cancellationToken);

    private static async Task EnsurePermissionAsync(
        IDbContext dbContext,
        ICurrentUserService currentUserService,
        string permissionCode,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(currentUserService.UserId, out var userId) || userId == Guid.Empty)
        {
            throw new BusinessException(401, "登录状态无效");
        }

        if (!await dbContext.SystemUsers.AnyAsync(user => user.Id == userId && user.IsActive && !user.IsDeleted, cancellationToken))
        {
            throw new BusinessException(401, "当前用户不可用");
        }

        var hasPermission = await (
            from user in dbContext.SystemUsers
            join rolePermission in dbContext.SystemRolePermissions on user.RoleId equals rolePermission.RoleId
            join permission in dbContext.SystemPermissions on rolePermission.PermissionId equals permission.Id
            where user.Id == userId && user.IsActive && !user.IsDeleted
                && !rolePermission.IsDeleted && !permission.IsDeleted
                && permission.Code == permissionCode
            select permission.Id)
            .AnyAsync(cancellationToken);

        if (!hasPermission)
        {
            throw new BusinessException(403, "无权访问撮合机会");
        }
    }
}

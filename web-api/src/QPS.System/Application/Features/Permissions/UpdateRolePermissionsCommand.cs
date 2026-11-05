using QPS.System.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Shared.Contracts;
using QPS.System.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.System.Application.Features.Permissions;

/// <summary>
/// 更新角色权限
/// </summary>
public class UpdateRolePermissionsCommand : IRequest<bool>
{
    /// <summary>
    /// 角色代码
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// 权限代码列表
    /// </summary>
    public List<string> Permissions { get; set; } = new();
}

public class UpdateRolePermissionsHandler : IRequestHandler<UpdateRolePermissionsCommand, bool>
{
    private readonly ISystemDbContext _dbContext;

    public UpdateRolePermissionsHandler(ISystemDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(UpdateRolePermissionsCommand request, CancellationToken cancellationToken)
    {
        // 查找角色
        var role = await _dbContext.SystemRoles
            .FirstOrDefaultAsync(r => r.Code == request.Role, cancellationToken);

        if (role == null)
            throw new BusinessException(404, "角色不存在");

        // 删除该角色所有旧的权限
        var existing = await _dbContext.SystemRolePermissions
            .Where(rp => rp.RoleId == role.Id)
            .ToListAsync(cancellationToken);

        _dbContext.SystemRolePermissions.RemoveRange(existing);

        // 查询权限 ID 映射
        var permCodes = request.Permissions.Distinct().ToList();
        var permIdMap = await _dbContext.SystemPermissions
            .Where(p => permCodes.Contains(p.Code))
            .ToDictionaryAsync(p => p.Code, p => p.Id, cancellationToken);

        // 插入新的权限（使用 PermissionId）
        foreach (var permCode in permCodes)
        {
            if (permIdMap.TryGetValue(permCode, out var permId))
            {
                var rp = new SystemRolePermission(role.Id, permId);
                _dbContext.SystemRolePermissions.Add(rp);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}


using QPS.System.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.System.Contracts.DTOs.Auth;
using QPS.Shared.Contracts;

namespace QPS.System.Application.Features.Auth;

/// <summary>
/// 获取当前登录用户权限代码列表
/// </summary>
public class GetUserPermissionsQuery : IRequest<UserPermissionsResponse>
{
}

public class GetUserPermissionsHandler : IRequestHandler<GetUserPermissionsQuery, UserPermissionsResponse>
{
    private readonly ISystemDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public GetUserPermissionsHandler(ISystemDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<UserPermissionsResponse> Handle(GetUserPermissionsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        var permissions = new List<string>();

        if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var userGuid))
        {
            var user = await _dbContext.SystemUsers
                .Where(u => u.Id == userGuid && !u.IsDeleted)
                .Select(u => new { u.RoleId })
                .FirstOrDefaultAsync(cancellationToken);

            if (user != null)
            {
                var permIds = await _dbContext.SystemRolePermissions
                    .Where(rp => rp.RoleId == user.RoleId && !rp.IsDeleted)
                    .Select(rp => rp.PermissionId)
                    .ToListAsync(cancellationToken);

                permissions = await _dbContext.SystemPermissions
                    .Where(p => permIds.Contains(p.Id) && !p.IsDeleted)
                    .Select(p => p.Code)
                    .ToListAsync(cancellationToken);
            }
        }

        return new UserPermissionsResponse
        {
            Permissions = permissions
        };
    }
}



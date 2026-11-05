using QPS.System.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.System.Contracts.DTOs.Permissions;
using QPS.Shared.Contracts;
using QPS.System.Domain.Entities;

namespace QPS.System.Application.Features.Permissions;

/// <summary>
/// 获取权限树
/// </summary>
public class GetPermissionTreeQuery : IRequest<List<PermissionTreeNodeDto>>
{
}

public class GetPermissionTreeHandler : IRequestHandler<GetPermissionTreeQuery, List<PermissionTreeNodeDto>>
{
    private readonly ISystemDbContext _dbContext;

    public GetPermissionTreeHandler(ISystemDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<PermissionTreeNodeDto>> Handle(GetPermissionTreeQuery request, CancellationToken cancellationToken)
    {
        var permissions = await _dbContext.SystemPermissions
            
            .ToListAsync(cancellationToken);

        // 构建树：根节点（ParentId 为 null）
        var roots = permissions
            .Where(p => p.ParentId == null)
            .OrderBy(p => p.Code)
            .ToList();

        var result = roots.Select(r => BuildTreeNode(r, permissions)).ToList();
        return result;
    }

    private static PermissionTreeNodeDto BuildTreeNode(SystemPermission node, List<SystemPermission> all)
    {
        var dto = new PermissionTreeNodeDto
        {
            Id = node.Id.ToString(),
            Code = node.Code,
            Name = node.Name
        };

        var children = all
            .Where(p => p.ParentId == node.Id)
            .OrderBy(p => p.Code)
            .Select(c => BuildTreeNode(c, all))
            .ToList();

        if (children.Count > 0)
        {
            dto.Children = children;
        }

        return dto;
    }
}


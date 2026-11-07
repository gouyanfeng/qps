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
    /// <summary>
    /// 按权限类型筛选（Menu / Button），为空返回全部
    /// </summary>
    public string? Type { get; set; }
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

        var visible = permissions;
        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            // 按类型筛选时保留匹配节点的全部祖先，保证返回的仍是一棵完整的树
            var byId = permissions.ToDictionary(p => p.Id);
            var included = new HashSet<Guid>();
            foreach (var permission in permissions.Where(p => p.PermissionType == request.Type))
            {
                SystemPermission? current = permission;
                while (current != null && included.Add(current.Id))
                {
                    current = current.ParentId.HasValue
                        ? byId.GetValueOrDefault(current.ParentId.Value)
                        : null;
                }
            }
            visible = permissions.Where(p => included.Contains(p.Id)).ToList();
        }

        // 构建树：根节点（ParentId 为 null）
        var roots = visible
            .Where(p => p.ParentId == null)
            .OrderBy(p => p.Code)
            .ToList();

        var result = roots.Select(r => BuildTreeNode(r, visible)).ToList();
        return result;
    }

    private static PermissionTreeNodeDto BuildTreeNode(SystemPermission node, List<SystemPermission> all)
    {
        var dto = new PermissionTreeNodeDto
        {
            Id = node.Id.ToString(),
            Code = node.Code,
            Name = node.Name,
            Type = node.PermissionType
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


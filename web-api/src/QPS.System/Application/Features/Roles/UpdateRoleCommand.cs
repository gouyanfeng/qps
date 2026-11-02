using QPS.System.Contracts;
using MediatR;
using QPS.System.Contracts.DTOs.Roles;
using QPS.Shared.Contracts;
using QPS.Shared.Exceptions;

namespace QPS.System.Application.Features.Roles;

/// <summary>
/// 更新角色命令
/// </summary>
public class UpdateRoleCommand : IRequest<bool>
{
    /// <summary>
    /// 角色ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 更新角色请求
    /// </summary>
    public RoleUpdateRequest Request { get; set; }
}

/// <summary>
/// 更新角色处理器
/// </summary>
public class UpdateRoleHandler : IRequestHandler<UpdateRoleCommand, bool>
{
    private readonly ISystemDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>

    public UpdateRoleHandler(ISystemDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 处理更新角色请求
    /// </summary>
    /// <param name="request">更新角色命令</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>

    public async Task<bool> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        // 查询角色
        var role = await _dbContext.SystemRoles.FindAsync(request.Id, cancellationToken);

        if (role == null)
        {
            throw new BusinessException(404, "角色不存在");
        }

        // 更新角色信息
        role.Update(request.Request.Name, request.Request.Code);

        // 保存到数据库
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}



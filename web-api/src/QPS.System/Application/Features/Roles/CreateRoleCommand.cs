using QPS.System.Contracts;
using MediatR;
using QPS.System.Contracts.DTOs.Roles;
using QPS.Shared.Contracts;
using QPS.System.Domain.Entities;

namespace QPS.System.Application.Features.Roles;

/// <summary>
/// 创建角色命令
/// </summary>
public class CreateRoleCommand : IRequest<bool>
{
    /// <summary>
    /// 创建角色请求
    /// </summary>
    public RoleCreateRequest Request { get; set; }
}

/// <summary>
/// 创建角色处理器
/// </summary>
public class CreateRoleHandler : IRequestHandler<CreateRoleCommand, bool>
{
    private readonly ISystemDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>

    public CreateRoleHandler(ISystemDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 处理创建角色请求
    /// </summary>
    /// <param name="request">创建角色命令</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>

    public async Task<bool> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = new SystemRole(request.Request.Name, request.Request.Code);

        // 保存到数据库
        _dbContext.SystemRoles.Add(role);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}



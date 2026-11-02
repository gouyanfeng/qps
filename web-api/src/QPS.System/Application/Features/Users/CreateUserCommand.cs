using QPS.System.Contracts;
using MediatR;
using QPS.System.Contracts.DTOs.Users;
using QPS.Shared.Contracts;
using QPS.System.Domain.Entities;

namespace QPS.System.Application.Features.Users;

/// <summary>
/// 创建用户命令
/// </summary>
public class CreateUserCommand : IRequest<bool>
{
    /// <summary>
    /// 创建用户请求
    /// </summary>
    public UserCreateRequest Request { get; set; }
}

/// <summary>
/// 创建用户处理器
/// </summary>
public class CreateUserHandler : IRequestHandler<CreateUserCommand, bool>
{
    private readonly ISystemDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>

    public CreateUserHandler(ISystemDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 处理创建用户请求
    /// </summary>
    /// <param name="request">创建用户命令</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>

    public async Task<bool> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // 创建用户（注意：这里应该对密码进行哈希处理，为了测试方便，暂时直接使用明文）
        var user = SystemUser.Create(
            request.Request.Username,
            request.Request.Password,
            request.Request.RealName,
            request.Request.RoleId
        );

        // 保存到数据库
        _dbContext.SystemUsers.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}



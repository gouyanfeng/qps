using QPS.System.Contracts;
using MediatR;
using QPS.System.Contracts.DTOs.Users;
using QPS.Shared.Contracts;
using QPS.Shared.Exceptions;

namespace QPS.System.Application.Features.Users;

/// <summary>
/// 获取用户详情查询
/// </summary>
public class GetUserQuery : IRequest<UserDto>
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid Id { get; set; }
}

/// <summary>
/// 获取用户详情处理器
/// </summary>
public class GetUserHandler : IRequestHandler<GetUserQuery, UserDto>
{
    private readonly ISystemDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>

    public GetUserHandler(ISystemDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 处理获取用户详情请求
    /// </summary>
    /// <param name="request">获取用户详情查询</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户DTO</returns>

    public async Task<UserDto> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        // 查询用户
        var user = await _dbContext.SystemUsers.FindAsync(request.Id, cancellationToken);

        if (user == null)
        {
            throw new BusinessException(404, "用户不存在");
        }

        // 转换为DTO
        return new UserDto
        {
            Id = user.Id,
            RoleId = user.RoleId,
            Username = user.Username,
            RealName = user.RealName,
            IsActive = user.IsActive
        };
    }
}



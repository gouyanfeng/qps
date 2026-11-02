using QPS.Shared.Common;
using QPS.System.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.System.Contracts.DTOs.Users;
using QPS.Shared.Contracts;
using QPS.Shared.Extensions;

namespace QPS.System.Application.Features.Users;

/// <summary>
/// 获取用户列表查询
/// </summary>
public class GetUsersQuery : PaginationRequest, IRequest<PaginationResponse<UserDto>>
{
    /// <summary>
    /// 用户名
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// 真实姓名
    /// </summary>
    public string? RealName { get; set; }

    /// <summary>
    /// 是否激活
    /// </summary>
    public bool? IsActive { get; set; }
}

/// <summary>
/// 获取用户列表处理器
/// </summary>
public class GetUsersHandler : IRequestHandler<GetUsersQuery, PaginationResponse<UserDto>>
{
    private readonly ISystemDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>

    public GetUsersHandler(ISystemDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 处理获取用户列表请求
    /// </summary>
    /// <param name="request">获取用户列表查询</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户DTO分页响应</returns>

    public async Task<PaginationResponse<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var query = from a in _dbContext.SystemUsers.AsNoTracking()
                    join b in _dbContext.SystemRoles.AsNoTracking() on a.RoleId equals b.Id into ab
                    from b in ab.DefaultIfEmpty()
                    select new
                    {
                        Id = a.Id,
                        RoleId = a.RoleId,
                        Username = a.Username,
                        RealName = a.RealName,
                        IsActive = a.IsActive,
                        RoleName = b != null ? b.Name : null
                    };

        // 应用查询条件
        if (!string.IsNullOrEmpty(request.Username))
        {
            query = query.Where(u => u.Username.Contains(request.Username));
        }

        if (!string.IsNullOrEmpty(request.RealName))
        {
            query = query.Where(u => u.RealName.Contains(request.RealName));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(u => u.IsActive == request.IsActive.Value);
        }

        // 转换为DTO
        var dtoQuery = query.Select(u => new UserDto
        {
            Id = u.Id,
            RoleId = u.RoleId,
            Username = u.Username,
            RealName = u.RealName,
            IsActive = u.IsActive,
            RoleName = u.RoleName
        });

        // 执行分页查询
        return await dtoQuery.ToPaginationResponseAsync(request);
    }
}



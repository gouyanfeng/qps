using QPS.System.Contracts;
using MediatR;
using QPS.System.Application.Services;
using QPS.System.Contracts.DTOs.Messages;
using QPS.Shared.Contracts;

namespace QPS.System.Application.Features.Messages;

/// <summary>
/// 未读数：广播（可见窗口内 - 已读/已隐藏）+ 私信（状态行未读）。
/// 口径统一收敛在 MessageUnreadCounter，实时推送复用同一份计算。
/// </summary>
public record GetUnreadCountQuery : IRequest<UnreadCountDto>
{
}

public class GetUnreadCountQueryHandler : IRequestHandler<GetUnreadCountQuery, UnreadCountDto>
{
    private readonly ISystemDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public GetUnreadCountQueryHandler(ISystemDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public Task<UnreadCountDto> Handle(GetUnreadCountQuery request, CancellationToken cancellationToken)
    {
        Guid.TryParse(_currentUserService.UserId, out var userId);
        return MessageUnreadCounter.CountAsync(_dbContext, userId, cancellationToken);
    }
}

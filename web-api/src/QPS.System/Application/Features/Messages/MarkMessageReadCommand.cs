using QPS.System.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.System.Application.Services;
using QPS.System.Domain.Entities;
using QPS.System.Realtime;
using QPS.Shared.Contracts;
using QPS.Shared.Exceptions;

namespace QPS.System.Application.Features.Messages;

/// <summary>标记已读（懒写：广播首次读才插状态行；软删行恢复后置已读）</summary>
public record MarkMessageReadCommand : IRequest<bool>
{
    public Guid MessageId { get; set; }
}

public class MarkMessageReadCommandHandler : IRequestHandler<MarkMessageReadCommand, bool>
{
    private readonly ISystemDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISystemMessageNotifier _notifier;

    public MarkMessageReadCommandHandler(
        ISystemDbContext dbContext,
        ICurrentUserService currentUserService,
        ISystemMessageNotifier notifier)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _notifier = notifier;
    }

    public async Task<bool> Handle(MarkMessageReadCommand request, CancellationToken cancellationToken)
    {
        Guid.TryParse(_currentUserService.UserId, out var userId);
        if (userId == Guid.Empty)
        {
            throw new BusinessException(401, "未获取到当前用户。");
        }

        var message = await _dbContext.SystemMessages
            .FirstOrDefaultAsync(item => item.Id == request.MessageId, cancellationToken)
            ?? throw new BusinessException(404, "消息不存在。");

        var visible = message.MessageType == SystemMessageType.Direct
            ? await _dbContext.SystemMessageRecipients
                .AnyAsync(state => state.MessageId == message.Id && state.UserId == userId, cancellationToken)
            : message.IsPublished;

        if (!visible)
        {
            throw new BusinessException(403, "该消息对当前用户不可见。");
        }

        // 含软删行：恢复 + 置已读，避免撞唯一索引 (MessageId, UserId, IsDeleted)
        var state = await _dbContext.SystemMessageRecipients.IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.MessageId == message.Id && item.UserId == userId, cancellationToken);

        if (state == null)
        {
            state = new SystemMessageRecipient(message.Id, userId, isRead: true);
            await _dbContext.SystemMessageRecipients.AddAsync(state, cancellationToken);
        }
        else
        {
            if (state.IsDeleted)
            {
                state.Restore();
            }
            if (!state.IsRead)
            {
                state.MarkRead();
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // 多端同步：同一账号的其他设备同步角标
        var unread = await MessageUnreadCounter.CountAsync(_dbContext, userId, cancellationToken);
        await _notifier.PushUnreadAsync(userId, unread, cancellationToken);

        return true;
    }
}

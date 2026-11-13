using QPS.System.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.System.Contracts.DTOs.Messages;
using QPS.System.Domain.Entities;
using QPS.System.Realtime;
using QPS.System.Application.Services;
using QPS.Shared.Contracts;

namespace QPS.System.Application.Features.Messages;

/// <summary>全部已读：Notices 广播补状态行；Inbox 私信批量置已读</summary>
public record MarkAllMessagesReadCommand : IRequest<bool>
{
    public MyMessageBox Box { get; set; } = MyMessageBox.Notices;
}

public class MarkAllMessagesReadCommandHandler : IRequestHandler<MarkAllMessagesReadCommand, bool>
{
    private readonly ISystemDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISystemMessageNotifier _notifier;

    public MarkAllMessagesReadCommandHandler(
        ISystemDbContext dbContext,
        ICurrentUserService currentUserService,
        ISystemMessageNotifier notifier)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _notifier = notifier;
    }

    public async Task<bool> Handle(MarkAllMessagesReadCommand request, CancellationToken cancellationToken)
    {
        Guid.TryParse(_currentUserService.UserId, out var userId);
        if (userId == Guid.Empty)
        {
            return true;
        }

        if (request.Box == MyMessageBox.Notices)
        {
            var now = DateTime.Now;
            // 可见广播 - 已有状态行（含软删）= 还没产生过状态行的，补一行已读
            var handledIds = await _dbContext.SystemMessageRecipients.IgnoreQueryFilters()
                .Where(state => state.UserId == userId)
                .Select(state => state.MessageId)
                .ToListAsync(cancellationToken);

            var unreadBroadcastIds = await _dbContext.SystemMessages
                .Where(message => message.MessageType == SystemMessageType.Broadcast
                    && message.IsPublished
                    && (message.StartDate == null || message.StartDate <= now)
                    && (message.EndDate == null || message.EndDate >= now)
                    && !handledIds.Contains(message.Id))
                .Select(message => message.Id)
                .ToListAsync(cancellationToken);

            foreach (var messageId in unreadBroadcastIds)
            {
                await _dbContext.SystemMessageRecipients.AddAsync(
                    new SystemMessageRecipient(messageId, userId, isRead: true), cancellationToken);
            }
        }
        else
        {
            var unreadStates = await _dbContext.SystemMessageRecipients
                .Where(state => state.UserId == userId && !state.IsRead)
                .ToListAsync(cancellationToken);

            foreach (var state in unreadStates)
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

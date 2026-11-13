using QPS.System.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.System.Contracts.DTOs.Messages;
using QPS.System.Domain.Entities;
using QPS.System.Realtime;
using QPS.Shared.Exceptions;

namespace QPS.System.Application.Features.Messages;

/// <summary>发布 / 下架公告（重新发布不清空已读状态行）</summary>
public record ChangeMessagePublishStatusCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public bool IsPublished { get; set; }
}

public class ChangeMessagePublishStatusCommandHandler : IRequestHandler<ChangeMessagePublishStatusCommand, bool>
{
    private readonly ISystemDbContext _dbContext;
    private readonly ISystemMessageNotifier _notifier;

    public ChangeMessagePublishStatusCommandHandler(ISystemDbContext dbContext, ISystemMessageNotifier notifier)
    {
        _dbContext = dbContext;
        _notifier = notifier;
    }

    public async Task<bool> Handle(ChangeMessagePublishStatusCommand request, CancellationToken cancellationToken)
    {
        var message = await _dbContext.SystemMessages
            .FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken)
            ?? throw new BusinessException(404, "消息不存在。");

        if (message.MessageType != SystemMessageType.Broadcast)
        {
            throw new BusinessException(400, "仅广播公告支持发布/下架。");
        }

        message.ChangePublishStatus(request.IsPublished);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (request.IsPublished)
        {
            var now = DateTime.Now;
            if (message.StartDate == null || message.StartDate <= now)
            {
                var senderName = await ResolveSenderNameAsync(message.SenderId, cancellationToken);
                await _notifier.PushBroadcastAsync(new MyMessageDto
                {
                    Id = message.Id,
                    Title = message.Title,
                    Content = message.Content,
                    MessageType = message.MessageType,
                    SenderName = senderName,
                    IsTop = message.IsTop,
                    IsRead = false,
                    IsHidden = false,
                    CreatedAt = message.CreatedAt
                }, cancellationToken);
            }
        }
        else
        {
            // 下架：让在线客户端立刻从列表里移除
            await _notifier.PushRemovedAsync(message.Id, cancellationToken);
        }

        return true;
    }

    private async Task<string?> ResolveSenderNameAsync(Guid? senderId, CancellationToken cancellationToken)
    {
        if (senderId == null || senderId == Guid.Empty)
        {
            return null;
        }

        return await _dbContext.SystemUsers
            .Where(user => user.Id == senderId)
            .Select(user => user.RealName ?? user.Username)
            .FirstOrDefaultAsync(cancellationToken);
    }
}

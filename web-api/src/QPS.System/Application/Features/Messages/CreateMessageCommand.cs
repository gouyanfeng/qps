using QPS.System.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.System.Contracts.DTOs.Messages;
using QPS.System.Domain.Entities;
using QPS.System.Realtime;
using QPS.Shared.Contracts;
using QPS.Shared.Exceptions;

namespace QPS.System.Application.Features.Messages;

public record CreateMessageCommand : IRequest<bool>
{
    public MessageCreateRequest Request { get; set; } = new();
}

public class CreateMessageCommandHandler : IRequestHandler<CreateMessageCommand, bool>
{
    private readonly ISystemDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISystemMessageNotifier _notifier;

    public CreateMessageCommandHandler(
        ISystemDbContext dbContext,
        ICurrentUserService currentUserService,
        ISystemMessageNotifier notifier)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _notifier = notifier;
    }

    public async Task<bool> Handle(CreateMessageCommand request, CancellationToken cancellationToken)
    {
        var title = request.Request.Title?.Trim();
        var content = request.Request.Content?.Trim();

        if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(content))
        {
            throw new BusinessException(400, "标题和内容不能为空。");
        }

        Guid.TryParse(_currentUserService.UserId, out var senderId);
        var senderName = await ResolveSenderNameAsync(senderId, cancellationToken);

        // 无收件人 = 广播公告；有收件人 = 点对点私信
        if (request.Request.RecipientIds == null || request.Request.RecipientIds.Count == 0)
        {
            var broadcast = SystemMessage.CreateBroadcast(
                title, content, senderId,
                request.Request.IsTop, request.Request.SortOrder,
                request.Request.StartDate, request.Request.EndDate,
                request.Request.IsPublished);

            await _dbContext.SystemMessages.AddAsync(broadcast, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // 仅「已发布且在展示窗口内」才推送；草稿/定时未到点不打扰用户
            var now = DateTime.Now;
            if (broadcast.IsPublished
                && (broadcast.StartDate == null || broadcast.StartDate <= now)
                && (broadcast.EndDate == null || broadcast.EndDate >= now))
            {
                await _notifier.PushBroadcastAsync(ToMyMessageDto(broadcast, senderName), cancellationToken);
            }

            return true;
        }

        var recipientIds = request.Request.RecipientIds.Distinct().ToList();
        if (senderId == Guid.Empty)
        {
            throw new BusinessException(401, "未获取到当前用户，无法发送私信。");
        }

        var existsCount = await _dbContext.SystemUsers
            .CountAsync(user => recipientIds.Contains(user.Id), cancellationToken);
        if (existsCount != recipientIds.Count)
        {
            throw new BusinessException(400, "部分收件用户不存在。");
        }

        var message = SystemMessage.CreateDirect(title, content, senderId);
        var actualRecipients = recipientIds.Where(id => id != senderId).ToList();
        if (actualRecipients.Count == 0)
        {
            throw new BusinessException(400, "收件人不能只有自己，无法发送私信。");
        }

        foreach (var recipientId in actualRecipients)
        {
            message.Recipients.Add(new SystemMessageRecipient(message.Id, recipientId));
        }

        await _dbContext.SystemMessages.AddAsync(message, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 自己发给自己时不推（发送者收件箱里不会有这条）
        var pushTargets = message.Recipients.Select(state => state.UserId).Distinct().ToList();
        await _notifier.PushDirectAsync(pushTargets, ToMyMessageDto(message, senderName), cancellationToken);

        return true;
    }

    private async Task<string?> ResolveSenderNameAsync(Guid senderId, CancellationToken cancellationToken)
    {
        if (senderId == Guid.Empty)
        {
            return null;
        }

        return await _dbContext.SystemUsers
            .Where(user => user.Id == senderId)
            .Select(user => user.RealName ?? user.Username)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static MyMessageDto ToMyMessageDto(SystemMessage message, string? senderName)
    {
        return new MyMessageDto
        {
            Id = message.Id,
            Title = message.Title,
            Content = message.Content,
            MessageType = message.MessageType,
            SenderName = senderName,
            IsTop = message.IsTop,
            IsRead = false,
            IsHidden = false,
            CreatedAt = message.CreatedAt,
            ReadAt = null
        };
    }
}

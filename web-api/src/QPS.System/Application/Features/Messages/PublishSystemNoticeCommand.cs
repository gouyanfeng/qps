using MediatR;
using QPS.Shared.Contracts;
using QPS.Shared.Exceptions;
using QPS.System.Contracts;
using QPS.System.Contracts.DTOs.Messages;
using QPS.System.Domain.Entities;
using QPS.System.Realtime;

namespace QPS.System.Application.Features.Messages;

/// <summary>
/// 系统任务发布广播公告（定时任务等无 HTTP 上下文的场景）。
/// 实现 IAllowAnonymousRequest：Hangfire 后台没有登录用户，必须跳过 PermissionBehaviour 的
/// 登录校验，否则任务会以「登录状态无效」反复失败。
/// 注意：仅供系统内部任务调用，HTTP 接口请走 CreateMessageCommand（受权限管道约束）。
/// </summary>
public record PublishSystemNoticeCommand : IRequest<bool>, IAllowAnonymousRequest
{
    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    /// <summary>广播：置顶</summary>
    public bool IsTop { get; set; }

    /// <summary>广播：排序权重，小的在前</summary>
    public int SortOrder { get; set; }
}

public class PublishSystemNoticeCommandHandler : IRequestHandler<PublishSystemNoticeCommand, bool>
{
    private readonly ISystemDbContext _dbContext;
    private readonly ISystemMessageNotifier _notifier;

    public PublishSystemNoticeCommandHandler(ISystemDbContext dbContext, ISystemMessageNotifier notifier)
    {
        _dbContext = dbContext;
        _notifier = notifier;
    }

    public async Task<bool> Handle(PublishSystemNoticeCommand request, CancellationToken cancellationToken)
    {
        var title = request.Title?.Trim();
        var content = request.Content?.Trim();

        if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(content))
        {
            throw new BusinessException(400, "标题和内容不能为空。");
        }

        // SenderId 为空 = 系统消息（后台列表发布人显示 "-"）
        var broadcast = SystemMessage.CreateBroadcast(
            title, content, null,
            request.IsTop, request.SortOrder,
            null, null,
            isPublished: true);

        await _dbContext.SystemMessages.AddAsync(broadcast, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _notifier.PushBroadcastAsync(new MyMessageDto
        {
            Id = broadcast.Id,
            Title = broadcast.Title,
            Content = broadcast.Content,
            MessageType = broadcast.MessageType,
            SenderName = null,
            IsTop = broadcast.IsTop,
            IsRead = false,
            IsHidden = false,
            CreatedAt = broadcast.CreatedAt,
            ReadAt = null
        }, cancellationToken);

        return true;
    }
}

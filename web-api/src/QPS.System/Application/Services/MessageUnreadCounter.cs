using Microsoft.EntityFrameworkCore;
using QPS.System.Contracts;
using QPS.System.Contracts.DTOs.Messages;
using QPS.System.Domain.Entities;

namespace QPS.System.Application.Services;

/// <summary>
/// 未读数计算（查询与实时推送共用同一份口径，避免两处逻辑漂移）：
/// 通知 = 可见窗口内的已发布广播 − 我已读/已隐藏的条数；消息 = 我状态行里的未读私信。
/// 注意：EF Core 的 IgnoreQueryFilters 作用于整个查询（含子查询中的所有实体），
/// 因此"可见消息"与"我的状态行"必须拆成独立查询，避免统计口径互相污染。
/// </summary>
public static class MessageUnreadCounter
{
    public static async Task<UnreadCountDto> CountAsync(
        ISystemDbContext dbContext,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var result = new UnreadCountDto();
        if (userId == Guid.Empty)
        {
            return result;
        }

        var now = DateTime.Now;

        // 可见广播 Id（全局软删过滤器自动生效）
        var visibleIds = await dbContext.SystemMessages
            .Where(message => message.MessageType == SystemMessageType.Broadcast
                && message.IsPublished
                && (message.StartDate == null || message.StartDate <= now)
                && (message.EndDate == null || message.EndDate >= now))
            .Select(message => message.Id)
            .ToListAsync(cancellationToken);

        // 我对广播的处理状态行（独立单表查询，含软删：读过或隐藏过都算"已处理"），
        // 与上面已物化的可见消息集合取交集，已删公告的状态行不会误伤未读数
        var handledCount = await dbContext.SystemMessageRecipients.IgnoreQueryFilters()
            .Where(state => state.UserId == userId
                && (state.IsRead || state.IsDeleted)
                && visibleIds.Contains(state.MessageId))
            .CountAsync(cancellationToken);

        result.Notices = Math.Max(0, visibleIds.Count - handledCount);
        result.Inbox = await dbContext.SystemMessageRecipients
            .Where(state => state.UserId == userId && !state.IsRead)
            .CountAsync(cancellationToken);
        result.Total = result.Notices + result.Inbox;

        return result;
    }
}

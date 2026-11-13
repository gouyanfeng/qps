using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Shared.Common;
using QPS.Shared.Contracts;
using QPS.System.Contracts;
using QPS.System.Contracts.DTOs.Messages;
using QPS.System.Domain.Entities;

namespace QPS.System.Application.Features.Messages;

/// <summary>
/// 当前用户的消息列表（分 Tab 不混合）：
/// Notices = 可见广播公告（已发布且在展示窗口内，含已读状态，隐藏的除外）；
/// Inbox = 发给我的私信。
/// 注意：EF Core 的 IgnoreQueryFilters 作用于整个查询（含 join 与子查询中的所有实体），
/// 因此"消息主体"与"状态行"必须拆成独立查询，避免状态行查软删时把消息本身的软删过滤也禁用。
/// </summary>
public record GetMyMessagesQuery : IRequest<PaginationResponse<MyMessageDto>>
{
    public MyMessageBox Box { get; set; } = MyMessageBox.Notices;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public bool UnreadOnly { get; set; }
}

public class GetMyMessagesQueryHandler : IRequestHandler<GetMyMessagesQuery, PaginationResponse<MyMessageDto>>
{
    private readonly ISystemDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public GetMyMessagesQueryHandler(ISystemDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<PaginationResponse<MyMessageDto>> Handle(GetMyMessagesQuery request, CancellationToken cancellationToken)
    {
        Guid.TryParse(_currentUserService.UserId, out var userId);
        var now = DateTime.Now;

        if (userId == Guid.Empty)
        {
            return new PaginationResponse<MyMessageDto>(new List<MyMessageDto>(), 0, request.Page, request.PageSize);
        }

        return request.Box == MyMessageBox.Notices
            ? await QueryNoticesAsync(userId, now, request, cancellationToken)
            : await QueryInboxAsync(userId, request, cancellationToken);
    }

    /// <summary>广播：主体消息（带软删过滤）与状态行（独立查询、含软删）分开取，内存组装。</summary>
    private async Task<PaginationResponse<MyMessageDto>> QueryNoticesAsync(
        Guid userId, DateTime now, GetMyMessagesQuery request, CancellationToken cancellationToken)
    {
        var source = _dbContext.SystemMessages
            .Where(message => message.MessageType == SystemMessageType.Broadcast
                && message.IsPublished
                && (message.StartDate == null || message.StartDate <= now)
                && (message.EndDate == null || message.EndDate >= now))
            .OrderByDescending(message => message.IsTop)
            .ThenByDescending(message => message.CreatedAt);

        var totalCount = await source.CountAsync(cancellationToken);
        var messages = await source
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);
        var messageIds = messages.Select(message => message.Id).ToList();

        // 状态行独立查询（IgnoreQueryFilters 只影响这一次单表查询），用于识别已读/已隐藏
        var states = await _dbContext.SystemMessageRecipients.IgnoreQueryFilters()
            .Where(state => state.UserId == userId && messageIds.Contains(state.MessageId))
            .ToDictionaryAsync(state => state.MessageId, cancellationToken);

        var items = messages
            .Select(message =>
            {
                states.TryGetValue(message.Id, out var state);
                return new MyMessageDto
                {
                    Id = message.Id,
                    Title = message.Title,
                    Content = message.Content,
                    MessageType = message.MessageType,
                    IsTop = message.IsTop,
                    IsRead = state != null && state.IsRead,
                    IsHidden = state != null && state.IsDeleted,
                    CreatedAt = message.CreatedAt,
                    ReadAt = state?.ReadAt
                };
            })
            .Where(dto => !dto.IsHidden && (!request.UnreadOnly || !dto.IsRead))
            .ToList();

        // 隐藏/已读过滤发生在内存阶段，仅影响当页条目数，total 保持可见消息口径
        return new PaginationResponse<MyMessageDto>(items, totalCount, request.Page, request.PageSize);
    }

    /// <summary>私信：主体在状态行（软删行被全局过滤器排除 = 从收件箱消失）。</summary>
    private async Task<PaginationResponse<MyMessageDto>> QueryInboxAsync(
        Guid userId, GetMyMessagesQuery request, CancellationToken cancellationToken)
    {
        var source = from state in _dbContext.SystemMessageRecipients
                     join message in _dbContext.SystemMessages on state.MessageId equals message.Id
                     join sender in _dbContext.SystemUsers on message.SenderId equals sender.Id into senderJoin
                     from sender in senderJoin.DefaultIfEmpty()
                     where state.UserId == userId
                         && message.MessageType == SystemMessageType.Direct
                         && (!request.UnreadOnly || !state.IsRead)
                     orderby message.CreatedAt descending
                     select new MyMessageDto
                     {
                         Id = message.Id,
                         Title = message.Title,
                         Content = message.Content,
                         MessageType = message.MessageType,
                         SenderName = sender != null ? (sender.RealName ?? sender.Username) : null,
                         IsTop = false,
                         IsRead = state.IsRead,
                         IsHidden = false,
                         CreatedAt = message.CreatedAt,
                         ReadAt = state.ReadAt
                     };

        var totalCount = await source.CountAsync(cancellationToken);
        var items = await source
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);
        return new PaginationResponse<MyMessageDto>(items, totalCount, request.Page, request.PageSize);
    }
}

using QPS.System.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.System.Contracts.DTOs.Messages;
using QPS.Shared.Common;

namespace QPS.System.Application.Features.Messages;

public record GetMessagesQuery : IRequest<PaginationResponse<MessageDto>>
{
    public MessageQueryRequest Request { get; set; } = new();
}

public class GetMessagesQueryHandler : IRequestHandler<GetMessagesQuery, PaginationResponse<MessageDto>>
{
    private readonly ISystemDbContext _dbContext;

    public GetMessagesQueryHandler(ISystemDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginationResponse<MessageDto>> Handle(GetMessagesQuery request, CancellationToken cancellationToken)
    {
        var query = request.Request;
        var source = from message in _dbContext.SystemMessages
                     join sender in _dbContext.SystemUsers on message.SenderId equals sender.Id into senderJoin
                     from sender in senderJoin.DefaultIfEmpty()
                     // 关键字条件必须自带括号：&& 优先级高于 ||，缺括号会让后面所有筛选条件失效
                     where (string.IsNullOrEmpty(query.Keyword) || message.Title.Contains(query.Keyword))
                         && (query.MessageType == null || message.MessageType == query.MessageType)
                         && (query.IsPublished == null || message.IsPublished == query.IsPublished)
                     orderby message.IsTop descending, message.SortOrder, message.CreatedAt descending
                     select new MessageDto
                     {
                         Id = message.Id,
                         Title = message.Title,
                         Content = message.Content,
                         MessageType = message.MessageType,
                         SenderId = message.SenderId,
                         SenderName = sender != null ? (sender.RealName ?? sender.Username) : null,
                         IsTop = message.IsTop,
                         SortOrder = message.SortOrder,
                         StartDate = message.StartDate,
                         EndDate = message.EndDate,
                         IsPublished = message.IsPublished,
                         CreatedAt = message.CreatedAt
                     };

        var totalCount = await source.CountAsync(cancellationToken);
        var items = await source
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PaginationResponse<MessageDto>(items, totalCount, query.Page, query.PageSize);
    }
}

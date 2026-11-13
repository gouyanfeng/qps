using QPS.System.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.System.Contracts.DTOs.Messages;
using QPS.Shared.Exceptions;

namespace QPS.System.Application.Features.Messages;

public record GetMessageQuery : IRequest<MessageDto>
{
    public Guid Id { get; set; }
}

public class GetMessageQueryHandler : IRequestHandler<GetMessageQuery, MessageDto>
{
    private readonly ISystemDbContext _dbContext;

    public GetMessageQueryHandler(ISystemDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MessageDto> Handle(GetMessageQuery request, CancellationToken cancellationToken)
    {
        var message = await _dbContext.SystemMessages
            .FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken)
            ?? throw new BusinessException(404, "消息不存在。");

        string? senderName = null;
        if (message.SenderId.HasValue)
        {
            senderName = await _dbContext.SystemUsers
                .Where(user => user.Id == message.SenderId.Value)
                .Select(user => user.RealName ?? user.Username)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new MessageDto
        {
            Id = message.Id,
            Title = message.Title,
            Content = message.Content,
            MessageType = message.MessageType,
            SenderId = message.SenderId,
            SenderName = senderName,
            IsTop = message.IsTop,
            SortOrder = message.SortOrder,
            StartDate = message.StartDate,
            EndDate = message.EndDate,
            IsPublished = message.IsPublished,
            CreatedAt = message.CreatedAt
        };
    }
}

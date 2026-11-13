using QPS.System.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.System.Contracts.DTOs.Messages;
using QPS.System.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.System.Application.Features.Messages;

public record UpdateMessageCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public MessageUpdateRequest Request { get; set; } = new();
}

public class UpdateMessageCommandHandler : IRequestHandler<UpdateMessageCommand, bool>
{
    private readonly ISystemDbContext _dbContext;

    public UpdateMessageCommandHandler(ISystemDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(UpdateMessageCommand request, CancellationToken cancellationToken)
    {
        var message = await _dbContext.SystemMessages
            .FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken)
            ?? throw new BusinessException(404, "消息不存在。");

        if (message.MessageType != SystemMessageType.Broadcast)
        {
            throw new BusinessException(400, "点对点私信不支持编辑。");
        }

        var title = request.Request.Title?.Trim();
        var content = request.Request.Content?.Trim();
        if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(content))
        {
            throw new BusinessException(400, "标题和内容不能为空。");
        }

        // 编辑已发布公告不重置已读状态（重新提醒走"重新发布"流程）
        message.Update(title, content,
            request.Request.IsTop, request.Request.SortOrder,
            request.Request.StartDate, request.Request.EndDate);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}

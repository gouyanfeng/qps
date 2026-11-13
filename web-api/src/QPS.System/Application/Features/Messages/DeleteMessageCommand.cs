using QPS.System.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.System.Realtime;
using QPS.Shared.Exceptions;

namespace QPS.System.Application.Features.Messages;

public record DeleteMessageCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}

public class DeleteMessageCommandHandler : IRequestHandler<DeleteMessageCommand, bool>
{
    private readonly ISystemDbContext _dbContext;
    private readonly ISystemMessageNotifier _notifier;

    public DeleteMessageCommandHandler(ISystemDbContext dbContext, ISystemMessageNotifier notifier)
    {
        _dbContext = dbContext;
        _notifier = notifier;
    }

    public async Task<bool> Handle(DeleteMessageCommand request, CancellationToken cancellationToken)
    {
        var message = await _dbContext.SystemMessages
            .FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken)
            ?? throw new BusinessException(404, "消息不存在。");

        message.SoftDelete();
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 已删除：通知在线客户端把这条从列表移除
        await _notifier.PushRemovedAsync(message.Id, cancellationToken);

        return true;
    }
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallActivities;

public class DeleteMallActivityCommand : IRequest<bool>
{
    public Guid ActivityId { get; set; }
}

public class DeleteMallActivityHandler : IRequestHandler<DeleteMallActivityCommand, bool>
{
    private readonly IMallDbContext _dbContext;

    public DeleteMallActivityHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(DeleteMallActivityCommand command, CancellationToken cancellationToken)
    {
        var activity = await _dbContext.MallActivities
            .FirstOrDefaultAsync(item => item.Id == command.ActivityId && !item.IsDeleted, cancellationToken);

        if (activity is null)
        {
            throw new BusinessException(404, "活动不存在");
        }

        activity.ChangeStatus(false);
        activity.IsDeleted = true;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
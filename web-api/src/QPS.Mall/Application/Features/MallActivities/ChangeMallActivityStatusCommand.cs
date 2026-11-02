using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallActivities;

public class ChangeMallActivityStatusCommand : IRequest<bool>
{
    public Guid ActivityId { get; set; }

    public bool IsActive { get; set; }
}

public class ChangeMallActivityStatusHandler : IRequestHandler<ChangeMallActivityStatusCommand, bool>
{
    private readonly IMallDbContext _dbContext;

    public ChangeMallActivityStatusHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(
        ChangeMallActivityStatusCommand command,
        CancellationToken cancellationToken)
    {
        var activity = await _dbContext.MallActivities
            .FirstOrDefaultAsync(
                item => item.Id == command.ActivityId && !item.IsDeleted,
                cancellationToken);

        if (activity is null)
        {
            throw new BusinessException(404, "活动不存在");
        }

        activity.ChangeStatus(command.IsActive);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}

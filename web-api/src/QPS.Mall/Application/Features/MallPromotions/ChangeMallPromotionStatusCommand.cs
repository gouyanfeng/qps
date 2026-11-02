using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallPromotions;

public sealed class ChangeMallPromotionStatusCommand : IRequest<bool>
{
    public Guid PromotionId { get; init; }

    public bool IsActive { get; init; }
}

public sealed class ChangeMallPromotionStatusHandler : IRequestHandler<ChangeMallPromotionStatusCommand, bool>
{
    private readonly IMallDbContext _dbContext;

    public ChangeMallPromotionStatusHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(
        ChangeMallPromotionStatusCommand command,
        CancellationToken cancellationToken)
    {
        var promotion = await _dbContext.MallPromotions
            .FirstOrDefaultAsync(item => item.Id == command.PromotionId, cancellationToken);

        if (promotion is null)
        {
            throw new BusinessException(404, "促销活动不存在");
        }

        promotion.ChangeStatus(command.IsActive);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}

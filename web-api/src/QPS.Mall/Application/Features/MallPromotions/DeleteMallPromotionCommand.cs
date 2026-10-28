using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallPromotions;

public class DeleteMallPromotionCommand : IRequest<bool>
{
    public Guid PromotionId { get; set; }
}

public class DeleteMallPromotionHandler : IRequestHandler<DeleteMallPromotionCommand, bool>
{
    private readonly IMallDbContext _dbContext;

    public DeleteMallPromotionHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(DeleteMallPromotionCommand command, CancellationToken cancellationToken)
    {
        var promotion = await _dbContext.MallPromotions
            .FirstOrDefaultAsync(p => p.Id == command.PromotionId && !p.IsDeleted, cancellationToken);

        if (promotion is null)
        {
            throw new BusinessException(404, "促销活动不存在");
        }

        promotion.ChangeStatus(false);
        promotion.IsDeleted = true;

        var scopes = await _dbContext.MallPromotionScopes
            .Where(s => s.PromotionId == command.PromotionId)
            .ToListAsync(cancellationToken);
        foreach (var scope in scopes)
        {
            scope.IsDeleted = true;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
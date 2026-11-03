using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallProducts;

/// <summary>
/// 后台变更商品评价审核状态。
/// </summary>
public class ChangeMallProductReviewStatusCommand : IRequest<bool>
{
    public Guid Id { get; set; }

    public MallProductReviewAuditStatusRequest Request { get; set; } = null!;
}

public class ChangeMallProductReviewStatusHandler : IRequestHandler<ChangeMallProductReviewStatusCommand, bool>
{
    private readonly IMallDbContext _dbContext;

    public ChangeMallProductReviewStatusHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(ChangeMallProductReviewStatusCommand request, CancellationToken cancellationToken)
    {
        var review = await _dbContext.MallProductReviews
            .FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken);

        if (review is null)
        {
            throw new BusinessException(404, "评价不存在");
        }

        review.ChangeAuditStatus(request.Request.AuditStatus);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}

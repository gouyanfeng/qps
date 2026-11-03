using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallProducts;

/// <summary>
/// 后台删除商品评价（软删除）。
/// </summary>
public class DeleteMallProductReviewCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}

public class DeleteMallProductReviewHandler : IRequestHandler<DeleteMallProductReviewCommand, bool>
{
    private readonly IMallDbContext _dbContext;

    public DeleteMallProductReviewHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(DeleteMallProductReviewCommand request, CancellationToken cancellationToken)
    {
        var review = await _dbContext.MallProductReviews
            .FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken);

        if (review is null)
        {
            throw new BusinessException(404, "评价不存在");
        }

        review.IsDeleted = true;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}

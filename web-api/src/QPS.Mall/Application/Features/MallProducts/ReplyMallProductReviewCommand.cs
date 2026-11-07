using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallProducts;

/// <summary>
/// 后台回复商品评价，内容为空表示清除回复。
/// </summary>
public class ReplyMallProductReviewCommand : IRequest<bool>
{
    public Guid Id { get; set; }

    public MallProductReviewReplyRequest Request { get; set; } = null!;
}

public class ReplyMallProductReviewHandler : IRequestHandler<ReplyMallProductReviewCommand, bool>
{
    private readonly IMallDbContext _dbContext;

    public ReplyMallProductReviewHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(ReplyMallProductReviewCommand request, CancellationToken cancellationToken)
    {
        var review = await _dbContext.MallProductReviews
            .FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken);

        if (review is null)
        {
            throw new BusinessException(404, "评价不存在");
        }

        review.Reply(request.Request.ReplyContent);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}

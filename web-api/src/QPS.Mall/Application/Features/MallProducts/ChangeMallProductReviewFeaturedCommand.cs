using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallProducts;

/// <summary>
/// 后台设置评价是否在商品详情展示及排序值。
/// </summary>
public class ChangeMallProductReviewFeaturedCommand : IRequest<MallProductReviewAdminDto>
{
    public Guid Id { get; set; }

    public MallProductReviewFeaturedRequest Request { get; set; } = null!;
}

public class ChangeMallProductReviewFeaturedHandler
    : IRequestHandler<ChangeMallProductReviewFeaturedCommand, MallProductReviewAdminDto>
{
    private readonly IMallDbContext _dbContext;

    public ChangeMallProductReviewFeaturedHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MallProductReviewAdminDto> Handle(
        ChangeMallProductReviewFeaturedCommand request,
        CancellationToken cancellationToken)
    {
        var review = await _dbContext.MallProductReviews
            .FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken);

        if (review is null)
        {
            throw new BusinessException(404, "评价不存在");
        }

        review.SetFeatured(request.Request.IsFeatured, request.Request.SortOrder);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var productName = await _dbContext.MallProducts
            .AsNoTracking()
            .Where(product => product.Id == review.ProductId)
            .Select(product => product.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        return new MallProductReviewAdminDto
        {
            Id = review.Id,
            ProductId = review.ProductId,
            ProductName = productName,
            MemberId = review.MemberId,
            OrderId = review.OrderId,
            MemberNickname = review.MemberNickname,
            MemberLevelName = review.MemberLevelName,
            Rating = review.Rating,
            Content = review.Content,
            SpecText = review.SpecText,
            Tags = SplitTags(review.Tags),
            Images = SplitImages(review.Images),
            IsFeatured = review.IsFeatured,
            SortOrder = review.SortOrder,
            AuditStatus = review.AuditStatus,
            ReplyContent = review.ReplyContent,
            ReviewTime = review.ReviewTime,
            CreatedAt = review.CreatedAt
        };
    }

    private static string[] SplitTags(string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags))
        {
            return Array.Empty<string>();
        }

        return tags
            .Split(new[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(tag => tag.Trim())
            .Where(tag => tag.Length > 0)
            .Distinct()
            .ToArray();
    }

    private static string[] SplitImages(string? images)
    {
        if (string.IsNullOrWhiteSpace(images))
        {
            return Array.Empty<string>();
        }

        return images
            .Split(new[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(image => image.Trim())
            .Where(image => image.Length > 0)
            .ToArray();
    }
}

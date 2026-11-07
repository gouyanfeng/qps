using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Common;

namespace QPS.Mall.Application.Features.MallProducts;

/// <summary>
/// 后台分页查询商品评价，支持关键词、商品、评分、审核状态与标签筛选。
/// </summary>
public class GetMallProductReviewsAdminQuery : PaginationRequest, IRequest<PaginationResponse<MallProductReviewAdminDto>>
{
    public string? Keyword { get; set; }

    public Guid? ProductId { get; set; }

    public int? Rating { get; set; }

    public int? AuditStatus { get; set; }

    public string? Tag { get; set; }
}

public class GetMallProductReviewsAdminHandler
    : IRequestHandler<GetMallProductReviewsAdminQuery, PaginationResponse<MallProductReviewAdminDto>>
{
    private readonly IMallDbContext _dbContext;

    public GetMallProductReviewsAdminHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginationResponse<MallProductReviewAdminDto>> Handle(
        GetMallProductReviewsAdminQuery request,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.MallProductReviews.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(review =>
                review.MemberNickname.Contains(keyword) ||
                review.Content.Contains(keyword));
        }

        if (request.ProductId.HasValue && request.ProductId.Value != Guid.Empty)
        {
            query = query.Where(review => review.ProductId == request.ProductId.Value);
        }

        if (request.Rating.HasValue)
        {
            query = query.Where(review => review.Rating == request.Rating.Value);
        }

        if (request.AuditStatus.HasValue)
        {
            query = query.Where(review => review.AuditStatus == request.AuditStatus.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Tag))
        {
            var tag = request.Tag.Trim();
            query = query.Where(review => review.Tags.Contains(tag));
        }

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 10 : request.PageSize;

        var totalCount = await query.CountAsync(cancellationToken);

        var reviews = await query
            .OrderByDescending(review => review.CreatedAt)
            .ThenByDescending(review => review.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(review => new
            {
                review.Id,
                review.ProductId,
                review.MemberId,
                review.OrderId,
                review.MemberNickname,
                review.MemberLevelName,
                review.Rating,
                review.Content,
                review.SpecText,
                review.Tags,
                review.Images,
                review.IsFeatured,
                review.SortOrder,
                review.AuditStatus,
                review.ReplyContent,
                review.ReviewTime,
                review.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var productIds = reviews
            .Select(review => review.ProductId)
            .Distinct()
            .ToArray();

        var productNames = await _dbContext.MallProducts
            .AsNoTracking()
            .Where(product => productIds.Contains(product.Id))
            .Select(product => new { product.Id, product.Name })
            .ToDictionaryAsync(product => product.Id, product => product.Name, cancellationToken);

        var list = reviews
            .Select(review => new MallProductReviewAdminDto
            {
                Id = review.Id,
                ProductId = review.ProductId,
                ProductName = productNames.TryGetValue(review.ProductId, out var productName)
                    ? productName
                    : string.Empty,
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
            })
            .ToList();

        return new PaginationResponse<MallProductReviewAdminDto>(list, totalCount, page, pageSize);
    }

    /// <summary>
    /// 将逗号分隔的标签字符串拆分为标签数组。
    /// </summary>
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

    /// <summary>
    /// 将逗号分隔的图片路径拆分为数组。
    /// </summary>
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

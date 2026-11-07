using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallProducts;

/// <summary>
/// 查询商品评价分页数据，包含好评率与标签统计。
/// </summary>
public class GetMallProductReviewsQuery : IRequest<MallProductReviewPageDto>
{
    public Guid ProductId { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 5;

    /// <summary>
    /// 按标签筛选，为空表示不筛选。
    /// </summary>
    public string? Tag { get; set; }

    /// <summary>
    /// 仅返回后台勾选为「详情展示」的评价。
    /// </summary>
    public bool FeaturedOnly { get; set; }
}

public class GetMallProductReviewsHandler : IRequestHandler<GetMallProductReviewsQuery, MallProductReviewPageDto>
{
    private readonly IMallDbContext _dbContext;

    public GetMallProductReviewsHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MallProductReviewPageDto> Handle(
        GetMallProductReviewsQuery request,
        CancellationToken cancellationToken)
    {
        var productExists = await _dbContext.MallProducts
            .AsNoTracking()
            .AnyAsync(product => product.Id == request.ProductId, cancellationToken);

        if (!productExists)
        {
            throw new BusinessException(404, "商品不存在");
        }

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = Math.Clamp(request.PageSize, 1, 50);

        var approvedQuery = _dbContext.MallProductReviews
            .AsNoTracking()
            .Where(review =>
                review.ProductId == request.ProductId &&
                review.AuditStatus == MallProductReview.AuditStatusApproved);

        var allTotalCount = await approvedQuery.CountAsync(cancellationToken);
        var allPositiveCount = await approvedQuery.CountAsync(review => review.Rating >= 4, cancellationToken);
        var allPositiveRate = allTotalCount == 0
            ? 0m
            : Math.Round(allPositiveCount * 100m / allTotalCount, 1, MidpointRounding.AwayFromZero);

        var tagStrings = await approvedQuery
            .Select(review => review.Tags)
            .ToListAsync(cancellationToken);
        var tagStats = BuildTagStats(tagStrings);

        var query = approvedQuery;

        var tag = request.Tag?.Trim();
        if (!string.IsNullOrEmpty(tag))
        {
            query = query.Where(review => review.Tags.Contains(tag));
        }

        if (request.FeaturedOnly)
        {
            query = query.Where(review => review.IsFeatured);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var positiveCount = await query.CountAsync(review => review.Rating >= 4, cancellationToken);
        var positiveRate = totalCount == 0
            ? 0m
            : Math.Round(positiveCount * 100m / totalCount, 1, MidpointRounding.AwayFromZero);

        var orderedQuery = request.FeaturedOnly
            ? query
                .OrderBy(review => review.SortOrder)
                .ThenByDescending(review => review.ReviewTime)
                .ThenByDescending(review => review.Id)
            : query
                .OrderByDescending(review => review.ReviewTime)
                .ThenByDescending(review => review.Id);

        var rows = await orderedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(review => new
            {
                review.Id,
                review.MemberNickname,
                review.MemberLevelName,
                review.Rating,
                review.Content,
                review.SpecText,
                review.Tags,
                review.Images,
                review.ReplyContent,
                review.IsFeatured,
                review.SortOrder,
                review.ReviewTime
            })
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(row => new MallProductReviewDto
            {
                Id = row.Id,
                MemberNickname = row.MemberNickname,
                MemberLevelName = row.MemberLevelName,
                Rating = row.Rating,
                Content = row.Content,
                SpecText = row.SpecText,
                Tags = SplitTags(row.Tags),
                Images = SplitImages(row.Images),
                ReplyContent = row.ReplyContent,
                IsFeatured = row.IsFeatured,
                SortOrder = row.SortOrder,
                ReviewTime = row.ReviewTime
            })
            .ToList();

        return new MallProductReviewPageDto
        {
            Items = items,
            TotalCount = totalCount,
            PositiveRate = positiveRate,
            AllTotalCount = allTotalCount,
            AllPositiveRate = allPositiveRate,
            TagStats = tagStats
        };
    }

    /// <summary>
    /// 在内存中统计标签出现次数，按次数倒序。
    /// </summary>
    private static List<MallProductReviewTagStatDto> BuildTagStats(IEnumerable<string> tagStrings)
    {
        var counts = new Dictionary<string, int>();

        foreach (var tags in tagStrings)
        {
            foreach (var tag in SplitTags(tags))
            {
                counts[tag] = counts.TryGetValue(tag, out var count) ? count + 1 : 1;
            }
        }

        return counts
            .OrderByDescending(item => item.Value)
            .ThenBy(item => item.Key)
            .Select(item => new MallProductReviewTagStatDto
            {
                Tag = item.Key,
                Count = item.Value
            })
            .ToList();
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

using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallProducts;

/// <summary>
/// 会员对已完成订单中的商品发表评价。
/// </summary>
public class CreateMallProductReviewCommand : IRequest<MallProductReviewDto>
{
    public Guid ProductId { get; set; }

    public Guid? MemberId { get; set; }

    public MallProductReviewCreateRequest Request { get; set; } = null!;
}

public class CreateMallProductReviewHandler
    : IRequestHandler<CreateMallProductReviewCommand, MallProductReviewDto>
{
    private const int MaxTagCount = 3;
    private const int MaxTagLength = 10;
    private const int MinContentLength = 5;
    private const int MaxContentLength = 500;
    private const int MaxImageCount = 6;

    private readonly IMallDbContext _dbContext;

    public CreateMallProductReviewHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MallProductReviewDto> Handle(
        CreateMallProductReviewCommand request,
        CancellationToken cancellationToken)
    {
        if (!request.MemberId.HasValue || request.MemberId.Value == Guid.Empty)
        {
            throw new BusinessException(401, "请先登录再评价");
        }

        var memberId = request.MemberId.Value;
        var payload = request.Request;

        if (string.IsNullOrWhiteSpace(payload.OrderNo))
        {
            throw new BusinessException(400, "订单号不能为空");
        }

        var order = await _dbContext.MallOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.OrderNo == payload.OrderNo.Trim(), cancellationToken);

        if (order is null)
        {
            throw new BusinessException(404, "订单不存在");
        }

        if (order.MemberId != memberId)
        {
            throw new BusinessException(403, "无权评价该订单");
        }

        if (order.Status != MallOrderStatus.Completed)
        {
            throw new BusinessException(400, "订单完成后才能评价");
        }

        var orderItem = await _dbContext.MallOrderItems
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == payload.OrderItemId, cancellationToken);

        if (orderItem is null ||
            orderItem.OrderId != order.Id ||
            orderItem.ProductId != request.ProductId)
        {
            throw new BusinessException(400, "评价商品与订单不一致");
        }

        var exists = await _dbContext.MallProductReviews
            .AnyAsync(review => review.OrderItemId == orderItem.Id, cancellationToken);

        if (exists)
        {
            throw new BusinessException(400, "该商品已评价");
        }

        var content = payload.Content?.Trim() ?? string.Empty;
        if (content.Length < MinContentLength || content.Length > MaxContentLength)
        {
            throw new BusinessException(400, $"评价内容需在 {MinContentLength}-{MaxContentLength} 字之间");
        }

        var tags = (payload.Tags ?? Array.Empty<string>())
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct()
            .ToArray();

        if (tags.Length > MaxTagCount)
        {
            throw new BusinessException(400, "评价标签最多选择 3 个");
        }

        if (tags.Any(tag => tag.Length > MaxTagLength))
        {
            throw new BusinessException(400, "单个标签不能超过 10 个字");
        }

        var images = (payload.Images ?? Array.Empty<string>())
            .Where(image => !string.IsNullOrWhiteSpace(image))
            .Select(image => image.Trim())
            .Distinct()
            .ToArray();

        if (images.Length > MaxImageCount)
        {
            throw new BusinessException(400, "评价图片最多 6 张");
        }

        if (images.Any(image => !image.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase)))
        {
            throw new BusinessException(400, "评价图片地址不合法");
        }

        var member = await _dbContext.MallMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == memberId, cancellationToken);

        if (member is null)
        {
            throw new BusinessException(401, "会员不存在");
        }

        var levelName = string.Empty;
        if (member.LevelId.HasValue)
        {
            levelName = await _dbContext.MallMemberLevels
                .AsNoTracking()
                .Where(level => level.Id == member.LevelId.Value)
                .Select(level => level.Name)
                .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;
        }

        var review = MallProductReview.CreateFromOrder(
            request.ProductId,
            memberId,
            order.Id,
            orderItem.Id,
            member.Nickname,
            levelName,
            payload.Rating,
            content,
            orderItem.SpecName,
            tags.Length == 0 ? null : string.Join(',', tags),
            images);

        _dbContext.MallProductReviews.Add(review);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new MallProductReviewDto
        {
            Id = review.Id,
            MemberNickname = review.MemberNickname,
            MemberLevelName = review.MemberLevelName,
            Rating = review.Rating,
            Content = review.Content,
            SpecText = review.SpecText,
            Tags = tags,
            Images = images,
            ReplyContent = review.ReplyContent,
            IsFeatured = review.IsFeatured,
            SortOrder = review.SortOrder,
            ReviewTime = review.ReviewTime
        };
    }
}

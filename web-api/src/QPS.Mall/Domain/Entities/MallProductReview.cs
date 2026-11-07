using QPS.Shared.Common;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Domain.Entities;

/// <summary>
/// 商品评价（用户口碑），记录会员对商品的评分与内容。
/// </summary>
public class MallProductReview : BaseEntity
{
    /// <summary>
    /// 审核状态：待审核。
    /// </summary>
    public const int AuditStatusPending = 0;

    /// <summary>
    /// 审核状态：已通过。
    /// </summary>
    public const int AuditStatusApproved = 1;

    /// <summary>
    /// 审核状态：已隐藏。
    /// </summary>
    public const int AuditStatusHidden = 2;

    /// <summary>
    /// 所属商品。
    /// </summary>
    public Guid ProductId { get; private set; }

    /// <summary>
    /// 评价会员，历史演示数据可为空。
    /// </summary>
    public Guid? MemberId { get; private set; }

    /// <summary>
    /// 关联订单，历史演示数据可为空。
    /// </summary>
    public Guid? OrderId { get; private set; }

    /// <summary>
    /// 关联订单明细，一件商品仅允许评价一次。
    /// </summary>
    public Guid? OrderItemId { get; private set; }

    /// <summary>
    /// 会员昵称。
    /// </summary>
    public string MemberNickname { get; private set; } = string.Empty;

    /// <summary>
    /// 会员等级名称。
    /// </summary>
    public string MemberLevelName { get; private set; } = string.Empty;

    /// <summary>
    /// 评分，1-5 分。
    /// </summary>
    public int Rating { get; private set; }

    /// <summary>
    /// 评价内容。
    /// </summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>
    /// 规格快照，例如「500g 装」。
    /// </summary>
    public string SpecText { get; private set; } = string.Empty;

    /// <summary>
    /// 评价标签，逗号分隔，可为空。
    /// </summary>
    public string Tags { get; private set; } = string.Empty;

    /// <summary>
    /// 评价图片，逗号分隔的相对路径，可为空。
    /// </summary>
    public string Images { get; private set; } = string.Empty;

    /// <summary>
    /// 是否在商品详情展示。
    /// </summary>
    public bool IsFeatured { get; private set; }

    /// <summary>
    /// 详情展示排序值，越小越靠前。
    /// </summary>
    public int SortOrder { get; private set; }

    /// <summary>
    /// 业务评价时间。
    /// </summary>
    public DateTime ReviewTime { get; private set; }

    /// <summary>
    /// 审核状态（0 待审核、1 已通过、2 已隐藏）。
    /// </summary>
    public int AuditStatus { get; private set; } = AuditStatusApproved;

    /// <summary>
    /// 商家回复内容，空字符串表示未回复。
    /// </summary>
    public string ReplyContent { get; private set; } = string.Empty;

    private MallProductReview()
    {
    }

    private MallProductReview(
        Guid productId,
        string memberNickname,
        string memberLevelName,
        int rating,
        string content,
        string specText,
        string? tags,
        DateTime reviewTime)
    {
        ProductId = productId;
        MemberNickname = memberNickname;
        MemberLevelName = memberLevelName;
        Rating = rating;
        Content = content;
        SpecText = specText;
        Tags = tags ?? string.Empty;
        ReviewTime = reviewTime;
        AuditStatus = AuditStatusApproved;
        ReplyContent = string.Empty;
    }

    /// <summary>
    /// 创建商品评价。
    /// </summary>
    public static MallProductReview Create(
        Guid productId,
        string memberNickname,
        string memberLevelName,
        int rating,
        string content,
        string specText,
        string? tags,
        DateTime reviewTime)
    {
        if (rating is < 1 or > 5)
        {
            throw new BusinessException(400, "评分必须在 1-5 之间");
        }

        return new MallProductReview(
            productId,
            memberNickname,
            memberLevelName,
            rating,
            content,
            specText,
            tags,
            reviewTime);
    }

    /// <summary>
    /// 会员基于已完成订单发表评价，默认待审核。
    /// </summary>
    public static MallProductReview CreateFromOrder(
        Guid productId,
        Guid memberId,
        Guid orderId,
        Guid orderItemId,
        string memberNickname,
        string memberLevelName,
        int rating,
        string content,
        string specText,
        string? tags,
        string[]? images)
    {
        if (rating is < 1 or > 5)
        {
            throw new BusinessException(400, "评分必须在 1-5 之间");
        }

        var review = new MallProductReview(
            productId,
            memberNickname,
            memberLevelName,
            rating,
            content,
            specText,
            tags,
            DateTime.Now);

        review.MemberId = memberId;
        review.OrderId = orderId;
        review.OrderItemId = orderItemId;
        review.AuditStatus = AuditStatusPending;
        review.SetImages(images);

        return review;
    }

    /// <summary>
    /// 设置评价图片，最多 6 张。
    /// </summary>
    public void SetImages(string[]? images)
    {
        if (images is null || images.Length == 0)
        {
            Images = string.Empty;
            return;
        }

        var normalized = images
            .Where(image => !string.IsNullOrWhiteSpace(image))
            .Select(image => image.Trim())
            .Distinct()
            .ToArray();

        if (normalized.Length > 6)
        {
            throw new BusinessException(400, "评价图片最多 6 张");
        }

        if (normalized.Any(image => image.Length > 200))
        {
            throw new BusinessException(400, "评价图片地址不合法");
        }

        Images = string.Join(',', normalized);
    }

    /// <summary>
    /// 变更审核状态，仅允许待审核、已通过、已隐藏三种状态。
    /// </summary>
    public void ChangeAuditStatus(int status)
    {
        if (status is not AuditStatusPending and not AuditStatusApproved and not AuditStatusHidden)
        {
            throw new BusinessException(400, "评价审核状态不合法");
        }

        AuditStatus = status;

        if (status != AuditStatusApproved)
        {
            IsFeatured = false;
            SortOrder = 0;
        }
    }

    /// <summary>
    /// 设置是否在商品详情展示，仅已通过评价可设置。
    /// </summary>
    public void SetFeatured(bool isFeatured, int sortOrder)
    {
        if (isFeatured && AuditStatus != AuditStatusApproved)
        {
            throw new BusinessException(400, "请先审核通过再设置详情展示");
        }

        IsFeatured = isFeatured;
        SortOrder = isFeatured ? Math.Clamp(sortOrder, 0, 99) : 0;
    }

    /// <summary>
    /// 设置商家回复，传入空内容表示清除回复。
    /// </summary>
    public void Reply(string? content)
    {
        var normalized = string.IsNullOrWhiteSpace(content) ? string.Empty : content.Trim();

        if (normalized.Length > 500)
        {
            throw new BusinessException(400, "回复内容不能超过 500 字");
        }

        ReplyContent = normalized;
    }
}

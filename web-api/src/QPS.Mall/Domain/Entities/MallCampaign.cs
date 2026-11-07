using QPS.Shared.Common;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Domain.Entities;

/// <summary>
/// 活动专题类型，直接映射到数据库列 MallCampaign.Type。
/// 当前已实现商品推荐型（ProductShowcase）与领券型（Coupon）；Signup（报名）为后续期次预留，
/// 暂不提供后台配置入口，也不参与任何业务判定。
/// </summary>
public enum MallCampaignType
{
    ProductShowcase = 0,   // 商品推荐型：图文介绍 + 推荐商品组
    Signup = 1,            // 报名型（预留，本期不实现）
    Coupon = 2             // 领券型：图文介绍 + 一键领券
}

/// <summary>
/// 活动专题：面向移动端的独立专题页，包含图文介绍与推荐商品组。
/// </summary>
public class MallCampaign : BaseEntity
{
    /// <summary>活动类型（0 商品推荐 / 1 报名 / 2 领券）。</summary>
    public int Type { get; private set; }

    /// <summary>活动名称。</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>头图地址，存储相对路径（如 /uploads/activity/202611/xxx.png）。</summary>
    public string CoverImageUrl { get; private set; } = string.Empty;

    /// <summary>图文介绍富文本 HTML，图片地址同样存储相对路径。</summary>
    public string ContentHtml { get; private set; } = string.Empty;

    /// <summary>活动开始时间（含），服务器本地时间。</summary>
    public DateTime StartAt { get; private set; }

    /// <summary>活动结束时间（不含），服务器本地时间。</summary>
    public DateTime EndAt { get; private set; }

    public bool IsActive { get; private set; }

    public int SortOrder { get; private set; }

    private MallCampaign()
    {
    }

    private MallCampaign(
        MallCampaignType type,
        string name,
        string coverImageUrl,
        string contentHtml,
        DateTime startAt,
        DateTime endAt,
        bool isActive,
        int sortOrder)
    {
        Type = (int)type;
        Name = name;
        CoverImageUrl = coverImageUrl;
        ContentHtml = contentHtml;
        StartAt = startAt;
        EndAt = endAt;
        IsActive = isActive;
        SortOrder = sortOrder;
    }

    public static MallCampaign Create(
        MallCampaignType type,
        string name,
        string coverImageUrl,
        string contentHtml,
        DateTime startAt,
        DateTime endAt,
        bool isActive,
        int sortOrder)
    {
        return new MallCampaign(
            type,
            name,
            coverImageUrl,
            contentHtml,
            startAt,
            endAt,
            isActive,
            sortOrder);
    }

    public void Update(
        string name,
        string coverImageUrl,
        string contentHtml,
        DateTime startAt,
        DateTime endAt,
        int sortOrder)
    {
        Name = name;
        CoverImageUrl = coverImageUrl;
        ContentHtml = contentHtml;
        StartAt = startAt;
        EndAt = endAt;
        SortOrder = sortOrder;
    }

    public void ChangeStatus(bool isActive) => IsActive = isActive;

    /// <summary>切换活动类型（商品推荐 / 领券），报名型暂不允许。</summary>
    public void ChangeType(MallCampaignType type)
    {
        if (type == MallCampaignType.Signup)
        {
            throw new BusinessException(400, "本期暂不支持报名型活动");
        }

        Type = (int)type;
    }

    /// <summary>
    /// 时间窗判定：左闭右开 [StartAt, EndAt)，与商城中其他时间口径保持一致。
    /// </summary>
    public bool IsWithinWindow(DateTime now) => StartAt <= now && now < EndAt;

    public bool HasStarted(DateTime now) => now >= StartAt;

    public bool HasEnded(DateTime now) => now >= EndAt;
}

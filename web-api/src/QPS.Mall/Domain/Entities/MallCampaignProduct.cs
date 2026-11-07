using QPS.Shared.Common;

namespace QPS.Mall.Domain.Entities;

/// <summary>
/// 活动专题选品：记录专题内推荐的商品、所属分组以及展示顺序。
/// </summary>
public class MallCampaignProduct : BaseEntity
{
    public Guid CampaignId { get; private set; }

    public Guid ProductId { get; private set; }

    /// <summary>分组名，为空表示不分组（移动端归入同一组展示）。</summary>
    public string GroupName { get; private set; } = string.Empty;

    public int SortOrder { get; private set; }

    private MallCampaignProduct()
    {
    }

    private MallCampaignProduct(Guid campaignId, Guid productId, string groupName, int sortOrder)
    {
        CampaignId = campaignId;
        ProductId = productId;
        GroupName = groupName;
        SortOrder = sortOrder;
    }

    public static MallCampaignProduct Create(Guid campaignId, Guid productId, string groupName, int sortOrder)
    {
        return new MallCampaignProduct(campaignId, productId, groupName, sortOrder);
    }

    public void Update(string groupName, int sortOrder)
    {
        GroupName = groupName;
        SortOrder = sortOrder;
    }
}

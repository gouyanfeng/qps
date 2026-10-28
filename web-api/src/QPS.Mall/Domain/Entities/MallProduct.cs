using QPS.Shared.Common;

namespace QPS.Mall.Domain.Entities;

/// <summary>
/// 商场商品。
/// </summary>
public class MallProduct : BaseEntity
{
    /// <summary>
    /// 商品编码，唯一。
    /// </summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>
    /// 商品名称。
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// 商品描述。
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// 所属分类，可为空。
    /// </summary>
    public Guid? CategoryId { get; private set; }

    /// <summary>
    /// 主图地址。
    /// </summary>
    public string MainImageUrl { get; private set; } = string.Empty;

    /// <summary>
    /// 相册图片地址，以 JSON 数组存储。
    /// </summary>
    public string AlbumImages { get; private set; } = string.Empty;

    /// <summary>
    /// 商品详情 HTML。
    /// </summary>
    public string DetailHtml { get; private set; } = string.Empty;

    /// <summary>
    /// 排序值，越小越靠前。
    /// </summary>
    public int SortOrder { get; private set; }

    /// <summary>
    /// 是否上架。
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// 是否热销。
    /// </summary>
    public bool IsHot { get; private set; }

    /// <summary>
    /// 累计销量。
    /// </summary>
    public int Sales { get; private set; }

    /// <summary>
    /// 运费模板ID，为空时使用门店默认模板。
    /// </summary>
    public Guid? FreightTemplateId { get; private set; }

    public ICollection<MallTag> Tags { get; private set; } = new List<MallTag>();

    private MallProduct()
    {
    }

    private MallProduct(
        string code,
        string name,
        string description,
        Guid? categoryId,
        string mainImageUrl,
        string albumImages,
        string detailHtml,
        int sortOrder,
        bool isActive,
        bool isHot,
        Guid? freightTemplateId)
    {
        Code = code;
        Name = name;
        Description = description;
        CategoryId = categoryId;
        MainImageUrl = mainImageUrl;
        AlbumImages = albumImages;
        DetailHtml = detailHtml;
        SortOrder = sortOrder;
        IsActive = isActive;
        IsHot = isHot;
        FreightTemplateId = freightTemplateId;
    }

    public static MallProduct Create(
        string code,
        string name,
        string description,
        Guid? categoryId,
        string mainImageUrl,
        string albumImages,
        string detailHtml,
        int sortOrder,
        bool isActive,
        bool isHot = false,
        Guid? freightTemplateId = null)
    {
        return new MallProduct(
            code,
            name,
            description,
            categoryId,
            mainImageUrl,
            albumImages,
            detailHtml,
            sortOrder,
            isActive,
            isHot,
            freightTemplateId);
    }

    public void Update(
        string code,
        string name,
        string description,
        Guid? categoryId,
        string mainImageUrl,
        string albumImages,
        string detailHtml,
        int sortOrder,
        bool isHot,
        Guid? freightTemplateId)
    {
        Code = code;
        Name = name;
        Description = description;
        CategoryId = categoryId;
        MainImageUrl = mainImageUrl;
        AlbumImages = albumImages;
        DetailHtml = detailHtml;
        SortOrder = sortOrder;
        IsHot = isHot;
        FreightTemplateId = freightTemplateId;
    }

    public void ChangeStatus(bool isActive)
    {
        IsActive = isActive;
    }

    public void SetHot(bool isHot)
    {
        IsHot = isHot;
    }

    public void AddSales(int amount)
    {
        if (amount > 0) Sales += amount;
    }
}
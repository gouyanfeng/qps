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
        bool isActive)
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
        bool isActive)
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
            isActive);
    }

    public void Update(
        string code,
        string name,
        string description,
        Guid? categoryId,
        string mainImageUrl,
        string albumImages,
        string detailHtml,
        int sortOrder)
    {
        Code = code;
        Name = name;
        Description = description;
        CategoryId = categoryId;
        MainImageUrl = mainImageUrl;
        AlbumImages = albumImages;
        DetailHtml = detailHtml;
        SortOrder = sortOrder;
    }

    public void ChangeStatus(bool isActive)
    {
        IsActive = isActive;
    }
}

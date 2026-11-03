using QPS.Shared.Common;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Domain.Entities;

/// <summary>
/// 商城标签类型（内置常用值，不做枚举限制，允许自由扩展）。
/// </summary>
public static class MallTagType
{
    /// <summary>
    /// 商品标签。
    /// </summary>
    public const string Product = "Product";

    /// <summary>
    /// 评价标签。
    /// </summary>
    public const string Review = "Review";

    /// <summary>
    /// 内置常用类型，供前端下拉预置。
    /// </summary>
    public static readonly string[] Common = { Product, Review };
}

public class MallTag : BaseEntity
{
    public string Type { get; private set; } = MallTagType.Product;

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string Color { get; private set; } = string.Empty;

    public int SortOrder { get; private set; }

    public bool IsActive { get; private set; }

    private MallTag()
    {
    }

    public static MallTag Create(
        string code,
        string name,
        string color,
        int sortOrder,
        bool isActive,
        string? type = null)
    {
        var normalizedType = string.IsNullOrWhiteSpace(type) ? MallTagType.Product : type.Trim();
        if (normalizedType.Length > 20)
        {
            throw new BusinessException(400, "标签类型不能超过 20 个字符");
        }

        return new MallTag
        {
            Type = normalizedType,
            Code = code,
            Name = name,
            Color = color,
            SortOrder = sortOrder,
            IsActive = isActive
        };
    }

    public void Update(string code, string name, string color, int sortOrder)
    {
        Code = code;
        Name = name;
        Color = color;
        SortOrder = sortOrder;
    }

    public void ChangeStatus(bool isActive) => IsActive = isActive;
}
using QPS.Shared.Common;

namespace QPS.Mall.Domain.Entities;

/// <summary>
/// 商场商品分类。
/// </summary>
public class MallCategory : BaseEntity
{
    /// <summary>
    /// 分类编码，唯一。
    /// </summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>
    /// 分类名称。
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// 排序值，越小越靠前。
    /// </summary>
    public int SortOrder { get; private set; }

    /// <summary>
    /// 是否启用。
    /// </summary>
    public bool IsActive { get; private set; }

    private MallCategory()
    {
    }

    private MallCategory(string code, string name, int sortOrder, bool isActive)
    {
        Code = code;
        Name = name;
        SortOrder = sortOrder;
        IsActive = isActive;
    }

    public static MallCategory Create(string code, string name, int sortOrder, bool isActive)
    {
        return new MallCategory(code, name, sortOrder, isActive);
    }

    public void Update(string code, string name, int sortOrder)
    {
        Code = code;
        Name = name;
        SortOrder = sortOrder;
    }

    public void ChangeStatus(bool isActive)
    {
        IsActive = isActive;
    }
}

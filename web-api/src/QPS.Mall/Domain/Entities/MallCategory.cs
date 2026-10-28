using QPS.Shared.Common;

namespace QPS.Mall.Domain.Entities;

public class MallCategory : BaseEntity
{
    public Guid StoreId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public Guid? ParentId { get; private set; }

    public string? Icon { get; private set; }

    public int SortOrder { get; private set; }

    public bool IsActive { get; private set; }

    private MallCategory()
    {
    }

    private MallCategory(
        Guid storeId,
        string code,
        string name,
        Guid? parentId,
        string? icon,
        int sortOrder,
        bool isActive)
    {
        StoreId = storeId;
        Code = code;
        Name = name;
        ParentId = parentId;
        Icon = icon;
        SortOrder = sortOrder;
        IsActive = isActive;
    }

    public static MallCategory Create(
        Guid storeId,
        string code,
        string name,
        Guid? parentId,
        string? icon,
        int sortOrder,
        bool isActive)
    {
        return new MallCategory(storeId, code, name, parentId, icon, sortOrder, isActive);
    }

    public void Update(string code, string name, Guid? parentId, string? icon, int sortOrder)
    {
        Code = code;
        Name = name;
        ParentId = parentId;
        Icon = icon;
        SortOrder = sortOrder;
    }

    public void ChangeStatus(bool isActive)
    {
        IsActive = isActive;
    }
}
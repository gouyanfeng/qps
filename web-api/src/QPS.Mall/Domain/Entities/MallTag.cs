using QPS.Shared.Common;

namespace QPS.Mall.Domain.Entities;

public class MallTag : BaseEntity
{
    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string Color { get; private set; } = string.Empty;

    public int SortOrder { get; private set; }

    public bool IsActive { get; private set; }

    private MallTag()
    {
    }

    public static MallTag Create(string code, string name, string color, int sortOrder, bool isActive) =>
        new()
        {
            Code = code,
            Name = name,
            Color = color,
            SortOrder = sortOrder,
            IsActive = isActive
        };

    public void Update(string code, string name, string color, int sortOrder)
    {
        Code = code;
        Name = name;
        Color = color;
        SortOrder = sortOrder;
    }

    public void ChangeStatus(bool isActive) => IsActive = isActive;
}
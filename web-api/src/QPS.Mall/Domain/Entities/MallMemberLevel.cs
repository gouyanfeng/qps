using QPS.Shared.Common;

namespace QPS.Mall.Domain.Entities;

public class MallMemberLevel : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public int LevelCode { get; private set; }
    public decimal? DiscountRate { get; private set; }
    public int MinSpent { get; private set; }
    public int MaxSpent { get; private set; }
    public int UpgradePoints { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; }

    private MallMemberLevel()
    {
    }

    private MallMemberLevel(
        string name, int levelCode,
        decimal? discountRate, int minSpent, int maxSpent,
        int upgradePoints, int sortOrder, bool isDefault)
    {
        Name = name;
        LevelCode = levelCode;
        DiscountRate = discountRate;
        MinSpent = minSpent;
        MaxSpent = maxSpent;
        UpgradePoints = upgradePoints;
        SortOrder = sortOrder;
        IsDefault = isDefault;
        IsActive = true;
    }

    public static MallMemberLevel Create(
        string name, int levelCode,
        decimal? discountRate, int minSpent, int maxSpent,
        int upgradePoints, int sortOrder, bool isDefault)
    {
        return new MallMemberLevel(name, levelCode,
            discountRate, minSpent, maxSpent, upgradePoints, sortOrder, isDefault);
    }

    public void Update(
        string name, decimal? discountRate,
        int minSpent, int maxSpent,
        int upgradePoints, int sortOrder)
    {
        Name = name;
        DiscountRate = discountRate;
        MinSpent = minSpent;
        MaxSpent = maxSpent;
        UpgradePoints = upgradePoints;
        SortOrder = sortOrder;
    }

    public void SetDefault(bool isDefault) => IsDefault = isDefault;
    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    public bool MatchesSpent(decimal spent)
    {
        var amount = (int)spent;
        return amount >= MinSpent && (MaxSpent == 0 || amount <= MaxSpent);
    }
}
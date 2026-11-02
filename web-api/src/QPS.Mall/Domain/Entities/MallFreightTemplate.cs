using QPS.Shared.Common;

namespace QPS.Mall.Domain.Entities;

public enum FreightBillingType
{
    Weight = 1,
    Quantity = 2,
    Volume = 3
}

public class MallFreightTemplate : BaseEntity
{
    public string Name { get; private set; } = string.Empty;

    public FreightBillingType BillingType { get; private set; }

    public bool IsDefault { get; private set; }

    public bool IsActive { get; private set; }

    private MallFreightTemplate()
    {
    }

    private MallFreightTemplate(
        string name,
        FreightBillingType billingType,
        bool isDefault,
        bool isActive)
    {
        Name = name;
        BillingType = billingType;
        IsDefault = isDefault;
        IsActive = isActive;
    }

    public static MallFreightTemplate Create(
        string name,
        FreightBillingType billingType,
        bool isDefault,
        bool isActive)
    {
        return new MallFreightTemplate(name, billingType, isDefault, isActive);
    }

    public void Update(string name, FreightBillingType billingType, bool isDefault, bool isActive)
    {
        Name = name;
        BillingType = billingType;
        IsDefault = isDefault;
        IsActive = isActive;
    }
}
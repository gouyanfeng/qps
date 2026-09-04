using QPS.Domain.Common;

namespace QPS.Domain.Entities.Crm;

public class CrmHerbBaseSupply : BaseEntity
{
    public const string Pending = "待确认";
    public const string Active = "有效";
    public const string Paused = "暂停";
    public const string SoldOut = "已售罄";
    public const string Expired = "已失效";

    public Guid HerbBaseId { get; private set; }
    public Guid? HerbBaseSubjectId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public decimal? AvailableQuantity { get; private set; }
    public string QuantityUnit { get; private set; } = string.Empty;
    public string Specification { get; private set; } = string.Empty;
    public string QualityRequirement { get; private set; } = string.Empty;
    public string HarvestSeason { get; private set; } = string.Empty;
    public decimal? ExpectedPrice { get; private set; }
    public string PriceUnit { get; private set; } = string.Empty;
    public string SupplyCycle { get; private set; } = string.Empty;
    public DateTime? ConfirmedAt { get; private set; }
    public DateTime? ValidUntil { get; private set; }
    public string Status { get; private set; } = Pending;
    public string Remark { get; private set; } = string.Empty;
    public virtual CrmHerbBase? HerbBase { get; private set; }

    private CrmHerbBaseSupply() { }

    public static CrmHerbBaseSupply Create(Guid herbBaseId, Guid? herbBaseSubjectId, string productName,
        decimal? availableQuantity, string quantityUnit, string specification, string qualityRequirement,
        string harvestSeason, decimal? expectedPrice, string priceUnit, string supplyCycle,
        DateTime? confirmedAt, DateTime? validUntil, string remark)
    {
        var supply = new CrmHerbBaseSupply { HerbBaseId = herbBaseId, HerbBaseSubjectId = herbBaseSubjectId };
        supply.Update(productName, availableQuantity, quantityUnit, specification, qualityRequirement, harvestSeason,
            expectedPrice, priceUnit, supplyCycle, confirmedAt, validUntil, remark);
        return supply;
    }

    public void Update(string productName, decimal? availableQuantity, string quantityUnit, string specification,
        string qualityRequirement, string harvestSeason, decimal? expectedPrice, string priceUnit, string supplyCycle,
        DateTime? confirmedAt, DateTime? validUntil, string remark)
    {
        if (Status == Expired) throw new InvalidOperationException("已失效供应信息不可编辑");
        ProductName = productName?.Trim() ?? string.Empty;
        AvailableQuantity = availableQuantity;
        QuantityUnit = quantityUnit?.Trim() ?? string.Empty;
        Specification = specification?.Trim() ?? string.Empty;
        QualityRequirement = qualityRequirement?.Trim() ?? string.Empty;
        HarvestSeason = harvestSeason?.Trim() ?? string.Empty;
        ExpectedPrice = expectedPrice;
        PriceUnit = priceUnit?.Trim() ?? string.Empty;
        SupplyCycle = supplyCycle?.Trim() ?? string.Empty;
        ConfirmedAt = confirmedAt;
        ValidUntil = validUntil;
        Remark = remark?.Trim() ?? string.Empty;
        if (Status == Active) EnsureValidForActivation();
    }

    public void ChangeStatus(string status)
    {
        status = status?.Trim() ?? string.Empty;
        if (Status == Expired) throw new InvalidOperationException("已失效供应信息不可恢复");
        if (status is not (Pending or Active or Paused or SoldOut or Expired)) throw new InvalidOperationException("供应信息状态无效");
        if (status == Active) EnsureValidForActivation();
        Status = status;
    }

    public void SyncHerbBaseSubject(Guid? herbBaseSubjectId) => HerbBaseSubjectId = herbBaseSubjectId;

    public bool IsEffectiveOn(DateTime date) => Status == Active && ValidUntil.HasValue && ValidUntil.Value.Date >= date.Date;

    private void EnsureValidForActivation()
    {
        if (string.IsNullOrWhiteSpace(ProductName) || !AvailableQuantity.HasValue || AvailableQuantity <= 0 ||
            string.IsNullOrWhiteSpace(QuantityUnit) || !ConfirmedAt.HasValue || !ValidUntil.HasValue ||
            ValidUntil.Value.Date < ConfirmedAt.Value.Date)
        {
            throw new InvalidOperationException("供应信息转为有效时必须补齐品类、可供量、单位、核实日期和有效截止日");
        }
    }
}

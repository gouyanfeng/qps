using QPS.Domain.Common;

namespace QPS.Domain.Entities.Crm;

public class CrmMatchOpportunity : BaseEntity
{
    public const string InProgress = "推进中";
    public const string Paused = "暂停";
    public const string Closed = "已关闭";

    public Guid DemandItemId { get; private set; }
    public Guid DemandId { get; private set; }
    public Guid VendorId { get; private set; }
    public string DemandProductName { get; private set; } = string.Empty;
    public decimal? DemandQuantity { get; private set; }
    public string QuantityUnit { get; private set; } = string.Empty;
    public string Specification { get; private set; } = string.Empty;
    public string QualityRequirement { get; private set; } = string.Empty;
    public DateTime? ExpectedDeliveryAt { get; private set; }
    public string DeliveryAddress { get; private set; } = string.Empty;
    public Guid? OwnerUserId { get; private set; }
    public string Status { get; private set; } = InProgress;
    public string NextAction { get; private set; } = string.Empty;
    public DateTime? NextActionAt { get; private set; }
    public string ClosedReason { get; private set; } = string.Empty;

    private CrmMatchOpportunity()
    {
    }

    public static CrmMatchOpportunity Create(
        Guid demandItemId,
        Guid demandId,
        Guid vendorId,
        string demandProductName,
        decimal? demandQuantity,
        string quantityUnit,
        string specification,
        string qualityRequirement,
        DateTime? expectedDeliveryAt,
        string deliveryAddress,
        Guid? ownerUserId)
    {
        if (demandItemId == Guid.Empty)
        {
            throw new InvalidOperationException("撮合机会需求明细不能为空");
        }

        if (demandId == Guid.Empty)
        {
            throw new InvalidOperationException("撮合机会需求不能为空");
        }

        if (vendorId == Guid.Empty)
        {
            throw new InvalidOperationException("撮合机会需求厂商不能为空");
        }

        if (string.IsNullOrWhiteSpace(demandProductName))
        {
            throw new InvalidOperationException("撮合机会需求品类不能为空");
        }

        return new CrmMatchOpportunity
        {
            DemandItemId = demandItemId,
            DemandId = demandId,
            VendorId = vendorId,
            DemandProductName = Trim(demandProductName),
            DemandQuantity = demandQuantity,
            QuantityUnit = Trim(quantityUnit),
            Specification = Trim(specification),
            QualityRequirement = Trim(qualityRequirement),
            ExpectedDeliveryAt = expectedDeliveryAt,
            DeliveryAddress = Trim(deliveryAddress),
            OwnerUserId = ownerUserId,
            Status = InProgress
        };
    }

    public void UpdateNextAction(string nextAction, DateTime? nextActionAt)
    {
        EnsureCanUpdate();
        NextAction = Trim(nextAction);
        NextActionAt = nextActionAt;
    }

    public void Pause()
    {
        if (Status != InProgress)
        {
            throw new InvalidOperationException("仅推进中的撮合机会可以暂停");
        }

        Status = Paused;
    }

    public void Resume()
    {
        if (Status != Paused)
        {
            throw new InvalidOperationException("仅暂停的撮合机会可以恢复");
        }

        Status = InProgress;
    }

    public void Close(string closedReason)
    {
        EnsureCanUpdate();

        if (string.IsNullOrWhiteSpace(closedReason))
        {
            throw new InvalidOperationException("关闭撮合机会必须填写关闭原因");
        }

        Status = Closed;
        ClosedReason = closedReason.Trim();
    }

    private void EnsureCanUpdate()
    {
        if (Status == Closed)
        {
            throw new InvalidOperationException("已关闭撮合机会不可编辑");
        }
    }

    private static string Trim(string? value) => value?.Trim() ?? string.Empty;
}

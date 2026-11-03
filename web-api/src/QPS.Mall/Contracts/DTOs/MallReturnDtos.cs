namespace QPS.Mall.Contracts.DTOs;

public class MallReturnListDto
{
    public Guid Id { get; set; }

    public string ReturnNo { get; set; } = string.Empty;

    public Guid OrderId { get; set; }

    public string OrderNo { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal RefundAmount { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}

public class MallReturnDetailDto : MallReturnListDto
{
    public string UserPhone { get; set; } = string.Empty;

    public string SkuSpec { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? RejectReason { get; set; }

    public string? TrackingCompany { get; set; }

    public string? TrackingNo { get; set; }
}
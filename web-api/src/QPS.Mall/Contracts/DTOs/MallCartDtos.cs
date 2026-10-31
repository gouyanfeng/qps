namespace QPS.Mall.Contracts.DTOs;

public class MallCartDto
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public int TotalQuantity { get; set; }

    public decimal TotalAmount { get; set; }

    public DateTime LastActiveAt { get; set; }

    public List<MallCartItemDto> Items { get; set; } = new();
}

public class MallCartItemDto
{
    public Guid Id { get; set; }

    public Guid SkuId { get; set; }

    public Guid ProductId { get; set; }

    public int Quantity { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string ProductCode { get; set; } = string.Empty;

    public string ProductImage { get; set; } = string.Empty;

    public string SkuCode { get; set; } = string.Empty;

    public string SpecName { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int Stock { get; set; }

    public int WeightGram { get; set; }

    public int AvailableStock { get; set; }

    public decimal LineAmount { get; set; }

    public bool IsValid { get; set; } = true;

    public string? InvalidReason { get; set; }
}

public class MallCartAddRequest
{
    public Guid UserId { get; set; }

    public Guid SkuId { get; set; }

    public int Quantity { get; set; }
}

public class MallCartUpdateItemRequest
{
    public Guid UserId { get; set; }

    public Guid SkuId { get; set; }

    public int Quantity { get; set; }
}
using QPS.Shared.Common;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Domain.Entities;

public class MallCart : BaseEntity
{
    public Guid UserId { get; private set; }

    public DateTime LastActiveAt { get; private set; }

    private MallCart()
    {
    }

    private MallCart(Guid userId)
    {
        UserId = userId;
        LastActiveAt = DateTime.UtcNow;
    }

    public static MallCart Create(Guid userId)
    {
        return new MallCart(userId);
    }

    public void Touch()
    {
        LastActiveAt = DateTime.UtcNow;
    }
}

public class MallCartItem : BaseEntity
{
    public Guid CartId { get; private set; }

    public Guid SkuId { get; private set; }

    public Guid ProductId { get; private set; }

    public int Quantity { get; private set; }

    private MallCartItem()
    {
    }

    private MallCartItem(Guid cartId, Guid skuId, Guid productId, int quantity)
    {
        CartId = cartId;
        SkuId = skuId;
        ProductId = productId;
        Quantity = quantity;
    }

    public static MallCartItem Create(Guid cartId, Guid skuId, Guid productId, int quantity)
    {
        return new MallCartItem(cartId, skuId, productId, quantity);
    }

    public void SetQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new BusinessException(400, "数量必须大于 0");
        }

        Quantity = quantity;
    }

    public void AddQuantity(int delta)
    {
        if (delta <= 0)
        {
            throw new BusinessException(400, "增量必须大于 0");
        }

        Quantity += delta;
    }
}
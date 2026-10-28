using QPS.Shared.Common;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Domain.Entities;

public class MallCart : BaseEntity
{
    public Guid UserId { get; private set; }

    public Guid StoreId { get; private set; }

    public DateTime LastActiveAt { get; private set; }

    public List<MallCartItem> Items { get; private set; } = new();

    private MallCart()
    {
    }

    private MallCart(Guid userId, Guid storeId)
    {
        UserId = userId;
        StoreId = storeId;
        LastActiveAt = DateTime.UtcNow;
    }

    public static MallCart Create(Guid userId, Guid storeId)
    {
        return new MallCart(userId, storeId);
    }

    public void Touch()
    {
        LastActiveAt = DateTime.UtcNow;
    }

    public void AddItem(Guid skuId, Guid productId, int quantity)
    {
        if (quantity <= 0)
        {
            throw new BusinessException(400, "数量必须大于 0");
        }

        var existing = Items.FirstOrDefault(i => i.SkuId == skuId);
        if (existing is not null)
        {
            existing.AddQuantity(quantity);
        }
        else
        {
            Items.Add(MallCartItem.Create(Id, skuId, productId, quantity));
        }

        Touch();
    }

    public void UpdateQuantity(Guid skuId, int quantity)
    {
        var item = Items.FirstOrDefault(i => i.SkuId == skuId);
        if (item is null)
        {
            throw new BusinessException(400, "购物车中没有该商品");
        }

        if (quantity <= 0)
        {
            Items.Remove(item);
        }
        else
        {
            item.SetQuantity(quantity);
        }

        Touch();
    }

    public void RemoveItem(Guid skuId)
    {
        var item = Items.FirstOrDefault(i => i.SkuId == skuId);
        if (item is not null)
        {
            Items.Remove(item);
            Touch();
        }
    }

    public void Clear()
    {
        Items.Clear();
        Touch();
    }

    public int TotalQuantity() => Items.Sum(i => i.Quantity);
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
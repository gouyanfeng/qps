using QPS.Shared.Common;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Domain.Entities;

/// <summary>
/// 购物车明细：直接按会员归属，不再有购物车车头；无明细行即视为空购物车。
/// </summary>
public class MallCartItem : BaseEntity
{
    public Guid UserId { get; private set; }

    public Guid SkuId { get; private set; }

    public Guid ProductId { get; private set; }

    public int Quantity { get; private set; }

    private MallCartItem()
    {
    }

    private MallCartItem(Guid userId, Guid skuId, Guid productId, int quantity)
    {
        UserId = userId;
        SkuId = skuId;
        ProductId = productId;
        Quantity = quantity;
    }

    public static MallCartItem Create(Guid userId, Guid skuId, Guid productId, int quantity)
    {
        return new MallCartItem(userId, skuId, productId, quantity);
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

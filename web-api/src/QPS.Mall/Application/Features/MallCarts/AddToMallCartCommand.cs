using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;
using System.Text;

namespace QPS.Mall.Application.Features.MallCarts;

public class AddToMallCartCommand : IRequest<MallCartDto>
{
    public MallCartAddRequest Request { get; set; } = null!;
}

public class AddToMallCartHandler : IRequestHandler<AddToMallCartCommand, MallCartDto>
{
    private readonly IMallDbContext _dbContext;
    private readonly GetMallCartHandler _getCartHandler;

    public AddToMallCartHandler(IMallDbContext dbContext, GetMallCartHandler getCartHandler)
    {
        _dbContext = dbContext;
        _getCartHandler = getCartHandler;
    }

    public async Task<MallCartDto> Handle(AddToMallCartCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;

        if (req.Quantity <= 0) throw new BusinessException(400, "数量必须大于 0");

        var skuRow = await (
                from sku in _dbContext.MallSkus
                join product in _dbContext.MallProducts on sku.ProductId equals product.Id
                where sku.Id == req.SkuId && !sku.IsDeleted && !product.IsDeleted
                select new { Sku = sku, Product = product })
            .FirstOrDefaultAsync(cancellationToken);

        if (skuRow is null) throw new BusinessException(404, "商品规格不存在或已下架");
        if (!skuRow.Sku.IsActive || !skuRow.Product.IsActive)
            throw new BusinessException(400, "商品已下架");
        if (skuRow.Sku.Stock < req.Quantity)
            throw new BusinessException(400, $"库存不足，仅剩 {skuRow.Sku.Stock}");

        var cart = await _dbContext.MallCarts
            .FirstOrDefaultAsync(
                c => c.UserId == req.UserId && !c.IsDeleted,
                cancellationToken);

        if (cart is null)
        {
            cart = MallCart.Create(req.UserId);
            _dbContext.MallCarts.Add(cart);
        }

        var existingItem = await _dbContext.MallCartItems
            .FirstOrDefaultAsync(i => i.CartId == cart.Id && i.SkuId == req.SkuId, cancellationToken);
        if (existingItem is not null)
        {
            existingItem.AddQuantity(req.Quantity);
        }
        else
        {
            var newItem = MallCartItem.Create(cart.Id, req.SkuId, skuRow.Product.Id, req.Quantity);
            _dbContext.MallCartItems.Add(newItem);
        }

        cart.Touch();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await _getCartHandler.Handle(new GetMallCartQuery
        {
            UserId = req.UserId
        }, cancellationToken);
    }
}
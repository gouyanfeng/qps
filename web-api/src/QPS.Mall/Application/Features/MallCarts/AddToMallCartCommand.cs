using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

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

        var store = await _dbContext.MallStores
            .FirstOrDefaultAsync(s => s.Id == req.StoreId && !s.IsDeleted && s.IsActive, cancellationToken);
        if (store is null) throw new BusinessException(404, "门店不存在或已停用");

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
            .Include(c => c.Items)
            .FirstOrDefaultAsync(
                c => c.UserId == req.UserId && c.StoreId == req.StoreId && !c.IsDeleted,
                cancellationToken);

        if (cart is null)
        {
            cart = MallCart.Create(req.UserId, req.StoreId);
            _dbContext.MallCarts.Add(cart);
        }

        cart.AddItem(req.SkuId, skuRow.Product.Id, req.Quantity);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await _getCartHandler.Handle(new GetMallCartQuery
        {
            UserId = req.UserId,
            StoreId = req.StoreId
        }, cancellationToken);
    }
}
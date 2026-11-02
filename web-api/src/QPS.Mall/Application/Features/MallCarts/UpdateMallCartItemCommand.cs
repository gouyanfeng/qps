using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallCarts;

public class UpdateMallCartItemCommand : IRequest<MallCartDto>
{
    public MallCartUpdateItemRequest Request { get; set; } = null!;
}

public class UpdateMallCartItemHandler : IRequestHandler<UpdateMallCartItemCommand, MallCartDto>
{
    private readonly IMallDbContext _dbContext;
    private readonly GetMallCartHandler _getCartHandler;

    public UpdateMallCartItemHandler(IMallDbContext dbContext, GetMallCartHandler getCartHandler)
    {
        _dbContext = dbContext;
        _getCartHandler = getCartHandler;
    }

    public async Task<MallCartDto> Handle(
        UpdateMallCartItemCommand command,
        CancellationToken cancellationToken)
    {
        var req = command.Request;

        var cart = await _dbContext.MallCarts
            .FirstOrDefaultAsync(
                c => c.UserId == req.UserId && !c.IsDeleted,
                cancellationToken);

        if (cart is null) throw new BusinessException(404, "购物车不存在");

        var item = await _dbContext.MallCartItems
            .FirstOrDefaultAsync(i => i.CartId == cart.Id && i.SkuId == req.SkuId && !i.IsDeleted, cancellationToken);
        if (item is null) throw new BusinessException(400, "购物车中没有该商品");

        if (req.Quantity > 0)
        {
            var sku = await _dbContext.MallSkus
                .FirstOrDefaultAsync(s => s.Id == req.SkuId && !s.IsDeleted, cancellationToken);
            if (sku is null) throw new BusinessException(404, "商品规格不存在");
            if (sku.Stock < req.Quantity)
                throw new BusinessException(400, $"库存不足，仅剩 {sku.Stock}");

            item.SetQuantity(req.Quantity);
        }
        else
        {
            _dbContext.MallCartItems.Remove(item);
        }

        cart.Touch();

        var remainingCount = await _dbContext.MallCartItems
            .CountAsync(i => i.CartId == cart.Id && !i.IsDeleted, cancellationToken);

        if (remainingCount == 0)
        {
            _dbContext.MallCarts.Remove(cart);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (remainingCount == 0)
        {
            return new MallCartDto
            {
                UserId = req.UserId,
                TotalQuantity = 0,
                TotalAmount = 0,
                Items = new List<MallCartItemDto>()
            };
        }

        return await _getCartHandler.Handle(new GetMallCartQuery
        {
            UserId = req.UserId
        }, cancellationToken);
    }
}
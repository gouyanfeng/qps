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
            .Include(c => c.Items)
            .FirstOrDefaultAsync(
                c => c.UserId == req.UserId && c.StoreId == req.StoreId && !c.IsDeleted,
                cancellationToken);

        if (cart is null) throw new BusinessException(404, "购物车不存在");

        if (req.Quantity > 0)
        {
            var sku = await _dbContext.MallSkus
                .FirstOrDefaultAsync(s => s.Id == req.SkuId && !s.IsDeleted, cancellationToken);
            if (sku is null) throw new BusinessException(404, "商品规格不存在");
            if (sku.Stock < req.Quantity)
                throw new BusinessException(400, $"库存不足，仅剩 {sku.Stock}");
        }

        cart.UpdateQuantity(req.SkuId, req.Quantity);

        if (cart.Items.Count == 0)
        {
            _dbContext.MallCarts.Remove(cart);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (cart.Items.Count == 0)
        {
            return new MallCartDto
            {
                UserId = req.UserId,
                StoreId = req.StoreId,
                TotalQuantity = 0,
                TotalAmount = 0,
                Items = new List<MallCartItemDto>()
            };
        }

        return await _getCartHandler.Handle(new GetMallCartQuery
        {
            UserId = req.UserId,
            StoreId = req.StoreId
        }, cancellationToken);
    }
}
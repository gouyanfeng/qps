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

        var cartItems = await _dbContext.MallCartItems
            .Where(i => i.UserId == req.UserId)
            .ToListAsync(cancellationToken);

        if (cartItems.Count == 0) throw new BusinessException(404, "购物车不存在");

        var item = cartItems.FirstOrDefault(i => i.SkuId == req.SkuId);
        if (item is null) throw new BusinessException(400, "购物车中没有该商品");

        var isRemoving = req.Quantity <= 0;
        var remainingCount = cartItems.Count - (isRemoving ? 1 : 0);

        if (!isRemoving)
        {
            var sku = await _dbContext.MallSkus
                .FirstOrDefaultAsync(s => s.Id == req.SkuId, cancellationToken);
            if (sku is null) throw new BusinessException(404, "商品规格不存在");
            if (sku.Stock < req.Quantity)
                throw new BusinessException(400, $"库存不足，仅剩 {sku.Stock}");

            item.SetQuantity(req.Quantity);
        }
        else
        {
            _dbContext.MallCartItems.Remove(item);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (remainingCount == 0)
        {
            return new MallCartDto
            {
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
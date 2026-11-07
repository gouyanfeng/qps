using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallCarts;

/// <summary>按 SKU 批量移除购物车明细：下单成功后只清理本次提交的商品，不影响未购买商品。</summary>
public class RemoveMallCartItemsCommand : IRequest<MallCartDto>
{
    public Guid UserId { get; set; }

    public IReadOnlyCollection<Guid> SkuIds { get; set; } = Array.Empty<Guid>();
}

public class RemoveMallCartItemsHandler : IRequestHandler<RemoveMallCartItemsCommand, MallCartDto>
{
    private readonly IMallDbContext _dbContext;
    private readonly GetMallCartHandler _getCartHandler;

    public RemoveMallCartItemsHandler(IMallDbContext dbContext, GetMallCartHandler getCartHandler)
    {
        _dbContext = dbContext;
        _getCartHandler = getCartHandler;
    }

    public async Task<MallCartDto> Handle(
        RemoveMallCartItemsCommand command,
        CancellationToken cancellationToken)
    {
        var skuIds = command.SkuIds.Distinct().ToHashSet();
        if (skuIds.Count == 0)
        {
            throw new BusinessException(400, "请指定要移除的商品规格");
        }

        var cartItems = await _dbContext.MallCartItems
            .Where(item => item.UserId == command.UserId)
            .ToListAsync(cancellationToken);

        var toRemove = cartItems.Where(item => skuIds.Contains(item.SkuId)).ToList();
        var remaining = cartItems.Except(toRemove).ToList();

        if (toRemove.Count > 0)
        {
            _dbContext.MallCartItems.RemoveRange(toRemove);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (remaining.Count == 0)
        {
            return new MallCartDto
            {
                TotalQuantity = 0,
                TotalAmount = 0,
                Items = new List<MallCartItemDto>()
            };
        }

        return await _getCartHandler.Handle(
            new GetMallCartQuery { UserId = command.UserId },
            cancellationToken);
    }
}

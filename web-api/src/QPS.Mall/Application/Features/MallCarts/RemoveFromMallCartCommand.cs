using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallCarts;

public class RemoveFromMallCartCommand : IRequest<MallCartDto>
{
    public Guid UserId { get; set; }

    public Guid SkuId { get; set; }
}

public class RemoveFromMallCartHandler : IRequestHandler<RemoveFromMallCartCommand, MallCartDto>
{
    private readonly IMallDbContext _dbContext;
    private readonly GetMallCartHandler _getCartHandler;

    public RemoveFromMallCartHandler(IMallDbContext dbContext, GetMallCartHandler getCartHandler)
    {
        _dbContext = dbContext;
        _getCartHandler = getCartHandler;
    }

    public async Task<MallCartDto> Handle(
        RemoveFromMallCartCommand command,
        CancellationToken cancellationToken)
    {
        var cart = await _dbContext.MallCarts
            .FirstOrDefaultAsync(
                c => c.UserId == command.UserId,
                cancellationToken);

        if (cart is null) throw new BusinessException(404, "购物车不存在");

        var item = await _dbContext.MallCartItems
            .FirstOrDefaultAsync(i => i.CartId == cart.Id && i.SkuId == command.SkuId, cancellationToken);

        if (item is not null)
        {
            _dbContext.MallCartItems.Remove(item);
        }

        cart.Touch();

        var remainingCount = await _dbContext.MallCartItems
            .CountAsync(i => i.CartId == cart.Id, cancellationToken);

        if (remainingCount == 0)
        {
            _dbContext.MallCarts.Remove(cart);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (remainingCount == 0)
        {
            return new MallCartDto
            {
                UserId = command.UserId,
                TotalQuantity = 0,
                TotalAmount = 0,
                Items = new List<MallCartItemDto>()
            };
        }

        return await _getCartHandler.Handle(new GetMallCartQuery
        {
            UserId = command.UserId
        }, cancellationToken);
    }
}
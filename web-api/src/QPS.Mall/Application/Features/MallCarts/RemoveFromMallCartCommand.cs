using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallCarts;

public class RemoveFromMallCartCommand : IRequest<MallCartDto>
{
    public Guid UserId { get; set; }

    public Guid StoreId { get; set; }

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
            .Include(c => c.Items)
            .FirstOrDefaultAsync(
                c => c.UserId == command.UserId && c.StoreId == command.StoreId && !c.IsDeleted,
                cancellationToken);

        if (cart is null) throw new BusinessException(404, "购物车不存在");

        cart.RemoveItem(command.SkuId);

        if (cart.Items.Count == 0)
        {
            _dbContext.MallCarts.Remove(cart);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (cart.Items.Count == 0)
        {
            return new MallCartDto
            {
                UserId = command.UserId,
                StoreId = command.StoreId,
                TotalQuantity = 0,
                TotalAmount = 0,
                Items = new List<MallCartItemDto>()
            };
        }

        return await _getCartHandler.Handle(new GetMallCartQuery
        {
            UserId = command.UserId,
            StoreId = command.StoreId
        }, cancellationToken);
    }
}
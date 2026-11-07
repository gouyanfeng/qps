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
        var cartItems = await _dbContext.MallCartItems
            .Where(i => i.UserId == command.UserId)
            .ToListAsync(cancellationToken);

        if (cartItems.Count == 0) throw new BusinessException(404, "购物车不存在");

        var item = cartItems.FirstOrDefault(i => i.SkuId == command.SkuId);
        var remainingCount = cartItems.Count - (item is null ? 0 : 1);

        if (item is not null)
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
            UserId = command.UserId
        }, cancellationToken);
    }
}
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallCarts;

public class ClearMallCartCommand : IRequest<bool>
{
    public Guid UserId { get; set; }
}

public class ClearMallCartHandler : IRequestHandler<ClearMallCartCommand, bool>
{
    private readonly IMallDbContext _dbContext;

    public ClearMallCartHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(ClearMallCartCommand command, CancellationToken cancellationToken)
    {
        var cartItems = await _dbContext.MallCartItems
            .Where(i => i.UserId == command.UserId)
            .ToListAsync(cancellationToken);

        if (cartItems.Count == 0) return true;

        _dbContext.MallCartItems.RemoveRange(cartItems);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
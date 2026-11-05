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
        var cart = await _dbContext.MallCarts
            .FirstOrDefaultAsync(
                c => c.UserId == command.UserId,
                cancellationToken);

        if (cart is null) return true;

        _dbContext.MallCarts.Remove(cart);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
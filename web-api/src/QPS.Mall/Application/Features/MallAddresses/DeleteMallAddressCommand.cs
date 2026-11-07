using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallAddresses;

public class DeleteMallAddressCommand : IRequest<bool>
{
    public Guid AddressId { get; set; }

    public Guid UserId { get; set; }
}

public class DeleteMallAddressHandler : IRequestHandler<DeleteMallAddressCommand, bool>
{
    private readonly IMallDbContext _dbContext;

    public DeleteMallAddressHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(DeleteMallAddressCommand command, CancellationToken cancellationToken)
    {
        var address = await _dbContext.MallAddresses
            .FirstOrDefaultAsync(a => a.Id == command.AddressId && a.UserId == command.UserId, cancellationToken);

        if (address is null)
        {
            throw new BusinessException(404, "收货地址不存在");
        }

        _dbContext.MallAddresses.Remove(address);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallAddresses;

public class CreateMallAddressCommand : IRequest<Guid>
{
    public MallAddressCreateRequest Request { get; set; } = null!;
}

public class CreateMallAddressHandler : IRequestHandler<CreateMallAddressCommand, Guid>
{
    private readonly IMallDbContext _dbContext;

    public CreateMallAddressHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid> Handle(CreateMallAddressCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;
        MallAddressRules.Validate(req);

        if (req.IsDefault && req.UserId.HasValue)
        {
            var existingDefaults = await _dbContext.MallAddresses
                .Where(a => a.UserId == req.UserId.Value && a.IsDefault)
                .ToListAsync(cancellationToken);

            foreach (var a in existingDefaults) a.SetDefault(false);
        }

        var address = MallAddress.Create(
            req.UserId,
            req.ReceiverName.Trim(),
            req.ReceiverPhone.Trim(),
            req.ProvinceName.Trim(),
            req.CityName.Trim(),
            req.DistrictName.Trim(),
            req.DetailAddress.Trim(),
            req.IsDefault);

        _dbContext.MallAddresses.Add(address);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return address.Id;
    }
}
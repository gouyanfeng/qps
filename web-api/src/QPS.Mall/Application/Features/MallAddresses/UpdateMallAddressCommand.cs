using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallAddresses;

public class UpdateMallAddressCommand : IRequest<bool>
{
    public Guid AddressId { get; set; }

    public MallAddressUpdateRequest Request { get; set; } = null!;
}

public class UpdateMallAddressHandler : IRequestHandler<UpdateMallAddressCommand, bool>
{
    private readonly IMallDbContext _dbContext;

    public UpdateMallAddressHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(UpdateMallAddressCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;
        MallAddressRules.ValidateUpdate(req);

        var address = await _dbContext.MallAddresses
            .FirstOrDefaultAsync(a => a.Id == command.AddressId, cancellationToken);

        if (address is null)
        {
            throw new BusinessException(404, "收货地址不存在");
        }

        if (req.IsDefault && address.UserId.HasValue)
        {
            var existingDefaults = await _dbContext.MallAddresses
                .Where(a => a.UserId == address.UserId.Value && a.IsDefault && a.Id != address.Id)
                .ToListAsync(cancellationToken);

            foreach (var a in existingDefaults) a.SetDefault(false);
        }

        address.Update(
            req.ReceiverName.Trim(),
            req.ReceiverPhone.Trim(),
            req.ProvinceName.Trim(),
            req.CityName.Trim(),
            req.DistrictName.Trim(),
            req.DetailAddress.Trim());

        address.SetDefault(req.IsDefault);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
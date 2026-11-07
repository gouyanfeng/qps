using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;

namespace QPS.Mall.Application.Features.MallAddresses;

public class GetMallAddressesQuery : IRequest<List<MallAddressDto>>
{
    public Guid UserId { get; set; }
}

public class GetMallAddressesHandler : IRequestHandler<GetMallAddressesQuery, List<MallAddressDto>>
{
    private readonly IMallDbContext _dbContext;

    public GetMallAddressesHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<MallAddressDto>> Handle(
        GetMallAddressesQuery request,
        CancellationToken cancellationToken)
    {
        var addresses = await _dbContext.MallAddresses
            .AsNoTracking()
            .Where(a => a.UserId == request.UserId)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        return addresses.Select(Map).ToList();
    }

    private static MallAddressDto Map(QPS.Mall.Domain.Entities.MallAddress a) => new()
    {
        Id = a.Id,
        UserId = a.UserId,
        ReceiverName = a.ReceiverName,
        ReceiverPhone = a.ReceiverPhone,
        ProvinceName = a.ProvinceName,
        CityName = a.CityName,
        DistrictName = a.DistrictName,
        DetailAddress = a.DetailAddress,
        FullAddress = a.BuildFullAddress(),
        IsDefault = a.IsDefault,
        CreatedAt = a.CreatedAt,
        UpdatedAt = a.UpdatedAt
    };
}
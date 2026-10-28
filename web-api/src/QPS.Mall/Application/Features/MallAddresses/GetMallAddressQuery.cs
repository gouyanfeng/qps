using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallAddresses;

public class GetMallAddressQuery : IRequest<MallAddressDto>
{
    public Guid AddressId { get; set; }
}

public class GetMallAddressHandler : IRequestHandler<GetMallAddressQuery, MallAddressDto>
{
    private readonly IMallDbContext _dbContext;

    public GetMallAddressHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MallAddressDto> Handle(
        GetMallAddressQuery request,
        CancellationToken cancellationToken)
    {
        var a = await _dbContext.MallAddresses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.AddressId && !x.IsDeleted, cancellationToken);

        if (a is null)
        {
            throw new BusinessException(404, "收货地址不存在");
        }

        return new MallAddressDto
        {
            Id = a.Id,
            UserId = a.UserId,
            ReceiverName = a.ReceiverName,
            ReceiverPhone = a.ReceiverPhone,
            ProvinceCode = a.ProvinceCode,
            ProvinceName = a.ProvinceName,
            CityCode = a.CityCode,
            CityName = a.CityName,
            DistrictCode = a.DistrictCode,
            DistrictName = a.DistrictName,
            DetailAddress = a.DetailAddress,
            FullAddress = a.BuildFullAddress(),
            IsDefault = a.IsDefault,
            CreatedAt = a.CreatedAt,
            UpdatedAt = a.UpdatedAt
        };
    }
}
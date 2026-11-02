using QPS.Shared.Common;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Domain.Entities;

public class MallAddress : BaseEntity
{
    public Guid? UserId { get; private set; }

    public string ReceiverName { get; private set; } = string.Empty;

    public string ReceiverPhone { get; private set; } = string.Empty;

    public string ProvinceName { get; private set; } = string.Empty;

    public string CityName { get; private set; } = string.Empty;

    public string DistrictName { get; private set; } = string.Empty;

    public string DetailAddress { get; private set; } = string.Empty;

    public bool IsDefault { get; private set; }

    private MallAddress()
    {
    }

    private MallAddress(
        Guid? userId,
        string receiverName,
        string receiverPhone,
        string provinceName,
        string cityName,
        string districtName,
        string detailAddress,
        bool isDefault)
    {
        UserId = userId;
        ReceiverName = receiverName;
        ReceiverPhone = receiverPhone;
        ProvinceName = provinceName;
        CityName = cityName;
        DistrictName = districtName;
        DetailAddress = detailAddress;
        IsDefault = isDefault;
    }

    public static MallAddress Create(
        Guid? userId,
        string receiverName,
        string receiverPhone,
        string provinceName,
        string cityName,
        string districtName,
        string detailAddress,
        bool isDefault)
    {
        return new MallAddress(
            userId, receiverName, receiverPhone,
            provinceName, cityName, districtName,
            detailAddress, isDefault);
    }

    public void Update(
        string receiverName,
        string receiverPhone,
        string provinceName,
        string cityName,
        string districtName,
        string detailAddress)
    {
        ReceiverName = receiverName;
        ReceiverPhone = receiverPhone;
        ProvinceName = provinceName;
        CityName = cityName;
        DistrictName = districtName;
        DetailAddress = detailAddress;
    }

    public void SetDefault(bool isDefault)
    {
        IsDefault = isDefault;
    }

    public string BuildFullAddress()
    {
        return $"{ProvinceName}{CityName}{DistrictName}{DetailAddress}";
    }
}
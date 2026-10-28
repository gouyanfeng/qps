using QPS.Shared.Common;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Domain.Entities;

public class MallAddress : BaseEntity
{
    public Guid? UserId { get; private set; }

    public string ReceiverName { get; private set; } = string.Empty;

    public string ReceiverPhone { get; private set; } = string.Empty;

    public string ProvinceCode { get; private set; } = string.Empty;

    public string ProvinceName { get; private set; } = string.Empty;

    public string CityCode { get; private set; } = string.Empty;

    public string CityName { get; private set; } = string.Empty;

    public string DistrictCode { get; private set; } = string.Empty;

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
        string provinceCode,
        string provinceName,
        string cityCode,
        string cityName,
        string districtCode,
        string districtName,
        string detailAddress,
        bool isDefault)
    {
        UserId = userId;
        ReceiverName = receiverName;
        ReceiverPhone = receiverPhone;
        ProvinceCode = provinceCode;
        ProvinceName = provinceName;
        CityCode = cityCode;
        CityName = cityName;
        DistrictCode = districtCode;
        DistrictName = districtName;
        DetailAddress = detailAddress;
        IsDefault = isDefault;
    }

    public static MallAddress Create(
        Guid? userId,
        string receiverName,
        string receiverPhone,
        string provinceCode,
        string provinceName,
        string cityCode,
        string cityName,
        string districtCode,
        string districtName,
        string detailAddress,
        bool isDefault)
    {
        return new MallAddress(
            userId, receiverName, receiverPhone,
            provinceCode, provinceName,
            cityCode, cityName,
            districtCode, districtName,
            detailAddress, isDefault);
    }

    public void Update(
        string receiverName,
        string receiverPhone,
        string provinceCode,
        string provinceName,
        string cityCode,
        string cityName,
        string districtCode,
        string districtName,
        string detailAddress)
    {
        ReceiverName = receiverName;
        ReceiverPhone = receiverPhone;
        ProvinceCode = provinceCode;
        ProvinceName = provinceName;
        CityCode = cityCode;
        CityName = cityName;
        DistrictCode = districtCode;
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

    public string BuildFullAddressWithCodes()
    {
        return $"{ProvinceCode}|{CityCode}|{DistrictCode}|{ProvinceName}{CityName}{DistrictName}{DetailAddress}";
    }
}
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallAddresses;

internal static class MallAddressRules
{
    public const int ReceiverNameMax = 50;
    public const int ReceiverPhoneMax = 30;
    public const int ProvinceNameMax = 20;
    public const int CityNameMax = 20;
    public const int DistrictNameMax = 20;
    public const int DetailAddressMax = 300;

    public static void Validate(MallAddressCreateRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.ReceiverName))
            throw new BusinessException(400, "收货人姓名不能为空");
        if (req.ReceiverName.Length > ReceiverNameMax)
            throw new BusinessException(400, $"收货人姓名不能超过 {ReceiverNameMax} 个字符");

        if (string.IsNullOrWhiteSpace(req.ReceiverPhone))
            throw new BusinessException(400, "收货人电话不能为空");
        if (req.ReceiverPhone.Length > ReceiverPhoneMax)
            throw new BusinessException(400, $"收货人电话不能超过 {ReceiverPhoneMax} 个字符");

        if (string.IsNullOrWhiteSpace(req.ProvinceName) || string.IsNullOrWhiteSpace(req.CityName))
            throw new BusinessException(400, "请选择完整的省市区");

        ValidateNames(req.ProvinceName, req.CityName, req.DistrictName);

        if (string.IsNullOrWhiteSpace(req.DetailAddress))
            throw new BusinessException(400, "详细地址不能为空");
        if (req.DetailAddress.Length > DetailAddressMax)
            throw new BusinessException(400, $"详细地址不能超过 {DetailAddressMax} 个字符");
    }

    public static void ValidateUpdate(MallAddressUpdateRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.ReceiverName))
            throw new BusinessException(400, "收货人姓名不能为空");
        if (req.ReceiverName.Length > ReceiverNameMax)
            throw new BusinessException(400, $"收货人姓名不能超过 {ReceiverNameMax} 个字符");

        if (string.IsNullOrWhiteSpace(req.ReceiverPhone))
            throw new BusinessException(400, "收货人电话不能为空");
        if (req.ReceiverPhone.Length > ReceiverPhoneMax)
            throw new BusinessException(400, $"收货人电话不能超过 {ReceiverPhoneMax} 个字符");

        if (string.IsNullOrWhiteSpace(req.ProvinceName) || string.IsNullOrWhiteSpace(req.CityName))
            throw new BusinessException(400, "请选择完整的省市区");

        ValidateNames(req.ProvinceName, req.CityName, req.DistrictName);

        if (string.IsNullOrWhiteSpace(req.DetailAddress))
            throw new BusinessException(400, "详细地址不能为空");
        if (req.DetailAddress.Length > DetailAddressMax)
            throw new BusinessException(400, $"详细地址不能超过 {DetailAddressMax} 个字符");
    }

    private static void ValidateNames(string pn, string cn, string dn)
    {
        if (pn.Length > ProvinceNameMax) throw new BusinessException(400, "省名称过长");
        if (cn.Length > CityNameMax) throw new BusinessException(400, "市名称过长");
        if (!string.IsNullOrEmpty(dn) && dn.Length > DistrictNameMax)
            throw new BusinessException(400, "区名称过长");
    }
}
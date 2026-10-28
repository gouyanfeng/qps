using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallFreightTemplates;

internal static class MallFreightTemplateRules
{
    public static void ValidateTemplateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new BusinessException(400, "模板名称不能为空");
        }

        if (name.Length > 50)
        {
            throw new BusinessException(400, "模板名称不能超过 50 个字符");
        }
    }

    public static void ValidateBillingType(FreightBillingType billingType)
    {
        if (!Enum.IsDefined(billingType))
        {
            throw new BusinessException(400, "无效的计费方式");
        }
    }

    public static void ValidateRegions(List<MallFreightRegionRequest> regions)
    {
        if (regions == null || regions.Count == 0)
        {
            throw new BusinessException(400, "至少需要配置一个区域规则");
        }

        var hasDefault = false;
        var regionNameSet = new HashSet<string>();

        foreach (var region in regions)
        {
            if (string.IsNullOrWhiteSpace(region.RegionName))
            {
                throw new BusinessException(400, "区域名称不能为空");
            }

            if (!regionNameSet.Add(region.RegionName))
            {
                throw new BusinessException(400, $"区域名称「{region.RegionName}」重复");
            }

            if (region.FirstValue <= 0)
            {
                throw new BusinessException(400, $"区域「{region.RegionName}」首值必须大于 0");
            }

            if (region.FirstFee < 0)
            {
                throw new BusinessException(400, $"区域「{region.RegionName}」首费不能小于 0");
            }

            if (region.StepValue <= 0)
            {
                throw new BusinessException(400, $"区域「{region.RegionName}」续步长必须大于 0");
            }

            if (region.StepFee < 0)
            {
                throw new BusinessException(400, $"区域「{region.RegionName}」续费不能小于 0");
            }

            if (region.FreeShippingAmount.HasValue && region.FreeShippingAmount.Value <= 0)
            {
                throw new BusinessException(400, $"区域「{region.RegionName}」满额包邮金额必须大于 0");
            }

            if (region.FreeShippingQuantity.HasValue && region.FreeShippingQuantity.Value <= 0)
            {
                throw new BusinessException(400, $"区域「{region.RegionName}」满件包邮数量必须大于 0");
            }

            if (region.IsDefaultRegion)
            {
                if (hasDefault)
                {
                    throw new BusinessException(400, "只能有一个默认兜底区域");
                }

                hasDefault = true;
            }

            if (!region.IsDefaultRegion)
            {
                if (string.IsNullOrWhiteSpace(region.ProvinceCodes))
                {
                    throw new BusinessException(400, $"区域「{region.RegionName}」必须配置省份编码");
                }
            }
        }

        if (!hasDefault)
        {
            throw new BusinessException(400, "必须配置一个默认兜底区域");
        }
    }

    public static string GetBillingTypeName(FreightBillingType type) => type switch
    {
        FreightBillingType.Weight => "按重量",
        FreightBillingType.Quantity => "按件数",
        FreightBillingType.Volume => "按体积",
        _ => "未知"
    };
}
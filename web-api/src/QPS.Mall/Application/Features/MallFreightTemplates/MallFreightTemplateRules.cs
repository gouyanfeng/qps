using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallFreightTemplates;

internal static class MallFreightTemplateRules
{
    public const int TemplateNameMax = 50;
    public const int TemplateDescMax = 500;
    public const int RegionNameMax = 50;
    public const int ProvinceNamesMax = 2000;
    public const int CityNamesMax = 2000;

    public static void ValidateTemplateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessException(400, "模板名称不能为空");
        if (name.Length > TemplateNameMax)
            throw new BusinessException(400, $"模板名称不能超过 {TemplateNameMax} 个字符");
    }

    public static void ValidateBillingType(FreightBillingType billingType)
    {
        if (!Enum.IsDefined(billingType))
            throw new BusinessException(400, "无效的计费方式");
    }

    public static void ValidateRegions(List<MallFreightRegionRequest> regions)
    {
        if (regions == null || regions.Count == 0)
            throw new BusinessException(400, "至少需要 1 条区域规则");

        for (int i = 0; i < regions.Count; i++)
            ValidateRegion(regions[i], i);

        ValidateRegionsNoDuplicates(regions);
    }

    public static string GetBillingTypeName(FreightBillingType type) => type switch
    {
        FreightBillingType.Quantity => "按件数",
        FreightBillingType.Weight => "按重量",
        FreightBillingType.Volume => "按体积",
        _ => "未知"
    };

    public static void ValidateTemplateBasic(MallFreightTemplateCreateRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            throw new BusinessException(400, "运费模板名称不能为空");
        if (req.Name.Length > TemplateNameMax)
            throw new BusinessException(400, $"运费模板名称不能超过 {TemplateNameMax} 个字符");
        if (!string.IsNullOrEmpty(req.Description) && req.Description.Length > TemplateDescMax)
            throw new BusinessException(400, $"运费模板描述不能超过 {TemplateDescMax} 个字符");
    }

    public static void ValidateRegion(MallFreightRegionRequest region, int index)
    {
        if (string.IsNullOrWhiteSpace(region.RegionName))
            throw new BusinessException(400, $"第 {index + 1} 条区域规则的区域名称不能为空");
        if (region.RegionName.Length > RegionNameMax)
            throw new BusinessException(400, $"第 {index + 1} 条区域规则的区域名称过长");

        if (region.IsDefaultRegion)
        {
            if (!string.IsNullOrWhiteSpace(region.ProvinceNames) || !string.IsNullOrWhiteSpace(region.CityNames))
            {
                throw new BusinessException(400, "默认区域规则不能配置省/市名称");
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(region.ProvinceNames))
                throw new BusinessException(400, $"第 {index + 1} 条区域规则的省份名称不能为空");
            if (region.ProvinceNames.Length > ProvinceNamesMax)
                throw new BusinessException(400, $"第 {index + 1} 条区域规则的省份名称过长");
            if (!string.IsNullOrEmpty(region.CityNames) && region.CityNames.Length > CityNamesMax)
                throw new BusinessException(400, $"第 {index + 1} 条区域规则的市名称过长");
        }

        if (region.FirstValue <= 0)
            throw new BusinessException(400, $"第 {index + 1} 条区域规则的首件/首重/首体积必须大于 0");
        if (region.FirstFee < 0)
            throw new BusinessException(400, $"第 {index + 1} 条区域规则的首费不能为负数");
        if (region.StepValue <= 0)
            throw new BusinessException(400, $"第 {index + 1} 条区域规则的续件/续重/续体积必须大于 0");
        if (region.StepFee < 0)
            throw new BusinessException(400, $"第 {index + 1} 条区域规则的续费不能为负数");

        if (region.FreeShippingAmount.HasValue && region.FreeShippingAmount < 0)
            throw new BusinessException(400, "满额包邮金额不能为负数");
        if (region.FreeShippingQuantity.HasValue && region.FreeShippingQuantity <= 0)
            throw new BusinessException(400, "满件包邮件数必须大于 0");
    }

    public static void ValidateRegionsNoDuplicates(List<MallFreightRegionRequest> regions)
    {
        var defaults = regions.Count(r => r.IsDefaultRegion);
        if (defaults > 1)
            throw new BusinessException(400, "运费模板只能有一个默认区域规则");

        var allProvinceNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var r in regions.Where(x => !x.IsDefaultRegion))
        {
            foreach (var code in SplitNames(r.ProvinceNames))
            {
                if (!allProvinceNames.Add(code))
                {
                    throw new BusinessException(400, $"省份名称 {code} 被多个区域规则重复配置");
                }
            }
        }
    }

    private static IEnumerable<string> SplitNames(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) yield break;
        foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!string.IsNullOrEmpty(part)) yield return part;
        }
    }
}
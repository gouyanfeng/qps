using QPS.Mall.Domain.Entities;

namespace QPS.Mall.Application.Features.MallFreightRules;

public class FreightItemRow
{
    public Guid SkuId { get; set; }

    public Guid ProductId { get; set; }

    public Guid? FreightTemplateId { get; set; }

    public int Quantity { get; set; }

    public int WeightGram { get; set; }

    public int VolumeCm3 { get; set; }

    public decimal LineAmount { get; set; }
}

public class FreightAddressInfo
{
    public string? ProvinceName { get; set; }

    public string? CityName { get; set; }
}

public class FreightTemplateGroup
{
    public MallFreightTemplate Template { get; set; } = null!;

    public List<FreightItemRow> Items { get; set; } = new();

    public MallFreightRegion MatchedRegion { get; set; } = null!;

    public decimal GoodsAmount { get; set; }

    public decimal FreightAmount { get; set; }
}

public class FreightCalcResult
{
    public decimal TotalFreight { get; set; }

    public List<FreightTemplateGroup> Groups { get; set; } = new();
}

internal static class MallFreightCalculator
{
    public static FreightCalcResult Calculate(
        IEnumerable<FreightItemRow> items,
        Guid storeDefaultFreightTemplateId,
        IReadOnlyDictionary<Guid, MallFreightTemplate> templateMap,
        IReadOnlyDictionary<Guid, List<MallFreightRegion>> regionMap,
        FreightAddressInfo address)
    {
        var resolvedTemplateIds = new Dictionary<Guid, Guid?>();

        foreach (var item in items)
        {
            if (!resolvedTemplateIds.ContainsKey(item.SkuId))
            {
                resolvedTemplateIds[item.SkuId] = item.FreightTemplateId ?? storeDefaultFreightTemplateId;
            }
        }

        var groups = new Dictionary<Guid, FreightTemplateGroup>();

        foreach (var item in items)
        {
            var templateId = resolvedTemplateIds[item.SkuId];
            if (!templateId.HasValue)
            {
                throw new InvalidOperationException("商品未关联运费模板且门店未配置默认运费模板");
            }

            if (!templateMap.TryGetValue(templateId.Value, out var template) || !template.IsActive)
            {
                throw new InvalidOperationException("运费模板不存在或已停用");
            }

            if (!groups.TryGetValue(templateId.Value, out var group))
            {
                var regions = regionMap.TryGetValue(templateId.Value, out var r) ? r : new List<MallFreightRegion>();
                var matched = MatchRegion(regions, address.ProvinceName, address.CityName);

                group = new FreightTemplateGroup
                {
                    Template = template,
                    MatchedRegion = matched
                };
                groups[templateId.Value] = group;
            }

            group.Items.Add(item);
            group.GoodsAmount += item.LineAmount;
        }

        var result = new FreightCalcResult();
        foreach (var group in groups.Values)
        {
            var freight = CalculateGroup(group);
            group.FreightAmount = freight;
            result.TotalFreight += freight;
            result.Groups.Add(group);
        }

        return result;
    }

    private static MallFreightRegion MatchRegion(
        List<MallFreightRegion> regions,
        string? provinceName,
        string? cityName)
    {
        if (regions.Count == 0)
        {
            throw new InvalidOperationException("运费模板未配置任何区域规则");
        }

        if (!string.IsNullOrEmpty(cityName))
        {
            var cityMatch = regions.FirstOrDefault(r =>
                !r.IsDefaultRegion &&
                !string.IsNullOrEmpty(r.CityNames) &&
                ParseCodes(r.CityNames).Contains(cityName));
            if (cityMatch != null) return cityMatch;
        }

        if (!string.IsNullOrEmpty(provinceName))
        {
            var provinceMatch = regions.FirstOrDefault(r =>
                !r.IsDefaultRegion &&
                !string.IsNullOrEmpty(r.ProvinceNames) &&
                ParseCodes(r.ProvinceNames).Contains(provinceName));
            if (provinceMatch != null) return provinceMatch;
        }

        var defaultRegion = regions.FirstOrDefault(r => r.IsDefaultRegion);
        if (defaultRegion != null) return defaultRegion;

        return regions[0];
    }

    private static HashSet<string> ParseCodes(string raw)
    {
        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet();
    }

    private static decimal CalculateGroup(FreightTemplateGroup group)
    {
        var region = group.MatchedRegion;
        var billingType = group.Template.BillingType;

        if (region.FreeShippingAmount.HasValue && group.GoodsAmount >= region.FreeShippingAmount.Value)
        {
            return 0m;
        }

        var totalQuantity = group.Items.Sum(i => i.Quantity);
        if (region.FreeShippingQuantity.HasValue && totalQuantity >= region.FreeShippingQuantity.Value)
        {
            return 0m;
        }

        var chargeValue = billingType switch
        {
            FreightBillingType.Weight => group.Items.Sum(i => i.WeightGram * i.Quantity),
            FreightBillingType.Quantity => totalQuantity,
            FreightBillingType.Volume => group.Items.Sum(i => i.VolumeCm3 * i.Quantity),
            _ => throw new InvalidOperationException($"未知的计费方式：{billingType}")
        };

        if (chargeValue <= region.FirstValue)
        {
            return region.FirstFee;
        }

        var extra = chargeValue - region.FirstValue;
        var steps = (int)Math.Ceiling(extra / (double)region.StepValue);

        return region.FirstFee + steps * region.StepFee;
    }
}
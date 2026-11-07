using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
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
    /// <summary>
    /// 统一运费入口：预览与下单共用同一套模板解析、区域匹配、满额/免邮判定。
    /// 无地址、无默认/商品模板、模板停用或无区域规则时返回 0，供预览与下单保持一致且不报错。
    /// </summary>
    public static async Task<decimal> CalculateAsync(
        IMallDbContext dbContext,
        FreightAddressInfo? address,
        IEnumerable<FreightItemRow> items,
        bool isFreightFree,
        CancellationToken cancellationToken)
    {
        if (isFreightFree || address is null)
        {
            return 0m;
        }

        var itemList = items.ToList();
        if (itemList.Count == 0)
        {
            return 0m;
        }

        var defaultTemplate = await dbContext.MallFreightTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.IsDefault && t.IsActive, cancellationToken);
        if (defaultTemplate is null &&
            !itemList.Any(item => item.FreightTemplateId.HasValue))
        {
            return 0m;
        }

        var templateIds = new HashSet<Guid>();
        if (defaultTemplate is not null)
        {
            templateIds.Add(defaultTemplate.Id);
        }

        foreach (var item in itemList.Where(item => item.FreightTemplateId.HasValue))
        {
            templateIds.Add(item.FreightTemplateId!.Value);
        }

        if (templateIds.Count == 0)
        {
            return 0m;
        }

        var templates = await dbContext.MallFreightTemplates
            .AsNoTracking()
            .Where(template => templateIds.Contains(template.Id))
            .ToListAsync(cancellationToken);
        var templateMap = templates.ToDictionary(template => template.Id);

        var regions = await dbContext.MallFreightRegions
            .AsNoTracking()
            .Where(region => templateIds.Contains(region.TemplateId))
            .ToListAsync(cancellationToken);
        var regionMap = regions
            .GroupBy(region => region.TemplateId)
            .ToDictionary(group => group.Key, group => group.OrderBy(region => region.IsDefaultRegion).ToList());

        // 模板缺失、停用或没有区域规则时按 0 运费处理，避免预览/下单在这个边界上报错
        foreach (var item in itemList)
        {
            var templateId = item.FreightTemplateId ?? defaultTemplate?.Id;
            if (!templateId.HasValue ||
                !templateMap.TryGetValue(templateId.Value, out var template) ||
                !template.IsActive ||
                !regionMap.TryGetValue(templateId.Value, out var templateRegions) ||
                templateRegions.Count == 0)
            {
                return 0m;
            }
        }

        return Calculate(
            itemList,
            defaultTemplate?.Id ?? Guid.Empty,
            templateMap,
            regionMap,
            address).TotalFreight;
    }

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
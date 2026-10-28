using QPS.Shared.Common;

namespace QPS.Mall.Domain.Entities;

public class MallFreightRegion : BaseEntity
{
    public Guid TemplateId { get; private set; }

    public string RegionName { get; private set; } = string.Empty;

    public string ProvinceCodes { get; private set; } = string.Empty;

    public string? CityCodes { get; private set; }

    public int FirstValue { get; private set; }

    public decimal FirstFee { get; private set; }

    public int StepValue { get; private set; }

    public decimal StepFee { get; private set; }

    public decimal? FreeShippingAmount { get; private set; }

    public int? FreeShippingQuantity { get; private set; }

    public bool IsDefaultRegion { get; private set; }

    private MallFreightRegion()
    {
    }

    private MallFreightRegion(
        Guid templateId,
        string regionName,
        string provinceCodes,
        string? cityCodes,
        int firstValue,
        decimal firstFee,
        int stepValue,
        decimal stepFee,
        decimal? freeShippingAmount,
        int? freeShippingQuantity,
        bool isDefaultRegion)
    {
        TemplateId = templateId;
        RegionName = regionName;
        ProvinceCodes = provinceCodes;
        CityCodes = cityCodes;
        FirstValue = firstValue;
        FirstFee = firstFee;
        StepValue = stepValue;
        StepFee = stepFee;
        FreeShippingAmount = freeShippingAmount;
        FreeShippingQuantity = freeShippingQuantity;
        IsDefaultRegion = isDefaultRegion;
    }

    public static MallFreightRegion Create(
        Guid templateId,
        string regionName,
        string provinceCodes,
        string? cityCodes,
        int firstValue,
        decimal firstFee,
        int stepValue,
        decimal stepFee,
        decimal? freeShippingAmount,
        int? freeShippingQuantity,
        bool isDefaultRegion)
    {
        return new MallFreightRegion(
            templateId, regionName, provinceCodes, cityCodes,
            firstValue, firstFee, stepValue, stepFee,
            freeShippingAmount, freeShippingQuantity, isDefaultRegion);
    }

    public void Update(
        string regionName,
        string provinceCodes,
        string? cityCodes,
        int firstValue,
        decimal firstFee,
        int stepValue,
        decimal stepFee,
        decimal? freeShippingAmount,
        int? freeShippingQuantity,
        bool isDefaultRegion)
    {
        RegionName = regionName;
        ProvinceCodes = provinceCodes;
        CityCodes = cityCodes;
        FirstValue = firstValue;
        FirstFee = firstFee;
        StepValue = stepValue;
        StepFee = stepFee;
        FreeShippingAmount = freeShippingAmount;
        FreeShippingQuantity = freeShippingQuantity;
        IsDefaultRegion = isDefaultRegion;
    }

    public bool MatchesAddress(string? provinceCode, string? cityCode)
    {
        if (IsDefaultRegion) return true;

        var provinces = ParseCodes(ProvinceCodes);
        if (!string.IsNullOrEmpty(cityCode) && !string.IsNullOrEmpty(CityCodes))
        {
            var cities = ParseCodes(CityCodes);
            if (cities.Contains(cityCode)) return true;
        }

        if (!string.IsNullOrEmpty(provinceCode) && provinces.Contains(provinceCode))
        {
            return true;
        }

        return false;
    }

    private static HashSet<string> ParseCodes(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return new HashSet<string>();
        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet();
    }
}
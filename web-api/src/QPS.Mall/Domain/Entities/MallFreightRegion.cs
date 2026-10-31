using QPS.Shared.Common;

namespace QPS.Mall.Domain.Entities;

public class MallFreightRegion : BaseEntity
{
    public Guid TemplateId { get; private set; }

    public string RegionName { get; private set; } = string.Empty;

    public string ProvinceNames { get; private set; } = string.Empty;

    public string? CityNames { get; private set; }

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
        string provinceNames,
        string? cityNames,
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
        ProvinceNames = provinceNames;
        CityNames = cityNames;
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
        string provinceNames,
        string? cityNames,
        int firstValue,
        decimal firstFee,
        int stepValue,
        decimal stepFee,
        decimal? freeShippingAmount,
        int? freeShippingQuantity,
        bool isDefaultRegion)
    {
        return new MallFreightRegion(
            templateId, regionName, provinceNames, cityNames,
            firstValue, firstFee, stepValue, stepFee,
            freeShippingAmount, freeShippingQuantity, isDefaultRegion);
    }

    public void Update(
        string regionName,
        string provinceNames,
        string? cityNames,
        int firstValue,
        decimal firstFee,
        int stepValue,
        decimal stepFee,
        decimal? freeShippingAmount,
        int? freeShippingQuantity,
        bool isDefaultRegion)
    {
        RegionName = regionName;
        ProvinceNames = provinceNames;
        CityNames = cityNames;
        FirstValue = firstValue;
        FirstFee = firstFee;
        StepValue = stepValue;
        StepFee = stepFee;
        FreeShippingAmount = freeShippingAmount;
        FreeShippingQuantity = freeShippingQuantity;
        IsDefaultRegion = isDefaultRegion;
    }

    public bool MatchesAddress(string? provinceName, string? cityName)
    {
        if (IsDefaultRegion) return true;

        var provinces = ParseCodes(ProvinceNames);
        if (!string.IsNullOrEmpty(cityName) && !string.IsNullOrEmpty(CityNames))
        {
            var cities = ParseCodes(CityNames);
            if (cities.Contains(cityName)) return true;
        }

        if (!string.IsNullOrEmpty(provinceName) && provinces.Contains(provinceName))
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
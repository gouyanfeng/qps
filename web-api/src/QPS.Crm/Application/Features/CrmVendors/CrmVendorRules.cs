namespace QPS.Crm.Application.Features.CrmVendors;

public static class CrmVendorRules
{

    public static string NormalizeVendorName(string vendorName)
    {
        return string.Concat(vendorName.Where(c => !char.IsWhiteSpace(c))).ToUpperInvariant();
    }

    public static string NormalizePriority(string? priorityLevel)
    {
        return priorityLevel?.Trim() switch
        {
            "高" or "High" => "高",
            "中" or "Medium" => "中",
            "低" or "Low" => "低",
            _ => "中"
        };
    }
}

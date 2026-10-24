using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallStocks;

/// <summary>
/// 库存调整字段校验。
/// </summary>
internal static class MallStockRules
{
    public const int RemarkMaxLength = 200;
    public const int MaxStock = 1000000;
    public const int MaxPrice = 9999999;

    public static void ValidateTargetStock(int targetStock)
    {
        if (targetStock < 0)
        {
            throw new BusinessException(400, "目标库存不能小于 0");
        }

        if (targetStock > MaxStock)
        {
            throw new BusinessException(400, $"目标库存不能超过 {MaxStock}");
        }
    }

    public static void ValidatePrice(decimal price)
    {
        if (price <= 0)
        {
            throw new BusinessException(400, "规格价格必须大于 0");
        }

        if (price > MaxPrice)
        {
            throw new BusinessException(400, $"规格价格不能超过 {MaxPrice}");
        }
    }

    public static string? NormalizeRemark(string? remark)
    {
        var normalized = remark?.Trim();

        if (string.IsNullOrEmpty(normalized))
        {
            return null;
        }

        if (normalized.Length > RemarkMaxLength)
        {
            throw new BusinessException(400, $"备注不能超过 {RemarkMaxLength} 个字符");
        }

        return normalized;
    }
}

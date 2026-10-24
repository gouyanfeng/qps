using QPS.Mall.Domain.Entities;

namespace QPS.Mall.Application.Features.MallFreightRules;

/// <summary>
/// 按重量计算运费：命中满额包邮为 0，否则首重 + 续重步长计价。
/// </summary>
internal static class MallFreightCalculator
{
    public static decimal Calculate(int totalWeightGram, decimal goodsAmount, MallFreightRule rule)
    {
        if (rule.FreeShippingAmount.HasValue && goodsAmount >= rule.FreeShippingAmount.Value)
        {
            return 0m;
        }

        if (totalWeightGram <= rule.FirstWeightGram)
        {
            return rule.FirstWeightFee;
        }

        var extraWeight = totalWeightGram - rule.FirstWeightGram;
        var steps = (int)Math.Ceiling(extraWeight / (double)rule.AdditionalWeightGram);

        return rule.FirstWeightFee + steps * rule.AdditionalWeightFee;
    }
}

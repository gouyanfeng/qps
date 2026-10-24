using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallFreightRules;

/// <summary>
/// 运费规则字段校验。
/// </summary>
internal static class MallFreightRuleRules
{
    public static void Validate(MallFreightRuleUpsertRequest request)
    {
        if (request.FirstWeightGram <= 0)
        {
            throw new BusinessException(400, "首重重量必须大于 0");
        }

        if (request.FirstWeightFee < 0)
        {
            throw new BusinessException(400, "首重价格不能小于 0");
        }

        if (request.AdditionalWeightGram <= 0)
        {
            throw new BusinessException(400, "续重步长必须大于 0");
        }

        if (request.AdditionalWeightFee < 0)
        {
            throw new BusinessException(400, "续重价格不能小于 0");
        }

        if (request.FreeShippingAmount.HasValue && request.FreeShippingAmount.Value <= 0)
        {
            throw new BusinessException(400, "满额包邮金额必须大于 0");
        }
    }
}

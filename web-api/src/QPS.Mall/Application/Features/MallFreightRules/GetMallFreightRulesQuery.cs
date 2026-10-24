using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;

namespace QPS.Mall.Application.Features.MallFreightRules;

/// <summary>
/// 查询全部门店运费规则。
/// </summary>
public class GetMallFreightRulesQuery : IRequest<List<MallFreightRuleDto>>
{
    /// <summary>
    /// 关键字，匹配门店编码或名称。
    /// </summary>
    public string? Keyword { get; set; }
}

public class GetMallFreightRulesHandler : IRequestHandler<GetMallFreightRulesQuery, List<MallFreightRuleDto>>
{
    private readonly IMallDbContext _dbContext;

    public GetMallFreightRulesHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<MallFreightRuleDto>> Handle(
        GetMallFreightRulesQuery request,
        CancellationToken cancellationToken)
    {
        var query =
            from rule in _dbContext.MallFreightRules
            join store in _dbContext.MallStores on rule.StoreId equals store.Id
            where !rule.IsDeleted && !store.IsDeleted
            select new { Rule = rule, Store = store };

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(item =>
                item.Store.Code.Contains(keyword) ||
                item.Store.Name.Contains(keyword));
        }

        return await query
            .OrderBy(item => item.Store.Code)
            .Select(item => new MallFreightRuleDto
            {
                Id = item.Rule.Id,
                StoreId = item.Rule.StoreId,
                StoreName = item.Store.Name,
                FirstWeightGram = item.Rule.FirstWeightGram,
                FirstWeightFee = item.Rule.FirstWeightFee,
                AdditionalWeightGram = item.Rule.AdditionalWeightGram,
                AdditionalWeightFee = item.Rule.AdditionalWeightFee,
                FreeShippingAmount = item.Rule.FreeShippingAmount,
                IsActive = item.Rule.IsActive,
                CreatedAt = item.Rule.CreatedAt,
                UpdatedAt = item.Rule.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }
}

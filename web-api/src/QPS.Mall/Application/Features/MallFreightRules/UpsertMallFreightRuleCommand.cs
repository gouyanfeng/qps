using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallFreightRules;

/// <summary>
/// 新增或更新门店运费规则。
/// </summary>
public class UpsertMallFreightRuleCommand : IRequest<bool>
{
    public Guid StoreId { get; set; }

    public MallFreightRuleUpsertRequest Request { get; set; } = null!;
}

public class UpsertMallFreightRuleHandler : IRequestHandler<UpsertMallFreightRuleCommand, bool>
{
    private readonly IMallDbContext _dbContext;

    public UpsertMallFreightRuleHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(UpsertMallFreightRuleCommand request, CancellationToken cancellationToken)
    {
        var storeExists = await _dbContext.MallStores.AnyAsync(
            store => store.Id == request.StoreId && !store.IsDeleted,
            cancellationToken);

        if (!storeExists)
        {
            throw new BusinessException(404, "门店不存在");
        }

        MallFreightRuleRules.Validate(request.Request);

        var rule = await _dbContext.MallFreightRules
            .FirstOrDefaultAsync(item => item.StoreId == request.StoreId && !item.IsDeleted, cancellationToken);

        if (rule is null)
        {
            _dbContext.MallFreightRules.Add(MallFreightRule.Create(
                request.StoreId,
                request.Request.FirstWeightGram,
                request.Request.FirstWeightFee,
                request.Request.AdditionalWeightGram,
                request.Request.AdditionalWeightFee,
                request.Request.FreeShippingAmount,
                request.Request.IsActive));
        }
        else
        {
            rule.Update(
                request.Request.FirstWeightGram,
                request.Request.FirstWeightFee,
                request.Request.AdditionalWeightGram,
                request.Request.AdditionalWeightFee,
                request.Request.FreeShippingAmount,
                request.Request.IsActive);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}

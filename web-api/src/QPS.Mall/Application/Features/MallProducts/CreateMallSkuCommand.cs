using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallProducts;

/// <summary>
/// 新增商品规格。
/// </summary>
public class CreateMallSkuCommand : IRequest<bool>
{
    public Guid ProductId { get; set; }

    public MallSkuCreateRequest Request { get; set; } = null!;
}

public class CreateMallSkuHandler : IRequestHandler<CreateMallSkuCommand, bool>
{
    private readonly IMallDbContext _dbContext;

    public CreateMallSkuHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(CreateMallSkuCommand request, CancellationToken cancellationToken)
    {
        var productExists = await _dbContext.MallProducts.AnyAsync(
            product => product.Id == request.ProductId && !product.IsDeleted,
            cancellationToken);

        if (!productExists)
        {
            throw new BusinessException(404, "商品不存在");
        }

        var skuCode = MallProductRules.NormalizeSkuCode(request.Request.SkuCode);
        var specName = MallProductRules.NormalizeSpecName(request.Request.SpecName);
        MallProductRules.ValidatePrice(request.Request.Price);
        MallProductRules.ValidateMarketPrice(request.Request.MarketPrice);
        MallProductRules.ValidateWeight(request.Request.WeightGram);
        MallProductRules.ValidateStock(request.Request.Stock);
        MallProductRules.ValidateWarnStock(request.Request.WarnStock);

        await MallProductRules.EnsureSkuCodeNotDuplicatedAsync(_dbContext, skuCode, null, cancellationToken);

        _dbContext.MallSkus.Add(MallSku.Create(
            request.ProductId,
            skuCode,
            specName,
            request.Request.Price,
            request.Request.WeightGram,
            request.Request.Stock,
            request.Request.WarnStock,
            request.Request.IsActive,
            marketPrice: request.Request.MarketPrice));

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallProducts;

public class CopyMallProductCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}

public class CopyMallProductHandler : IRequestHandler<CopyMallProductCommand, bool>
{
    private readonly IMallDbContext _dbContext;

    public CopyMallProductHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(CopyMallProductCommand request, CancellationToken cancellationToken)
    {
        var source = await _dbContext.MallProducts
            .FirstOrDefaultAsync(product => product.Id == request.Id, cancellationToken);

        if (source is null)
        {
            throw new BusinessException(404, "商品不存在");
        }

        var sourceSkus = await _dbContext.MallSkus
            .Where(sku => sku.ProductId == request.Id)
            .ToListAsync(cancellationToken);

        var sourceTagIds = await _dbContext.MallProductTags
            .Where(link => link.ProductId == request.Id)
            .Select(link => link.TagId)
            .ToListAsync(cancellationToken);

        var newCode = TruncateForCopy(source.Code, "-COPY-" + ShortId(), MallProductRules.CodeMaxLength);
        var newName = TruncateForCopy(source.Name, "拷贝", MallProductRules.NameMaxLength);

        var product = MallProduct.Create(
            source.StoreId,
            newCode,
            newName,
            source.Description,
            source.CategoryId,
            source.MainImageUrl,
            source.AlbumImages,
            source.DetailHtml,
            source.SortOrder,
            false,
            false,
            source.FreightTemplateId);
        _dbContext.MallProducts.Add(product);
        await _dbContext.SaveChangesAsync(cancellationToken);

        foreach (var sku in sourceSkus)
        {
            var newSkuCode = TruncateForCopy(sku.SkuCode, "-COPY-" + ShortId(), MallProductRules.SkuCodeMaxLength);

            var newSku = MallSku.Create(
                product.Id,
                newSkuCode,
                sku.SpecName,
                sku.Price,
                sku.WeightGram,
                sku.Stock,
                sku.WarnStock,
                sku.IsActive,
                sku.VolumeCm3,
                sku.MarketPrice);
            _dbContext.MallSkus.Add(newSku);
        }

        foreach (var tagId in sourceTagIds)
        {
            _dbContext.MallProductTags.Add(MallProductTag.Create(product.Id, tagId));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static string ShortId()
    {
        return Guid.NewGuid().ToString("N")[..6];
    }

    private static string TruncateForCopy(string original, string suffix, int maxLength)
    {
        if (suffix.Length >= maxLength)
        {
            return suffix[..maxLength];
        }

        var maxOriginal = maxLength - suffix.Length;

        return maxOriginal >= original.Length ? original + suffix : original[..maxOriginal] + suffix;
    }
}
using MediatR;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;

namespace QPS.Mall.Application.Features.MallProducts;

/// <summary>
/// 新增商场商品。
/// </summary>
public class CreateMallProductCommand : IRequest<bool>
{
    public MallProductCreateRequest Request { get; set; } = null!;
}

public class CreateMallProductHandler : IRequestHandler<CreateMallProductCommand, bool>
{
    private readonly IMallDbContext _dbContext;

    public CreateMallProductHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(CreateMallProductCommand request, CancellationToken cancellationToken)
    {
        var code = MallProductRules.NormalizeCode(request.Request.Code);
        var name = MallProductRules.NormalizeName(request.Request.Name);
        var description = MallProductRules.NormalizeDescription(request.Request.Description);
        var mainImageUrl = MallProductRules.NormalizeMainImageUrl(request.Request.MainImageUrl);
        var albumImages = MallProductRules.NormalizeAlbumImages(request.Request.AlbumImages);
        var detailHtml = MallProductRules.NormalizeDetailHtml(request.Request.DetailHtml);
        MallProductRules.ValidateSortOrder(request.Request.SortOrder);

        await MallProductRules.EnsureCodeNotDuplicatedAsync(_dbContext, code, null, cancellationToken);
        await MallProductRules.EnsureStoreValidAsync(_dbContext, request.Request.StoreId, cancellationToken);
        await MallProductRules.EnsureCategoryValidAsync(_dbContext, request.Request.CategoryId, cancellationToken);
        var tags = await MallProductRules.GetActiveTagsAsync(
            _dbContext,
            request.Request.TagIds,
            cancellationToken);

        var product = MallProduct.Create(
            request.Request.StoreId,
            code,
            name,
            description,
            request.Request.CategoryId,
            mainImageUrl,
            MallProductRules.SerializeAlbumImages(albumImages),
            detailHtml,
            request.Request.SortOrder,
            request.Request.IsActive,
            request.Request.IsHot);
        _dbContext.MallProducts.Add(product);

        foreach (var tag in tags)
        {
            _dbContext.MallProductTags.Add(MallProductTag.Create(product.Id, tag.Id));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
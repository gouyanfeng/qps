using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallProducts;

public class UpdateMallProductCommand : IRequest<bool>
{
    public Guid Id { get; set; }

    public MallProductUpdateRequest Request { get; set; } = null!;
}

public class UpdateMallProductHandler : IRequestHandler<UpdateMallProductCommand, bool>
{
    private readonly IMallDbContext _dbContext;

    public UpdateMallProductHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(UpdateMallProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _dbContext.MallProducts
            .FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken);

        if (product is null)
        {
            throw new BusinessException(404, "商品不存在");
        }

        var code = MallProductRules.NormalizeCode(request.Request.Code);
        var name = MallProductRules.NormalizeName(request.Request.Name);
        var description = MallProductRules.NormalizeDescription(request.Request.Description);
        var mainImageUrl = MallProductRules.NormalizeMainImageUrl(request.Request.MainImageUrl);
        var albumImages = MallProductRules.NormalizeAlbumImages(request.Request.AlbumImages);
        var detailHtml = MallProductRules.NormalizeDetailHtml(request.Request.DetailHtml);
        MallProductRules.ValidateSortOrder(request.Request.SortOrder);

        await MallProductRules.EnsureCodeNotDuplicatedAsync(_dbContext, code, request.Id, cancellationToken);
        await MallProductRules.EnsureStoreValidAsync(_dbContext, request.Request.StoreId, cancellationToken);
        await MallProductRules.EnsureCategoryValidAsync(_dbContext, request.Request.CategoryId, cancellationToken);
        await MallProductRules.EnsureFreightTemplateValidAsync(
            _dbContext,
            request.Request.FreightTemplateId,
            cancellationToken);
        var tags = await MallProductRules.GetActiveTagsAsync(
            _dbContext,
            request.Request.TagIds,
            cancellationToken);

        product.Update(
            request.Request.StoreId,
            code,
            name,
            description,
            request.Request.CategoryId,
            mainImageUrl,
            MallProductRules.SerializeAlbumImages(albumImages),
            detailHtml,
            request.Request.SortOrder,
            request.Request.IsHot,
            request.Request.FreightTemplateId);

        var existingLinks = await _dbContext.MallProductTags
            .Where(link => link.ProductId == product.Id)
            .ToListAsync(cancellationToken);
        foreach (var link in existingLinks)
        {
            link.SoftDelete();
        }

        foreach (var tag in tags)
        {
            _dbContext.MallProductTags.Add(MallProductTag.Create(product.Id, tag.Id));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
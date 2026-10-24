using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallProducts;

/// <summary>
/// 编辑商场商品。
/// </summary>
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
            .FirstOrDefaultAsync(item => item.Id == request.Id && !item.IsDeleted, cancellationToken);

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
        await MallProductRules.EnsureCategoryValidAsync(_dbContext, request.Request.CategoryId, cancellationToken);

        product.Update(
            code,
            name,
            description,
            request.Request.CategoryId,
            mainImageUrl,
            MallProductRules.SerializeAlbumImages(albumImages),
            detailHtml,
            request.Request.SortOrder);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}

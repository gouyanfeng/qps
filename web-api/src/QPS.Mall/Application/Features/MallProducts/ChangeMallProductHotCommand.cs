using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallProducts;

/// <summary>
/// 设置商品是否在首页精选展示。
/// </summary>
public class ChangeMallProductHotCommand : IRequest<bool>
{
    public Guid Id { get; set; }

    public MallProductHotRequest Request { get; set; } = null!;
}

public class ChangeMallProductHotHandler : IRequestHandler<ChangeMallProductHotCommand, bool>
{
    private readonly IMallDbContext _dbContext;

    public ChangeMallProductHotHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 更新商品首页精选标记，下架商品仍可保留标记但不会在首页展示。
    /// </summary>
    public async Task<bool> Handle(ChangeMallProductHotCommand request, CancellationToken cancellationToken)
    {
        var product = await _dbContext.MallProducts
            .FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken);

        if (product is null)
        {
            throw new BusinessException(404, "商品不存在");
        }

        product.SetHot(request.Request.IsHot);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}

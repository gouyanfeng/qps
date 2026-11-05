using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallCarts;

public class GetMallCartQuery : IRequest<MallCartDto>
{
    public Guid UserId { get; set; }
}

public class GetMallCartHandler : IRequestHandler<GetMallCartQuery, MallCartDto>
{
    private readonly IMallDbContext _dbContext;

    public GetMallCartHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MallCartDto> Handle(
        GetMallCartQuery request,
        CancellationToken cancellationToken)
    {
        var cart = await _dbContext.MallCarts
            .FirstOrDefaultAsync(
                c => c.UserId == request.UserId,
                cancellationToken);

        if (cart is null)
        {
            return new MallCartDto
            {
                UserId = request.UserId,
                TotalQuantity = 0,
                TotalAmount = 0,
                Items = new List<MallCartItemDto>()
            };
        }

        var cartItems = await _dbContext.MallCartItems
            .Where(i => i.CartId == cart.Id)
            .OrderBy(i => i.CreatedAt)
            .ToListAsync(cancellationToken);

        if (cartItems.Count == 0)
        {
            return new MallCartDto
            {
                Id = cart.Id,
                UserId = cart.UserId,
                TotalQuantity = 0,
                TotalAmount = 0,
                LastActiveAt = cart.LastActiveAt,
                Items = new List<MallCartItemDto>()
            };
        }

        var skuIds = cartItems.Select(i => i.SkuId).Distinct().ToList();
        var skuRows = await (
                from sku in _dbContext.MallSkus
                join product in _dbContext.MallProducts on sku.ProductId equals product.Id
                where skuIds.Contains(sku.Id)
                select new { Sku = sku, Product = product })
            .ToListAsync(cancellationToken);

        var skuMap = skuRows.ToDictionary(r => r.Sku.Id);
        var items = new List<MallCartItemDto>();
        var totalAmount = 0m;
        var totalQuantity = 0;

        foreach (var cartItem in cartItems)
        {
            if (skuMap.TryGetValue(cartItem.SkuId, out var row))
            {
                var lineAmount = row.Sku.Price * cartItem.Quantity;
                totalAmount += lineAmount;
                totalQuantity += cartItem.Quantity;

                var available = Math.Min(row.Sku.Stock, cartItem.Quantity);
                var isValid = row.Sku.IsActive && row.Product.IsActive && row.Sku.Stock >= cartItem.Quantity;

                items.Add(new MallCartItemDto
                {
                    Id = cartItem.Id,
                    SkuId = cartItem.SkuId,
                    ProductId = cartItem.ProductId,
                    Quantity = cartItem.Quantity,
                    ProductName = row.Product.Name,
                    ProductCode = row.Product.Code,
                    ProductImage = row.Product.MainImageUrl,
                    SkuCode = row.Sku.SkuCode,
                    SpecName = row.Sku.SpecName,
                    Price = row.Sku.Price,
                    Stock = row.Sku.Stock,
                    WeightGram = row.Sku.WeightGram,
                    AvailableStock = available,
                    LineAmount = lineAmount,
                    IsValid = isValid,
                    InvalidReason = !row.Sku.IsActive || !row.Product.IsActive
                        ? "商品已下架"
                        : row.Sku.Stock < cartItem.Quantity
                            ? $"库存不足（仅剩 {row.Sku.Stock}）"
                            : null
                });
            }
            else
            {
                items.Add(new MallCartItemDto
                {
                    Id = cartItem.Id,
                    SkuId = cartItem.SkuId,
                    ProductId = cartItem.ProductId,
                    Quantity = cartItem.Quantity,
                    IsValid = false,
                    InvalidReason = "商品已删除"
                });
            }
        }

        return new MallCartDto
        {
            Id = cart.Id,
            UserId = cart.UserId,
            TotalQuantity = totalQuantity,
            TotalAmount = totalAmount,
            LastActiveAt = cart.LastActiveAt,
            Items = items
        };
    }
}
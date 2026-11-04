using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Common;
using QPS.Shared.Extensions;

namespace QPS.Mall.Application.Features.MallStores;

/// <summary>
/// 分页查询商城门店。
/// </summary>
public class GetMallStoresQuery : PaginationRequest, IRequest<PaginationResponse<MallStoreDto>>
{
    public GetMallStoresQuery()
    {
        SortField = nameof(MallStoreDto.CreatedAt);
        SortDirection = "Descending";
    }

    /// <summary>
    /// 关键字，匹配门店编码、名称或地址。
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 按启用状态过滤。
    /// </summary>
    public bool? IsActive { get; set; }
}

public class GetMallStoresHandler : IRequestHandler<GetMallStoresQuery, PaginationResponse<MallStoreDto>>
{
    private readonly IMallDbContext _dbContext;

    public GetMallStoresHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginationResponse<MallStoreDto>> Handle(
        GetMallStoresQuery request,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.MallStores
            .Where(store => !store.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(store =>
                store.Code.Contains(keyword) ||
                store.Name.Contains(keyword) ||
                store.Address.Contains(keyword));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(store => store.IsActive == request.IsActive.Value);
        }

        var dtoQuery = query.Select(store => new MallStoreDto
        {
            Id = store.Id,
            Code = store.Code,
            Name = store.Name,
            Address = store.Address,
            IsActive = store.IsActive,
            CreatedAt = store.CreatedAt,
            UpdatedAt = store.UpdatedAt
        });

        return await dtoQuery.ToPaginationResponseAsync(request, cancellationToken);
    }
}
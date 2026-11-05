using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallStores;

/// <summary>
/// 查询商场门店详情。
/// </summary>
public class GetMallStoreQuery : IRequest<MallStoreDto>
{
    public Guid Id { get; set; }
}

public class GetMallStoreHandler : IRequestHandler<GetMallStoreQuery, MallStoreDto>
{
    private readonly IMallDbContext _dbContext;

    public GetMallStoreHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MallStoreDto> Handle(GetMallStoreQuery request, CancellationToken cancellationToken)
    {
        var dto = await _dbContext.MallStores
            .AsNoTracking()
            .Where(store => store.Id == request.Id)
            .Select(store => new MallStoreDto
            {
                Id = store.Id,
                Code = store.Code,
                Name = store.Name,
                Address = store.Address,
                IsActive = store.IsActive,
                CreatedAt = store.CreatedAt,
                UpdatedAt = store.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (dto is null)
        {
            throw new BusinessException(404, "门店不存在");
        }

        return dto;
    }
}

using QPS.System.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.System.Contracts.DTOs.ChinaRegions;
using QPS.Shared.Contracts;
using QPS.Shared.Common;

namespace QPS.System.Application.Features.ChinaRegions;

public class GetChinaRegionsQuery : IRequest<PaginationResponse<ChinaRegionDto>>, IAllowAnonymousRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Code { get; set; }
    public string? Name { get; set; }
    public int? Level { get; set; }
    public bool? IsActive { get; set; }
}

public class GetChinaRegionsQueryHandler : IRequestHandler<GetChinaRegionsQuery, PaginationResponse<ChinaRegionDto>>
{
    private readonly ISystemDbContext _dbContext;

    public GetChinaRegionsQueryHandler(ISystemDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginationResponse<ChinaRegionDto>> Handle(GetChinaRegionsQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.SystemChinaRegions
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            query = query.Where(region => region.Code.Contains(request.Code));
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            query = query.Where(region => region.Name.Contains(request.Name) || region.FullName.Contains(request.Name));
        }

        if (request.Level.HasValue)
        {
            query = query.Where(region => region.Level == request.Level.Value);
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(region => region.IsActive == request.IsActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var orderedQuery = query
            .OrderBy(region => region.Level)
            .ThenBy(region => region.SortOrder)
            .ThenBy(region => region.Code)
            .Select(region => new ChinaRegionDto
            {
                Code = region.Code,
                Name = region.Name,
                FullName = region.FullName,
                Level = region.Level,
                ParentCode = region.ParentCode,
                ProvinceCode = region.ProvinceCode,
                CityCode = region.CityCode,
                SortOrder = region.SortOrder,
                IsActive = region.IsActive
            });

        List<ChinaRegionDto> list;

        if (request.PageSize <= 0)
        {
            list = await orderedQuery.ToListAsync(cancellationToken);
            return new PaginationResponse<ChinaRegionDto>(list, list.Count, 1, list.Count);
        }

        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 500);

        list = await orderedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginationResponse<ChinaRegionDto>(list, totalCount, page, pageSize);
    }
}
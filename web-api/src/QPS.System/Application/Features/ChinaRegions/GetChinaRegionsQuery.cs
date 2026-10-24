using QPS.System.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.System.Contracts.DTOs.ChinaRegions;
using QPS.Shared.Contracts;

namespace QPS.System.Application.Features.ChinaRegions;

public class GetChinaRegionsQuery : IRequest<List<ChinaRegionDto>>, IAllowAnonymousRequest
{
    public bool ActiveOnly { get; set; } = true;
}

public class GetChinaRegionsQueryHandler : IRequestHandler<GetChinaRegionsQuery, List<ChinaRegionDto>>
{
    private readonly ISystemDbContext _dbContext;

    public GetChinaRegionsQueryHandler(ISystemDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<ChinaRegionDto>> Handle(GetChinaRegionsQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.SystemChinaRegions
            .AsNoTracking()
            .Where(region => !region.IsDeleted);

        if (request.ActiveOnly)
        {
            query = query.Where(region => region.IsActive);
        }

        return await query
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
            })
            .ToListAsync(cancellationToken);
    }
}



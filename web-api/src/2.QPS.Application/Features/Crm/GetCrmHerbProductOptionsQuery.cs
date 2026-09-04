using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Contracts.Crm;
using QPS.Application.Interfaces;

namespace QPS.Application.Features.Crm;

public class GetCrmHerbProductOptionsQuery : IRequest<List<AttributeOptionDto>>
{
    public string? Keyword { get; set; }

    public int PageSize { get; set; } = 100;
}

public class GetCrmHerbProductOptionsHandler : IRequestHandler<GetCrmHerbProductOptionsQuery, List<AttributeOptionDto>>
{
    private readonly IDbContext _dbContext;

    public GetCrmHerbProductOptionsHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<AttributeOptionDto>> Handle(
        GetCrmHerbProductOptionsQuery request,
        CancellationToken cancellationToken)
    {
        var rootId = await _dbContext.SystemDataDictionaries
            .Where(item => !item.IsDeleted && item.Code == CrmCodes.HerbProductDictionaryCode)
            .Select(item => (Guid?)item.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (!rootId.HasValue)
        {
            return [];
        }

        var query = _dbContext.SystemDataDictionaries
            .AsNoTracking()
            .Where(item => !item.IsDeleted && item.IsActive && item.ParentId == rootId.Value);
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            query = query.Where(item => item.Name.Contains(request.Keyword));
        }

        return await query
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Name)
            .Take(Math.Clamp(request.PageSize, 1, 200))
            .Select(item => new AttributeOptionDto { Label = item.Name, Value = item.Name })
            .ToListAsync(cancellationToken);
    }
}

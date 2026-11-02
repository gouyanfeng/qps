using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Crm.Contracts;
using QPS.Crm.Contracts.DTOs;

namespace QPS.Crm.Application.Features;

public class GetCrmHerbProductOptionsQuery : IRequest<List<AttributeOptionDto>>
{
    public string? Keyword { get; set; }

    public int PageSize { get; set; } = 100;
}

public class GetCrmHerbProductOptionsHandler : IRequestHandler<GetCrmHerbProductOptionsQuery, List<AttributeOptionDto>>
{
    private readonly ICrmDbContext _dbContext;

    public GetCrmHerbProductOptionsHandler(ICrmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<AttributeOptionDto>> Handle(
        GetCrmHerbProductOptionsQuery request,
        CancellationToken cancellationToken)
    {
        var rootId = await _dbContext.SystemDataDictionaries
            .AsNoTracking()
            .Where(item => item.Code == CrmCodes.HerbProductDictionaryCode)
            .Select(item => (Guid?)item.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (!rootId.HasValue)
        {
            return [];
        }

        var items = await _dbContext.SystemDataDictionaries
            .AsNoTracking()
            .Where(item => item.ParentId == rootId.Value && item.IsActive)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Name)
            .Select(item => item.Name)
            .ToListAsync(cancellationToken);

        var keyword = request.Keyword;
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            items = items
                .Where(item => item.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return items
            .Take(Math.Clamp(request.PageSize, 1, 200))
            .Select(item => new AttributeOptionDto { Label = item, Value = item })
            .ToList();
    }
}

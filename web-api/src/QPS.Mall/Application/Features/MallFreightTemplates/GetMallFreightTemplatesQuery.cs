using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;

namespace QPS.Mall.Application.Features.MallFreightTemplates;

public class GetMallFreightTemplatesQuery : IRequest<List<MallFreightTemplateListDto>>
{
    public string? Keyword { get; set; }
}

public class GetMallFreightTemplatesHandler : IRequestHandler<GetMallFreightTemplatesQuery, List<MallFreightTemplateListDto>>
{
    private readonly IMallDbContext _dbContext;

    public GetMallFreightTemplatesHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<MallFreightTemplateListDto>> Handle(
        GetMallFreightTemplatesQuery request,
        CancellationToken cancellationToken)
    {
        var templates = await _dbContext.MallFreightTemplates
            .Where(t => !t.IsDeleted)
            .ToListAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            templates = templates.Where(x =>
                x.Name.Contains(keyword)).ToList();
        }

        var templateIds = templates.Select(x => x.Id).ToList();

        var regionCounts = await _dbContext.MallFreightRegions
            .Where(r => templateIds.Contains(r.TemplateId) && !r.IsDeleted)
            .GroupBy(r => r.TemplateId)
            .Select(g => new { TemplateId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TemplateId, x => x.Count, cancellationToken);

        return templates
            .OrderBy(x => x.IsDefault ? 0 : 1)
            .ThenBy(x => x.Name)
            .Select(x => new MallFreightTemplateListDto
            {
                Id = x.Id,
                Name = x.Name,
                BillingType = x.BillingType,
                BillingTypeName = MallFreightTemplateRules.GetBillingTypeName(x.BillingType),
                IsDefault = x.IsDefault,
                IsActive = x.IsActive,
                RegionCount = regionCounts.TryGetValue(x.Id, out var count) ? count : 0,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToList();
    }
}
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;

namespace QPS.Mall.Application.Features.MallFreightTemplates;

public class GetMallFreightTemplatesQuery : IRequest<List<MallFreightTemplateListDto>>
{
    public Guid? StoreId { get; set; }

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
        var templates = await (
                from t in _dbContext.MallFreightTemplates
                join s in _dbContext.MallStores on t.StoreId equals s.Id
                where !t.IsDeleted && !s.IsDeleted
                select new { Template = t, Store = s })
            .ToListAsync(cancellationToken);

        if (request.StoreId.HasValue)
        {
            templates = templates.Where(x => x.Template.StoreId == request.StoreId.Value).ToList();
        }

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            templates = templates.Where(x =>
                x.Template.Name.Contains(keyword) ||
                x.Store.Name.Contains(keyword) ||
                x.Store.Code.Contains(keyword)).ToList();
        }

        var templateIds = templates.Select(x => x.Template.Id).ToList();

        var regionCounts = await _dbContext.MallFreightRegions
            .Where(r => templateIds.Contains(r.TemplateId) && !r.IsDeleted)
            .GroupBy(r => r.TemplateId)
            .Select(g => new { TemplateId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TemplateId, x => x.Count, cancellationToken);

        return templates
            .OrderBy(x => x.Store.Code)
            .ThenBy(x => x.Template.IsDefault ? 0 : 1)
            .ThenBy(x => x.Template.Name)
            .Select(x => new MallFreightTemplateListDto
            {
                Id = x.Template.Id,
                StoreId = x.Template.StoreId,
                StoreName = x.Store.Name,
                Name = x.Template.Name,
                BillingType = x.Template.BillingType,
                BillingTypeName = MallFreightTemplateRules.GetBillingTypeName(x.Template.BillingType),
                IsDefault = x.Template.IsDefault,
                IsActive = x.Template.IsActive,
                RegionCount = regionCounts.TryGetValue(x.Template.Id, out var count) ? count : 0,
                CreatedAt = x.Template.CreatedAt,
                UpdatedAt = x.Template.UpdatedAt
            })
            .ToList();
    }
}
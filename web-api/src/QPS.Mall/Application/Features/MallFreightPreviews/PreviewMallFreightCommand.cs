using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Application.Features.MallFreightRules;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallFreightPreviews;

public class PreviewMallFreightCommand : IRequest<MallFreightPreviewDto>
{
    public MallFreightPreviewRequest Request { get; set; } = null!;
}

public class PreviewMallFreightHandler : IRequestHandler<PreviewMallFreightCommand, MallFreightPreviewDto>
{
    private readonly IMallDbContext _dbContext;

    public PreviewMallFreightHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MallFreightPreviewDto> Handle(
        PreviewMallFreightCommand command,
        CancellationToken cancellationToken)
    {
        var req = command.Request;

        if (req.Items.Count == 0)
            throw new BusinessException(400, "请选择商品");
        if (req.Items.Any(i => i.Quantity <= 0))
            throw new BusinessException(400, "商品数量必须大于 0");
        if (string.IsNullOrWhiteSpace(req.ProvinceName) || string.IsNullOrWhiteSpace(req.CityName))
            throw new BusinessException(400, "请选择完整的收货地址");

        var defaultTemplate = await _dbContext.MallFreightTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.IsDefault && t.IsActive && !t.IsDeleted, cancellationToken);

        var skuIds = req.Items.Select(i => i.SkuId).Distinct().ToList();
        var skuRows = await (
                from sku in _dbContext.MallSkus
                join product in _dbContext.MallProducts on sku.ProductId equals product.Id
                where skuIds.Contains(sku.Id) && !sku.IsDeleted && !product.IsDeleted
                select new { Sku = sku, Product = product })
            .ToListAsync(cancellationToken);

        if (skuRows.Count != skuIds.Count)
            throw new BusinessException(400, "部分商品规格不存在或已下架");

        var skuMap = skuRows.ToDictionary(r => r.Sku.Id);

        var templateIds = new HashSet<Guid>();
        if (defaultTemplate is not null)
            templateIds.Add(defaultTemplate.Id);
        foreach (var row in skuRows)
        {
            if (row.Product.FreightTemplateId.HasValue)
                templateIds.Add(row.Product.FreightTemplateId.Value);
        }

        var templateMap = new Dictionary<Guid, MallFreightTemplate>();
        var regionMap = new Dictionary<Guid, List<MallFreightRegion>>();

        if (templateIds.Count > 0)
        {
            var templates = await _dbContext.MallFreightTemplates
                .AsNoTracking()
                .Where(t => templateIds.Contains(t.Id) && !t.IsDeleted && t.IsActive)
                .ToListAsync(cancellationToken);

            templateMap = templates.ToDictionary(t => t.Id);

            var regions = await _dbContext.MallFreightRegions
                .AsNoTracking()
                .Where(r => templateIds.Contains(r.TemplateId) && !r.IsDeleted)
                .ToListAsync(cancellationToken);

            regionMap = regions.GroupBy(r => r.TemplateId)
                .ToDictionary(g => g.Key, g => g.OrderBy(r => r.IsDefaultRegion).ToList());
        }

        var items = new List<FreightItemRow>();
        foreach (var item in req.Items)
        {
            var row = skuMap[item.SkuId];
            items.Add(new FreightItemRow
            {
                SkuId = row.Sku.Id,
                ProductId = row.Product.Id,
                FreightTemplateId = row.Product.FreightTemplateId,
                Quantity = item.Quantity,
                WeightGram = row.Sku.WeightGram,
                VolumeCm3 = row.Sku.VolumeCm3,
                LineAmount = row.Sku.Price * item.Quantity
            });
        }

        var calcResult = MallFreightCalculator.Calculate(
            items,
            defaultTemplate?.Id ?? Guid.Empty,
            templateMap,
            regionMap,
            new FreightAddressInfo { ProvinceName = req.ProvinceName, CityName = req.CityName });

        return new MallFreightPreviewDto
        {
            TotalFreight = calcResult.TotalFreight,
            Groups = calcResult.Groups.Select(g => new MallFreightPreviewGroupDto
            {
                TemplateName = g.Template.Name,
                BillingType = g.Template.BillingType,
                RegionName = g.MatchedRegion.RegionName,
                GoodsAmount = g.GoodsAmount,
                Freight = g.FreightAmount,
                IsFreeShipping = g.FreightAmount == 0
            }).ToList()
        };
    }
}
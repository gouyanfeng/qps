using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallFreightTemplates;

public class GetMallFreightTemplateQuery : IRequest<MallFreightTemplateDetailDto>
{
    public Guid TemplateId { get; set; }
}

public class GetMallFreightTemplateHandler : IRequestHandler<GetMallFreightTemplateQuery, MallFreightTemplateDetailDto>
{
    private readonly IMallDbContext _dbContext;

    public GetMallFreightTemplateHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MallFreightTemplateDetailDto> Handle(
        GetMallFreightTemplateQuery request,
        CancellationToken cancellationToken)
    {
        var template = await (
                from t in _dbContext.MallFreightTemplates
                join s in _dbContext.MallStores on t.StoreId equals s.Id
                where t.Id == request.TemplateId && !t.IsDeleted && !s.IsDeleted
                select new { Template = t, Store = s })
            .FirstOrDefaultAsync(cancellationToken);

        if (template is null)
        {
            throw new BusinessException(404, "运费模板不存在");
        }

        var regions = await _dbContext.MallFreightRegions
            .Where(r => r.TemplateId == request.TemplateId && !r.IsDeleted)
            .OrderBy(r => r.IsDefaultRegion ? 1 : 0)
            .ThenBy(r => r.RegionName)
            .Select(r => new MallFreightRegionDto
            {
                Id = r.Id,
                RegionName = r.RegionName,
                ProvinceCodes = r.ProvinceCodes,
                CityCodes = r.CityCodes,
                FirstValue = r.FirstValue,
                FirstFee = r.FirstFee,
                StepValue = r.StepValue,
                StepFee = r.StepFee,
                FreeShippingAmount = r.FreeShippingAmount,
                FreeShippingQuantity = r.FreeShippingQuantity,
                IsDefaultRegion = r.IsDefaultRegion
            })
            .ToListAsync(cancellationToken);

        return new MallFreightTemplateDetailDto
        {
            Id = template.Template.Id,
            StoreId = template.Template.StoreId,
            StoreName = template.Store.Name,
            Name = template.Template.Name,
            BillingType = template.Template.BillingType,
            BillingTypeName = MallFreightTemplateRules.GetBillingTypeName(template.Template.BillingType),
            IsDefault = template.Template.IsDefault,
            IsActive = template.Template.IsActive,
            CreatedAt = template.Template.CreatedAt,
            UpdatedAt = template.Template.UpdatedAt,
            Regions = regions
        };
    }
}
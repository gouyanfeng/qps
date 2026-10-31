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
        var template = await _dbContext.MallFreightTemplates
            .FirstOrDefaultAsync(t => t.Id == request.TemplateId && !t.IsDeleted, cancellationToken);

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
                ProvinceNames = r.ProvinceNames,
                CityNames = r.CityNames,
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
            Id = template.Id,
            Name = template.Name,
            BillingType = template.BillingType,
            BillingTypeName = MallFreightTemplateRules.GetBillingTypeName(template.BillingType),
            IsDefault = template.IsDefault,
            IsActive = template.IsActive,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt,
            Regions = regions
        };
    }
}
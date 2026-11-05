using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallFreightTemplates;

public class CreateMallFreightTemplateCommand : IRequest<Guid>
{
    public MallFreightTemplateCreateRequest Request { get; set; } = null!;
}

public class CreateMallFreightTemplateHandler : IRequestHandler<CreateMallFreightTemplateCommand, Guid>
{
    private readonly IMallDbContext _dbContext;

    public CreateMallFreightTemplateHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid> Handle(CreateMallFreightTemplateCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        MallFreightTemplateRules.ValidateTemplateName(request.Name);
        MallFreightTemplateRules.ValidateBillingType(request.BillingType);
        MallFreightTemplateRules.ValidateRegions(request.Regions);

        var template = MallFreightTemplate.Create(
            request.Name,
            request.BillingType,
            request.IsDefault,
            request.IsActive);

        _dbContext.MallFreightTemplates.Add(template);

        foreach (var regionReq in request.Regions)
        {
            var region = MallFreightRegion.Create(
                template.Id,
                regionReq.RegionName,
                regionReq.ProvinceNames,
                regionReq.CityNames,
                regionReq.FirstValue,
                regionReq.FirstFee,
                regionReq.StepValue,
                regionReq.StepFee,
                regionReq.FreeShippingAmount,
                regionReq.FreeShippingQuantity,
                regionReq.IsDefaultRegion);

            _dbContext.MallFreightRegions.Add(region);
        }

        if (request.IsDefault)
        {
            var existingDefaults = await _dbContext.MallFreightTemplates
                .Where(t => t.IsDefault)
                .ToListAsync(cancellationToken);

            foreach (var existing in existingDefaults)
            {
                if (existing.Id != template.Id)
                {
                    existing.Update(existing.Name, existing.BillingType, false, existing.IsActive);
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return template.Id;
    }
}
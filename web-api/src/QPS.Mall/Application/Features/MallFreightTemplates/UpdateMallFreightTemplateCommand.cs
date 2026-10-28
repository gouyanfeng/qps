using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallFreightTemplates;

public class UpdateMallFreightTemplateCommand : IRequest<bool>
{
    public Guid TemplateId { get; set; }

    public MallFreightTemplateUpdateRequest Request { get; set; } = null!;
}

public class UpdateMallFreightTemplateHandler : IRequestHandler<UpdateMallFreightTemplateCommand, bool>
{
    private readonly IMallDbContext _dbContext;

    public UpdateMallFreightTemplateHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(UpdateMallFreightTemplateCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        var template = await _dbContext.MallFreightTemplates
            .FirstOrDefaultAsync(t => t.Id == command.TemplateId && !t.IsDeleted, cancellationToken);

        if (template is null)
        {
            throw new BusinessException(404, "运费模板不存在");
        }

        MallFreightTemplateRules.ValidateTemplateName(request.Name);
        MallFreightTemplateRules.ValidateBillingType(request.BillingType);
        MallFreightTemplateRules.ValidateRegions(request.Regions);

        template.Update(request.Name, request.BillingType, request.IsDefault, request.IsActive);

        var existingRegions = await _dbContext.MallFreightRegions
            .Where(r => r.TemplateId == template.Id && !r.IsDeleted)
            .ToListAsync(cancellationToken);

        var existingRegionMap = existingRegions.ToDictionary(r => r.Id);

        foreach (var regionReq in request.Regions)
        {
            if (regionReq.Id.HasValue && existingRegionMap.TryGetValue(regionReq.Id.Value, out var existing))
            {
                existing.Update(
                    regionReq.RegionName,
                    regionReq.ProvinceCodes,
                    regionReq.CityCodes,
                    regionReq.FirstValue,
                    regionReq.FirstFee,
                    regionReq.StepValue,
                    regionReq.StepFee,
                    regionReq.FreeShippingAmount,
                    regionReq.FreeShippingQuantity,
                    regionReq.IsDefaultRegion);

                existingRegionMap.Remove(regionReq.Id.Value);
            }
            else
            {
                _dbContext.MallFreightRegions.Add(MallFreightRegion.Create(
                    template.Id,
                    regionReq.RegionName,
                    regionReq.ProvinceCodes,
                    regionReq.CityCodes,
                    regionReq.FirstValue,
                    regionReq.FirstFee,
                    regionReq.StepValue,
                    regionReq.StepFee,
                    regionReq.FreeShippingAmount,
                    regionReq.FreeShippingQuantity,
                    regionReq.IsDefaultRegion));
            }
        }

        foreach (var leftover in existingRegionMap.Values)
        {
            _dbContext.MallFreightRegions.Remove(leftover);
        }

        if (request.IsDefault)
        {
            var existingDefaults = await _dbContext.MallFreightTemplates
                .Where(t => t.StoreId == template.StoreId && t.IsDefault && !t.IsDeleted)
                .ToListAsync(cancellationToken);

            foreach (var existing in existingDefaults)
            {
                if (existing.Id != template.Id)
                {
                    existing.Update(existing.Name, existing.BillingType, false, existing.IsActive);
                }
            }

            var store = await _dbContext.MallStores
                .FirstOrDefaultAsync(s => s.Id == template.StoreId && !s.IsDeleted, cancellationToken);
            if (store is not null)
            {
                store.Update(store.Code, store.Name, store.Address, template.Id);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
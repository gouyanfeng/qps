using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Contracts.Crm;
using QPS.Application.Interfaces;
using QPS.Domain.Entities.Crm;
using QPS.Domain.Exceptions;

namespace QPS.Application.Features.Crm.CrmVendors;

public class CreateCrmPurchaseDemandCommand : IRequest<bool>
{
    public CrmPurchaseDemandSaveRequest Request { get; set; } = null!;
}

public class CreateCrmPurchaseDemandHandler : IRequestHandler<CreateCrmPurchaseDemandCommand, bool>
{
    private readonly IDbContext _dbContext;

    public CreateCrmPurchaseDemandHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(CreateCrmPurchaseDemandCommand request, CancellationToken cancellationToken)
    {
        var vendor = await _dbContext.CrmVendors
            .FirstOrDefaultAsync(vendor => vendor.Id == request.Request.VendorId && !vendor.IsDeleted, cancellationToken);
        if (vendor == null)
        {
            throw new BusinessException(404, "厂商不存在");
        }

        var demandName = request.Request.DemandName.Trim();
        if (string.IsNullOrWhiteSpace(demandName))
        {
            throw new BusinessException(400, "请输入采购需求名称");
        }
        if (request.Request.ContactId.HasValue && !await _dbContext.CrmContacts.AnyAsync(contact => contact.Id == request.Request.ContactId && contact.EntityType == CrmCodes.VendorEntityType && contact.EntityId == vendor.Id && contact.Status != "INVALID", cancellationToken)) throw new BusinessException(400, "联系人不属于该厂商或已失效");
        var items = request.Request.Items.Select((item, index) => new CrmPurchaseDemandItem(item.ProductName.Trim(), item.Quantity, item.QuantityUnit.Trim(), item.Specification.Trim(), item.QualityRequirement.Trim(), item.TargetPrice, item.PriceUnit.Trim(), item.ExpectedDeliveryAt, item.Remark.Trim(), index + 1)).ToList();
        await CrmHerbProductDictionary.ValidateActiveNamesAsync(_dbContext, items.Select(item => item.ProductName), cancellationToken);
        var demand = CrmPurchaseDemand.Create(vendor.Id, $"PD{DateTime.Now:yyyyMMddHHmmss}{Guid.NewGuid():N}"[..24], demandName, request.Request.DemandAt, "人工录入", request.Request.ContactId, request.Request.ExpectedDeliveryAt, request.Request.ReceivingAddress.Trim(), request.Request.SourceUrl.Trim(), request.Request.Remark.Trim(), items);
        _dbContext.CrmPurchaseDemands.Add(demand);
        await CrmPurchaseDemandProducts.ReplaceAsync(
            _dbContext,
            demand.Id,
            items.Select(item => item.ProductName),
            cancellationToken);
        await CrmPurchaseDemands.RefreshLatestAsync(_dbContext, vendor, cancellationToken, demand);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}

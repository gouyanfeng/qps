using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Contracts.Crm;
using QPS.Application.Interfaces;
using QPS.Domain.Exceptions;

namespace QPS.Application.Features.Crm.CrmVendors;

public class UpdateCrmVendorDemandCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public CrmVendorDemandSaveRequest Request { get; set; } = null!;
}

public class UpdateCrmVendorDemandHandler : IRequestHandler<UpdateCrmVendorDemandCommand, bool>
{
    private readonly IDbContext _dbContext;

    public UpdateCrmVendorDemandHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(UpdateCrmVendorDemandCommand request, CancellationToken cancellationToken)
    {
        var vendor = await _dbContext.CrmVendors
            .FirstOrDefaultAsync(item => item.Id == request.Request.VendorId && !item.IsDeleted, cancellationToken);
        if (vendor == null)
        {
            throw new BusinessException(404, "厂商不存在");
        }

        var plan = await _dbContext.CrmVendorDemands
            .Include(item => item.Items)
            .FirstOrDefaultAsync(item => item.Id == request.Id && item.VendorId == request.Request.VendorId && !item.IsDeleted, cancellationToken);
        if (plan == null)
        {
            throw new BusinessException(404, "采购需求不存在");
        }

        var demandName = request.Request.DemandName.Trim();
        if (string.IsNullOrWhiteSpace(demandName))
        {
            throw new BusinessException(400, "请输入采购需求名称");
        }
        if (request.Request.ContactId.HasValue && !await _dbContext.CrmContacts.AnyAsync(contact => contact.Id == request.Request.ContactId && contact.EntityType == CrmCodes.VendorEntityType && contact.EntityId == vendor.Id && contact.Status != "无效", cancellationToken)) throw new BusinessException(400, "联系人不属于该厂商或已失效");
        var existingItems = plan.Items.ToList();
        var existingItemsById = existingItems.ToDictionary(item => item.Id);
        var retainedItemIds = new HashSet<Guid>();
        var newItems = new List<QPS.Domain.Entities.Crm.CrmVendorDemandItem>();
        var items = request.Request.Items.Select((item, index) =>
        {
            if (!item.Id.HasValue)
            {
                var newItem = new QPS.Domain.Entities.Crm.CrmVendorDemandItem(item.ProductName.Trim(), item.Quantity, item.QuantityUnit.Trim(), item.Specification.Trim(), item.QualityRequirement.Trim(), item.TargetPrice, item.PriceUnit.Trim(), item.ExpectedDeliveryAt, item.Remark.Trim(), index + 1);
                newItems.Add(newItem);
                return newItem;
            }

            if (!existingItemsById.TryGetValue(item.Id.Value, out var currentItem))
            {
                throw new BusinessException(400, "采购明细不存在或不属于当前采购需求");
            }

            retainedItemIds.Add(currentItem.Id);
            currentItem.Update(item.ProductName.Trim(), item.Quantity, item.QuantityUnit.Trim(), item.Specification.Trim(), item.QualityRequirement.Trim(), item.TargetPrice, item.PriceUnit.Trim(), item.ExpectedDeliveryAt, item.Remark.Trim(), index + 1);
            return currentItem;
        }).ToList();
        await CrmHerbProductDictionary.ValidateActiveNamesAsync(_dbContext, items.Select(item => item.ProductName), cancellationToken);
        _dbContext.CrmVendorDemandItems.RemoveRange(existingItems.Where(item => !retainedItemIds.Contains(item.Id)));
        plan.Update(vendor.Id, demandName, request.Request.DemandAt, request.Request.ContactId, request.Request.ExpectedDeliveryAt, request.Request.ReceivingAddress.Trim(), request.Request.SourceUrl.Trim(), request.Request.Remark.Trim(), items);
        _dbContext.CrmVendorDemandItems.AddRange(newItems);
        await CrmVendorDemands.RefreshLatestAsync(_dbContext, vendor, cancellationToken, plan);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}

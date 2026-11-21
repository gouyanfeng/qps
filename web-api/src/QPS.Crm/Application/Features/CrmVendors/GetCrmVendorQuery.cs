using QPS.Crm.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Crm.Contracts.DTOs;
using QPS.Crm.Contracts.DTOs.CrmContacts;
using QPS.Crm.Contracts.DTOs.CrmHerbBaseSubjects;
using QPS.Crm.Contracts.DTOs.CrmVendors;
using QPS.Crm.Application.Features.CrmTransfers;
using QPS.Crm.Application.Features;
using QPS.Crm.Domain.Entities;
using QPS.Shared.Contracts;
using QPS.Shared.Exceptions;

namespace QPS.Crm.Application.Features.CrmVendors;

public class GetCrmVendorQuery : IRequest<CrmVendorDto>
{
    public Guid Id { get; set; }
}

public class GetCrmVendorHandler : IRequestHandler<GetCrmVendorQuery, CrmVendorDto>
{
    private const string VendorEntityType = CrmCodes.VendorEntityType;
    private const string InvalidContactStatus = "无效";

    private readonly ICrmDbContext _dbContext;

    public GetCrmVendorHandler(ICrmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CrmVendorDto> Handle(GetCrmVendorQuery request, CancellationToken cancellationToken)
    {
        var vendor = await _dbContext.CrmVendors
            .FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken);

        if (vendor is null)
        {
            throw new BusinessException(404, "厂商不存在");
        }

        var contacts = await _dbContext.CrmContacts
            .Where(contact =>
                contact.EntityType == VendorEntityType &&
                contact.EntityId == vendor.Id)
            .OrderByDescending(contact => contact.IsPrimary)
            .ThenBy(contact => contact.CreatedAt)
            .Select(contact => new CrmContactDto
            {
                Id = contact.Id,
                EntityType = contact.EntityType,
                EntityId = contact.EntityId,
                ContactName = contact.ContactName,
                Phone = contact.Phone,
                PhoneType = contact.PhoneType,
                Wechat = contact.Wechat,
                RoleName = contact.RoleName,
                IsPrimary = contact.IsPrimary,
                Status = contact.Status,
                Remark = contact.Remark,
                CreatedAt = contact.CreatedAt,
                UpdatedAt = contact.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        var products = (await CrmVendorDemandProductQuery.GetProductsAsync(
                _dbContext,
                [vendor.Id],
                cancellationToken))
            .GetValueOrDefault(vendor.Id, []);

        var transferRecords = await CrmTransferRecordQuery.GetAsync(
            _dbContext,
            VendorEntityType,
            vendor.Id,
            cancellationToken);

        var deals = await GetDealsAsync(vendor.Id, vendor.VendorName, cancellationToken);

        var dto = new CrmVendorDto
        {
            Id = vendor.Id,
            VendorName = vendor.VendorName,
            NormalizedVendorName = vendor.NormalizedVendorName,
            PriorityLevel = vendor.PriorityLevel,
            LatestPurchaseTime = vendor.LatestPurchaseTime,
            LatestPurchaseDemandName = vendor.LatestPurchaseDemandName,
            Remark = vendor.Remark,
            OwnerUserId = vendor.OwnerUserId,
            LastFollowAt = vendor.LastFollowAt,
            LastFollowResult = vendor.LastFollowResult,
            NextFollowAt = vendor.NextFollowAt,
            PrimaryContactName = contacts.FirstOrDefault(contact => contact.Status != InvalidContactStatus)?.ContactName ?? string.Empty,
            PrimaryContactPhone = contacts.FirstOrDefault(contact => contact.Status != InvalidContactStatus)?.Phone ?? string.Empty,
            PurchaseDemandCount = await _dbContext.CrmVendorDemands.CountAsync(plan => plan.VendorId == vendor.Id, cancellationToken),
            ProductCount = products.Count,
            ContactCount = contacts.Count,
            CreatedAt = vendor.CreatedAt,
            UpdatedAt = vendor.UpdatedAt,
            Contacts = contacts,
            Products = products,
            TransferRecords = transferRecords,
            Deals = deals
        };

        await CrmVendorOwners.FillAsync(_dbContext, new List<CrmVendorDto> { dto }, cancellationToken);
        return dto;
    }

    /// <summary>
    /// 该厂商作为采购方的历史成交：中选/发起需求 item -> 撮合机会 -> 供货方（实时取原明细）。
    /// </summary>
    private async Task<List<CrmSubjectDealDto>> GetDealsAsync(Guid vendorId, string vendorName, CancellationToken cancellationToken)
    {
        var deals = await (
            from item in _dbContext.CrmMatchOpportunityItems
            join demandItem in _dbContext.CrmVendorDemandItems on item.EntityId equals demandItem.Id
            join demand in _dbContext.CrmVendorDemands on demandItem.VendorDemandId equals demand.Id
            join opportunity in _dbContext.CrmMatchOpportunities on item.OpportunityId equals opportunity.Id
            where item.EntityType == CrmMatchOpportunityItem.EntityTypeDemand
                  && demand.VendorId == vendorId
                  && opportunity.Status == CrmMatchOpportunity.Closed
                  && opportunity.CloseResult == CrmMatchOpportunity.Matched
                  && (item.Status == CrmMatchOpportunityItem.Selected || item.Role == CrmMatchOpportunityItem.Initiator)
                  && !item.IsDeleted
                  && !opportunity.IsDeleted
            orderby opportunity.UpdatedAt descending
            select new CrmSubjectDealDto
            {
                Id = opportunity.Id,
                VendorName = vendorName,
                ProductName = (
                    from supplyItemRow in _dbContext.CrmMatchOpportunityItems
                    where supplyItemRow.OpportunityId == opportunity.Id &&
                        supplyItemRow.EntityType == CrmMatchOpportunityItem.EntityTypeSupply
                    from s in _dbContext.CrmHerbBaseSupplies.Where(s => s.Id == supplyItemRow.EntityId).Take(1)
                    select s.ProductName).FirstOrDefault() ?? demandItem.ProductName,
                BaseName = (
                    from supplyItemRow in _dbContext.CrmMatchOpportunityItems
                    where supplyItemRow.OpportunityId == opportunity.Id &&
                        supplyItemRow.EntityType == CrmMatchOpportunityItem.EntityTypeSupply
                    from s in _dbContext.CrmHerbBaseSupplies.Where(s => s.Id == supplyItemRow.EntityId).Take(1)
                    from hb in _dbContext.CrmHerbBases.Where(h => h.Id == s.HerbBaseId).Take(1)
                    select hb.BaseName).FirstOrDefault() ?? string.Empty,
                CloseResult = opportunity.CloseResult,
                ClosedReason = opportunity.ClosedReason,
                ClosedAt = opportunity.UpdatedAt
            }).ToListAsync(cancellationToken);

        return deals.DistinctBy(deal => deal.Id).ToList();
    }
}



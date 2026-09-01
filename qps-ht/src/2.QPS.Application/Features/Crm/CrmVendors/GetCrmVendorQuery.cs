using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Contracts.Crm;
using QPS.Application.Features.Crm;
using QPS.Application.Interfaces;
using QPS.Domain.Exceptions;

namespace QPS.Application.Features.Crm.CrmVendors;

public class GetCrmVendorQuery : IRequest<CrmVendorDto>
{
    public Guid Id { get; set; }
}

public class GetCrmVendorHandler : IRequestHandler<GetCrmVendorQuery, CrmVendorDto>
{
    private const string VendorEntityType = CrmCodes.VendorEntityType;
    private const string InvalidContactStatus = "INVALID";

    private readonly IDbContext _dbContext;

    public GetCrmVendorHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CrmVendorDto> Handle(GetCrmVendorQuery request, CancellationToken cancellationToken)
    {
        var vendor = await _dbContext.CrmVendors
            .FirstOrDefaultAsync(item => item.Id == request.Id && !item.IsDeleted, cancellationToken);

        if (vendor is null)
        {
            throw new BusinessException(404, "厂商不存在");
        }

        var contacts = await _dbContext.CrmContacts
            .Where(contact =>
                !contact.IsDeleted &&
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

        var products = (await CrmPurchaseDemandProductQuery.GetProductsAsync(
                _dbContext,
                [vendor.Id],
                cancellationToken))
            .GetValueOrDefault(vendor.Id, []);

        var transferRecords = await CrmTransferRecords.GetAsync(
            _dbContext,
            VendorEntityType,
            vendor.Id,
            cancellationToken);

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
            PurchaseDemandCount = await _dbContext.CrmPurchaseDemands.CountAsync(plan => !plan.IsDeleted && plan.VendorId == vendor.Id, cancellationToken),
            ProductCount = products.Count,
            ContactCount = contacts.Count,
            CreatedAt = vendor.CreatedAt,
            UpdatedAt = vendor.UpdatedAt,
            Contacts = contacts,
            Products = products,
            TransferRecords = transferRecords
        };

        await CrmVendorOwners.FillAsync(_dbContext, new List<CrmVendorDto> { dto }, cancellationToken);
        return dto;
    }
}



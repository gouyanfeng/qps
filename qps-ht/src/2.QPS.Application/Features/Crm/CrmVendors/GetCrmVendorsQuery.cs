using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Contracts.Crm;
using QPS.Application.Extensions;
using QPS.Application.Features.Crm;
using QPS.Application.Interfaces;

namespace QPS.Application.Features.Crm.CrmVendors;

public class GetCrmVendorsQuery : PaginationRequest, IRequest<PaginationResponse<CrmVendorDto>>
{
    public string? Keyword { get; set; }

    public string? PriorityLevel { get; set; }

    public bool? HasPhone { get; set; }

    public bool? HasProduct { get; set; }
}

public class GetCrmVendorsHandler : IRequestHandler<GetCrmVendorsQuery, PaginationResponse<CrmVendorDto>>
{
    private const string VendorEntityType = CrmCodes.VendorEntityType;
    private const string InvalidContactStatus = "INVALID";

    private readonly IDbContext _dbContext;

    public GetCrmVendorsHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginationResponse<CrmVendorDto>> Handle(GetCrmVendorsQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.CrmVendors
            .Where(vendor => !vendor.IsDeleted)
            .AsQueryable();
        var vendorIdsWithProducts = CrmPurchaseDemandProductQuery.GetVendorIdsWithProducts(_dbContext);

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword!;
            query = query.Where(vendor =>
                vendor.VendorName.Contains(keyword) ||
                vendor.NormalizedVendorName.Contains(keyword) ||
                vendor.LatestPurchaseDemandName.Contains(keyword) ||
                _dbContext.CrmContacts.Any(contact =>
                    !contact.IsDeleted &&
                    contact.EntityType == VendorEntityType &&
                    contact.EntityId == vendor.Id &&
                    (contact.ContactName.Contains(keyword) || contact.Phone.Contains(keyword))) ||
                CrmPurchaseDemandProductQuery.GetEffectiveItems(_dbContext).Any(item =>
                    item.VendorId == vendor.Id && item.ProductName.Contains(keyword)) ||
                _dbContext.CrmPurchaseDemands.Any(plan =>
                    !plan.IsDeleted &&
                    plan.VendorId == vendor.Id &&
                    plan.DemandName.Contains(keyword)));
        }

        if (!string.IsNullOrWhiteSpace(request.PriorityLevel))
        {
            query = query.Where(vendor => vendor.PriorityLevel == request.PriorityLevel);
        }

        if (request.HasPhone.HasValue)
        {
            query = request.HasPhone.Value
                ? query.Where(vendor => _dbContext.CrmContacts.Any(contact =>
                    !contact.IsDeleted &&
                    contact.EntityType == VendorEntityType &&
                    contact.EntityId == vendor.Id &&
                    contact.Phone != string.Empty))
                : query.Where(vendor => !_dbContext.CrmContacts.Any(contact =>
                    !contact.IsDeleted &&
                    contact.EntityType == VendorEntityType &&
                    contact.EntityId == vendor.Id &&
                    contact.Phone != string.Empty));
        }

        if (request.HasProduct.HasValue)
        {
            query = request.HasProduct.Value
                ? query.Where(vendor => vendorIdsWithProducts.Contains(vendor.Id))
                : query.Where(vendor => !vendorIdsWithProducts.Contains(vendor.Id));
        }

        var dtoQuery = query.Select(vendor => new CrmVendorDto
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
            PrimaryContactName = _dbContext.CrmContacts
                .Where(contact =>
                    !contact.IsDeleted &&
                    contact.EntityType == VendorEntityType &&
                    contact.EntityId == vendor.Id &&
                    contact.Status != InvalidContactStatus)
                .OrderByDescending(contact => contact.IsPrimary)
                .ThenBy(contact => contact.CreatedAt)
                .Select(contact => contact.ContactName)
                .FirstOrDefault() ?? string.Empty,
            PrimaryContactPhone = _dbContext.CrmContacts
                .Where(contact =>
                    !contact.IsDeleted &&
                    contact.EntityType == VendorEntityType &&
                    contact.EntityId == vendor.Id &&
                    contact.Status != InvalidContactStatus)
                .OrderByDescending(contact => contact.IsPrimary)
                .ThenBy(contact => contact.CreatedAt)
                .Select(contact => contact.Phone)
                .FirstOrDefault() ?? string.Empty,
            PurchaseDemandCount = _dbContext.CrmPurchaseDemands.Count(plan => !plan.IsDeleted && plan.VendorId == vendor.Id),
            ContactCount = _dbContext.CrmContacts.Count(contact =>
                !contact.IsDeleted &&
                contact.EntityType == VendorEntityType &&
                contact.EntityId == vendor.Id),
            CreatedAt = vendor.CreatedAt,
            UpdatedAt = vendor.UpdatedAt
        });

        var response = await dtoQuery.ToPaginationResponseAsync(request);
        var productNames = await CrmPurchaseDemandProductQuery.GetNamesAsync(
            _dbContext,
            response.List.Select(vendor => vendor.Id),
            cancellationToken);
        foreach (var vendor in response.List)
        {
            vendor.ProductCount = productNames.GetValueOrDefault(vendor.Id, []).Count;
        }
        await CrmVendorOwners.FillAsync(_dbContext, response.List, cancellationToken);
        return response;
    }
}



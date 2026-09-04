using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Contracts.Crm;
using QPS.Application.Contracts.Crm.CrmVendors;
using QPS.Application.Extensions;
using QPS.Application.Features.Crm;
using QPS.Application.Interfaces;

namespace QPS.Application.Features.Crm.CrmVendors;

public class GetCrmVendorsQuery : PaginationRequest, IRequest<PaginationResponse<CrmVendorDto>>
{
    public GetCrmVendorsQuery()
    {
        SortField = nameof(CrmVendorDto.UpdatedAt);
        SortDirection = "Descending";
    }

    public string? Keyword { get; set; }

    public string? PriorityLevel { get; set; }

    public bool? HasPhone { get; set; }

    public bool? HasProduct { get; set; }
}

public class GetCrmVendorsHandler : IRequestHandler<GetCrmVendorsQuery, PaginationResponse<CrmVendorDto>>
{
    private const string VendorEntityType = CrmCodes.VendorEntityType;
    private const string InvalidContactStatus = "无效";

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
        var vendorIdsWithProducts = CrmVendorDemandProductQuery.GetVendorIdsWithProducts(_dbContext);

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword!;
            var productVendorIds = from demand in _dbContext.CrmVendorDemands
                                   join item in _dbContext.CrmVendorDemandItems on demand.Id equals item.VendorDemandId
                                   where !demand.IsDeleted && !item.IsDeleted && item.ProductName.Contains(keyword)
                                   select demand.VendorId;

            query = query.Where(vendor =>
                vendor.VendorName.Contains(keyword) ||
                vendor.NormalizedVendorName.Contains(keyword) ||
                vendor.LatestPurchaseDemandName.Contains(keyword) ||
                _dbContext.CrmContacts.Any(contact =>
                    !contact.IsDeleted &&
                    contact.EntityType == VendorEntityType &&
                    contact.EntityId == vendor.Id &&
                    (contact.ContactName.Contains(keyword) || contact.Phone.Contains(keyword))) ||
                productVendorIds.Contains(vendor.Id) ||
                _dbContext.CrmVendorDemands.Any(plan =>
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
            PurchaseDemandCount = _dbContext.CrmVendorDemands.Count(plan => !plan.IsDeleted && plan.VendorId == vendor.Id),
            ContactCount = _dbContext.CrmContacts.Count(contact =>
                !contact.IsDeleted &&
                contact.EntityType == VendorEntityType &&
                contact.EntityId == vendor.Id),
            CreatedAt = vendor.CreatedAt,
            UpdatedAt = vendor.UpdatedAt
        });

        var response = await dtoQuery.ToPaginationResponseAsync(request);
        await FillLatestPurchaseDemandProductsAsync(response.List, cancellationToken);
        var productNames = await CrmVendorDemandProductQuery.GetNamesAsync(
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

    private async Task FillLatestPurchaseDemandProductsAsync(
        List<CrmVendorDto> vendors,
        CancellationToken cancellationToken)
    {
        var vendorIds = vendors.Select(vendor => vendor.Id).ToList();
        if (vendorIds.Count == 0)
        {
            return;
        }

        var latestDemandIds = await _dbContext.CrmVendorDemands
            .Where(demand => !demand.IsDeleted && vendorIds.Contains(demand.VendorId))
            .GroupBy(demand => demand.VendorId)
            .Select(group => group
                .OrderByDescending(demand => demand.DemandAt)
                .ThenByDescending(demand => demand.CreatedAt)
                .Select(demand => new { demand.VendorId, demand.Id })
                .First())
            .ToListAsync(cancellationToken);
        var demandIds = latestDemandIds.Select(demand => demand.Id).ToList();
        var products = await (
                from item in _dbContext.CrmVendorDemandItems
                join demand in _dbContext.CrmVendorDemands on item.VendorDemandId equals demand.Id
                where demandIds.Contains(item.VendorDemandId)
                orderby item.SortOrder, item.CreatedAt
                select new { demand.VendorId, item.ProductName })
            .ToListAsync(cancellationToken);
        var productsByVendor = products
            .GroupBy(product => product.VendorId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(product => product.ProductName).Distinct().ToList());

        foreach (var vendor in vendors)
        {
            vendor.ProductName = productsByVendor.GetValueOrDefault(vendor.Id, []);
        }
    }
}



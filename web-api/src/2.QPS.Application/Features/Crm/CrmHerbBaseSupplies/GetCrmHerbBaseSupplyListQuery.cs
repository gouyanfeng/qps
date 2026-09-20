using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Contracts.Crm.CrmHerbBaseSupplies;
using QPS.Application.Extensions;
using QPS.Application.Interfaces;
using QPS.Domain.Entities.Crm;

namespace QPS.Application.Features.Crm.CrmHerbBaseSupplies;

public class GetCrmHerbBaseSupplyListQuery : PaginationRequest, IRequest<PaginationResponse<CrmHerbBaseSupplyListDto>>
{
    public string ProductName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string OccupancyStatus { get; set; } = string.Empty;
}

public class GetCrmHerbBaseSupplyListHandler(IDbContext dbContext)
    : IRequestHandler<GetCrmHerbBaseSupplyListQuery, PaginationResponse<CrmHerbBaseSupplyListDto>>
{
    public async Task<PaginationResponse<CrmHerbBaseSupplyListDto>> Handle(
        GetCrmHerbBaseSupplyListQuery request,
        CancellationToken cancellationToken)
    {
        var selectedSupplyIds = dbContext.CrmMatchCandidates
            .Where(candidate => candidate.Status == CrmMatchCandidate.Selected)
            .Where(candidate => dbContext.CrmMatchOpportunities.Any(opportunity =>
                opportunity.Id == candidate.MatchOpportunityId &&
                opportunity.Status != CrmMatchOpportunity.Closed))
            .Select(candidate => candidate.HerbBaseSupplyId);

        var supplies = dbContext.CrmHerbBaseSupplies
            .Include(supply => supply.HerbBase)
            .Where(supply => supply.HerbBase != null)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.ProductName))
        {
            var productName = request.ProductName.Trim();
            supplies = supplies.Where(supply => supply.ProductName.Contains(productName));
        }

        if (!string.IsNullOrWhiteSpace(request.Region))
        {
            var region = request.Region.Trim();
            supplies = supplies.Where(supply => supply.HerbBase!.Province.Contains(region) ||
                supply.HerbBase.City.Contains(region) || supply.HerbBase.Area.Contains(region));
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            supplies = supplies.Where(supply => supply.Status == request.Status.Trim());
        }

        if (request.OccupancyStatus == "已中选")
        {
            supplies = supplies.Where(supply => selectedSupplyIds.Contains(supply.Id));
        }
        else if (request.OccupancyStatus == "未中选")
        {
            supplies = supplies.Where(supply => !selectedSupplyIds.Contains(supply.Id));
        }

        var totalCount = await supplies.CountAsync(cancellationToken);
        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Max(request.PageSize, 1);
        var list = await supplies
            .OrderByDescending(supply => supply.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(supply => new CrmHerbBaseSupplyListDto
            {
                Id = supply.Id,
                HerbBaseId = supply.HerbBaseId,
                HerbBaseSubjectId = supply.HerbBaseSubjectId,
                SubjectName = supply.HerbBase!.SubjectName,
                HerbBaseName = supply.HerbBase.BaseName,
                Province = supply.HerbBase.Province,
                City = supply.HerbBase.City,
                Area = supply.HerbBase.Area,
                ProductName = supply.ProductName,
                AvailableQuantity = supply.AvailableQuantity,
                QuantityUnit = supply.QuantityUnit,
                Specification = supply.Specification,
                QualityRequirement = supply.QualityRequirement,
                HarvestSeason = supply.HarvestSeason,
                ExpectedPrice = supply.ExpectedPrice,
                PriceUnit = supply.PriceUnit,
                SupplyCycle = supply.SupplyCycle,
                ConfirmedAt = supply.ConfirmedAt,
                ValidUntil = supply.ValidUntil,
                Status = supply.Status,
                Remark = supply.Remark,
                IsExpired = supply.Status == CrmHerbBaseSupply.Active && supply.ValidUntil < DateTime.Today,
                CandidateCount = dbContext.CrmMatchCandidates.Count(candidate => candidate.HerbBaseSupplyId == supply.Id),
                SelectedCount = dbContext.CrmMatchCandidates.Count(candidate =>
                    candidate.HerbBaseSupplyId == supply.Id &&
                    candidate.Status == CrmMatchCandidate.Selected &&
                    dbContext.CrmMatchOpportunities.Any(opportunity =>
                        opportunity.Id == candidate.MatchOpportunityId &&
                        opportunity.Status != CrmMatchOpportunity.Closed))
            })
            .ToListAsync(cancellationToken);

        var supplyIds = list.Select(item => item.Id).ToList();
        var occupiedDemands = await (
                from candidate in dbContext.CrmMatchCandidates
                join opportunity in dbContext.CrmMatchOpportunities on candidate.MatchOpportunityId equals opportunity.Id
                where supplyIds.Contains(candidate.HerbBaseSupplyId) &&
                    candidate.Status == CrmMatchCandidate.Selected &&
                    opportunity.Status != CrmMatchOpportunity.Closed
                select new { candidate.HerbBaseSupplyId, opportunity.DemandProductName })
            .ToListAsync(cancellationToken);

        foreach (var item in list)
        {
            item.OccupiedDemandSummary = string.Join("、", occupiedDemands
                .Where(demand => demand.HerbBaseSupplyId == item.Id)
                .Select(demand => demand.DemandProductName)
                .Distinct());
        }

        return new PaginationResponse<CrmHerbBaseSupplyListDto>(list, totalCount, page, pageSize);
    }
}

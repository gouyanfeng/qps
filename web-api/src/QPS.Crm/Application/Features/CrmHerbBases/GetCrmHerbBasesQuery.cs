using QPS.Shared.Common;
using QPS.Crm.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Crm.Contracts.DTOs;
using QPS.Crm.Contracts.DTOs.CrmHerbBases;
using QPS.Crm.Application.Features;
using QPS.Shared.Contracts;
using QPS.Shared.Extensions;

namespace QPS.Crm.Application.Features.CrmHerbBases;

public class GetCrmHerbBasesQuery : PaginationRequest, IRequest<PaginationResponse<CrmHerbBaseDto>>
{
    public string? BaseName { get; set; }

    public string? Keyword { get; set; }

    public string? Grade { get; set; }

    public string? Status { get; set; }

    public string? SourcePlatform { get; set; }

    public Guid? OwnerUserId { get; set; }

    public string? Province { get; set; }

    public string? City { get; set; }

    public DateTime? NextFollowFrom { get; set; }

    public DateTime? NextFollowTo { get; set; }

    public bool? OnlyOverdue { get; set; }

    public bool? OnlyNoNextFollow { get; set; }
}

public class GetCrmHerbBasesHandler : IRequestHandler<GetCrmHerbBasesQuery, PaginationResponse<CrmHerbBaseDto>>
{
    private readonly ICrmDbContext _dbContext;

    public GetCrmHerbBasesHandler(ICrmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginationResponse<CrmHerbBaseDto>> Handle(GetCrmHerbBasesQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.CrmHerbBases.AsQueryable();

        if (!string.IsNullOrEmpty(request.BaseName))
            query = query.Where(c => c.BaseName.Contains(request.BaseName));

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword!;
            query = query.Where(c =>
                c.BaseName.Contains(keyword) ||
                c.SubjectName.Contains(keyword) ||
                c.PrimaryContactName.Contains(keyword) ||
                c.PrimaryContactPhone.Contains(keyword));
        }

        if (!string.IsNullOrWhiteSpace(request.Province))
        {
            var province = request.Province!;
            query = query.Where(c => c.Province == province);
        }

        if (!string.IsNullOrWhiteSpace(request.City))
        {
            var city = request.City!;
            query = query.Where(c => c.City == city);
        }

        if (!string.IsNullOrEmpty(request.Grade))
        {
            query = query.Where(c => c.Grade == request.Grade);
        }

        if (!string.IsNullOrEmpty(request.Status))
        {
            query = query.Where(c => c.Status == request.Status);
        }

        if (!string.IsNullOrEmpty(request.SourcePlatform))
        {
            query = query.Where(c => c.SourcePlatform == request.SourcePlatform);
        }

        if (request.OwnerUserId.HasValue)
        {
            query = query.Where(c => c.OwnerUserId == request.OwnerUserId);
        }

        if (request.NextFollowFrom.HasValue)
        {
            query = query.Where(c => c.NextFollowAt >= request.NextFollowFrom.Value);
        }

        if (request.NextFollowTo.HasValue)
        {
            query = query.Where(c => c.NextFollowAt <= request.NextFollowTo.Value);
        }

        if (request.OnlyOverdue == true)
        {
            var now = DateTime.Now;
            query = query.Where(c => c.NextFollowAt.HasValue && c.NextFollowAt.Value < now);
        }

        if (request.OnlyNoNextFollow == true)
        {
            query = query.Where(c => c.NextFollowAt == null);
        }

        var dtoQuery =
            from c in query
            select new CrmHerbBaseDto
            {
                Id = c.Id,
                BaseName = c.BaseName,
                SubjectName = c.SubjectName,
                Grade = c.Grade,
                Score = c.Score,
                Scale = c.Scale,
                Province = c.Province,
                City = c.City,
                Area = c.Area,
                Address = c.Address,
                Lat = c.Lat,
                Lng = c.Lng,
                SourcePlatform = c.SourcePlatform,
                SourceId = c.SourceId,
                Status = c.Status,
                OwnerUserId = c.OwnerUserId,
                Remark = c.Remark,
                PrimaryContactName = c.PrimaryContactName,
                PrimaryContactPhone = c.PrimaryContactPhone,
                LastFollowAt = c.LastFollowAt,
                LastFollowResult = c.LastFollowResult,
                NextFollowAt = c.NextFollowAt,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            };

        var response = await dtoQuery.ToPaginationResponseAsync(request);
        await FillOwnerNamesAsync(response.List, cancellationToken);
        return response;
    }

    private async Task FillOwnerNamesAsync(List<CrmHerbBaseDto> herbBases, CancellationToken cancellationToken)
    {
        var ownerIds = herbBases
            .Where(herbBase => herbBase.OwnerUserId.HasValue)
            .Select(herbBase => herbBase.OwnerUserId!.Value)
            .Distinct()
            .ToList();
        if (ownerIds.Count == 0)
            return;

        var users = await CrmUserNames.GetAsync(_dbContext, ownerIds, cancellationToken);

        foreach (var herbBase in herbBases)
        {
            if (herbBase.OwnerUserId.HasValue &&
                users.TryGetValue(herbBase.OwnerUserId.Value, out var owner))
            {
                herbBase.OwnerUserName = owner.RealName != string.Empty ? owner.RealName : owner.Username;
            }
        }
    }
}
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Crm.Contracts;
using QPS.Crm.Contracts.DTOs.CrmHerbBases;

namespace QPS.Crm.Application.Features.CrmHerbBases;

public record GetCrmHerbBasesMapQuery : IRequest<List<CrmHerbBaseMapDto>>;

public class GetCrmHerbBasesMapHandler : IRequestHandler<GetCrmHerbBasesMapQuery, List<CrmHerbBaseMapDto>>
{
    private readonly ICrmDbContext _dbContext;

    public GetCrmHerbBasesMapHandler(ICrmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<CrmHerbBaseMapDto>> Handle(GetCrmHerbBasesMapQuery request, CancellationToken cancellationToken)
    {
        return (
            from c in _dbContext.CrmHerbBases
            where c.Lat != null && c.Lng != null
            select new CrmHerbBaseMapDto
            {
                Id = c.Id,
                BaseName = c.BaseName,
                Address = c.Address,
                Lat = c.Lat!.Value,
                Lng = c.Lng!.Value
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
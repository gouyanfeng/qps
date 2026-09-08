using MediatR;
using QPS.Application.Attributes;
using QPS.Application.Features.System;
using QPS.Domain.Entities.Crm;

namespace QPS.Application.Features.Crm.CrmMatchOpportunities;

public class CrmMatchOpportunityProgressOptionsDto
{
    public IReadOnlyList<string> Stages { get; init; } = [];
    public IReadOnlyList<string> NextActions { get; init; } = [];
    public IReadOnlyDictionary<string, IReadOnlyList<string>> NextActionsByStage { get; init; } = new Dictionary<string, IReadOnlyList<string>>();
}

[Permission(SystemPermissionCodes.CRM_MATCH_OPPORTUNITY_MANAGE)]
public class GetCrmMatchOpportunityProgressOptionsQuery : IRequest<CrmMatchOpportunityProgressOptionsDto>
{
}

public class GetCrmMatchOpportunityProgressOptionsHandler
    : IRequestHandler<GetCrmMatchOpportunityProgressOptionsQuery, CrmMatchOpportunityProgressOptionsDto>
{
    public Task<CrmMatchOpportunityProgressOptionsDto> Handle(
        GetCrmMatchOpportunityProgressOptionsQuery request,
        CancellationToken cancellationToken) =>
        Task.FromResult(new CrmMatchOpportunityProgressOptionsDto
        {
            Stages = CrmMatchOpportunityStage.All,
            NextActions = CrmMatchOpportunityNextAction.All,
            NextActionsByStage = CrmMatchOpportunityStage.All.ToDictionary(
                stage => stage,
                CrmMatchOpportunityNextAction.GetAvailableActions)
        });
}

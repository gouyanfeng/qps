using QPS.Crm.Contracts.Permissions;
using MediatR;
using QPS.Shared.Contracts;
using QPS.Crm.Domain.Entities;

namespace QPS.Crm.Application.Features.CrmMatchOpportunities;

public class CrmMatchOpportunityProgressOptionsDto
{
    public IReadOnlyList<string> Stages { get; init; } = [];
    public IReadOnlyList<string> NextActions { get; init; } = [];
    public IReadOnlyDictionary<string, IReadOnlyList<string>> NextActionsByStage { get; init; } = new Dictionary<string, IReadOnlyList<string>>();
}

[Permission(CrmPermissionCodes.CRM_MATCH_OPPORTUNITY_MANAGE)]
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

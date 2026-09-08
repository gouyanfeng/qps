using MediatR;
using Microsoft.AspNetCore.Mvc;
using QPS.Application.Features.Crm.CrmMatchOpportunities;

namespace QPS.WebAPI.Controllers.Admin.Crm;

[Route("api/admin/crm")]
[ApiController]
public class CrmMatchOpportunityController(IMediator mediator) : ControllerBase
{
    [HttpGet("match-opportunities")]
    public async Task<ActionResult<object>> GetList([FromQuery] GetCrmMatchOpportunitiesQuery query)
        => Ok(await mediator.Send(query));

    [HttpGet("match-opportunity-progress-options")]
    public async Task<ActionResult<object>> GetProgressOptions()
        => Ok(await mediator.Send(new GetCrmMatchOpportunityProgressOptionsQuery()));

    [HttpGet("match-opportunities/{id:guid}")]
    public async Task<ActionResult<object>> GetDetail(Guid id)
        => Ok(await mediator.Send(new GetCrmMatchOpportunityDetailQuery { Id = id }));

    [HttpGet("vendor-demand-items/{demandItemId:guid}/match-candidates")]
    public async Task<ActionResult<object>> GetCandidates(Guid demandItemId, [FromQuery] GetCrmMatchSupplyCandidatesQuery query)
    {
        query.DemandItemId = demandItemId;
        return Ok(await mediator.Send(query));
    }

    [HttpPost("vendor-demand-items/{demandItemId:guid}/match-opportunities")]
    public async Task<ActionResult<Guid>> Create(Guid demandItemId, [FromBody] CreateCrmMatchOpportunityCommand command)
    {
        command.DemandItemId = demandItemId;
        return Ok(await mediator.Send(command));
    }

    [HttpPost("match-opportunities/{id:guid}/candidates")]
    public async Task<ActionResult<bool>> AddCandidates(Guid id, [FromBody] AddCrmMatchOpportunityCandidatesCommand command)
    {
        command.OpportunityId = id;
        return Ok(await mediator.Send(command));
    }

    [HttpPatch("match-opportunities/{id:guid}/candidates/{candidateId:guid}/status")]
    public async Task<ActionResult<bool>> UpdateCandidateStatus(
        Guid id,
        Guid candidateId,
        [FromBody] UpdateCrmMatchCandidateStatusCommand command)
    {
        command.OpportunityId = id;
        command.CandidateId = candidateId;
        return Ok(await mediator.Send(command));
    }

    [HttpPatch("match-opportunities/{id:guid}/progress")]
    public async Task<ActionResult<bool>> Advance(
        Guid id,
        [FromBody] AdvanceCrmMatchOpportunityCommand command)
    {
        command.OpportunityId = id;
        return Ok(await mediator.Send(command));
    }

    [HttpPatch("match-opportunities/{id:guid}/status")]
    public async Task<ActionResult<bool>> UpdateStatus(
        Guid id,
        [FromBody] UpdateCrmMatchOpportunityStatusCommand command)
    {
        command.OpportunityId = id;
        return Ok(await mediator.Send(command));
    }

    [HttpGet("match-opportunities/{id:guid}/activities")]
    public async Task<ActionResult<object>> GetActivities(Guid id)
        => Ok(await mediator.Send(new GetCrmMatchOpportunityActivitiesQuery { OpportunityId = id }));
}

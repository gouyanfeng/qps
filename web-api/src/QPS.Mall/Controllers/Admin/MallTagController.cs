using MediatR;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallTags;
using QPS.Mall.Contracts.DTOs;

namespace QPS.Mall.Controllers.Admin;

[ApiController]
[Route("api/admin/mall/tags")]
public class MallTagController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<MallTagDto>>> GetTags(
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetMallTagsQuery { IsActive = isActive }, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<bool>> Create([FromBody] MallTagRequest request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new CreateMallTagCommand { Request = request }, cancellationToken));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<bool>> Update(Guid id, [FromBody] MallTagRequest request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new UpdateMallTagCommand { Id = id, Request = request }, cancellationToken));

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<bool>> ChangeStatus(Guid id, [FromBody] MallTagStatusRequest request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new ChangeMallTagStatusCommand { Id = id, IsActive = request.IsActive }, cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<bool>> Delete(Guid id, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new DeleteMallTagCommand { Id = id }, cancellationToken));
}

public class MallTagStatusRequest
{
    public bool IsActive { get; set; }
}

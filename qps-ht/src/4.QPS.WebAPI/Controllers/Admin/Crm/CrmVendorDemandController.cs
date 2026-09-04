using MediatR;
using Microsoft.AspNetCore.Mvc;
using QPS.Application.Contracts.Crm;
using QPS.Application.Features.Crm.CrmVendors;

namespace QPS.WebAPI.Controllers.Admin.Crm;

[Route("api/admin/crm/purchase-demands")]
[ApiController]
public class CrmVendorDemandController : ControllerBase
{
    private readonly IMediator _mediator;
    public CrmVendorDemandController(IMediator mediator) => _mediator = mediator;
    [HttpGet] public async Task<ActionResult<object>> GetList([FromQuery] GetCrmVendorDemandsQuery query) => Ok(await _mediator.Send(query));
    [HttpGet("{id:guid}")] public async Task<ActionResult<object>> Get(Guid id) => Ok(await _mediator.Send(new GetCrmVendorDemandsQuery { Id = id, Page = 1, PageSize = 1 }));
    [HttpPost] public async Task<ActionResult<bool>> Create([FromBody] CrmVendorDemandSaveRequest request) => Ok(await _mediator.Send(new CreateCrmVendorDemandCommand { Request = request }));
    [HttpPut("{id:guid}")] public async Task<ActionResult<bool>> Update(Guid id, [FromBody] CrmVendorDemandSaveRequest request) => Ok(await _mediator.Send(new UpdateCrmVendorDemandCommand { Id = id, Request = request }));
    [HttpDelete("{id:guid}")] public async Task<ActionResult<bool>> Delete(Guid id) => Ok(await _mediator.Send(new DeleteCrmVendorDemandCommand { Id = id }));
    [HttpPatch("{id:guid}/status")] public async Task<ActionResult<bool>> ChangeStatus(Guid id, [FromBody] CrmVendorDemandStatusRequest request) => Ok(await _mediator.Send(new ChangeCrmVendorDemandStatusCommand { Id = id, Request = request }));
}

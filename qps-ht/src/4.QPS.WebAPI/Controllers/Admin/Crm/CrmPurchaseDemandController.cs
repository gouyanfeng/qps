using MediatR;
using Microsoft.AspNetCore.Mvc;
using QPS.Application.Contracts.Crm;
using QPS.Application.Features.Crm.CrmVendors;

namespace QPS.WebAPI.Controllers.Admin.Crm;

[Route("api/admin/crm/purchase-demands")]
[ApiController]
public class CrmPurchaseDemandController : ControllerBase
{
    private readonly IMediator _mediator;
    public CrmPurchaseDemandController(IMediator mediator) => _mediator = mediator;
    [HttpGet] public async Task<ActionResult<object>> GetList([FromQuery] GetCrmPurchaseDemandsQuery query) => Ok(await _mediator.Send(query));
    [HttpGet("{id:guid}")] public async Task<ActionResult<object>> Get(Guid id) => Ok(await _mediator.Send(new GetCrmPurchaseDemandsQuery { Id = id, Page = 1, PageSize = 1 }));
    [HttpPost] public async Task<ActionResult<bool>> Create([FromBody] CrmPurchaseDemandSaveRequest request) => Ok(await _mediator.Send(new CreateCrmPurchaseDemandCommand { Request = request }));
    [HttpPut("{id:guid}")] public async Task<ActionResult<bool>> Update(Guid id, [FromBody] CrmPurchaseDemandSaveRequest request) => Ok(await _mediator.Send(new UpdateCrmPurchaseDemandCommand { Id = id, Request = request }));
    [HttpDelete("{id:guid}")] public async Task<ActionResult<bool>> Delete(Guid id) => Ok(await _mediator.Send(new DeleteCrmPurchaseDemandCommand { Id = id }));
    [HttpPatch("{id:guid}/status")] public async Task<ActionResult<bool>> ChangeStatus(Guid id, [FromBody] CrmPurchaseDemandStatusRequest request) => Ok(await _mediator.Send(new ChangeCrmPurchaseDemandStatusCommand { Id = id, Request = request }));
}

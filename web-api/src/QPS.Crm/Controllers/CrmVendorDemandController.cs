using MediatR;
using Microsoft.AspNetCore.Mvc;
using QPS.Crm.Contracts.DTOs;
using QPS.Crm.Contracts.DTOs.CrmVendors;
using QPS.Crm.Application.Features.CrmVendors;

namespace QPS.Crm.Controllers;

[Route("api/admin/crm/purchase-demands")]
[ApiController]
public class CrmVendorDemandController : ControllerBase
{
    private readonly IMediator _mediator;
    public CrmVendorDemandController(IMediator mediator) => _mediator = mediator;
    [HttpGet] public async Task<ActionResult<object>> GetList([FromQuery] GetCrmVendorDemandsQuery query) => Ok(await _mediator.Send(query));
    /// <summary>采购明细列表：一条采购明细一行，用于采购列表按品类展示。</summary>
    [HttpGet("items")] public async Task<ActionResult<object>> GetItemList([FromQuery] GetCrmVendorDemandItemsQuery query) => Ok(await _mediator.Send(query));
    [HttpGet("{id:guid}")] public async Task<ActionResult<object>> Get(Guid id) => Ok(await _mediator.Send(new GetCrmVendorDemandsQuery { Id = id, Page = 1, PageSize = 1 }));
    [HttpPost] public async Task<ActionResult<bool>> Create([FromBody] CrmVendorDemandSaveRequest request) => Ok(await _mediator.Send(new CreateCrmVendorDemandCommand { Request = request }));
    [HttpPut("{id:guid}")] public async Task<ActionResult<bool>> Update(Guid id, [FromBody] CrmVendorDemandSaveRequest request) => Ok(await _mediator.Send(new UpdateCrmVendorDemandCommand { Id = id, Request = request }));
    [HttpDelete("{id:guid}")] public async Task<ActionResult<bool>> Delete(Guid id) => Ok(await _mediator.Send(new DeleteCrmVendorDemandCommand { Id = id }));
    [HttpPatch("{id:guid}/status")] public async Task<ActionResult<bool>> ChangeStatus(Guid id, [FromBody] CrmVendorDemandStatusRequest request) => Ok(await _mediator.Send(new ChangeCrmVendorDemandStatusCommand { Id = id, Request = request }));
}

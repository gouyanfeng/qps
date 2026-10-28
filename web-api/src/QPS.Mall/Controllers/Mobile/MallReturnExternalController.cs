using MediatR;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallReturns;

namespace QPS.Mall.Controllers.Mobile;

[ApiController]
[Route("api/mall")]
public class MallReturnExternalController : ControllerBase
{
    private readonly IMediator _mediator;

    public MallReturnExternalController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("orders/{orderNo}/returns")]
    public async Task<ActionResult<MallReturnDto>> CreateReturn(
        string orderNo,
        [FromBody] CreateMallReturnRequest request)
    {
        return Ok(await _mediator.Send(new CreateMallReturnCommand
        {
            OrderNo = orderNo,
            Reason = request.Reason
        }));
    }

    [HttpPost("returns/{returnNo}/ship")]
    public async Task<ActionResult<MallReturnDto>> ShipReturn(
        string returnNo,
        [FromBody] ShipMallReturnRequest request)
    {
        return Ok(await _mediator.Send(new ShipMallReturnCommand
        {
            ReturnNo = returnNo,
            ExpressCompany = request.ExpressCompany,
            TrackingNo = request.TrackingNo
        }));
    }
}

public class CreateMallReturnRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class ShipMallReturnRequest
{
    public string ExpressCompany { get; set; } = string.Empty;
    public string TrackingNo { get; set; } = string.Empty;
}
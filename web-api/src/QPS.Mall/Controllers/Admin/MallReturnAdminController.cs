using MediatR;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallReturns;

namespace QPS.Mall.Controllers.Admin;

[ApiController]
[Route("api/admin/mall")]
public class MallReturnAdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public MallReturnAdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("returns/{returnNo}/approve")]
    public async Task<ActionResult<MallReturnDto>> ApproveReturn(
        string returnNo,
        [FromBody] ApproveMallReturnRequest request)
    {
        return Ok(await _mediator.Send(new ApproveMallReturnCommand
        {
            ReturnNo = returnNo,
            Approve = request.Approve
        }));
    }

    [HttpPost("returns/{returnNo}/confirm-receipt")]
    public async Task<ActionResult<MallReturnDto>> ConfirmReturnReceipt(string returnNo)
    {
        return Ok(await _mediator.Send(new ConfirmMallReturnReceiptCommand { ReturnNo = returnNo }));
    }
}

public class ApproveMallReturnRequest
{
    public bool Approve { get; set; }
}
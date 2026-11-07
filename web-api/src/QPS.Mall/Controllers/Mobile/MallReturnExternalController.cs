using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallReturns;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Contracts;
using QPS.Shared.Extensions;

namespace QPS.Mall.Controllers.Mobile;

[ApiController]
[Route("api/mall")]
[Authorize(Policy = "MallMember")]
public class MallReturnExternalController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public MallReturnExternalController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpPost("orders/{orderNo}/returns")]
    public async Task<ActionResult<MallReturnDto>> CreateReturn(
        string orderNo,
        [FromBody] CreateMallReturnRequest request)
    {
        return Ok(await _mediator.Send(new CreateMallReturnCommand
        {
            OrderNo = orderNo,
            MemberId = _currentUser.GetUserId(),
            Reason = request.Reason,
            Description = request.Description,
            EvidenceImages = request.EvidenceImages
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
            MemberId = _currentUser.GetUserId(),
            ExpressCompany = request.ExpressCompany,
            TrackingNo = request.TrackingNo
        }));
    }
}

public class CreateMallReturnRequest
{
    /// <summary>申请原因，必填。</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>补充说明，选择「其他」原因时填写。</summary>
    public string? Description { get; set; }

    /// <summary>凭证图片相对地址，最多 6 张。</summary>
    public string[]? EvidenceImages { get; set; }
}

public class ShipMallReturnRequest
{
    public string ExpressCompany { get; set; } = string.Empty;
    public string TrackingNo { get; set; } = string.Empty;
}
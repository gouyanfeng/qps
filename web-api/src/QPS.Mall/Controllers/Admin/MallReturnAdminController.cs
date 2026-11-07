using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallReturns;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Common;

namespace QPS.Mall.Controllers.Admin;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/admin/mall")]
public class MallReturnAdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public MallReturnAdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("returns")]
    public async Task<ActionResult<PaginationResponse<MallReturnDto>>> GetReturns(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortField = "CreatedAt",
        [FromQuery] string sortDirection = "Descending",
        [FromQuery] string? status = null,
        [FromQuery] string? keyword = null,
        [FromQuery] Guid? storeId = null)
    {
        var query = new GetMallReturnsQuery
        {
            Page = page,
            PageSize = pageSize,
            SortField = sortField,
            SortDirection = sortDirection,
            Status = status,
            Keyword = keyword,
            StoreId = storeId
        };

        return Ok(await _mediator.Send(query));
    }

    [HttpGet("returns/{id:guid}")]
    public async Task<ActionResult<MallReturnDto>> GetReturn(Guid id)
    {
        return Ok(await _mediator.Send(new GetMallReturnQuery { Id = id }));
    }

    [HttpPost("returns/{returnNo}/approve")]
    public async Task<ActionResult<MallReturnDto>> ApproveReturn(
        string returnNo,
        [FromBody] ApproveMallReturnRequest request)
    {
        return Ok(await _mediator.Send(new ApproveMallReturnCommand
        {
            ReturnNo = returnNo,
            Approve = request.Approve,
            RejectReason = request.RejectReason
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

    /// <summary>审核拒绝时的原因。</summary>
    public string? RejectReason { get; set; }
}
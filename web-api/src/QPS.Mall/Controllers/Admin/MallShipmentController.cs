using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallShipments;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Common;

namespace QPS.Mall.Controllers;

/// <summary>
/// 管理端发货管理：待发货由订单接口按 Paid 状态查询，发货单在此分页查询与作废。
/// </summary>
[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/admin/mall")]
public class MallShipmentController : ControllerBase
{
    private readonly IMediator _mediator;

    public MallShipmentController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// 分页查询发货单，已作废记录保留可查。
    /// </summary>
    [HttpGet("shipments")]
    public async Task<ActionResult<PaginationResponse<MallShipmentListDto>>> GetShipments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? keyword = null,
        [FromQuery] DateTime? startTime = null,
        [FromQuery] DateTime? endTime = null,
        [FromQuery] bool? isVoided = null,
        [FromQuery] string? status = null)
    {
        var query = new GetMallShipmentsQuery
        {
            Page = page,
            PageSize = pageSize,
            Keyword = keyword,
            StartTime = startTime,
            EndTime = endTime,
            IsVoided = isVoided,
            Status = status
        };

        return Ok(await _mediator.Send(query));
    }

    /// <summary>
    /// 查询发货单详情。
    /// </summary>
    [HttpGet("shipments/{id:guid}")]
    public async Task<ActionResult<MallShipmentDetailDto>> GetShipment(Guid id)
    {
        return Ok(await _mediator.Send(new GetMallShipmentDetailQuery { Id = id }));
    }

    /// <summary>
    /// 作废运单。
    /// </summary>
    [HttpPost("shipments/{id:guid}/void")]
    public async Task<ActionResult<bool>> VoidShipment(Guid id, [FromBody] VoidMallShipmentRequest request)
    {
        return Ok(await _mediator.Send(new VoidMallShipmentCommand
        {
            Id = id,
            Reason = request.Reason
        }));
    }
}

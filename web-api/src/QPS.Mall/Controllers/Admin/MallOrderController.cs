using MediatR;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallOrders;
using QPS.Mall.Application.Features.MallPayments;
using QPS.Mall.Application.Features.MallRefunds;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Common;

namespace QPS.Mall.Controllers;

/// <summary>
/// 管理端订单查看与处理。
/// </summary>
[ApiController]
[Route("api/admin/mall")]
public class MallOrderController : ControllerBase
{
    private readonly IMediator _mediator;

    public MallOrderController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// 分页查询订单。
    /// </summary>
    [HttpGet("orders")]
    public async Task<ActionResult<PaginationResponse<MallOrderDto>>> GetOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortField = "CreatedAt",
        [FromQuery] string sortDirection = "Descending",
        [FromQuery] string? status = null,
        [FromQuery] string? keyword = null,
        [FromQuery] DateTime? startTime = null,
        [FromQuery] DateTime? endTime = null)
    {
        var query = new GetMallOrdersQuery
        {
            Page = page,
            PageSize = pageSize,
            SortField = sortField,
            SortDirection = sortDirection,
            Status = status,
            Keyword = keyword,
            StartTime = startTime,
            EndTime = endTime
        };

        return Ok(await _mediator.Send(query));
    }

    /// <summary>
    /// 查询订单详情（含明细、支付、退款、物流）。
    /// </summary>
    [HttpGet("orders/{id:guid}")]
    public async Task<ActionResult<MallOrderDetailDto>> GetOrder(Guid id)
    {
        return Ok(await _mediator.Send(new GetMallOrderQuery { Id = id }));
    }

    /// <summary>
    /// 取消待支付订单。
    /// </summary>
    [HttpPost("orders/{id:guid}/cancel")]
    public async Task<ActionResult<bool>> CancelOrder(Guid id)
    {
        return Ok(await _mediator.Send(new CancelMallOrderCommand { OrderId = id }));
    }

    /// <summary>
    /// 录入运单并发货。
    /// </summary>
    [HttpPost("orders/{id:guid}/shipments")]
    public async Task<ActionResult<bool>> CreateShipment(Guid id, [FromBody] CreateMallShipmentRequest request)
    {
        return Ok(await _mediator.Send(new CreateMallShipmentCommand { OrderId = id, Request = request }));
    }

    /// <summary>
    /// 确认收货。
    /// </summary>
    [HttpPost("orders/{id:guid}/confirm-receipt")]
    public async Task<ActionResult<bool>> ConfirmReceipt(Guid id)
    {
        return Ok(await _mediator.Send(new ConfirmMallOrderReceiptCommand { OrderId = id }));
    }

    /// <summary>
    /// 申请整单退款。
    /// </summary>
    [HttpPost("orders/{id:guid}/refunds")]
    public async Task<ActionResult<MallRefundDto>> CreateRefund(Guid id, [FromBody] CreateMallRefundRequest request)
    {
        return Ok(await _mediator.Send(new CreateMallRefundCommand { OrderId = id, Request = request }));
    }

    /// <summary>
    /// 分页查询支付单。
    /// </summary>
    [HttpGet("payments")]
    public async Task<ActionResult<PaginationResponse<MallPaymentListDto>>> GetPayments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        [FromQuery] string? keyword = null,
        [FromQuery] string? method = null,
        [FromQuery] DateTime? startTime = null,
        [FromQuery] DateTime? endTime = null)
    {
        var query = new GetMallPaymentsQuery
        {
            Page = page,
            PageSize = pageSize,
            Status = status,
            Keyword = keyword,
            Method = method,
            StartTime = startTime,
            EndTime = endTime
        };

        return Ok(await _mediator.Send(query));
    }

    /// <summary>
    /// 分页查询退款单。
    /// </summary>
    [HttpGet("refunds")]
    public async Task<ActionResult<PaginationResponse<MallRefundListDto>>> GetRefunds(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        [FromQuery] string? keyword = null,
        [FromQuery] DateTime? startTime = null,
        [FromQuery] DateTime? endTime = null)
    {
        var query = new GetMallRefundsQuery
        {
            Page = page,
            PageSize = pageSize,
            Status = status,
            Keyword = keyword,
            StartTime = startTime,
            EndTime = endTime
        };

        return Ok(await _mediator.Send(query));
    }

    /// <summary>
    /// 模拟支付结果。
    /// </summary>
    [HttpPost("payments/{id:guid}/simulate")]
    public async Task<ActionResult<bool>> SimulatePayment(Guid id, [FromBody] SimulateMallPaymentRequest request)
    {
        return Ok(await _mediator.Send(new SimulateMallPaymentCommand { PaymentId = id, Request = request }));
    }

    /// <summary>
    /// 模拟退款结果。
    /// </summary>
    [HttpPost("refunds/{id:guid}/simulate")]
    public async Task<ActionResult<bool>> SimulateRefund(Guid id, [FromBody] SimulateMallRefundRequest request)
    {
        return Ok(await _mediator.Send(new SimulateMallRefundCommand { RefundId = id, Request = request }));
    }
}
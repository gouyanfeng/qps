using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallOrders;
using QPS.Mall.Application.Features.MallPayments;
using QPS.Mall.Application.Features.MallRefunds;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Common;
using QPS.Shared.Contracts;
using QPS.Shared.Extensions;

namespace QPS.Mall.Controllers.Mobile;

[ApiController]
[Route("api/mall")]
[Authorize(Policy = "MallMember")]
public class MallExternalOrderController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public MallExternalOrderController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpPost("orders")]
    public async Task<ActionResult<MallOrderDetailDto>> CreateOrder([FromBody] CreateMallOrderRequest request)
    {
        request.MemberId = _currentUser.GetUserId();
        return Ok(await _mediator.Send(new CreateMallOrderCommand { Request = request }));
    }

    [HttpGet("orders")]
    public async Task<ActionResult<PaginationResponse<MallOrderDto>>> GetMyOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null)
    {
        var memberId = _currentUser.GetUserId();
        var query = new GetMallOrdersQuery
        {
            Page = page,
            PageSize = pageSize,
            MemberId = memberId,
            Status = status
        };
        return Ok(await _mediator.Send(query));
    }

    [HttpGet("orders/{orderNo}")]
    public async Task<ActionResult<MallOrderDetailDto>> GetOrder(string orderNo)
    {
        var memberId = _currentUser.GetUserId();
        var order = await _mediator.Send(new GetMallOrderByNoQuery { OrderNo = orderNo });
        if (order.MemberId != memberId)
            return Forbid();
        return Ok(order);
    }

    [HttpPost("orders/{orderNo}/cancel")]
    public async Task<ActionResult<bool>> CancelOrder(string orderNo)
    {
        return Ok(await _mediator.Send(new CancelMallOrderCommand { OrderNo = orderNo }));
    }

    [HttpPost("orders/{orderNo}/payments")]
    public async Task<ActionResult<MallPaymentDto>> CreatePayment(
        string orderNo,
        [FromBody] CreateMallPaymentRequest request)
    {
        return Ok(await _mediator.Send(new CreateMallPaymentCommand { OrderNo = orderNo, Request = request }));
    }

    [HttpPost("payments/{paymentNo}/simulate")]
    public async Task<ActionResult<bool>> SimulatePayment(
        string paymentNo,
        [FromBody] SimulateMallPaymentRequest request)
    {
        return Ok(await _mediator.Send(new SimulateMallPaymentCommand { PaymentNo = paymentNo, Request = request }));
    }

    [HttpPost("orders/{orderNo}/confirm-receipt")]
    public async Task<ActionResult<bool>> ConfirmReceipt(string orderNo)
    {
        return Ok(await _mediator.Send(new ConfirmMallOrderReceiptCommand { OrderNo = orderNo }));
    }

    [HttpPost("orders/{orderNo}/refunds")]
    public async Task<ActionResult<MallRefundDto>> CreateRefund(
        string orderNo,
        [FromBody] CreateMallRefundRequest request)
    {
        return Ok(await _mediator.Send(new CreateMallRefundCommand { OrderNo = orderNo, Request = request }));
    }

    [HttpPost("refunds/{refundNo}/simulate")]
    public async Task<ActionResult<bool>> SimulateRefund(
        string refundNo,
        [FromBody] SimulateMallRefundRequest request)
    {
        return Ok(await _mediator.Send(new SimulateMallRefundCommand { RefundNo = refundNo, Request = request }));
    }
}
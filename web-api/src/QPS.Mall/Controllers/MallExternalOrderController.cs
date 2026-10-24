using MediatR;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallOrders;
using QPS.Mall.Application.Features.MallPayments;
using QPS.Mall.Application.Features.MallRefunds;
using QPS.Mall.Contracts.DTOs;

namespace QPS.Mall.Controllers;

/// <summary>
/// 外部系统下单与交易链路，复用现有 JWT 账号鉴权。
/// </summary>
[ApiController]
[Route("api/mall")]
public class MallExternalOrderController : ControllerBase
{
    private readonly IMediator _mediator;

    public MallExternalOrderController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// 下单。
    /// </summary>
    [HttpPost("orders")]
    public async Task<ActionResult<MallOrderDetailDto>> CreateOrder([FromBody] CreateMallOrderRequest request)
    {
        return Ok(await _mediator.Send(new CreateMallOrderCommand { Request = request }));
    }

    /// <summary>
    /// 按订单号查询订单详情。
    /// </summary>
    [HttpGet("orders/{orderNo}")]
    public async Task<ActionResult<MallOrderDetailDto>> GetOrder(string orderNo)
    {
        return Ok(await _mediator.Send(new GetMallOrderByNoQuery { OrderNo = orderNo }));
    }

    /// <summary>
    /// 取消待支付订单。
    /// </summary>
    [HttpPost("orders/{orderNo}/cancel")]
    public async Task<ActionResult<bool>> CancelOrder(string orderNo)
    {
        return Ok(await _mediator.Send(new CancelMallOrderCommand { OrderNo = orderNo }));
    }

    /// <summary>
    /// 创建支付单。
    /// </summary>
    [HttpPost("orders/{orderNo}/payments")]
    public async Task<ActionResult<MallPaymentDto>> CreatePayment(
        string orderNo,
        [FromBody] CreateMallPaymentRequest request)
    {
        return Ok(await _mediator.Send(new CreateMallPaymentCommand { OrderNo = orderNo, Request = request }));
    }

    /// <summary>
    /// 模拟支付结果。
    /// </summary>
    [HttpPost("payments/{paymentNo}/simulate")]
    public async Task<ActionResult<bool>> SimulatePayment(
        string paymentNo,
        [FromBody] SimulateMallPaymentRequest request)
    {
        return Ok(await _mediator.Send(new SimulateMallPaymentCommand { PaymentNo = paymentNo, Request = request }));
    }

    /// <summary>
    /// 确认收货。
    /// </summary>
    [HttpPost("orders/{orderNo}/confirm-receipt")]
    public async Task<ActionResult<bool>> ConfirmReceipt(string orderNo)
    {
        return Ok(await _mediator.Send(new ConfirmMallOrderReceiptCommand { OrderNo = orderNo }));
    }

    /// <summary>
    /// 申请整单退款。
    /// </summary>
    [HttpPost("orders/{orderNo}/refunds")]
    public async Task<ActionResult<MallRefundDto>> CreateRefund(
        string orderNo,
        [FromBody] CreateMallRefundRequest request)
    {
        return Ok(await _mediator.Send(new CreateMallRefundCommand { OrderNo = orderNo, Request = request }));
    }

    /// <summary>
    /// 模拟退款结果。
    /// </summary>
    [HttpPost("refunds/{refundNo}/simulate")]
    public async Task<ActionResult<bool>> SimulateRefund(
        string refundNo,
        [FromBody] SimulateMallRefundRequest request)
    {
        return Ok(await _mediator.Send(new SimulateMallRefundCommand { RefundNo = refundNo, Request = request }));
    }
}

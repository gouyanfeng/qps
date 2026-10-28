using MediatR;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallCarts;
using QPS.Mall.Contracts.DTOs;

namespace QPS.Mall.Controllers;

[ApiController]
[Route("api/mall")]
public class MallCartController : ControllerBase
{
    private readonly IMediator _mediator;

    public MallCartController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("carts/{userId:guid}/store/{storeId:guid}")]
    public async Task<ActionResult<MallCartDto>> GetCart(Guid userId, Guid storeId)
    {
        return Ok(await _mediator.Send(new GetMallCartQuery { UserId = userId, StoreId = storeId }));
    }

    [HttpPost("carts/add")]
    public async Task<ActionResult<MallCartDto>> AddToCart([FromBody] MallCartAddRequest request)
    {
        return Ok(await _mediator.Send(new AddToMallCartCommand { Request = request }));
    }

    [HttpPut("carts/update-item")]
    public async Task<ActionResult<MallCartDto>> UpdateCartItem([FromBody] MallCartUpdateItemRequest request)
    {
        return Ok(await _mediator.Send(new UpdateMallCartItemCommand { Request = request }));
    }

    [HttpDelete("carts/{userId:guid}/store/{storeId:guid}/items/{skuId:guid}")]
    public async Task<ActionResult<MallCartDto>> RemoveFromCart(Guid userId, Guid storeId, Guid skuId)
    {
        return Ok(await _mediator.Send(new RemoveFromMallCartCommand
        {
            UserId = userId,
            StoreId = storeId,
            SkuId = skuId
        }));
    }

    [HttpDelete("carts/{userId:guid}/store/{storeId:guid}")]
    public async Task<ActionResult<bool>> ClearCart(Guid userId, Guid storeId)
    {
        return Ok(await _mediator.Send(new ClearMallCartCommand { UserId = userId, StoreId = storeId }));
    }
}
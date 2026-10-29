using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallCarts;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Contracts;
using QPS.Shared.Extensions;

namespace QPS.Mall.Controllers;

[ApiController]
[Route("api/mall")]
[Authorize(Policy = "MallMember")]
public class MallCartController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public MallCartController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpGet("carts/{userId:guid}/store/{storeId:guid}")]
    public async Task<ActionResult<MallCartDto>> GetCart(Guid userId, Guid storeId)
    {
        return Ok(await _mediator.Send(new GetMallCartQuery
        {
            UserId = _currentUser.GetUserId(),
            StoreId = storeId
        }));
    }

    [HttpPost("carts/add")]
    public async Task<ActionResult<MallCartDto>> AddToCart([FromBody] MallCartAddRequest request)
    {
        request.UserId = _currentUser.GetUserId();
        return Ok(await _mediator.Send(new AddToMallCartCommand { Request = request }));
    }

    [HttpPut("carts/update-item")]
    public async Task<ActionResult<MallCartDto>> UpdateCartItem([FromBody] MallCartUpdateItemRequest request)
    {
        request.UserId = _currentUser.GetUserId();
        return Ok(await _mediator.Send(new UpdateMallCartItemCommand { Request = request }));
    }

    [HttpDelete("carts/{userId:guid}/store/{storeId:guid}/items/{skuId:guid}")]
    public async Task<ActionResult<MallCartDto>> RemoveFromCart(Guid userId, Guid storeId, Guid skuId)
    {
        return Ok(await _mediator.Send(new RemoveFromMallCartCommand
        {
            UserId = _currentUser.GetUserId(),
            StoreId = storeId,
            SkuId = skuId
        }));
    }

    [HttpDelete("carts/{userId:guid}/store/{storeId:guid}")]
    public async Task<ActionResult<bool>> ClearCart(Guid userId, Guid storeId)
    {
        return Ok(await _mediator.Send(new ClearMallCartCommand
        {
            UserId = _currentUser.GetUserId(),
            StoreId = storeId
        }));
    }
}
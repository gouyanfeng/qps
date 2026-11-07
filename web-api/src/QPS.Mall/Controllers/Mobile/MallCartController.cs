using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallCarts;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Contracts;
using QPS.Shared.Exceptions;
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

    [HttpGet("carts/{userId:guid}")]
    public async Task<ActionResult<MallCartDto>> GetCart(
        Guid userId,
        [FromQuery] string? skuIds = null)
    {
        EnsurePathUser(userId);

        // 带 skuIds 参数（即使为空串）表示按选中商品试算；不带表示全部选中
        var selectedSkuIds = Request.Query.ContainsKey("skuIds")
            ? ParseSkuIds(skuIds ?? string.Empty)
            : null;

        return Ok(await _mediator.Send(new GetMallCartQuery
        {
            UserId = _currentUser.GetUserId(),
            SelectedSkuIds = selectedSkuIds
        }));
    }

    /// <summary>路径中的用户标识必须与当前登录会员一致，空 Guid 视为占位值放行。</summary>
    private void EnsurePathUser(Guid userId)
    {
        if (userId != Guid.Empty && userId != _currentUser.GetUserId())
        {
            throw new BusinessException(400, "用户标识不匹配");
        }
    }

    /// <summary>解析选中 SKU 列表；空串返回空集合（未选中任何商品），非法值直接报参数错误。</summary>
    private static List<Guid> ParseSkuIds(string skuIds)
    {
        return skuIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value =>
            {
                if (!Guid.TryParse(value, out var id))
                {
                    throw new BusinessException(400, "skuIds 参数格式不正确");
                }

                return id;
            })
            .ToList();
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

    [HttpDelete("carts/{userId:guid}/items/{skuId:guid}")]
    public async Task<ActionResult<MallCartDto>> RemoveFromCart(Guid userId, Guid skuId)
    {
        EnsurePathUser(userId);
        return Ok(await _mediator.Send(new RemoveFromMallCartCommand
        {
            UserId = _currentUser.GetUserId(),
            SkuId = skuId
        }));
    }

    /// <summary>批量移除本次下单的规格明细，保留购物车中其他商品。</summary>
    [HttpPost("carts/remove-items")]
    public async Task<ActionResult<MallCartDto>> RemoveItems([FromBody] MallCartRemoveItemsRequest request)
    {
        return Ok(await _mediator.Send(new RemoveMallCartItemsCommand
        {
            UserId = _currentUser.GetUserId(),
            SkuIds = request.SkuIds
        }));
    }

    [HttpDelete("carts/{userId:guid}")]
    public async Task<ActionResult<bool>> ClearCart(Guid userId)
    {
        EnsurePathUser(userId);
        return Ok(await _mediator.Send(new ClearMallCartCommand
        {
            UserId = _currentUser.GetUserId()
        }));
    }
}
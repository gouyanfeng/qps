using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallProducts;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Contracts;
using QPS.Shared.Extensions;

namespace QPS.Mall.Controllers.Mobile;

/// <summary>
/// 移动端商品评价发表。
/// </summary>
[ApiController]
[Route("api/mall")]
[Authorize(Policy = "MallMember")]
public class MallProductReviewController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public MallProductReviewController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>
    /// 对已完成订单中的商品发表评价。
    /// </summary>
    [HttpPost("products/{productId:guid}/reviews")]
    public async Task<ActionResult<MallProductReviewDto>> Create(
        Guid productId,
        [FromBody] MallProductReviewCreateRequest request)
    {
        var memberId = _currentUser.GetUserId();

        return Ok(await _mediator.Send(new CreateMallProductReviewCommand
        {
            ProductId = productId,
            MemberId = memberId,
            Request = request
        }));
    }
}

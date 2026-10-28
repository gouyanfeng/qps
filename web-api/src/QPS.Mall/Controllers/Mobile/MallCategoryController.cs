using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallCategories;
using QPS.Mall.Contracts.DTOs;

namespace QPS.Mall.Controllers.Mobile;

[ApiController]
[Route("api/mall")]
public class MallCategoryController : ControllerBase
{
    private readonly IMediator _mediator;

    public MallCategoryController(IMediator mediator) => _mediator = mediator;

    [HttpGet("categories/tree")]
    [AllowAnonymous]
    public async Task<ActionResult<List<MallCategoryTreeNodeDto>>> GetTree(
        [FromQuery] Guid storeId,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetMallCategoryTreeQuery { StoreId = storeId }, cancellationToken));
    }

    [HttpGet("categories")]
    [AllowAnonymous]
    public async Task<ActionResult<List<MallCategoryDto>>> GetList(
        [FromQuery] Guid storeId,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(
            new GetMallCategoriesQuery { StoreId = storeId, IsActive = true },
            cancellationToken));
    }
}
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallCategories;
using QPS.Mall.Contracts.DTOs;

namespace QPS.Mall.Controllers.Mobile;

[ApiController]
[Route("api/mall")]
[AllowAnonymous]
public class MallCategoryController : ControllerBase
{
    private readonly IMediator _mediator;

    public MallCategoryController(IMediator mediator) => _mediator = mediator;

    [HttpGet("categories/tree")]
    public async Task<ActionResult<List<MallCategoryTreeNodeDto>>> GetTree(
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetMallCategoryTreeQuery(), cancellationToken));
    }

    [HttpGet("categories")]
    public async Task<ActionResult<List<MallCategoryDto>>> GetList(
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(
            new GetMallCategoriesQuery { IsActive = true },
            cancellationToken));
    }
}
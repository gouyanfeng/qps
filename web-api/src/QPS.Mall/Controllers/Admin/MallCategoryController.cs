using MediatR;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallCategories;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Common;

namespace QPS.Mall.Controllers;

/// <summary>
/// 管理端商品分类维护。
/// </summary>
[ApiController]
[Route("api/admin/mall/categories")]
public class MallCategoryController : ControllerBase
{
    private readonly IMediator _mediator;

    public MallCategoryController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// 分页查询商品分类。
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PaginationResponse<MallCategoryDto>>> GetCategories(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortField = "SortOrder",
        [FromQuery] string sortDirection = "Ascending",
        [FromQuery] string? keyword = null,
        [FromQuery] bool? isActive = null)
    {
        var query = new GetMallCategoriesQuery
        {
            Page = page,
            PageSize = pageSize,
            SortField = sortField,
            SortDirection = sortDirection,
            Keyword = keyword,
            IsActive = isActive
        };

        return Ok(await _mediator.Send(query));
    }

    /// <summary>
    /// 查询商品分类详情。
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MallCategoryDto>> GetCategory(Guid id)
    {
        return Ok(await _mediator.Send(new GetMallCategoryQuery { Id = id }));
    }

    /// <summary>
    /// 新增商品分类。
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<bool>> CreateCategory([FromBody] MallCategoryCreateRequest request)
    {
        return Ok(await _mediator.Send(new CreateMallCategoryCommand { Request = request }));
    }

    /// <summary>
    /// 编辑商品分类。
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<bool>> UpdateCategory(Guid id, [FromBody] MallCategoryUpdateRequest request)
    {
        return Ok(await _mediator.Send(new UpdateMallCategoryCommand { Id = id, Request = request }));
    }

    /// <summary>
    /// 商品分类启用或停用。
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<bool>> ChangeCategoryStatus(
        Guid id,
        [FromBody] MallCategoryStatusRequest request)
    {
        return Ok(await _mediator.Send(new ChangeMallCategoryStatusCommand { Id = id, Request = request }));
    }

    /// <summary>
    /// 删除商品分类（软删除）。
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<bool>> DeleteCategory(Guid id)
    {
        return Ok(await _mediator.Send(new DeleteMallCategoryCommand { Id = id }));
    }
}

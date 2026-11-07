using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallStores;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Common;

namespace QPS.Mall.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/admin/mall")]
public class MallController : ControllerBase
{
    private readonly IMediator _mediator;

    public MallController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// 模块存活检查：能访问说明商场模块已装配。
    /// </summary>
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok("QPS.Mall");
    }

    /// <summary>
    /// 分页查询商场门店。
    /// </summary>
    [HttpGet("stores")]
    public async Task<ActionResult<PaginationResponse<MallStoreDto>>> GetStores(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortField = "CreatedAt",
        [FromQuery] string sortDirection = "Descending",
        [FromQuery] string? keyword = null,
        [FromQuery] bool? isActive = null)
    {
        var query = new GetMallStoresQuery
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
    /// 查询商场门店详情。
    /// </summary>
    [HttpGet("stores/{id:guid}")]
    public async Task<ActionResult<MallStoreDto>> GetStore(Guid id)
    {
        return Ok(await _mediator.Send(new GetMallStoreQuery { Id = id }));
    }

    /// <summary>
    /// 新增商场门店。
    /// </summary>
    [HttpPost("stores")]
    public async Task<ActionResult<bool>> CreateStore([FromBody] MallStoreCreateRequest request)
    {
        return Ok(await _mediator.Send(new CreateMallStoreCommand { Request = request }));
    }

    /// <summary>
    /// 编辑商场门店。
    /// </summary>
    [HttpPut("stores/{id:guid}")]
    public async Task<ActionResult<bool>> UpdateStore(Guid id, [FromBody] MallStoreUpdateRequest request)
    {
        return Ok(await _mediator.Send(new UpdateMallStoreCommand { Id = id, Request = request }));
    }

    /// <summary>
    /// 启用或停用商场门店。
    /// </summary>
    [HttpPatch("stores/{id:guid}/status")]
    public async Task<ActionResult<bool>> ChangeStoreStatus(Guid id, [FromBody] MallStoreStatusRequest request)
    {
        return Ok(await _mediator.Send(new ChangeMallStoreStatusCommand { Id = id, Request = request }));
    }

    /// <summary>
    /// 删除商场门店（软删除）。
    /// </summary>
    [HttpDelete("stores/{id:guid}")]
    public async Task<ActionResult<bool>> DeleteStore(Guid id)
    {
        return Ok(await _mediator.Send(new DeleteMallStoreCommand { Id = id }));
    }
}

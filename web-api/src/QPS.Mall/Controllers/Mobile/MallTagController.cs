using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallTags;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;

namespace QPS.Mall.Controllers.Mobile;

/// <summary>
/// 移动端标签查询（仅返回启用中的标签）。
/// </summary>
[ApiController]
[Route("api/mall/tags")]
[AllowAnonymous]
public class MallTagController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// 按类型查询启用中的标签。
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<MallTagDto>>> GetTags(
        [FromQuery] string? type = MallTagType.Review,
        [FromQuery] bool? isActive = true,
        CancellationToken cancellationToken = default) =>
        Ok(await mediator.Send(
            new GetMallTagsQuery { Type = type, IsActive = isActive },
            cancellationToken));
}

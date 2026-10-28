using MediatR;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallArticles;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;

namespace QPS.Mall.Controllers;

[ApiController]
public class MallArticleController : ControllerBase
{
    private readonly IMediator _mediator;

    public MallArticleController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("api/admin/mall/articles")]
    public async Task<ActionResult<List<MallArticleListItemDto>>> GetList(
        [FromQuery] Guid storeId, MallArticleType? articleType, string? keyword, bool? isActive)
    {
        return Ok(await _mediator.Send(new GetMallArticlesQuery
        {
            StoreId = storeId, ArticleType = articleType, Keyword = keyword, IsActive = isActive
        }));
    }

    [HttpGet("api/admin/mall/articles/{id:guid}")]
    public async Task<ActionResult<MallArticleDto>> Get(Guid id)
    {
        return Ok(await _mediator.Send(new GetMallArticleQuery { Id = id }));
    }

    [HttpPost("api/admin/mall/articles")]
    public async Task<ActionResult<MallArticleDto>> Create([FromBody] MallArticleCreateRequest request)
    {
        return Ok(await _mediator.Send(new CreateMallArticleCommand { Request = request }));
    }

    [HttpPut("api/admin/mall/articles/{id:guid}")]
    public async Task<ActionResult<MallArticleDto>> Update(Guid id, [FromBody] MallArticleUpdateRequest request)
    {
        return Ok(await _mediator.Send(new UpdateMallArticleCommand { Id = id, Request = request }));
    }

    [HttpDelete("api/admin/mall/articles/{id:guid}")]
    public async Task<ActionResult<bool>> Delete(Guid id)
    {
        return Ok(await _mediator.Send(new DeleteMallArticleCommand { Id = id }));
    }

    [HttpPut("api/admin/mall/articles/{id:guid}/publish")]
    public async Task<ActionResult<bool>> Publish(Guid id)
    {
        return Ok(await _mediator.Send(new PublishMallArticleCommand { Id = id }));
    }

    [HttpPut("api/admin/mall/articles/{id:guid}/unpublish")]
    public async Task<ActionResult<bool>> Unpublish(Guid id)
    {
        return Ok(await _mediator.Send(new UnpublishMallArticleCommand { Id = id }));
    }

    [HttpPut("api/admin/mall/articles/{id:guid}/top")]
    public async Task<ActionResult<bool>> SetTop(Guid id, [FromQuery] bool isTop)
    {
        return Ok(await _mediator.Send(new SetMallArticleTopCommand { Id = id, IsTop = isTop }));
    }

    [HttpGet("api/mall/articles")]
    public async Task<ActionResult<List<MallArticleListItemDto>>> GetPublished(
        [FromQuery] Guid storeId, MallArticleType? articleType)
    {
        return Ok(await _mediator.Send(new GetPublishedMallArticlesQuery { StoreId = storeId, ArticleType = articleType }));
    }

    [HttpGet("api/mall/articles/{id:guid}")]
    public async Task<ActionResult<MallArticleDto>> GetPublishedDetail(Guid id)
    {
        return Ok(await _mediator.Send(new GetPublishedMallArticleDetailQuery { Id = id }));
    }
}
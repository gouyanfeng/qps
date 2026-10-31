using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallArticles;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;

namespace QPS.Mall.Controllers.Mobile;

[ApiController]
[Route("api/mall")]
[AllowAnonymous]
public class MallArticleController : ControllerBase
{
    private readonly IMediator _mediator;

    public MallArticleController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("articles")]
    public async Task<ActionResult<List<MallArticleListItemDto>>> GetPublished(
        MallArticleType? articleType)
    {
        return Ok(await _mediator.Send(new GetPublishedMallArticlesQuery { ArticleType = articleType }));
    }

    [HttpGet("articles/{id:guid}")]
    public async Task<ActionResult<MallArticleDto>> GetPublishedDetail(Guid id)
    {
        return Ok(await _mediator.Send(new GetPublishedMallArticleDetailQuery { Id = id }));
    }
}
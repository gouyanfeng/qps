using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallPages;
using QPS.Mall.Contracts.DTOs;

namespace QPS.Mall.Controllers.Mobile;

[ApiController]
[Route("api/mall")]
[AllowAnonymous]
public class MallPageController : ControllerBase
{
    private readonly IMediator _mediator;

    public MallPageController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("pages/{pageCode}")]
    public async Task<ActionResult<MallPageDto>> GetByCode(string pageCode)
    {
        return Ok(await _mediator.Send(new GetMallPageByCodeQuery { PageCode = pageCode }));
    }
}
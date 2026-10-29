using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallMembers;
using QPS.Mall.Contracts.DTOs.MallMembers;

namespace QPS.Mall.Controllers.Mobile;

[ApiController]
[Route("api/mall/auth")]
[AllowAnonymous]
public class MallAuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public MallAuthController(IMediator mediator) => _mediator = mediator;

    [HttpPost("register")]
    public async Task<ActionResult<MallMemberAuthResponse>> Register([FromBody] MallMemberRegisterRequest request)
    {
        return Ok(await _mediator.Send(new RegisterMallMemberCommand { Request = request }));
    }

    [HttpPost("login")]
    public async Task<ActionResult<MallMemberAuthResponse>> Login([FromBody] MallMemberLoginRequest request)
    {
        return Ok(await _mediator.Send(new LoginMallMemberCommand { Request = request }));
    }
}
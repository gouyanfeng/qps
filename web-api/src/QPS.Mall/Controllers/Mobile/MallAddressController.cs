using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallAddresses;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Contracts;
using QPS.Shared.Extensions;

namespace QPS.Mall.Controllers;

[ApiController]
[Route("api/mall")]
[Authorize(Policy = "MallMember")]
public class MallAddressController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public MallAddressController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpGet("addresses")]
    public async Task<ActionResult<List<MallAddressDto>>> GetAddresses()
    {
        return Ok(await _mediator.Send(new GetMallAddressesQuery { UserId = _currentUser.GetUserId() }));
    }

    [HttpGet("addresses/{addressId:guid}")]
    public async Task<ActionResult<MallAddressDto>> GetAddress(Guid addressId)
    {
        return Ok(await _mediator.Send(new GetMallAddressQuery
        {
            AddressId = addressId,
            UserId = _currentUser.GetUserId()
        }));
    }

    [HttpPost("addresses")]
    public async Task<ActionResult<Guid>> CreateAddress([FromBody] MallAddressCreateRequest request)
    {
        // 归属以令牌为准，忽略客户端传入的 userId
        request.UserId = _currentUser.GetUserId();
        return Ok(await _mediator.Send(new CreateMallAddressCommand { Request = request }));
    }

    [HttpPut("addresses/{addressId:guid}")]
    public async Task<ActionResult<bool>> UpdateAddress(
        Guid addressId,
        [FromBody] MallAddressUpdateRequest request)
    {
        return Ok(await _mediator.Send(new UpdateMallAddressCommand
        {
            AddressId = addressId,
            UserId = _currentUser.GetUserId(),
            Request = request
        }));
    }

    [HttpDelete("addresses/{addressId:guid}")]
    public async Task<ActionResult<bool>> DeleteAddress(Guid addressId)
    {
        return Ok(await _mediator.Send(new DeleteMallAddressCommand
        {
            AddressId = addressId,
            UserId = _currentUser.GetUserId()
        }));
    }
}
using MediatR;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallAddresses;
using QPS.Mall.Contracts.DTOs;

namespace QPS.Mall.Controllers;

[ApiController]
[Route("api/mall")]
public class MallAddressController : ControllerBase
{
    private readonly IMediator _mediator;

    public MallAddressController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("addresses")]
    public async Task<ActionResult<List<MallAddressDto>>> GetAddresses([FromQuery] Guid? userId = null)
    {
        return Ok(await _mediator.Send(new GetMallAddressesQuery { UserId = userId }));
    }

    [HttpGet("addresses/{addressId:guid}")]
    public async Task<ActionResult<MallAddressDto>> GetAddress(Guid addressId)
    {
        return Ok(await _mediator.Send(new GetMallAddressQuery { AddressId = addressId }));
    }

    [HttpPost("addresses")]
    public async Task<ActionResult<Guid>> CreateAddress([FromBody] MallAddressCreateRequest request)
    {
        return Ok(await _mediator.Send(new CreateMallAddressCommand { Request = request }));
    }

    [HttpPut("addresses/{addressId:guid}")]
    public async Task<ActionResult<bool>> UpdateAddress(
        Guid addressId,
        [FromBody] MallAddressUpdateRequest request)
    {
        return Ok(await _mediator.Send(new UpdateMallAddressCommand { AddressId = addressId, Request = request }));
    }

    [HttpDelete("addresses/{addressId:guid}")]
    public async Task<ActionResult<bool>> DeleteAddress(Guid addressId)
    {
        return Ok(await _mediator.Send(new DeleteMallAddressCommand { AddressId = addressId }));
    }
}
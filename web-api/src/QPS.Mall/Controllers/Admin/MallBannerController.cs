using MediatR;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallBanners;
using QPS.Mall.Contracts.DTOs;

namespace QPS.Mall.Controllers;

[ApiController]
public class MallBannerController : ControllerBase
{
    private readonly IMediator _mediator;

    public MallBannerController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("api/admin/mall/banners")]
    public async Task<ActionResult<List<MallBannerDto>>> GetList([FromQuery] Guid storeId, string? position, bool? isActive)
    {
        return Ok(await _mediator.Send(new GetMallBannersQuery { StoreId = storeId, Position = position, IsActive = isActive }));
    }

    [HttpGet("api/admin/mall/banners/{id:guid}")]
    public async Task<ActionResult<MallBannerDto>> Get(Guid id)
    {
        return Ok(await _mediator.Send(new GetMallBannerQuery { Id = id }));
    }

    [HttpPost("api/admin/mall/banners")]
    public async Task<ActionResult<MallBannerDto>> Create([FromBody] MallBannerCreateRequest request)
    {
        return Ok(await _mediator.Send(new CreateMallBannerCommand { Request = request }));
    }

    [HttpPut("api/admin/mall/banners/{id:guid}")]
    public async Task<ActionResult<MallBannerDto>> Update(Guid id, [FromBody] MallBannerUpdateRequest request)
    {
        return Ok(await _mediator.Send(new UpdateMallBannerCommand { Id = id, Request = request }));
    }

    [HttpDelete("api/admin/mall/banners/{id:guid}")]
    public async Task<ActionResult<bool>> Delete(Guid id)
    {
        return Ok(await _mediator.Send(new DeleteMallBannerCommand { Id = id }));
    }

    [HttpPut("api/admin/mall/banners/{id:guid}/status")]
    public async Task<ActionResult<bool>> ChangeStatus(Guid id, [FromQuery] bool isActive)
    {
        return Ok(await _mediator.Send(new ChangeMallBannerStatusCommand { Id = id, IsActive = isActive }));
    }

    [HttpPost("api/admin/mall/banners/batch-sort")]
    public async Task<ActionResult<bool>> BatchSort([FromBody] MallBannerBatchSortRequest request)
    {
        return Ok(await _mediator.Send(new BatchSortMallBannerCommand { Request = request }));
    }

}
using MediatR;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallFreightTemplates;
using QPS.Mall.Contracts.DTOs;

namespace QPS.Mall.Controllers;

[ApiController]
[Route("api/admin/mall")]
public class MallFreightTemplateController : ControllerBase
{
    private readonly IMediator _mediator;

    public MallFreightTemplateController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("freight-templates")]
    public async Task<ActionResult<List<MallFreightTemplateListDto>>> GetTemplates(
        [FromQuery] Guid? storeId = null,
        [FromQuery] string? keyword = null)
    {
        return Ok(await _mediator.Send(new GetMallFreightTemplatesQuery
        {
            StoreId = storeId,
            Keyword = keyword
        }));
    }

    [HttpGet("freight-templates/{templateId:guid}")]
    public async Task<ActionResult<MallFreightTemplateDetailDto>> GetTemplate(Guid templateId)
    {
        return Ok(await _mediator.Send(new GetMallFreightTemplateQuery { TemplateId = templateId }));
    }

    [HttpPost("freight-templates")]
    public async Task<ActionResult<Guid>> CreateTemplate([FromBody] MallFreightTemplateCreateRequest request)
    {
        return Ok(await _mediator.Send(new CreateMallFreightTemplateCommand { Request = request }));
    }

    [HttpPut("freight-templates/{templateId:guid}")]
    public async Task<ActionResult<bool>> UpdateTemplate(
        Guid templateId,
        [FromBody] MallFreightTemplateUpdateRequest request)
    {
        return Ok(await _mediator.Send(new UpdateMallFreightTemplateCommand
        {
            TemplateId = templateId,
            Request = request
        }));
    }

    [HttpDelete("freight-templates/{templateId:guid}")]
    public async Task<ActionResult<bool>> DeleteTemplate(Guid templateId)
    {
        return Ok(await _mediator.Send(new DeleteMallFreightTemplateCommand { TemplateId = templateId }));
    }
}
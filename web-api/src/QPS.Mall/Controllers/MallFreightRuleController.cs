using MediatR;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallFreightRules;
using QPS.Mall.Contracts.DTOs;

namespace QPS.Mall.Controllers;

/// <summary>
/// 管理端门店运费规则。
/// </summary>
[ApiController]
[Route("api/admin/mall")]
public class MallFreightRuleController : ControllerBase
{
    private readonly IMediator _mediator;

    public MallFreightRuleController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// 查询全部门店运费规则。
    /// </summary>
    [HttpGet("freight-rules")]
    public async Task<ActionResult<List<MallFreightRuleDto>>> GetFreightRules([FromQuery] string? keyword = null)
    {
        return Ok(await _mediator.Send(new GetMallFreightRulesQuery { Keyword = keyword }));
    }

    /// <summary>
    /// 新增或更新门店运费规则。
    /// </summary>
    [HttpPut("freight-rules/{storeId:guid}")]
    public async Task<ActionResult<bool>> UpsertFreightRule(
        Guid storeId,
        [FromBody] MallFreightRuleUpsertRequest request)
    {
        return Ok(await _mediator.Send(new UpsertMallFreightRuleCommand
        {
            StoreId = storeId,
            Request = request
        }));
    }
}

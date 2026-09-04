using MediatR;
using Microsoft.AspNetCore.Mvc;
using QPS.Application.Contracts.Crm;
using QPS.Application.Features.Crm;
using QPS.Application.Features.Crm.CrmContacts;
using QPS.Application.Features.Crm.CrmFollowRecords;
using QPS.Application.Features.Crm.CrmHerbBases;
using QPS.Application.Features.Crm.CrmHerbBaseSubjects;
using QPS.Application.Features.Crm.CrmHerbBaseSupplies;
using QPS.Application.Features.Crm.CrmTransfers;
using QPS.Application.Extensions;
using QPS.Application.Interfaces;
using QPS.Domain.Entities.Crm;

namespace QPS.WebAPI.Controllers.Admin.Crm;

/// <summary>
/// 药材基地控制器
/// </summary>
[Route("api/admin/crm/herb-bases")]
[ApiController]
public class CrmHerbBaseController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IDbContext _dbContext;

    public CrmHerbBaseController(IMediator mediator, IDbContext dbContext)
    {
        _mediator = mediator;
        _dbContext = dbContext;
    }

    [HttpGet("/api/admin/crm/herb-base-subjects")]
    public async Task<ActionResult<PaginationResponse<CrmHerbBaseSubjectDto>>> GetHerbBaseSubjects(
        [FromQuery] GetCrmHerbBaseSubjectsQuery query)
    {
        return Ok(await _mediator.Send(query));
    }

    [HttpGet("/api/admin/crm/herb-base-subjects/{id:guid}")]
    public async Task<ActionResult<CrmHerbBaseSubjectDetailDto>> GetHerbBaseSubject(Guid id)
    {
        return Ok(await _mediator.Send(new GetCrmHerbBaseSubjectQuery { Id = id }));
    }

    [HttpPatch("/api/admin/crm/herb-base-subjects/{subjectId:guid}/owner")]
    public async Task<ActionResult<bool>> ChangeHerbBaseSubjectOwner(
        Guid subjectId,
        [FromBody] CrmTransferOwnerChangeRequest request)
    {
        return Ok(await _mediator.Send(new ChangeCrmOwnerCommand
        {
            EntityType = CrmTransferEntityType.HerbBaseSubject,
            Request = CreateOwnerRequest(subjectId, request)
        }));
    }

    [HttpGet("/api/admin/crm/herb-base-subjects/{subjectId:guid}/contacts")]
    public async Task<ActionResult<List<CrmContactDto>>> GetSubjectContacts(Guid subjectId)
    {
        return Ok(await _mediator.Send(new GetCrmContactsQuery
        {
            EntityType = CrmCodes.HerbBaseSubjectEntityType,
            EntityId = subjectId
        }));
    }

    [HttpPost("/api/admin/crm/herb-base-subjects/{subjectId:guid}/contacts")]
    public async Task<ActionResult<bool>> CreateSubjectContact(
        Guid subjectId,
        [FromBody] CrmContactCreateRequest request)
    {
        return Ok(await _mediator.Send(new CreateCrmContactCommand
        {
            EntityType = CrmCodes.HerbBaseSubjectEntityType,
            EntityId = subjectId,
            Request = request
        }));
    }

    [HttpPut("/api/admin/crm/herb-base-subjects/{subjectId:guid}/contacts/{contactId:guid}")]
    public async Task<ActionResult<bool>> UpdateSubjectContact(
        Guid subjectId,
        Guid contactId,
        [FromBody] CrmContactUpdateRequest request)
    {
        return Ok(await _mediator.Send(new UpdateCrmContactCommand
        {
            EntityType = CrmCodes.HerbBaseSubjectEntityType,
            EntityId = subjectId,
            Id = contactId,
            Request = request
        }));
    }

    [HttpPatch("/api/admin/crm/herb-base-subjects/{subjectId:guid}/contacts/{contactId:guid}/primary")]
    public async Task<ActionResult<bool>> SetSubjectPrimaryContact(Guid subjectId, Guid contactId)
    {
        return Ok(await _mediator.Send(new SetPrimaryCrmContactCommand
        {
            EntityType = CrmCodes.HerbBaseSubjectEntityType,
            EntityId = subjectId,
            Id = contactId
        }));
    }

    [HttpPatch("/api/admin/crm/herb-base-subjects/{subjectId:guid}/contacts/{contactId:guid}/status")]
    public async Task<ActionResult<bool>> UpdateSubjectContactStatus(
        Guid subjectId,
        Guid contactId,
        [FromBody] CrmContactStatusRequest request)
    {
        return Ok(await _mediator.Send(new UpdateCrmContactStatusCommand
        {
            EntityType = CrmCodes.HerbBaseSubjectEntityType,
            EntityId = subjectId,
            Id = contactId,
            Request = request
        }));
    }

    [HttpGet("/api/admin/crm/herb-base-subjects/{subjectId:guid}/follow-records")]
    public async Task<ActionResult<List<CrmFollowRecordDto>>> GetSubjectFollowRecords(Guid subjectId)
    {
        return Ok(await _mediator.Send(new GetCrmFollowRecordsQuery
        {
            EntityType = CrmCodes.HerbBaseSubjectEntityType,
            EntityId = subjectId
        }));
    }

    [HttpPost("/api/admin/crm/herb-base-subjects/{subjectId:guid}/follow-records")]
    public async Task<ActionResult<bool>> CreateSubjectFollowRecord(
        Guid subjectId,
        [FromBody] CrmFollowRecordCreateRequest request)
    {
        return Ok(await _mediator.Send(new CreateCrmFollowRecordCommand
        {
            EntityType = CrmCodes.HerbBaseSubjectEntityType,
            EntityId = subjectId,
            Request = request
        }));
    }

    [HttpGet("/api/admin/crm/herb-base-subjects/{subjectId:guid}/transfer-records")]
    public async Task<ActionResult<List<CrmTransferRecordDto>>> GetSubjectTransferRecords(Guid subjectId)
    {
        return Ok(await CrmTransferRecordQuery.GetAsync(
            _dbContext,
            CrmTransferEntityType.HerbBaseSubject,
            subjectId,
            HttpContext.RequestAborted));
    }

    [HttpPut("/api/admin/crm/herb-base-subjects/{id:guid}")]
    public async Task<ActionResult<bool>> UpdateHerbBaseSubject(
        Guid id,
        [FromBody] CrmHerbBaseSubjectUpdateRequest request)
    {
        return Ok(await _mediator.Send(new UpdateCrmHerbBaseSubjectCommand { Id = id, Request = request }));
    }

    /// <summary>
    /// 获取药材基地列表
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PaginationResponse<CrmHerbBaseDto>>> GetCustomers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortField = "CreatedAt",
        [FromQuery] string sortDirection = "Descending",
        [FromQuery] string? baseName = null,
        [FromQuery] string? keyword = null,
        [FromQuery] string? grade = null,
        [FromQuery] string? status = null,
        [FromQuery] string? sourcePlatform = null,
        [FromQuery] Guid? ownerUserId = null,
        [FromQuery] string? province = null,
        [FromQuery] string? city = null,
        [FromQuery] DateTime? nextFollowFrom = null,
        [FromQuery] DateTime? nextFollowTo = null,
        [FromQuery] bool? onlyOverdue = null,
        [FromQuery] bool? onlyNoNextFollow = null)
    {
        var query = new GetCrmHerbBasesQuery
        {
            Page = page,
            PageSize = pageSize,
            SortField = sortField,
            SortDirection = sortDirection,
            BaseName = baseName,
            Keyword = keyword,
            Grade = grade,
            Status = status,
            SourcePlatform = sourcePlatform,
            OwnerUserId = ownerUserId,
            Province = province,
            City = city,
            NextFollowFrom = nextFollowFrom,
            NextFollowTo = nextFollowTo,
            OnlyOverdue = onlyOverdue,
            OnlyNoNextFollow = onlyNoNextFollow
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// 获取药材基地详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<CrmHerbBaseDto>> GetCustomer(Guid id)
    {
        var query = new GetCrmHerbBaseQuery { Id = id };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// 创建药材基地
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<bool>> CreateCustomer([FromBody] CrmHerbBaseCreateRequest request)
    {
        var command = new CreateCrmHerbBaseCommand { Request = request };
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// 更新药材基地
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<bool>> UpdateCustomer(Guid id, [FromBody] CrmHerbBaseUpdateRequest request)
    {
        var command = new UpdateCrmHerbBaseCommand { Id = id, Request = request };
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// 删除药材基地
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<bool>> DeleteCustomer(Guid id)
    {
        var command = new DeleteCrmHerbBaseCommand { Id = id };
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpGet("{herbBaseId:guid}/supplies")]
    public async Task<ActionResult<List<CrmHerbBaseSupplyDto>>> GetSupplies(Guid herbBaseId)
        => Ok(await _mediator.Send(new GetCrmHerbBaseSuppliesQuery { HerbBaseId = herbBaseId }));

    [HttpPost("{herbBaseId:guid}/supplies")]
    public async Task<ActionResult<bool>> CreateSupply(Guid herbBaseId, [FromBody] CrmHerbBaseSupplySaveRequest request)
        => Ok(await _mediator.Send(new CreateCrmHerbBaseSupplyCommand { HerbBaseId = herbBaseId, Request = request }));

    [HttpPut("/api/admin/crm/herb-base-supplies/{id:guid}")]
    public async Task<ActionResult<bool>> UpdateSupply(Guid id, [FromBody] CrmHerbBaseSupplySaveRequest request)
        => Ok(await _mediator.Send(new UpdateCrmHerbBaseSupplyCommand { Id = id, Request = request }));

    [HttpDelete("/api/admin/crm/herb-base-supplies/{id:guid}")]
    public async Task<ActionResult<bool>> DeleteSupply(Guid id)
        => Ok(await _mediator.Send(new DeleteCrmHerbBaseSupplyCommand { Id = id }));

    [HttpPatch("/api/admin/crm/herb-base-supplies/{id:guid}/status")]
    public async Task<ActionResult<bool>> ChangeSupplyStatus(Guid id, [FromBody] CrmHerbBaseSupplyStatusRequest request)
        => Ok(await _mediator.Send(new ChangeCrmHerbBaseSupplyStatusCommand { Id = id, Request = request }));

    private static CrmTransferOwnerChangeRequest CreateOwnerRequest(
        Guid entityId,
        CrmTransferOwnerChangeRequest request)
    {
        return new CrmTransferOwnerChangeRequest
        {
            EntityIds = new List<Guid> { entityId },
            ToOwnerUserId = request.ToOwnerUserId,
            Remark = request.Remark
        };
    }
}





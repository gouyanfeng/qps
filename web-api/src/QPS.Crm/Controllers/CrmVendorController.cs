using QPS.Shared.Common;
using QPS.Crm.Contracts;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using QPS.Crm.Contracts.DTOs;
using QPS.Crm.Contracts.DTOs.CrmContacts;
using QPS.Crm.Contracts.DTOs.CrmFollowRecords;
using QPS.Crm.Contracts.DTOs.CrmTransfers;
using QPS.Crm.Contracts.DTOs.CrmVendors;
using QPS.Shared.Extensions;
using QPS.Crm.Application.Features;
using QPS.Crm.Application.Features.CrmContacts;
using QPS.Crm.Application.Features.CrmFollowRecords;
using QPS.Crm.Application.Features.CrmTransfers;
using QPS.Crm.Application.Features.CrmVendors;
using QPS.Shared.Contracts;
using QPS.Crm.Domain.Entities;

namespace QPS.Crm.Controllers;

[Route("api/admin/crm/vendors")]
[ApiController]
public class CrmVendorController : ControllerBase
{
    private readonly IMediator _mediator;

    public CrmVendorController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<PaginationResponse<CrmVendorDto>>> GetVendors(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortField = "LatestPurchaseTime",
        [FromQuery] string sortDirection = "Descending",
        [FromQuery] string? keyword = null,
        [FromQuery] string? priorityLevel = null,
        [FromQuery] bool? hasPhone = null,
        [FromQuery] bool? hasProduct = null)
    {
        var query = new GetCrmVendorsQuery
        {
            Page = page,
            PageSize = pageSize,
            SortField = sortField,
            SortDirection = sortDirection,
            Keyword = keyword,
            PriorityLevel = priorityLevel,
            HasPhone = hasPhone,
            HasProduct = hasProduct
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CrmVendorDto>> GetVendor(Guid id)
    {
        var query = new GetCrmVendorQuery { Id = id };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<bool>> CreateVendor([FromBody] CrmVendorCreateRequest request)
    {
        var command = new CreateCrmVendorCommand { Request = request };
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<bool>> UpdateVendor(Guid id, [FromBody] CrmVendorUpdateRequest request)
    {
        var command = new UpdateCrmVendorCommand { Id = id, Request = request };
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/owner")]
    public async Task<ActionResult<bool>> ChangeOwner(Guid id, [FromBody] CrmTransferOwnerChangeRequest request)
    {
        var command = new ChangeCrmOwnerCommand
        {
            EntityType = CrmTransferEntityType.Vendor,
            Request = CreateOwnerRequest(id, request)
        };
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpGet("herb-product-options")]
    public async Task<ActionResult<List<AttributeOptionDto>>> GetHerbProductOptions(
        [FromQuery] string? keyword = null,
        [FromQuery] int pageSize = 100)
    {
        var result = await _mediator.Send(new GetCrmHerbProductOptionsQuery
        {
            Keyword = keyword,
            PageSize = pageSize
        });
        return Ok(result);
    }

    [HttpGet("{id:guid}/contacts")]
    public async Task<ActionResult<List<CrmContactDto>>> GetContacts(Guid id)
    {
        return Ok(await _mediator.Send(new GetCrmContactsQuery
        {
            EntityType = CrmCodes.VendorEntityType,
            EntityId = id
        }));
    }

    [HttpPost("{id:guid}/contacts")]
    public async Task<ActionResult<bool>> CreateContact(Guid id, [FromBody] CrmContactCreateRequest request)
    {
        return Ok(await _mediator.Send(new CreateCrmContactCommand
        {
            EntityType = CrmCodes.VendorEntityType,
            EntityId = id,
            Request = request
        }));
    }

    [HttpPut("{id:guid}/contacts/{contactId:guid}")]
    public async Task<ActionResult<bool>> UpdateContact(
        Guid id,
        Guid contactId,
        [FromBody] CrmContactUpdateRequest request)
    {
        return Ok(await _mediator.Send(new UpdateCrmContactCommand
        {
            EntityType = CrmCodes.VendorEntityType,
            EntityId = id,
            Id = contactId,
            Request = request
        }));
    }

    [HttpPatch("{id:guid}/contacts/{contactId:guid}/primary")]
    public async Task<ActionResult<bool>> SetPrimaryContact(Guid id, Guid contactId)
    {
        return Ok(await _mediator.Send(new SetPrimaryCrmContactCommand
        {
            EntityType = CrmCodes.VendorEntityType,
            EntityId = id,
            Id = contactId
        }));
    }

    [HttpPatch("{id:guid}/contacts/{contactId:guid}/status")]
    public async Task<ActionResult<bool>> UpdateContactStatus(
        Guid id,
        Guid contactId,
        [FromBody] CrmContactStatusRequest request)
    {
        return Ok(await _mediator.Send(new UpdateCrmContactStatusCommand
        {
            EntityType = CrmCodes.VendorEntityType,
            EntityId = id,
            Id = contactId,
            Request = request
        }));
    }

    [HttpGet("{id:guid}/follow-records")]
    public async Task<ActionResult<List<CrmFollowRecordDto>>> GetVendorFollowRecords(Guid id)
    {
        var query = new GetCrmFollowRecordsQuery { EntityType = CrmCodes.VendorEntityType, EntityId = id };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPost("{id:guid}/follow-records")]
    public async Task<ActionResult<bool>> CreateVendorFollowRecord(Guid id, [FromBody] CrmFollowRecordCreateRequest request)
    {
        var command = new CreateCrmFollowRecordCommand
        {
            EntityType = CrmCodes.VendorEntityType,
            EntityId = id,
            Request = request
        };
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpGet("{id:guid}/transfer-records")]
    public async Task<ActionResult<List<CrmTransferRecordDto>>> GetTransferRecords(Guid id)
    {
        return Ok(await _mediator.Send(new GetCrmTransferRecordsQuery
        {
            EntityType = CrmTransferEntityType.Vendor,
            EntityId = id
        }));
    }

    [HttpGet("{id}/purchase-demands")]
    public async Task<ActionResult<PaginationResponse<CrmVendorDemandDto>>> GetVendorPurchaseDemands(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortField = "DemandAt",
        [FromQuery] string sortDirection = "Descending")
    {
        var query = new GetCrmVendorDemandsQuery
        {
            VendorId = id,
            Page = page,
            PageSize = pageSize,
            SortField = sortField,
            SortDirection = sortDirection
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

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



using QPS.Shared.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.System.Contracts.DTOs.Messages;
using QPS.System.Application.Features.Messages;
using QPS.System.Domain.Entities;

namespace QPS.System.Controllers;

/// <summary>
/// 站内消息：管理端 CRUD + 当前用户通知/收件箱。
/// 广播公告全体可见；点对点私信仅收件人可见。
/// </summary>
[ApiController]
[Route("api/admin/system-messages")]
[Authorize]
public class SystemMessageController : ControllerBase
{
    private readonly IMediator _mediator;

    public SystemMessageController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // ===== 管理端 =====

    [HttpGet]
    public async Task<ActionResult<PaginationResponse<MessageDto>>> GetMessages(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? keyword = null,
        [FromQuery] SystemMessageType? messageType = null,
        [FromQuery] bool? isPublished = null)
    {
        var result = await _mediator.Send(new GetMessagesQuery
        {
            Request = new MessageQueryRequest
            {
                Page = page,
                PageSize = pageSize,
                Keyword = keyword,
                MessageType = messageType,
                IsPublished = isPublished
            }
        });
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MessageDto>> GetMessage(Guid id)
    {
        return Ok(await _mediator.Send(new GetMessageQuery { Id = id }));
    }

    [HttpPost]
    public async Task<ActionResult<bool>> CreateMessage([FromBody] MessageCreateRequest request)
    {
        return Ok(await _mediator.Send(new CreateMessageCommand { Request = request }));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<bool>> UpdateMessage(Guid id, [FromBody] MessageUpdateRequest request)
    {
        return Ok(await _mediator.Send(new UpdateMessageCommand { Id = id, Request = request }));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<bool>> DeleteMessage(Guid id)
    {
        return Ok(await _mediator.Send(new DeleteMessageCommand { Id = id }));
    }

    [HttpPut("{id}/publish-status")]
    public async Task<ActionResult<bool>> ChangePublishStatus(Guid id, [FromQuery] bool isPublished)
    {
        return Ok(await _mediator.Send(new ChangeMessagePublishStatusCommand { Id = id, IsPublished = isPublished }));
    }

    // ===== 当前用户（铃铛 / 通知中心） =====

    [HttpGet("my")]
    public async Task<ActionResult<PaginationResponse<MyMessageDto>>> GetMyMessages(
        [FromQuery] MyMessageBox box = MyMessageBox.Notices,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool unreadOnly = false)
    {
        return Ok(await _mediator.Send(new GetMyMessagesQuery
        {
            Box = box,
            Page = page,
            PageSize = pageSize,
            UnreadOnly = unreadOnly
        }));
    }

    [HttpGet("my/unread-count")]
    public async Task<ActionResult<UnreadCountDto>> GetUnreadCount()
    {
        return Ok(await _mediator.Send(new GetUnreadCountQuery()));
    }

    [HttpPut("my/{id}/read")]
    public async Task<ActionResult<bool>> MarkRead(Guid id)
    {
        return Ok(await _mediator.Send(new MarkMessageReadCommand { MessageId = id }));
    }

    [HttpPut("my/read-all")]
    public async Task<ActionResult<bool>> MarkAllRead([FromQuery] MyMessageBox box = MyMessageBox.Notices)
    {
        return Ok(await _mediator.Send(new MarkAllMessagesReadCommand { Box = box }));
    }
}

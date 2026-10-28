using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallStores;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Common;

namespace QPS.Mall.Controllers.Mobile;

[ApiController]
[Route("api/mall")]
public sealed class MallStoreController(IMediator mediator) : ControllerBase
{
    [HttpGet("store/default")]
    [AllowAnonymous]
    public async Task<ActionResult<MallStoreDto>> GetDefaultStore(CancellationToken cancellationToken)
    {
        var stores = await mediator.Send(new GetMallStoresQuery
        {
            Page = 1,
            PageSize = 1,
            IsActive = true
        }, cancellationToken);

        return stores.List.Count == 0 ? NotFound() : Ok(stores.List[0]);
    }
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallStores;

/// <summary>
/// 启用或停用商场门店。
/// </summary>
public class ChangeMallStoreStatusCommand : IRequest<bool>
{
    public Guid Id { get; set; }

    public MallStoreStatusRequest Request { get; set; } = null!;
}

public class ChangeMallStoreStatusHandler : IRequestHandler<ChangeMallStoreStatusCommand, bool>
{
    private readonly IMallDbContext _dbContext;

    public ChangeMallStoreStatusHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(ChangeMallStoreStatusCommand request, CancellationToken cancellationToken)
    {
        var store = await _dbContext.MallStores
            .FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken);

        if (store is null)
        {
            throw new BusinessException(404, "门店不存在");
        }

        store.ChangeStatus(request.Request.IsActive);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}

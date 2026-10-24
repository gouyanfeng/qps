using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallStores;

/// <summary>
/// 删除商场门店（软删除）。
/// </summary>
public class DeleteMallStoreCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}

public class DeleteMallStoreHandler : IRequestHandler<DeleteMallStoreCommand, bool>
{
    private readonly IMallDbContext _dbContext;

    public DeleteMallStoreHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(DeleteMallStoreCommand request, CancellationToken cancellationToken)
    {
        var store = await _dbContext.MallStores
            .FirstOrDefaultAsync(item => item.Id == request.Id && !item.IsDeleted, cancellationToken);

        if (store is null)
        {
            throw new BusinessException(404, "门店不存在");
        }

        store.IsDeleted = true;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallStores;

/// <summary>
/// 编辑商城门店。
/// </summary>
public class UpdateMallStoreCommand : IRequest<bool>
{
    public Guid Id { get; set; }

    public MallStoreUpdateRequest Request { get; set; } = null!;
}

public class UpdateMallStoreHandler : IRequestHandler<UpdateMallStoreCommand, bool>
{
    private readonly IMallDbContext _dbContext;

    public UpdateMallStoreHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(UpdateMallStoreCommand request, CancellationToken cancellationToken)
    {
        var store = await _dbContext.MallStores
            .FirstOrDefaultAsync(item => item.Id == request.Id && !item.IsDeleted, cancellationToken);

        if (store is null)
        {
            throw new BusinessException(404, "门店不存在");
        }

        var code = MallStoreRules.NormalizeCode(request.Request.Code);
        var name = MallStoreRules.NormalizeName(request.Request.Name);
        var address = MallStoreRules.NormalizeAddress(request.Request.Address);

        await MallStoreRules.EnsureCodeNotDuplicatedAsync(_dbContext, code, request.Id, cancellationToken);

        store.Update(code, name, address, request.Request.DefaultFreightTemplateId);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
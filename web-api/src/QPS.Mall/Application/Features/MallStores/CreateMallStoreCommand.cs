using MediatR;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;

namespace QPS.Mall.Application.Features.MallStores;

/// <summary>
/// 新增商场门店。
/// </summary>
public class CreateMallStoreCommand : IRequest<bool>
{
    public MallStoreCreateRequest Request { get; set; } = null!;
}

public class CreateMallStoreHandler : IRequestHandler<CreateMallStoreCommand, bool>
{
    private readonly IMallDbContext _dbContext;

    public CreateMallStoreHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(CreateMallStoreCommand request, CancellationToken cancellationToken)
    {
        var code = MallStoreRules.NormalizeCode(request.Request.Code);
        var name = MallStoreRules.NormalizeName(request.Request.Name);
        var address = MallStoreRules.NormalizeAddress(request.Request.Address);

        await MallStoreRules.EnsureCodeNotDuplicatedAsync(_dbContext, code, null, cancellationToken);

        _dbContext.MallStores.Add(MallStore.Create(code, name, address, request.Request.IsActive));

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}

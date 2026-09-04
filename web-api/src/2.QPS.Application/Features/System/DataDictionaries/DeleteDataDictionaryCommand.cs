using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Interfaces;
using QPS.Application.Features.Crm;
using QPS.Domain.Exceptions;

namespace QPS.Application.Features.System.DataDictionaries;

public record DeleteDataDictionaryCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}

public class DeleteDataDictionaryCommandHandler : IRequestHandler<DeleteDataDictionaryCommand, bool>
{
    private readonly IDbContext _dbContext;

    public DeleteDataDictionaryCommandHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(DeleteDataDictionaryCommand request, CancellationToken cancellationToken)
    {
        var dataDictionary = await _dbContext.SystemDataDictionaries
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (dataDictionary == null)
        {
            throw new BusinessException(404, "Data dictionary does not exist.");
        }

        var rootId = await _dbContext.SystemDataDictionaries
            .Where(d => d.Code == CrmCodes.HerbProductDictionaryCode && !d.IsDeleted)
            .Select(d => (Guid?)d.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (dataDictionary.Code == CrmCodes.HerbProductDictionaryCode ||
            (rootId.HasValue && dataDictionary.ParentId == rootId))
        {
            throw new BusinessException(400, "中药材品类只允许停用，不允许删除。");
        }

        var hasChildren = await _dbContext.SystemDataDictionaries
            .AnyAsync(d => d.ParentId == request.Id, cancellationToken);

        if (hasChildren)
        {
            throw new BusinessException(400, "Cannot delete a data dictionary with child nodes.");
        }

        _dbContext.SystemDataDictionaries.Remove(dataDictionary);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}



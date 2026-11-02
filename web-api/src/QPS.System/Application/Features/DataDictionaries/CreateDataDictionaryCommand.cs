using QPS.System.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.System.Contracts.DTOs.DataDictionaries;
using QPS.System.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.System.Application.Features.DataDictionaries;

public record CreateDataDictionaryCommand : IRequest<bool>
{
    public DataDictionaryCreateRequest Request { get; set; }
}

public class CreateDataDictionaryCommandHandler : IRequestHandler<CreateDataDictionaryCommand, bool>
{
    private readonly ISystemDbContext _dbContext;

    public CreateDataDictionaryCommandHandler(ISystemDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(CreateDataDictionaryCommand request, CancellationToken cancellationToken)
    {
        if (request.Request.ParentId.HasValue)
        {
            var parent = await _dbContext.SystemDataDictionaries
                .FirstOrDefaultAsync(d => d.Id == request.Request.ParentId.Value, cancellationToken);

            if (parent == null)
            {
                throw new BusinessException(404, "Parent data dictionary does not exist.");
            }
        }

        var exists = await _dbContext.SystemDataDictionaries
            .AnyAsync(d => d.Code == request.Request.Code, cancellationToken);

        if (exists)
        {
            throw new BusinessException(400, "Code already exists.");
        }

        var dataDictionary = new SystemDataDictionary(
            Guid.NewGuid(),
            request.Request.Code,
            request.Request.Name,
            request.Request.Value,
            request.Request.Description,
            request.Request.SortOrder,
            request.Request.IsActive,
            request.Request.ParentId);

        await _dbContext.SystemDataDictionaries.AddAsync(dataDictionary, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}



using QPS.System.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.System.Contracts.DTOs.DataDictionaries;
using QPS.System.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.System.Application.Features.DataDictionaries;

public record UpdateDataDictionaryCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public DataDictionaryUpdateRequest Request { get; set; }
}

public class UpdateDataDictionaryCommandHandler : IRequestHandler<UpdateDataDictionaryCommand, bool>
{
    private readonly ISystemDbContext _dbContext;

    public UpdateDataDictionaryCommandHandler(ISystemDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(UpdateDataDictionaryCommand request, CancellationToken cancellationToken)
    {
        var dataDictionary = await _dbContext.SystemDataDictionaries
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (dataDictionary == null)
        {
            throw new BusinessException(404, "Data dictionary does not exist.");
        }

        if (request.Request.ParentId.HasValue)
        {
            var parentExists = await _dbContext.SystemDataDictionaries
                .AnyAsync(d => d.Id == request.Request.ParentId.Value, cancellationToken);

            if (!parentExists)
            {
                throw new BusinessException(404, "Parent data dictionary does not exist.");
            }
        }

        dataDictionary.Update(
            request.Request.Name,
            request.Request.Value,
            request.Request.Description,
            request.Request.SortOrder,
            request.Request.IsActive,
            request.Request.ParentId);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}



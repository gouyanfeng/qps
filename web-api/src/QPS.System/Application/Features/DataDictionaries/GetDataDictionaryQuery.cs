using QPS.System.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.System.Contracts.DTOs.DataDictionaries;
using QPS.Shared.Contracts;
using QPS.System.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.System.Application.Features.DataDictionaries;

public record GetDataDictionaryQuery : IRequest<DataDictionaryDto>
{
    public Guid Id { get; set; }
}

public class GetDataDictionaryQueryHandler : IRequestHandler<GetDataDictionaryQuery, DataDictionaryDto>
{
    private readonly ISystemDbContext _dbContext;

    public GetDataDictionaryQueryHandler(ISystemDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DataDictionaryDto> Handle(GetDataDictionaryQuery request, CancellationToken cancellationToken)
    {
        var dataDictionary = await _dbContext.SystemDataDictionaries
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (dataDictionary == null)
        {
            throw new BusinessException(404, "Data dictionary does not exist.");
        }

        return ToDto(dataDictionary);
    }

    /// <summary>
    /// 转换为 DTO。
    /// </summary>
    private static DataDictionaryDto ToDto(SystemDataDictionary dataDictionary)
    {
        return new DataDictionaryDto
        {
            Id = dataDictionary.Id,
            ParentId = dataDictionary.ParentId,
            Code = dataDictionary.Code,
            Name = dataDictionary.Name,
            Value = dataDictionary.Value,
            Description = dataDictionary.Description,
            SortOrder = dataDictionary.SortOrder,
            IsActive = dataDictionary.IsActive
        };
    }
}



using QPS.System.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.System.Contracts.DTOs.DataDictionaries;
using QPS.Shared.Contracts;
using QPS.System.Domain.Entities;

namespace QPS.System.Application.Features.DataDictionaries;

public record GetDataDictionaryTreeQuery : IRequest<List<DataDictionaryDto>>
{
}

public class GetDataDictionaryTreeQueryHandler : IRequestHandler<GetDataDictionaryTreeQuery, List<DataDictionaryDto>>
{
    private readonly ISystemDbContext _dbContext;

    public GetDataDictionaryTreeQueryHandler(ISystemDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<DataDictionaryDto>> Handle(GetDataDictionaryTreeQuery request, CancellationToken cancellationToken)
    {
        var allDictionaries = await _dbContext.SystemDataDictionaries
            .Where(d => d.IsActive)
            .OrderBy(d => d.SortOrder)
            .ToListAsync(cancellationToken);

        var rootNodes = allDictionaries.Where(d => !d.ParentId.HasValue).ToList();

        return BuildTree(rootNodes, allDictionaries);
    }

    private static List<DataDictionaryDto> BuildTree(
        List<SystemDataDictionary> parents,
        List<SystemDataDictionary> allNodes)
    {
        var result = new List<DataDictionaryDto>();

        foreach (var parent in parents)
        {
            var children = allNodes.Where(n => n.ParentId == parent.Id).ToList();

            var dto = new DataDictionaryDto
            {
                Id = parent.Id,
                ParentId = parent.ParentId,
                Code = parent.Code,
                Name = parent.Name,
                Value = parent.Value,
                Description = parent.Description,
                SortOrder = parent.SortOrder,
                IsActive = parent.IsActive,
                Children = BuildTree(children, allNodes)
            };

            result.Add(dto);
        }

        return result;
    }
}



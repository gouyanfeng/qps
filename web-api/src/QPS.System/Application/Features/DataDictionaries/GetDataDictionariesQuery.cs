using QPS.Shared.Common;
using QPS.System.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.System.Contracts.DTOs.DataDictionaries;
using QPS.Shared.Extensions;
using QPS.Shared.Contracts;
using QPS.System.Domain.Entities;

namespace QPS.System.Application.Features.DataDictionaries;

public class GetDataDictionariesQuery : PaginationRequest, IRequest<PaginationResponse<DataDictionaryDto>>
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public bool? IsActive { get; set; }
    public Guid? ParentId { get; set; }
}

public class GetDataDictionariesQueryHandler : IRequestHandler<GetDataDictionariesQuery, PaginationResponse<DataDictionaryDto>>
{
    private readonly ISystemDbContext _dbContext;

    public GetDataDictionariesQueryHandler(ISystemDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginationResponse<DataDictionaryDto>> Handle(GetDataDictionariesQuery request, CancellationToken cancellationToken)
    {
        if (request.ParentId.HasValue)
        {
            return await HandleWithTreeFilter(request, cancellationToken);
        }

        var query = _dbContext.SystemDataDictionaries.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            var code = request.Code.Trim();
            query = query.Where(d => d.Code.Contains(code));
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var name = request.Name.Trim();
            query = query.Where(d => d.Name.Contains(name));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(d => d.IsActive == request.IsActive.Value);
        }

        var paged = await query
            .OrderBy(d => d.SortOrder)
            .ToPaginationResponseAsync(request, cancellationToken);

        var parentIds = paged.List
            .Where(d => d.ParentId.HasValue)
            .Select(d => d.ParentId!.Value)
            .Distinct()
            .ToList();

        var parentNames = parentIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _dbContext.SystemDataDictionaries
                .AsNoTracking()
                .Where(d => parentIds.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id, d => d.Name, cancellationToken);

        var data = paged.List.Select(d => new DataDictionaryDto
        {
            Id = d.Id,
            ParentId = d.ParentId,
            ParentName = d.ParentId.HasValue && parentNames.TryGetValue(d.ParentId.Value, out var pn) ? pn : null,
            Code = d.Code,
            Name = d.Name,
            Value = d.Value,
            Description = d.Description,
            SortOrder = d.SortOrder,
            IsActive = d.IsActive
        }).ToList();

        return new PaginationResponse<DataDictionaryDto>(data, paged.TotalCount, paged.CurrentPage, paged.PageSize);
    }

    private async Task<PaginationResponse<DataDictionaryDto>> HandleWithTreeFilter(
        GetDataDictionariesQuery request, CancellationToken cancellationToken)
    {
        var allDictionaries = await _dbContext.SystemDataDictionaries
            .AsNoTracking()
            .OrderBy(d => d.SortOrder)
            .ToListAsync(cancellationToken);

        IEnumerable<SystemDataDictionary> filtered = allDictionaries;

        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            var code = request.Code.Trim();
            filtered = filtered.Where(d => d.Code.Contains(code));
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var name = request.Name.Trim();
            filtered = filtered.Where(d => d.Name.Contains(name));
        }

        if (request.IsActive.HasValue)
        {
            var isActive = request.IsActive.Value;
            filtered = filtered.Where(d => d.IsActive == isActive);
        }

        var parentId = request.ParentId!.Value;
        filtered = filtered.Where(d => IsDescendantOf(d.Id, parentId, allDictionaries));

        return filtered
            .Select(d => new DataDictionaryDto
            {
                Id = d.Id,
                ParentId = d.ParentId,
                ParentName = d.ParentId.HasValue
                    ? GetParentName(allDictionaries, d.ParentId.Value) : null,
                Code = d.Code,
                Name = d.Name,
                Value = d.Value,
                Description = d.Description,
                SortOrder = d.SortOrder,
                IsActive = d.IsActive
            })
            .ToPaginationResponse(request);
    }

    private static bool IsDescendantOf(Guid nodeId, Guid parentId, IReadOnlyList<SystemDataDictionary> dictionaries)
    {
        var dictionaryById = dictionaries.ToDictionary(d => d.Id);
        var current = dictionaryById.GetValueOrDefault(nodeId);

        while (current != null)
        {
            if (current.ParentId == parentId) return true;
            if (!current.ParentId.HasValue) return false;
            current = dictionaryById.GetValueOrDefault(current.ParentId.Value);
        }

        return false;
    }

    private static string? GetParentName(IReadOnlyList<SystemDataDictionary> dictionaries, Guid parentId)
    {
        return dictionaries.FirstOrDefault(x => x.Id == parentId)?.Name;
    }
}
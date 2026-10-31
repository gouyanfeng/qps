using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;

namespace QPS.Mall.Application.Features.MallCategories;

public class GetMallCategoryTreeQuery : IRequest<List<MallCategoryTreeNodeDto>>
{
}

public class MallCategoryTreeNodeDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
    public List<MallCategoryTreeNodeDto> Children { get; set; } = new();
}

public class GetMallCategoryTreeHandler : IRequestHandler<GetMallCategoryTreeQuery, List<MallCategoryTreeNodeDto>>
{
    private readonly IMallDbContext _db;
    public GetMallCategoryTreeHandler(IMallDbContext db) => _db = db;

    public async Task<List<MallCategoryTreeNodeDto>> Handle(GetMallCategoryTreeQuery req, CancellationToken ct)
    {
        var list = await _db.MallCategories
            .AsNoTracking()
            .Where(x => x.IsActive && !x.IsDeleted)
            .OrderBy(x => x.ParentId)
            .ThenBy(x => x.SortOrder)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Name,
                x.Icon,
                x.SortOrder,
                x.ParentId
            })
            .ToListAsync(ct);

        var categoryIds = list.Select(category => category.Id).ToHashSet();
        var lookup = list.ToLookup(x =>
            x.ParentId.HasValue && categoryIds.Contains(x.ParentId.Value)
                ? x.ParentId.Value
                : Guid.Empty);

        List<MallCategoryTreeNodeDto> Build(Guid parentId)
        {
            return lookup[parentId].Select(x => new MallCategoryTreeNodeDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Icon = x.Icon,
                SortOrder = x.SortOrder,
                Children = Build(x.Id)
            }).ToList();
        }

        return Build(Guid.Empty);
    }
}
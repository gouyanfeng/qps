using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Common;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallTags;

public class GetMallTagsQuery : IRequest<List<MallTagDto>>
{
    public string? Type { get; set; }
    public bool? IsActive { get; set; }
}

public class CreateMallTagCommand : IRequest<bool>
{
    public MallTagRequest Request { get; set; } = null!;
}

public class UpdateMallTagCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public MallTagRequest Request { get; set; } = null!;
}

public class ChangeMallTagStatusCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public bool IsActive { get; set; }
}

public class DeleteMallTagCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}

public class MallTagHandlers :
    IRequestHandler<GetMallTagsQuery, List<MallTagDto>>,
    IRequestHandler<CreateMallTagCommand, bool>,
    IRequestHandler<UpdateMallTagCommand, bool>,
    IRequestHandler<ChangeMallTagStatusCommand, bool>,
    IRequestHandler<DeleteMallTagCommand, bool>
{
    private readonly IMallDbContext _dbContext;

    public MallTagHandlers(IMallDbContext dbContext) => _dbContext = dbContext;

    public async Task<List<MallTagDto>> Handle(GetMallTagsQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.MallTags.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            var type = request.Type.Trim();
            query = query.Where(tag => tag.Type == type);
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(tag => tag.IsActive == request.IsActive.Value);
        }

        return await query
            .OrderBy(tag => tag.SortOrder)
            .ThenBy(tag => tag.Name)
            .Select(tag => new MallTagDto
            {
                Id = tag.Id,
                Type = tag.Type,
                Code = tag.Code,
                Name = tag.Name,
                Color = tag.Color,
                SortOrder = tag.SortOrder,
                IsActive = tag.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> Handle(CreateMallTagCommand command, CancellationToken cancellationToken)
    {
        var normalized = Normalize(command.Request);
        await EnsureUniqueAsync(normalized.Type, normalized.Code, normalized.Name, null, cancellationToken);
        _dbContext.MallTags.Add(MallTag.Create(
            normalized.Code,
            normalized.Name,
            normalized.Color,
            normalized.SortOrder,
            command.Request.IsActive,
            normalized.Type));
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> Handle(UpdateMallTagCommand command, CancellationToken cancellationToken)
    {
        var tag = await _dbContext.MallTags.FirstOrDefaultAsync(
            item => item.Id == command.Id,
            cancellationToken);
        if (tag is null) throw new BusinessException(404, "标签不存在");

        var normalized = Normalize(command.Request);
        await EnsureUniqueAsync(tag.Type, normalized.Code, normalized.Name, command.Id, cancellationToken);
        tag.Update(normalized.Code, normalized.Name, normalized.Color, normalized.SortOrder);
        tag.ChangeStatus(command.Request.IsActive);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> Handle(ChangeMallTagStatusCommand command, CancellationToken cancellationToken)
    {
        var tag = await _dbContext.MallTags.FirstOrDefaultAsync(
            item => item.Id == command.Id,
            cancellationToken);
        if (tag is null) throw new BusinessException(404, "标签不存在");
        tag.ChangeStatus(command.IsActive);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> Handle(DeleteMallTagCommand command, CancellationToken cancellationToken)
    {
        var tag = await _dbContext.MallTags.FirstOrDefaultAsync(
            item => item.Id == command.Id, cancellationToken);
        if (tag is null) throw new BusinessException(404, "标签不存在");

        var hasLinkedProducts = await _dbContext.MallProductTags.AnyAsync(
            link => link.TagId == tag.Id,
            cancellationToken);
        if (hasLinkedProducts)
        {
            throw new BusinessException(400, "该标签仍关联商品，无法删除，请先移除商品上的标签");
        }

        tag.SoftDelete();
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task EnsureUniqueAsync(
        string type,
        string code,
        string name,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var duplicated = await _dbContext.MallTags.AnyAsync(
            tag =>
                   tag.Type == type &&
                   (tag.Code == code || tag.Name == name) &&
                   (!excludeId.HasValue || tag.Id != excludeId.Value),
            cancellationToken);
        if (duplicated) throw new BusinessException(400, "同类型下标签编码或名称已存在");
    }

    private static (string Type, string Code, string Name, string Color, int SortOrder) Normalize(
        MallTagRequest request)
    {
        var type = string.IsNullOrWhiteSpace(request.Type) ? MallTagType.Product : request.Type.Trim();
        var code = (request.Code ?? string.Empty).Trim();
        var name = (request.Name ?? string.Empty).Trim();
        var color = (request.Color ?? string.Empty).Trim();
        if (type.Length > 20)
            throw new BusinessException(400, "标签类型不能超过 20 个字符");
        if (string.IsNullOrWhiteSpace(code) || code.Length > 50)
            throw new BusinessException(400, "标签编码不能为空且不能超过 50 个字符");
        if (string.IsNullOrWhiteSpace(name) || name.Length > 50)
            throw new BusinessException(400, "标签名称不能为空且不能超过 50 个字符");
        if (color.Length > 20) throw new BusinessException(400, "标签颜色不能超过 20 个字符");
        if (request.SortOrder is < 0 or > 9999)
            throw new BusinessException(400, "标签排序值必须在 0 到 9999 之间");
        return (type, code, name, string.IsNullOrWhiteSpace(color) ? "success" : color, request.SortOrder);
    }
}

/// <summary>
/// 后台分页查询标签，支持类型、关键字与启用状态筛选。
/// </summary>
public class GetMallTagsPageQuery : PaginationRequest, IRequest<PaginationResponse<MallTagDto>>
{
    public string? Type { get; set; }

    public string? Keyword { get; set; }

    public bool? IsActive { get; set; }
}

public class GetMallTagsPageHandler
    : IRequestHandler<GetMallTagsPageQuery, PaginationResponse<MallTagDto>>
{
    private readonly IMallDbContext _dbContext;

    public GetMallTagsPageHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginationResponse<MallTagDto>> Handle(
        GetMallTagsPageQuery request,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.MallTags
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            var type = request.Type.Trim();
            query = query.Where(tag => tag.Type == type);
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(tag => tag.IsActive == request.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(tag => tag.Code.Contains(keyword) || tag.Name.Contains(keyword));
        }

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 10 : request.PageSize;

        var totalCount = await query.CountAsync(cancellationToken);
        var list = await query
            .OrderBy(tag => tag.SortOrder)
            .ThenBy(tag => tag.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(tag => new MallTagDto
            {
                Id = tag.Id,
                Type = tag.Type,
                Code = tag.Code,
                Name = tag.Name,
                Color = tag.Color,
                SortOrder = tag.SortOrder,
                IsActive = tag.IsActive
            })
            .ToListAsync(cancellationToken);

        return new PaginationResponse<MallTagDto>(list, totalCount, page, pageSize);
    }
}
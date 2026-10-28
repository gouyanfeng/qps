using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallTags;

public class GetMallTagsQuery : IRequest<List<MallTagDto>>
{
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
        var query = _dbContext.MallTags.AsNoTracking().Where(tag => !tag.IsDeleted);
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
        await EnsureUniqueAsync(normalized.Code, normalized.Name, null, cancellationToken);
        _dbContext.MallTags.Add(MallTag.Create(
            normalized.Code,
            normalized.Name,
            normalized.Color,
            normalized.SortOrder,
            command.Request.IsActive));
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> Handle(UpdateMallTagCommand command, CancellationToken cancellationToken)
    {
        var tag = await _dbContext.MallTags.FirstOrDefaultAsync(
            item => item.Id == command.Id && !item.IsDeleted,
            cancellationToken);
        if (tag is null) throw new BusinessException(404, "商品标签不存在");

        var normalized = Normalize(command.Request);
        await EnsureUniqueAsync(normalized.Code, normalized.Name, command.Id, cancellationToken);
        tag.Update(normalized.Code, normalized.Name, normalized.Color, normalized.SortOrder);
        tag.ChangeStatus(command.Request.IsActive);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> Handle(ChangeMallTagStatusCommand command, CancellationToken cancellationToken)
    {
        var tag = await _dbContext.MallTags.FirstOrDefaultAsync(
            item => item.Id == command.Id && !item.IsDeleted,
            cancellationToken);
        if (tag is null) throw new BusinessException(404, "商品标签不存在");
        tag.ChangeStatus(command.IsActive);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> Handle(DeleteMallTagCommand command, CancellationToken cancellationToken)
    {
        var tag = await _dbContext.MallTags
            .Include(item => item.Products)
            .FirstOrDefaultAsync(item => item.Id == command.Id && !item.IsDeleted, cancellationToken);
        if (tag is null) throw new BusinessException(404, "商品标签不存在");
        if (tag.Products.Any(product => !product.IsDeleted))
        {
            throw new BusinessException(400, "该标签仍关联商品，无法删除，请先移除商品上的标签");
        }
        tag.IsDeleted = true;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task EnsureUniqueAsync(
        string code,
        string name,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var duplicated = await _dbContext.MallTags.AnyAsync(
            tag => !tag.IsDeleted &&
                   (tag.Code == code || tag.Name == name) &&
                   (!excludeId.HasValue || tag.Id != excludeId.Value),
            cancellationToken);
        if (duplicated) throw new BusinessException(400, "标签编码或名称已存在");
    }

    private static (string Code, string Name, string Color, int SortOrder) Normalize(MallTagRequest request)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        var name = request.Name.Trim();
        var color = request.Color.Trim();
        if (string.IsNullOrWhiteSpace(code) || code.Length > 50)
            throw new BusinessException(400, "标签编码不能为空且不能超过 50 个字符");
        if (string.IsNullOrWhiteSpace(name) || name.Length > 50)
            throw new BusinessException(400, "标签名称不能为空且不能超过 50 个字符");
        if (color.Length > 20) throw new BusinessException(400, "标签颜色不能超过 20 个字符");
        if (request.SortOrder is < 0 or > 9999)
            throw new BusinessException(400, "标签排序值必须在 0 到 9999 之间");
        return (code, name, string.IsNullOrWhiteSpace(color) ? "success" : color, request.SortOrder);
    }
}

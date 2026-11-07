using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallPages;

public class GetMallPagesQuery : IRequest<List<MallPageDto>>
{
}

public class GetMallPageQuery : IRequest<MallPageDto>
{
    public Guid Id { get; set; }
}

public class GetMallPageByCodeQuery : IRequest<MallPageDto>
{
    public string PageCode { get; set; } = string.Empty;
}

public class CreateMallPageCommand : IRequest<MallPageDto>
{
    public MallPageCreateRequest Request { get; set; } = null!;
}

public class UpdateMallPageCommand : IRequest<MallPageDto>
{
    public Guid Id { get; set; }
    public MallPageUpdateRequest Request { get; set; } = null!;
}

public class DeleteMallPageCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}

public class ChangeMallPageStatusCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public bool IsActive { get; set; }
}

public class MallPageHandlers :
    IRequestHandler<GetMallPagesQuery, List<MallPageDto>>,
    IRequestHandler<GetMallPageQuery, MallPageDto>,
    IRequestHandler<GetMallPageByCodeQuery, MallPageDto>,
    IRequestHandler<CreateMallPageCommand, MallPageDto>,
    IRequestHandler<UpdateMallPageCommand, MallPageDto>,
    IRequestHandler<DeleteMallPageCommand, bool>,
    IRequestHandler<ChangeMallPageStatusCommand, bool>
{
    private readonly IMallDbContext _dbContext;

    public MallPageHandlers(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<MallPageDto>> Handle(GetMallPagesQuery query, CancellationToken ct)
    {
        return await _dbContext.MallPages.AsNoTracking()
            
            .OrderBy(p => p.PageCode)
            .Select(p => Map(p)).ToListAsync(ct);
    }

    public async Task<MallPageDto> Handle(GetMallPageQuery query, CancellationToken ct)
    {
        var p = await _dbContext.MallPages.AsNoTracking().FirstOrDefaultAsync(x => x.Id == query.Id, ct);
        if (p is null) throw new BusinessException(404, "页面不存在");

        return Map(p);
    }

    public async Task<MallPageDto> Handle(GetMallPageByCodeQuery query, CancellationToken ct)
    {
        var p = await _dbContext.MallPages.AsNoTracking()
            .FirstOrDefaultAsync(x => x.PageCode == query.PageCode && x.IsActive, ct);
        if (p is null) throw new BusinessException(404, "页面不存在");

        return Map(p);
    }

    public async Task<MallPageDto> Handle(CreateMallPageCommand command, CancellationToken ct)
    {
        var r = command.Request;
        var page = MallPage.Create(r.PageCode, r.PageName, r.Title, r.ContentHtml,
            r.SeoTitle, r.SeoKeywords, r.SeoDescription, r.IsActive);
        _dbContext.MallPages.Add(page);
        await _dbContext.SaveChangesAsync(ct);

        return Map(page);
    }

    public async Task<MallPageDto> Handle(UpdateMallPageCommand command, CancellationToken ct)
    {
        var page = await _dbContext.MallPages.FirstOrDefaultAsync(p => p.Id == command.Id, ct);
        if (page is null) throw new BusinessException(404, "页面不存在");

        var r = command.Request;
        page.Update(r.PageName, r.Title, r.ContentHtml, r.SeoTitle, r.SeoKeywords, r.SeoDescription);
        await _dbContext.SaveChangesAsync(ct);

        return Map(page);
    }

    public async Task<bool> Handle(DeleteMallPageCommand command, CancellationToken ct)
    {
        var page = await _dbContext.MallPages.FirstOrDefaultAsync(p => p.Id == command.Id, ct);
        if (page is null) throw new BusinessException(404, "页面不存在");

        _dbContext.MallPages.Remove(page);
        await _dbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> Handle(ChangeMallPageStatusCommand command, CancellationToken ct)
    {
        var page = await _dbContext.MallPages.FirstOrDefaultAsync(p => p.Id == command.Id, ct);
        if (page is null) throw new BusinessException(404, "页面不存在");

        page.ChangeStatus(command.IsActive);
        await _dbContext.SaveChangesAsync(ct);
        return true;
    }

    private static MallPageDto Map(MallPage p) => new()
    {
        Id = p.Id, PageCode = p.PageCode, PageName = p.PageName,
        Title = p.Title, ContentHtml = p.ContentHtml, SeoTitle = p.SeoTitle,
        SeoKeywords = p.SeoKeywords, SeoDescription = p.SeoDescription,
        IsActive = p.IsActive, CreatedAt = p.CreatedAt
    };
}
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallArticles;

public class GetMallArticlesQuery : IRequest<List<MallArticleListItemDto>>
{
    public Guid StoreId { get; set; }
    public MallArticleType? ArticleType { get; set; }
    public string? Keyword { get; set; }
    public bool? IsActive { get; set; }
}

public class GetMallArticleQuery : IRequest<MallArticleDto>
{
    public Guid Id { get; set; }
}

public class GetPublishedMallArticlesQuery : IRequest<List<MallArticleListItemDto>>
{
    public Guid StoreId { get; set; }
    public MallArticleType? ArticleType { get; set; }
}

public class GetPublishedMallArticleDetailQuery : IRequest<MallArticleDto>
{
    public Guid Id { get; set; }
}

public class CreateMallArticleCommand : IRequest<MallArticleDto>
{
    public MallArticleCreateRequest Request { get; set; } = null!;
}

public class UpdateMallArticleCommand : IRequest<MallArticleDto>
{
    public Guid Id { get; set; }
    public MallArticleUpdateRequest Request { get; set; } = null!;
}

public class DeleteMallArticleCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}

public class PublishMallArticleCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}

public class UnpublishMallArticleCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}

public class SetMallArticleTopCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public bool IsTop { get; set; }
}

public class MallArticleHandlers :
    IRequestHandler<GetMallArticlesQuery, List<MallArticleListItemDto>>,
    IRequestHandler<GetMallArticleQuery, MallArticleDto>,
    IRequestHandler<GetPublishedMallArticlesQuery, List<MallArticleListItemDto>>,
    IRequestHandler<GetPublishedMallArticleDetailQuery, MallArticleDto>,
    IRequestHandler<CreateMallArticleCommand, MallArticleDto>,
    IRequestHandler<UpdateMallArticleCommand, MallArticleDto>,
    IRequestHandler<DeleteMallArticleCommand, bool>,
    IRequestHandler<PublishMallArticleCommand, bool>,
    IRequestHandler<UnpublishMallArticleCommand, bool>,
    IRequestHandler<SetMallArticleTopCommand, bool>
{
    private readonly IMallDbContext _dbContext;

    public MallArticleHandlers(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<MallArticleListItemDto>> Handle(GetMallArticlesQuery query, CancellationToken ct)
    {
        var q = _dbContext.MallArticles.AsNoTracking().Where(a => a.StoreId == query.StoreId && !a.IsDeleted);
        if (query.ArticleType.HasValue) q = q.Where(a => a.ArticleType == query.ArticleType.Value);
        if (!string.IsNullOrEmpty(query.Keyword)) q = q.Where(a => a.Title.Contains(query.Keyword!) || a.Summary.Contains(query.Keyword!));
        if (query.IsActive.HasValue) q = q.Where(a => a.IsActive == query.IsActive.Value);

        return await q.OrderByDescending(a => a.IsTop).ThenBy(a => a.SortOrder).ThenByDescending(a => a.CreatedAt)
            .Select(a => new MallArticleListItemDto
            {
                Id = a.Id, Title = a.Title, Summary = a.Summary, ArticleType = a.ArticleType,
                Author = a.Author, ViewCount = a.ViewCount, IsTop = a.IsTop,
                PublishedAt = a.PublishedAt, IsActive = a.IsActive, CreatedAt = a.CreatedAt
            }).ToListAsync(ct);
    }

    public async Task<MallArticleDto> Handle(GetMallArticleQuery query, CancellationToken ct)
    {
        var a = await _dbContext.MallArticles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == query.Id && !x.IsDeleted, ct);
        if (a is null) throw new BusinessException(404, "文章不存在");

        return Map(a);
    }

    public async Task<List<MallArticleListItemDto>> Handle(GetPublishedMallArticlesQuery query, CancellationToken ct)
    {
        var q = _dbContext.MallArticles.AsNoTracking()
            .Where(a => a.StoreId == query.StoreId && !a.IsDeleted && a.IsActive && a.PublishedAt != null);
        if (query.ArticleType.HasValue) q = q.Where(a => a.ArticleType == query.ArticleType.Value);

        return await q.OrderByDescending(a => a.IsTop).ThenBy(a => a.SortOrder).ThenByDescending(a => a.PublishedAt)
            .Select(a => new MallArticleListItemDto
            {
                Id = a.Id, Title = a.Title, Summary = a.Summary, ArticleType = a.ArticleType,
                Author = a.Author, ViewCount = a.ViewCount, IsTop = a.IsTop,
                PublishedAt = a.PublishedAt, IsActive = a.IsActive, CreatedAt = a.CreatedAt
            }).ToListAsync(ct);
    }

    public async Task<MallArticleDto> Handle(GetPublishedMallArticleDetailQuery query, CancellationToken ct)
    {
        var a = await _dbContext.MallArticles.FirstOrDefaultAsync(x => x.Id == query.Id && !x.IsDeleted && x.IsActive && x.PublishedAt != null, ct);
        if (a is null) throw new BusinessException(404, "文章不存在或未发布");

        a.IncrementViewCount();
        await _dbContext.SaveChangesAsync(ct);

        return Map(a);
    }

    public async Task<MallArticleDto> Handle(CreateMallArticleCommand command, CancellationToken ct)
    {
        var r = command.Request;
        var article = MallArticle.Create(r.StoreId, r.Title, r.Summary, r.ContentHtml, r.ArticleType,
            r.Author, r.SeoTitle, r.SeoKeywords, r.SeoDescription, r.IsTop, r.SortOrder, r.IsActive);
        _dbContext.MallArticles.Add(article);
        await _dbContext.SaveChangesAsync(ct);

        return Map(article);
    }

    public async Task<MallArticleDto> Handle(UpdateMallArticleCommand command, CancellationToken ct)
    {
        var article = await _dbContext.MallArticles.FirstOrDefaultAsync(a => a.Id == command.Id && !a.IsDeleted, ct);
        if (article is null) throw new BusinessException(404, "文章不存在");

        var r = command.Request;
        article.Update(r.Title, r.Summary, r.ContentHtml, r.ArticleType, r.Author,
            r.SeoTitle, r.SeoKeywords, r.SeoDescription, r.SortOrder);
        await _dbContext.SaveChangesAsync(ct);

        return Map(article);
    }

    public async Task<bool> Handle(DeleteMallArticleCommand command, CancellationToken ct)
    {
        var article = await _dbContext.MallArticles.FirstOrDefaultAsync(a => a.Id == command.Id && !a.IsDeleted, ct);
        if (article is null) throw new BusinessException(404, "文章不存在");

        _dbContext.MallArticles.Remove(article);
        await _dbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> Handle(PublishMallArticleCommand command, CancellationToken ct)
    {
        var article = await _dbContext.MallArticles.FirstOrDefaultAsync(a => a.Id == command.Id && !a.IsDeleted, ct);
        if (article is null) throw new BusinessException(404, "文章不存在");

        article.Publish();
        await _dbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> Handle(UnpublishMallArticleCommand command, CancellationToken ct)
    {
        var article = await _dbContext.MallArticles.FirstOrDefaultAsync(a => a.Id == command.Id && !a.IsDeleted, ct);
        if (article is null) throw new BusinessException(404, "文章不存在");

        article.Unpublish();
        await _dbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> Handle(SetMallArticleTopCommand command, CancellationToken ct)
    {
        var article = await _dbContext.MallArticles.FirstOrDefaultAsync(a => a.Id == command.Id && !a.IsDeleted, ct);
        if (article is null) throw new BusinessException(404, "文章不存在");

        article.SetTop(command.IsTop);
        await _dbContext.SaveChangesAsync(ct);
        return true;
    }

    private static MallArticleDto Map(MallArticle a) => new()
    {
        Id = a.Id, StoreId = a.StoreId, Title = a.Title, Summary = a.Summary,
        ContentHtml = a.ContentHtml, ArticleType = a.ArticleType, Author = a.Author,
        ViewCount = a.ViewCount, SeoTitle = a.SeoTitle, SeoKeywords = a.SeoKeywords,
        SeoDescription = a.SeoDescription, IsTop = a.IsTop, SortOrder = a.SortOrder,
        PublishedAt = a.PublishedAt, IsActive = a.IsActive, CreatedAt = a.CreatedAt
    };
}
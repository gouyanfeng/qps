using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallBanners;

public class GetMallBannersQuery : IRequest<List<MallBannerDto>>
{
    public string? Position { get; set; }
    public bool? IsActive { get; set; }
}

public class GetMallBannerQuery : IRequest<MallBannerDto>
{
    public Guid Id { get; set; }
}

public class GetActiveMallBannersQuery : IRequest<List<MallBannerDto>>
{
    public string? Position { get; set; }
}

public class CreateMallBannerCommand : IRequest<MallBannerDto>
{
    public MallBannerCreateRequest Request { get; set; } = null!;
}

public class UpdateMallBannerCommand : IRequest<MallBannerDto>
{
    public Guid Id { get; set; }
    public MallBannerUpdateRequest Request { get; set; } = null!;
}

public class DeleteMallBannerCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}

public class ChangeMallBannerStatusCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public bool IsActive { get; set; }
}

public class BatchSortMallBannerCommand : IRequest<bool>
{
    public MallBannerBatchSortRequest Request { get; set; } = null!;
}

public class MallBannerHandlers :
    IRequestHandler<GetMallBannersQuery, List<MallBannerDto>>,
    IRequestHandler<GetMallBannerQuery, MallBannerDto>,
    IRequestHandler<GetActiveMallBannersQuery, List<MallBannerDto>>,
    IRequestHandler<CreateMallBannerCommand, MallBannerDto>,
    IRequestHandler<UpdateMallBannerCommand, MallBannerDto>,
    IRequestHandler<DeleteMallBannerCommand, bool>,
    IRequestHandler<ChangeMallBannerStatusCommand, bool>,
    IRequestHandler<BatchSortMallBannerCommand, bool>
{
    private readonly IMallDbContext _dbContext;

    public MallBannerHandlers(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<MallBannerDto>> Handle(GetMallBannersQuery query, CancellationToken ct)
    {
        var q = _dbContext.MallBanners.AsNoTracking().Where(b => !b.IsDeleted);
        if (!string.IsNullOrEmpty(query.Position)) q = q.Where(b => b.Position == query.Position);
        if (query.IsActive.HasValue) q = q.Where(b => b.IsActive == query.IsActive.Value);

        return await q.OrderBy(b => b.SortOrder).ThenByDescending(b => b.CreatedAt)
            .Select(b => new MallBannerDto
            {
                Id = b.Id, Position = b.Position, ImageUrl = b.ImageUrl,
                LinkType = b.LinkType, LinkValue = b.LinkValue, Title = b.Title,
                SortOrder = b.SortOrder, StartDate = b.StartDate, EndDate = b.EndDate,
                IsActive = b.IsActive, CreatedAt = b.CreatedAt
            }).ToListAsync(ct);
    }

    public async Task<MallBannerDto> Handle(GetMallBannerQuery query, CancellationToken ct)
    {
        var b = await _dbContext.MallBanners.AsNoTracking().FirstOrDefaultAsync(x => x.Id == query.Id && !x.IsDeleted, ct);
        if (b is null) throw new BusinessException(404, "Banner不存在");

        return new MallBannerDto
        {
            Id = b.Id, Position = b.Position, ImageUrl = b.ImageUrl,
            LinkType = b.LinkType, LinkValue = b.LinkValue, Title = b.Title,
            SortOrder = b.SortOrder, StartDate = b.StartDate, EndDate = b.EndDate,
            IsActive = b.IsActive, CreatedAt = b.CreatedAt
        };
    }

    public async Task<List<MallBannerDto>> Handle(GetActiveMallBannersQuery query, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var q = _dbContext.MallBanners.AsNoTracking()
            .Where(b => !b.IsDeleted && b.IsActive
                && (b.StartDate == null || b.StartDate <= now)
                && (b.EndDate == null || b.EndDate >= now));
        if (!string.IsNullOrEmpty(query.Position)) q = q.Where(b => b.Position == query.Position);

        return await q.OrderBy(b => b.SortOrder)
            .Select(b => new MallBannerDto
            {
                Id = b.Id, Position = b.Position, ImageUrl = b.ImageUrl,
                LinkType = b.LinkType, LinkValue = b.LinkValue, Title = b.Title,
                SortOrder = b.SortOrder, StartDate = b.StartDate, EndDate = b.EndDate,
                IsActive = b.IsActive, CreatedAt = b.CreatedAt
            }).ToListAsync(ct);
    }

    public async Task<MallBannerDto> Handle(CreateMallBannerCommand command, CancellationToken ct)
    {
        var r = command.Request;
        await ValidateLinkAsync(r.LinkType, r.LinkValue, ct);
        var banner = MallBanner.Create(r.Position, r.ImageUrl, r.LinkType, r.LinkValue,
            r.Title, r.SortOrder, r.StartDate, r.EndDate, r.IsActive);
        _dbContext.MallBanners.Add(banner);
        await _dbContext.SaveChangesAsync(ct);

        return new MallBannerDto
        {
            Id = banner.Id, Position = banner.Position, ImageUrl = banner.ImageUrl,
            LinkType = banner.LinkType, LinkValue = banner.LinkValue, Title = banner.Title,
            SortOrder = banner.SortOrder, StartDate = banner.StartDate, EndDate = banner.EndDate,
            IsActive = banner.IsActive, CreatedAt = banner.CreatedAt
        };
    }

    public async Task<MallBannerDto> Handle(UpdateMallBannerCommand command, CancellationToken ct)
    {
        var banner = await _dbContext.MallBanners.FirstOrDefaultAsync(b => b.Id == command.Id && !b.IsDeleted, ct);
        if (banner is null) throw new BusinessException(404, "Banner不存在");

        var r = command.Request;
        await ValidateLinkAsync(r.LinkType, r.LinkValue, ct);
        banner.Update(r.Position, r.ImageUrl, r.LinkType, r.LinkValue, r.Title, r.StartDate, r.EndDate);
        await _dbContext.SaveChangesAsync(ct);

        return new MallBannerDto
        {
            Id = banner.Id, Position = banner.Position, ImageUrl = banner.ImageUrl,
            LinkType = banner.LinkType, LinkValue = banner.LinkValue, Title = banner.Title,
            SortOrder = banner.SortOrder, StartDate = banner.StartDate, EndDate = banner.EndDate,
            IsActive = banner.IsActive, CreatedAt = banner.CreatedAt
        };
    }

    public async Task<bool> Handle(DeleteMallBannerCommand command, CancellationToken ct)
    {
        var banner = await _dbContext.MallBanners.FirstOrDefaultAsync(b => b.Id == command.Id && !b.IsDeleted, ct);
        if (banner is null) throw new BusinessException(404, "Banner不存在");

        _dbContext.MallBanners.Remove(banner);
        await _dbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> Handle(ChangeMallBannerStatusCommand command, CancellationToken ct)
    {
        var banner = await _dbContext.MallBanners.FirstOrDefaultAsync(b => b.Id == command.Id && !b.IsDeleted, ct);
        if (banner is null) throw new BusinessException(404, "Banner不存在");

        banner.ChangeStatus(command.IsActive);
        await _dbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> Handle(BatchSortMallBannerCommand command, CancellationToken ct)
    {
        var ids = command.Request.Items.Select(i => i.Id).ToList();
        var banners = await _dbContext.MallBanners.Where(b => ids.Contains(b.Id) && !b.IsDeleted).ToListAsync(ct);

        foreach (var item in command.Request.Items)
        {
            var banner = banners.FirstOrDefault(b => b.Id == item.Id);
            if (banner is not null) banner.SetSortOrder(item.SortOrder);
        }

        await _dbContext.SaveChangesAsync(ct);
        return true;
    }

    private async Task ValidateLinkAsync(MallBannerLinkType linkType, string linkValue, CancellationToken ct)
    {
        if (!Enum.IsDefined(linkType))
        {
            throw new BusinessException(400, "Banner跳转类型无效");
        }

        if (linkType == MallBannerLinkType.InternalActivity &&
            (!Guid.TryParse(linkValue, out var activityId) ||
             !await _dbContext.MallActivities.AnyAsync(activity => activity.Id == activityId && !activity.IsDeleted, ct)))
        {
            throw new BusinessException(400, "请填写有效的活动ID");
        }
    }
}
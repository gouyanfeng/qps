using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallNotices;

public class GetMallNoticesQuery : IRequest<List<MallNoticeDto>>
{
    public bool? IsActive { get; set; }
}

public class GetMallNoticeQuery : IRequest<MallNoticeDto>
{
    public Guid Id { get; set; }
}

public class GetActiveMallNoticesQuery : IRequest<List<MallNoticeDto>>
{
}

public class CreateMallNoticeCommand : IRequest<MallNoticeDto>
{
    public MallNoticeCreateRequest Request { get; set; } = null!;
}

public class UpdateMallNoticeCommand : IRequest<MallNoticeDto>
{
    public Guid Id { get; set; }
    public MallNoticeUpdateRequest Request { get; set; } = null!;
}

public class DeleteMallNoticeCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}

public class ChangeMallNoticeStatusCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public bool IsActive { get; set; }
}

public class SetMallNoticeTopCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public bool IsTop { get; set; }
}

public class MallNoticeHandlers :
    IRequestHandler<GetMallNoticesQuery, List<MallNoticeDto>>,
    IRequestHandler<GetMallNoticeQuery, MallNoticeDto>,
    IRequestHandler<GetActiveMallNoticesQuery, List<MallNoticeDto>>,
    IRequestHandler<CreateMallNoticeCommand, MallNoticeDto>,
    IRequestHandler<UpdateMallNoticeCommand, MallNoticeDto>,
    IRequestHandler<DeleteMallNoticeCommand, bool>,
    IRequestHandler<ChangeMallNoticeStatusCommand, bool>,
    IRequestHandler<SetMallNoticeTopCommand, bool>
{
    private readonly IMallDbContext _dbContext;

    public MallNoticeHandlers(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<MallNoticeDto>> Handle(GetMallNoticesQuery query, CancellationToken ct)
    {
        var q = _dbContext.MallNotices.AsNoTracking().Where(n => !n.IsDeleted);
        if (query.IsActive.HasValue) q = q.Where(n => n.IsActive == query.IsActive.Value);

        return await q.OrderByDescending(n => n.IsTop).ThenBy(n => n.SortOrder).ThenByDescending(n => n.CreatedAt)
            .Select(n => new MallNoticeDto
            {
                Id = n.Id, Title = n.Title, Content = n.Content,
                NoticeType = n.NoticeType, IsTop = n.IsTop, SortOrder = n.SortOrder,
                StartDate = n.StartDate, EndDate = n.EndDate, IsActive = n.IsActive, CreatedAt = n.CreatedAt
            }).ToListAsync(ct);
    }

    public async Task<MallNoticeDto> Handle(GetMallNoticeQuery query, CancellationToken ct)
    {
        var n = await _dbContext.MallNotices.AsNoTracking().FirstOrDefaultAsync(x => x.Id == query.Id && !x.IsDeleted, ct);
        if (n is null) throw new BusinessException(404, "公告不存在");

        return new MallNoticeDto
        {
            Id = n.Id, Title = n.Title, Content = n.Content,
            NoticeType = n.NoticeType, IsTop = n.IsTop, SortOrder = n.SortOrder,
            StartDate = n.StartDate, EndDate = n.EndDate, IsActive = n.IsActive, CreatedAt = n.CreatedAt
        };
    }

    public async Task<List<MallNoticeDto>> Handle(GetActiveMallNoticesQuery query, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        return await _dbContext.MallNotices.AsNoTracking()
            .Where(n => !n.IsDeleted && n.IsActive
                && (n.StartDate == null || n.StartDate <= now)
                && (n.EndDate == null || n.EndDate >= now))
            .OrderByDescending(n => n.IsTop).ThenBy(n => n.SortOrder)
            .Select(n => new MallNoticeDto
            {
                Id = n.Id, Title = n.Title, Content = n.Content,
                NoticeType = n.NoticeType, IsTop = n.IsTop, SortOrder = n.SortOrder,
                StartDate = n.StartDate, EndDate = n.EndDate, IsActive = n.IsActive, CreatedAt = n.CreatedAt
            }).ToListAsync(ct);
    }

    public async Task<MallNoticeDto> Handle(CreateMallNoticeCommand command, CancellationToken ct)
    {
        var r = command.Request;
        var notice = MallNotice.Create(r.Title, r.Content, r.NoticeType,
            r.IsTop, r.SortOrder, r.StartDate, r.EndDate, r.IsActive);
        _dbContext.MallNotices.Add(notice);
        await _dbContext.SaveChangesAsync(ct);

        return new MallNoticeDto
        {
            Id = notice.Id, Title = notice.Title, Content = notice.Content,
            NoticeType = notice.NoticeType, IsTop = notice.IsTop, SortOrder = notice.SortOrder,
            StartDate = notice.StartDate, EndDate = notice.EndDate, IsActive = notice.IsActive, CreatedAt = notice.CreatedAt
        };
    }

    public async Task<MallNoticeDto> Handle(UpdateMallNoticeCommand command, CancellationToken ct)
    {
        var notice = await _dbContext.MallNotices.FirstOrDefaultAsync(n => n.Id == command.Id && !n.IsDeleted, ct);
        if (notice is null) throw new BusinessException(404, "公告不存在");

        var r = command.Request;
        notice.Update(r.Title, r.Content, r.NoticeType, r.SortOrder, r.StartDate, r.EndDate);
        await _dbContext.SaveChangesAsync(ct);

        return new MallNoticeDto
        {
            Id = notice.Id, Title = notice.Title, Content = notice.Content,
            NoticeType = notice.NoticeType, IsTop = notice.IsTop, SortOrder = notice.SortOrder,
            StartDate = notice.StartDate, EndDate = notice.EndDate, IsActive = notice.IsActive, CreatedAt = notice.CreatedAt
        };
    }

    public async Task<bool> Handle(DeleteMallNoticeCommand command, CancellationToken ct)
    {
        var notice = await _dbContext.MallNotices.FirstOrDefaultAsync(n => n.Id == command.Id && !n.IsDeleted, ct);
        if (notice is null) throw new BusinessException(404, "公告不存在");

        _dbContext.MallNotices.Remove(notice);
        await _dbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> Handle(ChangeMallNoticeStatusCommand command, CancellationToken ct)
    {
        var notice = await _dbContext.MallNotices.FirstOrDefaultAsync(n => n.Id == command.Id && !n.IsDeleted, ct);
        if (notice is null) throw new BusinessException(404, "公告不存在");

        notice.ChangeStatus(command.IsActive);
        await _dbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> Handle(SetMallNoticeTopCommand command, CancellationToken ct)
    {
        var notice = await _dbContext.MallNotices.FirstOrDefaultAsync(n => n.Id == command.Id && !n.IsDeleted, ct);
        if (notice is null) throw new BusinessException(404, "公告不存在");

        notice.SetTop(command.IsTop);
        await _dbContext.SaveChangesAsync(ct);
        return true;
    }
}
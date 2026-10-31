using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs.MallMembers;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallMemberLevels;

public class GetMallMemberLevelsQuery : IRequest<List<MallMemberLevelDto>>
{
    public MallMemberLevelQueryRequest Request { get; set; } = null!;
}

public class GetMallMemberLevelQuery : IRequest<MallMemberLevelDto>
{
    public Guid Id { get; set; }
}

public class CreateMallMemberLevelCommand : IRequest<MallMemberLevelDto>
{
    public MallMemberLevelCreateRequest Request { get; set; } = null!;
}

public class UpdateMallMemberLevelCommand : IRequest<MallMemberLevelDto>
{
    public Guid Id { get; set; }
    public MallMemberLevelUpdateRequest Request { get; set; } = null!;
}

public class DeleteMallMemberLevelCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}

public class ChangeMallMemberLevelStatusCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public bool IsActive { get; set; }
}

public class SetDefaultMallMemberLevelCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}

public class MallMemberLevelHandlers :
    IRequestHandler<GetMallMemberLevelsQuery, List<MallMemberLevelDto>>,
    IRequestHandler<GetMallMemberLevelQuery, MallMemberLevelDto>,
    IRequestHandler<CreateMallMemberLevelCommand, MallMemberLevelDto>,
    IRequestHandler<UpdateMallMemberLevelCommand, MallMemberLevelDto>,
    IRequestHandler<DeleteMallMemberLevelCommand, bool>,
    IRequestHandler<ChangeMallMemberLevelStatusCommand, bool>,
    IRequestHandler<SetDefaultMallMemberLevelCommand, bool>
{
    private readonly IMallDbContext _db;

    public MallMemberLevelHandlers(IMallDbContext db) => _db = db;

    public async Task<List<MallMemberLevelDto>> Handle(GetMallMemberLevelsQuery req, CancellationToken ct)
    {
        var r = req.Request;
        var q = _db.MallMemberLevels.AsNoTracking();
        if (r.IsActive.HasValue) q = q.Where(x => x.IsActive == r.IsActive);
        return await q.OrderBy(x => x.SortOrder).ThenBy(x => x.LevelCode)
            .Select(x => ToDto(x)).ToListAsync(ct);
    }

    public async Task<MallMemberLevelDto> Handle(GetMallMemberLevelQuery req, CancellationToken ct)
    {
        var l = await _db.MallMemberLevels.AsNoTracking().FirstOrDefaultAsync(x => x.Id == req.Id, ct);
        if (l == null) throw new BusinessException(404, "等级不存在");
        return ToDto(l);
    }

    public async Task<MallMemberLevelDto> Handle(CreateMallMemberLevelCommand req, CancellationToken ct)
    {
        var r = req.Request;
        var exists = await _db.MallMemberLevels
            .AnyAsync(x => x.LevelCode == r.LevelCode, ct);
        if (exists) throw new BusinessException(409, "已存在同级别的等级编码");

        if (r.IsDefault)
        {
            var currentDefault = await _db.MallMemberLevels
                .FirstOrDefaultAsync(x => x.IsDefault, ct);
            currentDefault?.SetDefault(false);
        }

        var level = MallMemberLevel.Create(r.Name, r.LevelCode,
            r.DiscountRate, r.MinSpent, r.MaxSpent, r.UpgradePoints, r.SortOrder, r.IsDefault);
        _db.MallMemberLevels.Add(level);
        await _db.SaveChangesAsync(ct);
        return ToDto(level);
    }

    public async Task<MallMemberLevelDto> Handle(UpdateMallMemberLevelCommand req, CancellationToken ct)
    {
        var l = await _db.MallMemberLevels.FirstOrDefaultAsync(x => x.Id == req.Id, ct);
        if (l == null) throw new BusinessException(404, "等级不存在");
        var r = req.Request;
        l.Update(r.Name, r.DiscountRate, r.MinSpent, r.MaxSpent, r.UpgradePoints, r.SortOrder);
        await _db.SaveChangesAsync(ct);
        return ToDto(l);
    }

    public async Task<bool> Handle(DeleteMallMemberLevelCommand req, CancellationToken ct)
    {
        var l = await _db.MallMemberLevels.FirstOrDefaultAsync(x => x.Id == req.Id, ct);
        if (l == null) throw new BusinessException(404, "等级不存在");
        if (l.IsDefault) throw new BusinessException(409, "默认等级不能删除");
        _db.MallMemberLevels.Remove(l);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> Handle(ChangeMallMemberLevelStatusCommand req, CancellationToken ct)
    {
        var l = await _db.MallMemberLevels.FirstOrDefaultAsync(x => x.Id == req.Id, ct);
        if (l == null) throw new BusinessException(404, "等级不存在");
        if (req.IsActive) l.Activate(); else l.Deactivate();
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> Handle(SetDefaultMallMemberLevelCommand req, CancellationToken ct)
    {
        var l = await _db.MallMemberLevels.FirstOrDefaultAsync(x => x.Id == req.Id, ct);
        if (l == null) throw new BusinessException(404, "等级不存在");
        var current = await _db.MallMemberLevels
            .FirstOrDefaultAsync(x => x.IsDefault, ct);
        current?.SetDefault(false);
        l.SetDefault(true);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private static MallMemberLevelDto ToDto(MallMemberLevel l) => new()
    {
        Id = l.Id, Name = l.Name, LevelCode = l.LevelCode,
        DiscountRate = l.DiscountRate, MinSpent = l.MinSpent, MaxSpent = l.MaxSpent,
        UpgradePoints = l.UpgradePoints, SortOrder = l.SortOrder,
        IsDefault = l.IsDefault, IsActive = l.IsActive
    };
}
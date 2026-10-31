using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs.MallMembers;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Contracts;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallMembers;

public class QueryMallMembersCommand : IRequest<(List<MallMemberDto> Items, int Total)>
{
    public MallMemberQueryRequest Request { get; set; } = null!;
}

public class GetMallMemberQuery : IRequest<MallMemberDto>
{
    public Guid Id { get; set; }
}

public class GetMallMemberByPhoneQuery : IRequest<MallMemberDto?>
{
    public string Phone { get; set; } = string.Empty;
}

public class CreateMallMemberCommand : IRequest<MallMemberDto>
{
    public MallMemberCreateRequest Request { get; set; } = null!;
}

public class UpdateMallMemberCommand : IRequest<MallMemberDto>
{
    public Guid Id { get; set; }
    public MallMemberUpdateRequest Request { get; set; } = null!;
}

public class DeleteMallMemberCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}

public class AdjustMallMemberPointsCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public MallMemberAdjustPointsRequest Request { get; set; } = null!;
}

public class ChangeMallMemberStatusCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public bool IsActive { get; set; }
}

public class RegisterMallMemberCommand : IRequest<MallMemberAuthResponse>, IAllowAnonymousRequest
{
    public MallMemberRegisterRequest Request { get; set; } = null!;
}

public class LoginMallMemberCommand : IRequest<MallMemberAuthResponse>, IAllowAnonymousRequest
{
    public MallMemberLoginRequest Request { get; set; } = null!;
}

public class MallMemberHandlers :
    IRequestHandler<QueryMallMembersCommand, (List<MallMemberDto> Items, int Total)>,
    IRequestHandler<GetMallMemberQuery, MallMemberDto>,
    IRequestHandler<GetMallMemberByPhoneQuery, MallMemberDto?>,
    IRequestHandler<CreateMallMemberCommand, MallMemberDto>,
    IRequestHandler<UpdateMallMemberCommand, MallMemberDto>,
    IRequestHandler<DeleteMallMemberCommand, bool>,
    IRequestHandler<AdjustMallMemberPointsCommand, bool>,
    IRequestHandler<ChangeMallMemberStatusCommand, bool>,
    IRequestHandler<RegisterMallMemberCommand, MallMemberAuthResponse>,
    IRequestHandler<LoginMallMemberCommand, MallMemberAuthResponse>
{
    private readonly IMallDbContext _db;
    private readonly IJwtGenerator _jwtGenerator;

    public MallMemberHandlers(IMallDbContext db, IJwtGenerator jwtGenerator)
    {
        _db = db;
        _jwtGenerator = jwtGenerator;
    }

    public async Task<(List<MallMemberDto> Items, int Total)> Handle(QueryMallMembersCommand req, CancellationToken ct)
    {
        var r = req.Request;
        var q = _db.MallMembers.AsNoTracking();

        if (r.LevelId.HasValue) q = q.Where(x => x.LevelId == r.LevelId);
        if (r.IsActive.HasValue) q = q.Where(x => x.IsActive == r.IsActive);
        if (!string.IsNullOrWhiteSpace(r.Keyword))
        {
            var kw = r.Keyword;
            q = q.Where(x => x.Nickname.Contains(kw) || x.Phone.Contains(kw));
        }

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(x => x.JoinAt)
            .Skip((r.PageIndex - 1) * r.PageSize)
            .Take(r.PageSize)
            .Select(x => ToDto(x))
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<MallMemberDto> Handle(GetMallMemberQuery req, CancellationToken ct)
    {
        var m = await _db.MallMembers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == req.Id, ct);
        if (m == null) throw new BusinessException(404, "会员不存在");
        return ToDto(m);
    }

    public async Task<MallMemberDto?> Handle(GetMallMemberByPhoneQuery req, CancellationToken ct)
    {
        var m = await _db.MallMembers.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Phone == req.Phone, ct);
        return m == null ? null : ToDto(m);
    }

    public async Task<MallMemberDto> Handle(CreateMallMemberCommand req, CancellationToken ct)
    {
        var r = req.Request;
        if (string.IsNullOrWhiteSpace(r.Phone) || r.Phone.Length != 11)
            throw new BusinessException(400, "请输入正确的11位手机号");
        if (string.IsNullOrWhiteSpace(r.Password) || r.Password.Length < 6)
            throw new BusinessException(400, "密码至少6位");

        var exists = await _db.MallMembers.AnyAsync(x => x.Phone == r.Phone, ct);
        if (exists) throw new BusinessException(400, "该手机号已注册");

        var member = MallMember.Register(r.Phone, r.Password,
            string.IsNullOrWhiteSpace(r.Nickname) ? null : r.Nickname);
        _db.MallMembers.Add(member);
        await _db.SaveChangesAsync(ct);
        return ToDto(member);
    }

    public async Task<MallMemberDto> Handle(UpdateMallMemberCommand req, CancellationToken ct)
    {
        var m = await _db.MallMembers.FirstOrDefaultAsync(x => x.Id == req.Id, ct);
        if (m == null) throw new BusinessException(404, "会员不存在");

        m.UpdateProfile(req.Request.Nickname, req.Request.Avatar, req.Request.Birthday, req.Request.Gender);
        if (req.Request.LevelId.HasValue) m.SetLevel(req.Request.LevelId.Value);
        await _db.SaveChangesAsync(ct);
        return ToDto(m);
    }

    public async Task<bool> Handle(DeleteMallMemberCommand req, CancellationToken ct)
    {
        var m = await _db.MallMembers.FirstOrDefaultAsync(x => x.Id == req.Id, ct);
        if (m == null) throw new BusinessException(404, "会员不存在");
        m.Deactivate();
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> Handle(AdjustMallMemberPointsCommand req, CancellationToken ct)
    {
        var m = await _db.MallMembers.FirstOrDefaultAsync(x => x.Id == req.Id, ct);
        if (m == null) throw new BusinessException(404, "会员不存在");

        var r = req.Request;
        if (r.Points > 0)
            m.AddPoints(r.Points);
        else if (r.Points < 0)
        {
            if (!m.TryConsumePoints(-r.Points))
                throw new BusinessException(400, "可用积分不足");
        }
        else
            return false;

        var log = MallMemberPoint.Create(m.Id, r.Points, m.AvailablePoints, r.Type, null, r.Remark);
        _db.MallMemberPoints.Add(log);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> Handle(ChangeMallMemberStatusCommand req, CancellationToken ct)
    {
        var m = await _db.MallMembers.FirstOrDefaultAsync(x => x.Id == req.Id, ct);
        if (m == null) throw new BusinessException(404, "会员不存在");
        if (req.IsActive) m.Activate(); else m.Deactivate();
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<MallMemberAuthResponse> Handle(RegisterMallMemberCommand req, CancellationToken ct)
    {
        var r = req.Request;
        if (string.IsNullOrWhiteSpace(r.Phone) || r.Phone.Length != 11)
            throw new BusinessException(400, "请输入正确的11位手机号");
        if (string.IsNullOrWhiteSpace(r.Password) || r.Password.Length < 6)
            throw new BusinessException(400, "密码至少6位");

        var exists = await _db.MallMembers.AnyAsync(x => x.Phone == r.Phone, ct);
        if (exists) throw new BusinessException(400, "该手机号已注册");

        var member = MallMember.Register(r.Phone, r.Password, r.Nickname);
        _db.MallMembers.Add(member);
        await _db.SaveChangesAsync(ct);

        var token = _jwtGenerator.GenerateToken(member.Id, member.Phone, "member");

        return new MallMemberAuthResponse
        {
            Token = token,
            MemberId = member.Id,
            Phone = member.Phone,
            Nickname = member.Nickname,
            Avatar = member.Avatar,
            Role = "member",
            AvailablePoints = member.AvailablePoints,
            LevelId = member.LevelId
        };
    }

    public async Task<MallMemberAuthResponse> Handle(LoginMallMemberCommand req, CancellationToken ct)
    {
        var r = req.Request;
        var member = await _db.MallMembers
            .FirstOrDefaultAsync(x => x.Phone == r.Phone, ct);

        if (member == null || !member.VerifyPassword(r.Password))
            throw new BusinessException(401, "手机号或密码错误");

        if (!member.IsActive)
            throw new BusinessException(403, "账号已被禁用");

        member.MarkLogin();
        await _db.SaveChangesAsync(ct);

        var token = _jwtGenerator.GenerateToken(member.Id, member.Phone, "member");

        return new MallMemberAuthResponse
        {
            Token = token,
            MemberId = member.Id,
            Phone = member.Phone,
            Nickname = member.Nickname,
            Avatar = member.Avatar,
            Role = "member",
            AvailablePoints = member.AvailablePoints,
            LevelId = member.LevelId
        };
    }

    private static MallMemberDto ToDto(MallMember m) => new()
    {
        Id = m.Id, Phone = m.Phone,
        LevelId = m.LevelId, Nickname = m.Nickname, Avatar = m.Avatar,
        Birthday = m.Birthday, Gender = m.Gender,
        TotalPoints = m.TotalPoints, AvailablePoints = m.AvailablePoints,
        TotalSpent = m.TotalSpent, TotalOrders = m.TotalOrders,
        LastConsumeAt = m.LastConsumeAt, JoinAt = m.JoinAt,
        LastLoginAt = m.LastLoginAt, IsActive = m.IsActive
    };
}
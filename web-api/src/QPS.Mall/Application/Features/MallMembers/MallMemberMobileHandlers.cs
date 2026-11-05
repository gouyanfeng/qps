using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs.MallMembers;
using QPS.Mall.Domain;

namespace QPS.Mall.Application.Features.MallMembers;

public class GetMallMemberPointLogsQuery : IRequest<List<MallMemberPointLogDto>>
{
    public Guid MemberId { get; set; }
    public int Limit { get; set; } = 50;
}

public class MallMemberPointLogDto
{
    public Guid Id { get; set; }
    public int ChangeAmount { get; set; }
    public int Balance { get; set; }
    public string ChangeType { get; set; } = string.Empty;
    public string? RefNo { get; set; }
    public string Remark { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class GetMallMemberPointLogsHandler : IRequestHandler<GetMallMemberPointLogsQuery, List<MallMemberPointLogDto>>
{
    private readonly IMallDbContext _db;
    public GetMallMemberPointLogsHandler(IMallDbContext db) => _db = db;

    public async Task<List<MallMemberPointLogDto>> Handle(GetMallMemberPointLogsQuery req, CancellationToken ct)
    {
        return await _db.MallMemberPoints
            .AsNoTracking()
            .Where(x => x.MemberId == req.MemberId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(req.Limit)
            .Select(x => new MallMemberPointLogDto
            {
                Id = x.Id,
                ChangeAmount = x.ChangeAmount,
                Balance = x.Balance,
                ChangeType = x.ChangeType,
                RefNo = x.RefNo,
                Remark = x.Remark,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(ct);
    }
}

public class ChangeMallMemberPasswordCommand : IRequest<bool>
{
    public Guid MemberId { get; set; }
    public string OldPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class ChangeMallMemberPasswordHandler : IRequestHandler<ChangeMallMemberPasswordCommand, bool>
{
    private readonly IMallDbContext _db;
    public ChangeMallMemberPasswordHandler(IMallDbContext db) => _db = db;

    public async Task<bool> Handle(ChangeMallMemberPasswordCommand req, CancellationToken ct)
    {
        var m = await _db.MallMembers.FirstOrDefaultAsync(x => x.Id == req.MemberId, ct);
        if (m == null) throw new QPS.Shared.Exceptions.BusinessException(404, "会员不存在");

        if (!m.VerifyPassword(req.OldPassword))
            throw new QPS.Shared.Exceptions.BusinessException(400, "原密码错误");

        if (string.IsNullOrWhiteSpace(req.NewPassword) || req.NewPassword.Length < 6)
            throw new QPS.Shared.Exceptions.BusinessException(400, "新密码至少6位");

        m.ChangePassword(req.NewPassword);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

public class GetMallMemberOrderBadgeQuery : IRequest<MallMemberOrderBadgeDto>
{
    public Guid MemberId { get; set; }
}

public class MallMemberOrderBadgeDto
{
    public int PendingPayment { get; set; }
    public int Paid { get; set; }
    public int Shipped { get; set; }
    public int Completed { get; set; }
    public int Returning { get; set; }
}

public class GetMallMemberOrderBadgeHandler : IRequestHandler<GetMallMemberOrderBadgeQuery, MallMemberOrderBadgeDto>
{
    private readonly IMallDbContext _db;
    public GetMallMemberOrderBadgeHandler(IMallDbContext db) => _db = db;

    public async Task<MallMemberOrderBadgeDto> Handle(GetMallMemberOrderBadgeQuery req, CancellationToken ct)
    {
        var counts = await _db.MallOrders
            .AsNoTracking()
            .Where(x => x.MemberId == req.MemberId)
            .Select(x => x.Status)
            .GroupBy(status => status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.Status, group => group.Count, ct);

        return new MallMemberOrderBadgeDto
        {
            PendingPayment = GetCount(counts, MallOrderStatus.PendingPayment),
            Paid = GetCount(counts, MallOrderStatus.Paid),
            Shipped = GetCount(counts, MallOrderStatus.Shipped),
            Completed = GetCount(counts, MallOrderStatus.Completed),
            Returning = GetCount(counts, MallOrderStatus.Returning)
        };
    }

    private static int GetCount(IReadOnlyDictionary<string, int> counts, string status) =>
        counts.TryGetValue(status, out var count) ? count : 0;
}
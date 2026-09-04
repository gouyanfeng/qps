using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Contracts.Crm;
using QPS.Application.Contracts.Crm.CrmTransfers;
using QPS.Application.Interfaces;
using QPS.Domain.Entities.Crm;
using QPS.Domain.Exceptions;

namespace QPS.Application.Features.Crm.CrmTransfers;

public class ChangeCrmOwnerCommand : IRequest<bool>
{
    public string EntityType { get; init; } = string.Empty;

    public CrmTransferOwnerChangeRequest Request { get; init; } = null!;
}

public class ChangeCrmOwnerHandler : IRequestHandler<ChangeCrmOwnerCommand, bool>
{
    private const string TransferPermissionCode = "CRM_TRANSFER";

    private readonly IDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public ChangeCrmOwnerHandler(IDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<bool> Handle(ChangeCrmOwnerCommand request, CancellationToken cancellationToken)
    {
        var entityIds = request.Request.EntityIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();
        if (entityIds.Count == 0)
            throw new BusinessException(400, "请选择要流转的数据");

        var operatorUserId = GetOperatorUserId();
        await EnsureOperatorActiveAsync(operatorUserId, cancellationToken);
        await EnsureTargetOwnerActiveAsync(request.Request.ToOwnerUserId, cancellationToken);

        var canManage = await HasTransferPermissionAsync(operatorUserId, cancellationToken);
        var remark = request.Request.Remark?.Trim() ?? string.Empty;

        switch (request.EntityType)
        {
            case CrmTransferEntityType.HerbBaseSubject:
                await ChangeSubjectOwnersAsync(entityIds, request.Request.ToOwnerUserId, operatorUserId, canManage, remark, cancellationToken);
                break;
            case CrmTransferEntityType.Vendor:
                await ChangeVendorOwnersAsync(entityIds, request.Request.ToOwnerUserId, operatorUserId, canManage, remark, cancellationToken);
                break;
            default:
                throw new BusinessException(400, "不支持的流转对象类型");
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private Guid GetOperatorUserId()
    {
        return Guid.TryParse(_currentUserService.UserId, out var operatorUserId)
            ? operatorUserId
            : throw new BusinessException(401, "登录状态无效");
    }

    private async Task EnsureOperatorActiveAsync(Guid operatorUserId, CancellationToken cancellationToken)
    {
        if (!await _dbContext.SystemUsers.AnyAsync(user => user.Id == operatorUserId && user.IsActive, cancellationToken))
            throw new BusinessException(401, "当前用户不可用");
    }

    private async Task EnsureTargetOwnerActiveAsync(Guid? ownerUserId, CancellationToken cancellationToken)
    {
        if (!ownerUserId.HasValue)
            return;

        if (!await _dbContext.SystemUsers.AnyAsync(user => user.Id == ownerUserId.Value && user.IsActive, cancellationToken))
            throw new BusinessException(404, "负责人不存在");
    }

    private async Task<bool> HasTransferPermissionAsync(Guid operatorUserId, CancellationToken cancellationToken)
    {
        return await (
                from user in _dbContext.SystemUsers
                join rolePermission in _dbContext.SystemRolePermissions on user.RoleId equals rolePermission.RoleId
                join permission in _dbContext.SystemPermissions on rolePermission.PermissionId equals permission.Id
                where user.Id == operatorUserId && permission.Code == TransferPermissionCode
                select permission.Id)
            .AnyAsync(cancellationToken);
    }

    private async Task ChangeSubjectOwnersAsync(
        List<Guid> entityIds,
        Guid? toOwnerUserId,
        Guid operatorUserId,
        bool canManage,
        string remark,
        CancellationToken cancellationToken)
    {
        var subjects = await _dbContext.CrmHerbBaseSubjects
            .Where(subject => entityIds.Contains(subject.Id))
            .ToListAsync(cancellationToken);
        if (subjects.Count != entityIds.Count)
            throw new BusinessException(404, "药材基地主体不存在");

        foreach (var subject in subjects)
        {
            EnsureCanChangeOwner(subject.OwnerUserId, toOwnerUserId, operatorUserId, canManage);
        }

        foreach (var subject in subjects)
        {
            _dbContext.CrmTransferRecords.Add(subject.ChangeOwner(toOwnerUserId, operatorUserId, remark));
        }
    }

    private async Task ChangeVendorOwnersAsync(
        List<Guid> entityIds,
        Guid? toOwnerUserId,
        Guid operatorUserId,
        bool canManage,
        string remark,
        CancellationToken cancellationToken)
    {
        var vendors = await _dbContext.CrmVendors
            .Where(vendor => entityIds.Contains(vendor.Id))
            .ToListAsync(cancellationToken);
        if (vendors.Count != entityIds.Count)
            throw new BusinessException(404, "厂商不存在");

        foreach (var vendor in vendors)
        {
            EnsureCanChangeOwner(vendor.OwnerUserId, toOwnerUserId, operatorUserId, canManage);
        }

        foreach (var vendor in vendors)
        {
            _dbContext.CrmTransferRecords.Add(vendor.ChangeOwner(toOwnerUserId, operatorUserId, remark));
        }
    }

    private static void EnsureCanChangeOwner(Guid? fromOwnerUserId, Guid? toOwnerUserId, Guid operatorUserId, bool canManage)
    {
        if (!canManage && (toOwnerUserId.HasValue || fromOwnerUserId != operatorUserId))
            throw new BusinessException(403, "无权执行该流转操作");

        if (fromOwnerUserId == toOwnerUserId)
            throw new BusinessException(400, fromOwnerUserId.HasValue ? "负责人未变化，无需流转" : "待分配对象不能退回待分配池");
    }
}

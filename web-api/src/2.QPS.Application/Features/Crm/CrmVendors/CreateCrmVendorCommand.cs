using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Contracts.Crm;
using QPS.Application.Contracts.Crm.CrmVendors;
using QPS.Application.Features.Crm;
using QPS.Application.Interfaces;
using QPS.Domain.Entities.Crm;
using QPS.Domain.Exceptions;

namespace QPS.Application.Features.Crm.CrmVendors;

public class CreateCrmVendorCommand : IRequest<bool>
{
    public CrmVendorCreateRequest Request { get; set; } = null!;
}

public class CreateCrmVendorHandler : IRequestHandler<CreateCrmVendorCommand, bool>
{
    private readonly IDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    /// <summary>
    /// 创建厂商处理器。
    /// </summary>
    public CreateCrmVendorHandler(IDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// 编排创建厂商用例。
    /// </summary>
    public async Task<bool> Handle(CreateCrmVendorCommand request, CancellationToken cancellationToken)
    {
        // 编排创建厂商用例：
        // 规范化名称、校验重复、确认负责人、创建厂商和默认流转记录。
        var vendorName = NormalizeVendorDisplayName(request.Request.VendorName);

        var normalizedVendorName = CrmVendorRules.NormalizeVendorName(vendorName);

        await EnsureVendorNameNotExists(normalizedVendorName, cancellationToken);

        var operatorUserId = GetOperatorUserId();
        var ownerUserId = operatorUserId;
        await EnsureOwnerExists(ownerUserId, cancellationToken);

        var vendor = CreateVendor(request.Request, vendorName, normalizedVendorName, ownerUserId);

        _dbContext.CrmVendors.Add(vendor);
        _dbContext.CrmTransferRecords.Add(CrmTransferRecord.CreateEntry(
            CrmTransferEntityType.Vendor,
            vendor.Id,
            vendor.OwnerUserId,
            operatorUserId,
            request.Request.Remark.Trim()));

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// 规范化厂商展示名称。
    /// </summary>
    private static string NormalizeVendorDisplayName(string vendorName)
    {
        var normalizedName = vendorName.Trim();

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new BusinessException(400, "请输入厂商名称");
        }

        return normalizedName;
    }

    /// <summary>
    /// 确认厂商名称未被占用。
    /// </summary>
    private async Task EnsureVendorNameNotExists(string normalizedVendorName, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.CrmVendors
            .AnyAsync(
                vendor => !vendor.IsDeleted && vendor.NormalizedVendorName == normalizedVendorName,
                cancellationToken);

        if (exists)
        {
            throw new BusinessException(400, "厂商已存在");
        }
    }

    /// <summary>
    /// 请求带负责人时确认负责人存在。
    /// </summary>
    private async Task EnsureOwnerExists(Guid ownerUserId, CancellationToken cancellationToken)
    {
        var ownerExists = await _dbContext.SystemUsers
            .AsNoTracking()
            .AnyAsync(user => user.Id == ownerUserId && user.IsActive, cancellationToken);

        if (!ownerExists)
        {
            throw new BusinessException(404, "负责人不存在");
        }
    }

    /// <summary>
    /// 根据请求创建厂商实体。
    /// </summary>
    private static CrmVendor CreateVendor(
        CrmVendorCreateRequest request,
        string vendorName,
        string normalizedVendorName,
        Guid ownerUserId)
    {
        return CrmVendor.Create(
            vendorName,
            normalizedVendorName,
            CrmVendorRules.NormalizePriority(request.PriorityLevel),
            request.Remark.Trim(),
            ownerUserId);
    }

    private Guid GetOperatorUserId()
    {
        return Guid.TryParse(_currentUserService.UserId, out var operatorUserId)
            ? operatorUserId
            : throw new BusinessException(401, "登录状态无效");
    }

}

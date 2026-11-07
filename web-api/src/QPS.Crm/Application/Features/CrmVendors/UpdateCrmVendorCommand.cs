using QPS.Crm.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Crm.Contracts.DTOs;
using QPS.Crm.Contracts.DTOs.CrmVendors;
using QPS.Shared.Contracts;
using QPS.Crm.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Crm.Application.Features.CrmVendors;

public class UpdateCrmVendorCommand : IRequest<bool>
{
    public Guid Id { get; set; }

    public CrmVendorUpdateRequest Request { get; set; } = null!;
}

public class UpdateCrmVendorHandler : IRequestHandler<UpdateCrmVendorCommand, bool>
{
    private readonly ICrmDbContext _dbContext;

    /// <summary>
    /// 更新厂商处理器。
    /// </summary>
    public UpdateCrmVendorHandler(ICrmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 编排更新厂商用例。
    /// </summary>
    public async Task<bool> Handle(UpdateCrmVendorCommand request, CancellationToken cancellationToken)
    {
        // 编排更新厂商用例：
        // 获取厂商、规范化名称、校验重复、更新并保存。
        var vendor = await GetVendor(request.Id, cancellationToken);

        var vendorName = NormalizeVendorDisplayName(request.Request.VendorName);

        var normalizedVendorName = CrmVendorRules.NormalizeVendorName(vendorName);

        await EnsureVendorNameNotDuplicated(request.Id, normalizedVendorName, cancellationToken);

        UpdateVendor(vendor, request.Request, vendorName, normalizedVendorName);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// 获取要更新的厂商。
    /// </summary>
    private async Task<CrmVendor> GetVendor(Guid vendorId, CancellationToken cancellationToken)
    {
        var vendor = await _dbContext.CrmVendors
            .FirstOrDefaultAsync(item => item.Id == vendorId, cancellationToken);

        if (vendor is null)
        {
            throw new BusinessException(404, "厂商不存在");
        }

        return vendor;
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
    /// 确认厂商名称未被其他厂商占用。
    /// </summary>
    private async Task EnsureVendorNameNotDuplicated(
        Guid vendorId,
        string normalizedVendorName,
        CancellationToken cancellationToken)
    {
        var duplicated = await _dbContext.CrmVendors
            .AnyAsync(
                item =>
                    item.Id != vendorId &&
                    item.NormalizedVendorName == normalizedVendorName,
                cancellationToken);

        if (duplicated)
        {
            throw new BusinessException(400, "厂商已存在");
        }
    }

    /// <summary>
    /// 更新厂商实体资料。
    /// </summary>
    private static void UpdateVendor(
        CrmVendor vendor,
        CrmVendorUpdateRequest request,
        string vendorName,
        string normalizedVendorName)
    {
        vendor.Update(
            vendorName,
            normalizedVendorName,
            CrmVendorRules.NormalizePriority(request.PriorityLevel),
            request.Remark.Trim());
    }
}

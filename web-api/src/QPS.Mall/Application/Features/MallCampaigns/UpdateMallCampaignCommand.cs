using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallCampaigns;

/// <summary>
/// 管理端编辑活动专题：更新基本信息并整体替换选品。
/// </summary>
public class UpdateMallCampaignCommand : IRequest<Guid>
{
    public Guid Id { get; set; }

    public MallCampaignUpdateRequest Request { get; set; } = null!;
}

public class UpdateMallCampaignHandler : IRequestHandler<UpdateMallCampaignCommand, Guid>
{
    private readonly IMallDbContext _dbContext;

    public UpdateMallCampaignHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid> Handle(UpdateMallCampaignCommand command, CancellationToken cancellationToken)
    {
        var campaign = await _dbContext.MallCampaigns
            .FirstOrDefaultAsync(item => item.Id == command.Id, cancellationToken);

        if (campaign is null)
        {
            throw new BusinessException(404, "活动专题不存在");
        }

        var request = command.Request;
        var type = MallCampaignRules.NormalizeType(request.Type);
        var name = MallCampaignRules.NormalizeName(request.Name);
        var coverImageUrl = MallCampaignRules.NormalizeCoverImageUrl(request.CoverImageUrl);
        var contentHtml = MallCampaignRules.NormalizeContentHtml(request.ContentHtml);
        var sortOrder = MallCampaignRules.NormalizeSortOrder(request.SortOrder);
        MallCampaignRules.ValidateTimeWindow(request.StartAt, request.EndAt);

        var isCouponCampaign = type == MallCampaignType.Coupon;
        var items = isCouponCampaign
            ? MallCampaignRules.NormalizeOptionalItems(request.Items)
            : MallCampaignRules.NormalizeItems(request.Items);
        var couponIds = isCouponCampaign
            ? MallCampaignRules.NormalizeCouponIds(request.CouponIds, required: true)
            : new List<Guid>();

        if (items.Count > 0)
        {
            await MallCampaignRules.ValidateProductsExistAsync(_dbContext, items, cancellationToken);
        }

        await MallCampaignRules.ValidateCouponsExistAsync(_dbContext, couponIds, cancellationToken);

        campaign.ChangeType(type);
        campaign.Update(
            name,
            coverImageUrl,
            contentHtml,
            request.StartAt,
            request.EndAt,
            sortOrder);

        var existingItems = await _dbContext.MallCampaignProducts
            .Where(item => item.CampaignId == campaign.Id)
            .ToListAsync(cancellationToken);

        foreach (var existing in existingItems)
        {
            existing.SoftDelete();
        }

        foreach (var item in items)
        {
            _dbContext.MallCampaignProducts.Add(
                MallCampaignProduct.Create(campaign.Id, item.ProductId, item.GroupName, item.SortOrder));
        }

        var existingCoupons = await _dbContext.MallCampaignCoupons
            .Where(item => item.CampaignId == campaign.Id)
            .ToListAsync(cancellationToken);

        foreach (var existing in existingCoupons)
        {
            existing.SoftDelete();
        }

        for (var index = 0; index < couponIds.Count; index++)
        {
            _dbContext.MallCampaignCoupons.Add(
                MallCampaignCoupon.Create(campaign.Id, couponIds[index], index));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return campaign.Id;
    }
}

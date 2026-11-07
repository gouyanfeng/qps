using MediatR;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;

namespace QPS.Mall.Application.Features.MallCampaigns;

/// <summary>
/// 管理端新增活动专题（含选品）。
/// </summary>
public class CreateMallCampaignCommand : IRequest<Guid>
{
    public MallCampaignCreateRequest Request { get; set; } = null!;
}

public class CreateMallCampaignHandler : IRequestHandler<CreateMallCampaignCommand, Guid>
{
    private readonly IMallDbContext _dbContext;

    public CreateMallCampaignHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid> Handle(CreateMallCampaignCommand command, CancellationToken cancellationToken)
    {
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

        var campaign = MallCampaign.Create(
            type,
            name,
            coverImageUrl,
            contentHtml,
            request.StartAt,
            request.EndAt,
            request.IsActive,
            sortOrder);

        _dbContext.MallCampaigns.Add(campaign);

        foreach (var item in items)
        {
            _dbContext.MallCampaignProducts.Add(
                MallCampaignProduct.Create(campaign.Id, item.ProductId, item.GroupName, item.SortOrder));
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

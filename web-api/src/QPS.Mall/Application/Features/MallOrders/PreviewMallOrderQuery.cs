using MediatR;
using QPS.Mall.Application.Features.MallPromotions;
using QPS.Mall.Contracts.DTOs;

namespace QPS.Mall.Application.Features.MallOrders;

/// <summary>订单确认页价格预览：按当前商品与数量试算商品金额、促销优惠及明细。</summary>
public class PreviewMallOrderQuery : IRequest<MallOrderPreviewDto>
{
    public Guid MemberId { get; set; }

    public PreviewMallOrderRequest Request { get; set; } = new();
}

public class PreviewMallOrderHandler : IRequestHandler<PreviewMallOrderQuery, MallOrderPreviewDto>
{
    private readonly MallPromotionPreviewService _promotionPreview;

    public PreviewMallOrderHandler(MallPromotionPreviewService promotionPreview)
    {
        _promotionPreview = promotionPreview;
    }

    public async Task<MallOrderPreviewDto> Handle(
        PreviewMallOrderQuery request,
        CancellationToken cancellationToken)
    {
        var items = request.Request.Items
            .Select(item => new MallPromotionPreviewItem(item.SkuId, item.Quantity))
            .ToList();

        var preview = await _promotionPreview.PreviewAsync(
            request.MemberId,
            items,
            cancellationToken);

        return new MallOrderPreviewDto
        {
            GoodsAmount = preview.GoodsAmount,
            PromotionDiscountAmount = preview.DiscountAmount,
            PromotionDetails = preview.Lines
                .Select(line => new MallOrderPromotionDto
                {
                    PromotionId = line.PromotionId,
                    Name = line.Name,
                    DiscountAmount = line.DiscountAmount
                })
                .ToList(),
            PayableAmount = Math.Max(0, preview.GoodsAmount - preview.DiscountAmount)
        };
    }
}

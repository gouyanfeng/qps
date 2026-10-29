using Microsoft.EntityFrameworkCore;
using QPS.Mall.Domain.Entities;

namespace QPS.Mall.Contracts;

public interface IMallDbContext
{
    DbSet<MallStore> MallStores { get; }

    DbSet<MallAddress> MallAddresses { get; }

    DbSet<MallCategory> MallCategories { get; }

    DbSet<MallTag> MallTags { get; }

    DbSet<MallProduct> MallProducts { get; }

    DbSet<MallSku> MallSkus { get; }

    DbSet<MallOrder> MallOrders { get; }

    DbSet<MallOrderItem> MallOrderItems { get; }

    DbSet<MallPayment> MallPayments { get; }

    DbSet<MallRefund> MallRefunds { get; }

    DbSet<MallReturn> MallReturns { get; }

    DbSet<MallShipment> MallShipments { get; }

    DbSet<MallFreightTemplate> MallFreightTemplates { get; }

    DbSet<MallFreightRegion> MallFreightRegions { get; }

    DbSet<MallStockLog> MallStockLogs { get; }

    DbSet<MallPromotion> MallPromotions { get; }

    DbSet<MallPromotionScope> MallPromotionScopes { get; }

    DbSet<MallPromotionUsage> MallPromotionUsages { get; }

    DbSet<MallActivity> MallActivities { get; }

    DbSet<MallActivityPromotion> MallActivityPromotions { get; }

    DbSet<MallCart> MallCarts { get; }

    DbSet<MallCartItem> MallCartItems { get; }

    DbSet<MallBanner> MallBanners { get; }

    DbSet<MallNotice> MallNotices { get; }

    DbSet<MallArticle> MallArticles { get; }

    DbSet<MallPage> MallPages { get; }

    DbSet<MallMember> MallMembers { get; }

    DbSet<MallMemberLevel> MallMemberLevels { get; }

    DbSet<MallMemberPoint> MallMemberPoints { get; }

    DbSet<MallCoupon> MallCoupons { get; }

    DbSet<MallMemberCoupon> MallMemberCoupons { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
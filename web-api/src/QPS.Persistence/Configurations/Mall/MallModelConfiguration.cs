using Microsoft.EntityFrameworkCore;
using QPS.Mall.Domain.Entities;

namespace QPS.Persistence.Configurations.Mall;

public static class MallModelConfiguration
{
    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MallStore>(entity =>
        {
            entity.ToTable("MallStores");
            entity.Property(store => store.Code).HasMaxLength(50).IsRequired();
            entity.Property(store => store.Name).HasMaxLength(100).IsRequired();
            entity.Property(store => store.Address).HasMaxLength(200).IsRequired();
            entity.HasIndex(store => store.Code)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
            entity.HasOne<MallFreightTemplate>()
                .WithMany()
                .HasForeignKey(store => store.DefaultFreightTemplateId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MallAddress>(entity =>
        {
            entity.ToTable("MallAddresses");
            entity.Property(a => a.ReceiverName).HasMaxLength(50).IsRequired();
            entity.Property(a => a.ReceiverPhone).HasMaxLength(30).IsRequired();
            entity.Property(a => a.ProvinceName).HasMaxLength(30).IsRequired();
            entity.Property(a => a.CityName).HasMaxLength(30).IsRequired();
            entity.Property(a => a.DistrictName).HasMaxLength(30).IsRequired();
            entity.Property(a => a.DetailAddress).HasMaxLength(200).IsRequired();
            entity.HasIndex(a => a.UserId);
            entity.HasIndex(a => new { a.UserId, a.IsDefault });
        });

        modelBuilder.Entity<MallCategory>(entity =>
        {
            entity.ToTable("MallCategories");
            entity.Property(category => category.Code).HasMaxLength(50).IsRequired();
            entity.Property(category => category.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(category => category.Code)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
        });

        modelBuilder.Entity<MallTag>(entity =>
        {
            entity.ToTable("MallTags");
            entity.Property(tag => tag.Type)
                .HasMaxLength(20)
                .IsRequired()
                .HasDefaultValue(MallTagType.Product);
            entity.Property(tag => tag.Code).HasMaxLength(50).IsRequired();
            entity.Property(tag => tag.Name).HasMaxLength(50).IsRequired();
            entity.Property(tag => tag.Color).HasMaxLength(20).IsRequired();
            entity.HasIndex(tag => new { tag.Type, tag.Code })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
            entity.HasIndex(tag => new { tag.Type, tag.Name })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
        });

        modelBuilder.Entity<MallProduct>(entity =>
        {
            entity.ToTable("MallProducts");
            entity.Property(product => product.Code).HasMaxLength(50).IsRequired();
            entity.Property(product => product.Name).HasMaxLength(100).IsRequired();
            entity.Property(product => product.Description).HasMaxLength(500);
            entity.Property(product => product.MainImageUrl).HasMaxLength(500);
            entity.Property(product => product.AlbumImages).HasColumnType("nvarchar(max)");
            entity.Property(product => product.DetailHtml).HasColumnType("nvarchar(max)");
            entity.HasIndex(product => product.Code)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
            entity.HasIndex(product => product.StoreId);
            entity.HasIndex(product => product.CategoryId);
            entity.HasOne<MallStore>()
                .WithMany()
                .HasForeignKey(product => product.StoreId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<MallCategory>()
                .WithMany()
                .HasForeignKey(product => product.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<MallFreightTemplate>()
                .WithMany()
                .HasForeignKey(product => product.FreightTemplateId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MallProductTag>(entity =>
        {
            entity.ToTable("MallProductTags");
            entity.HasKey(pt => pt.Id);
            entity.HasIndex(pt => new { pt.ProductId, pt.TagId })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
            entity.Property(pt => pt.ProductId).HasColumnName("ProductsId");
            entity.Property(pt => pt.TagId).HasColumnName("TagsId");
        });

        modelBuilder.Entity<MallProductReview>(entity =>
        {
            entity.ToTable("MallProductReviews");
            entity.Property(review => review.MemberNickname).HasMaxLength(50).IsRequired();
            entity.Property(review => review.MemberLevelName).HasMaxLength(50).IsRequired();
            entity.Property(review => review.Content).HasMaxLength(500).IsRequired();
            entity.Property(review => review.SpecText).HasMaxLength(100).IsRequired();
            entity.Property(review => review.Tags).HasMaxLength(200);
            entity.Property(review => review.Images).HasMaxLength(1000).IsRequired().HasDefaultValue(string.Empty);
            entity.Property(review => review.Rating).IsRequired();
            entity.Property(review => review.IsFeatured).IsRequired().HasDefaultValue(false);
            entity.Property(review => review.SortOrder).IsRequired().HasDefaultValue(0);
            entity.Property(review => review.ReviewTime).IsRequired();
            entity.Property(review => review.AuditStatus).IsRequired().HasDefaultValue(MallProductReview.AuditStatusApproved);
            entity.Property(review => review.ReplyContent).HasMaxLength(500).IsRequired().HasDefaultValue(string.Empty);
            entity.HasIndex(review => new { review.ProductId, review.ReviewTime });
            entity.HasIndex(review => review.OrderItemId)
                .IsUnique()
                .HasFilter("[OrderItemId] IS NOT NULL AND [IsDeleted] = 0");
        });

        modelBuilder.Entity<MallSku>(entity =>
        {
            entity.ToTable("MallSkus");
            entity.Property(sku => sku.SkuCode).HasMaxLength(50).IsRequired();
            entity.Property(sku => sku.SpecName).HasMaxLength(100).IsRequired();
            entity.Property(sku => sku.Price).HasPrecision(18, 2);
            entity.Property(sku => sku.MarketPrice).HasPrecision(18, 2);
            entity.HasIndex(sku => sku.SkuCode)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
            entity.HasIndex(sku => sku.ProductId);
            entity.HasOne<MallProduct>()
                .WithMany()
                .HasForeignKey(sku => sku.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MallOrder>(entity =>
        {
            entity.ToTable("MallOrders");
            entity.Property(order => order.OrderNo).HasMaxLength(50).IsRequired();
            entity.Property(order => order.Status).HasMaxLength(32).IsRequired();
            entity.Property(order => order.ReceiverName).HasMaxLength(50).IsRequired();
            entity.Property(order => order.ReceiverPhone).HasMaxLength(30).IsRequired();
            entity.Property(order => order.ReceiverAddress).HasMaxLength(300).IsRequired();
            entity.Property(order => order.Remark).HasMaxLength(500);
            entity.Property(order => order.GoodsAmount).HasPrecision(18, 2);
            entity.Property(order => order.FreightAmount).HasPrecision(18, 2);
            entity.Property(order => order.PayableAmount).HasPrecision(18, 2);
            entity.Property(order => order.PaidAmount).HasPrecision(18, 2);
            entity.Property(order => order.RefundedAmount).HasPrecision(18, 2);
            entity.HasIndex(order => order.OrderNo)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
            entity.HasIndex(order => order.Status);
            entity.HasIndex(order => order.MemberId);
            entity.HasOne<MallMember>()
                .WithMany()
                .HasForeignKey(order => order.MemberId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<MallAddress>()
                .WithMany()
                .HasForeignKey(order => order.AddressId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MallOrderItem>(entity =>
        {
            entity.ToTable("MallOrderItems");
            entity.Property(item => item.ProductName).HasMaxLength(100).IsRequired();
            entity.Property(item => item.SkuCode).HasMaxLength(50).IsRequired();
            entity.Property(item => item.SpecName).HasMaxLength(100).IsRequired();
            entity.Property(item => item.UnitPrice).HasPrecision(18, 2);
            entity.Property(item => item.ItemAmount).HasPrecision(18, 2);
            entity.HasIndex(item => item.OrderId);
            entity.HasOne<MallOrder>()
                .WithMany()
                .HasForeignKey(item => item.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MallPayment>(entity =>
        {
            entity.ToTable("MallPayments");
            entity.Property(payment => payment.PaymentNo).HasMaxLength(50).IsRequired();
            entity.Property(payment => payment.Method).HasMaxLength(32).IsRequired();
            entity.Property(payment => payment.Status).HasMaxLength(32).IsRequired();
            entity.Property(payment => payment.TransactionNo).HasMaxLength(64);
            entity.Property(payment => payment.FailureReason).HasMaxLength(200);
            entity.Property(payment => payment.Amount).HasPrecision(18, 2);
            entity.HasIndex(payment => payment.PaymentNo)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
            entity.HasIndex(payment => payment.OrderId)
                .IsUnique()
                .HasFilter("[Status] = 'Success' AND [IsDeleted] = 0");
            entity.HasOne<MallOrder>()
                .WithMany()
                .HasForeignKey(payment => payment.OrderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MallRefund>(entity =>
        {
            entity.ToTable("MallRefunds");
            entity.Property(refund => refund.RefundNo).HasMaxLength(50).IsRequired();
            entity.Property(refund => refund.Reason).HasMaxLength(200);
            entity.Property(refund => refund.Status).HasMaxLength(32).IsRequired();
            entity.Property(refund => refund.Amount).HasPrecision(18, 2);
            entity.HasIndex(refund => refund.RefundNo)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
            entity.HasIndex(refund => refund.OrderId);
            entity.HasOne<MallOrder>()
                .WithMany()
                .HasForeignKey(refund => refund.OrderId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<MallPayment>()
                .WithMany()
                .HasForeignKey(refund => refund.PaymentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MallReturn>(entity =>
        {
            entity.ToTable("MallReturns");
            entity.Property(r => r.ReturnNo).HasMaxLength(50).IsRequired();
            entity.Property(r => r.Status).HasMaxLength(32).IsRequired();
            entity.Property(r => r.Reason).HasMaxLength(200);
            entity.Property(r => r.ExpressCompany).HasMaxLength(50);
            entity.Property(r => r.TrackingNo).HasMaxLength(64);
            entity.Property(r => r.Amount).HasPrecision(18, 2);
            entity.HasIndex(r => r.ReturnNo)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
            entity.HasIndex(r => r.OrderId);
            entity.HasOne<MallOrder>()
                .WithMany()
                .HasForeignKey(r => r.OrderId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<MallPayment>()
                .WithMany()
                .HasForeignKey(r => r.PaymentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MallShipment>(entity =>
        {
            entity.ToTable("MallShipments");
            entity.Property(shipment => shipment.ShipmentNo).HasMaxLength(50).IsRequired();
            entity.Property(shipment => shipment.ExpressCompany).HasMaxLength(50).IsRequired();
            entity.Property(shipment => shipment.TrackingNo).HasMaxLength(64).IsRequired();
            entity.Property(shipment => shipment.Status).HasMaxLength(32).IsRequired();
            entity.Property(shipment => shipment.Remark).HasMaxLength(200);
            entity.HasIndex(shipment => shipment.ShipmentNo)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
            entity.HasIndex(shipment => shipment.OrderId).IsUnique();
            entity.HasIndex(shipment => shipment.TrackingNo)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
            entity.HasOne<MallOrder>()
                .WithMany()
                .HasForeignKey(shipment => shipment.OrderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MallStockLog>(entity =>
        {
            entity.ToTable("MallStockLogs");
            entity.Property(log => log.SkuCode).HasMaxLength(50).IsRequired();
            entity.Property(log => log.SpecName).HasMaxLength(100).IsRequired();
            entity.Property(log => log.ChangeType).HasMaxLength(32).IsRequired();
            entity.Property(log => log.RelatedOrderNo).HasMaxLength(50);
            entity.Property(log => log.Remark).HasMaxLength(200);
            entity.HasIndex(log => log.SkuId);
            entity.HasIndex(log => new { log.ChangeType, log.CreatedAt });
        });

        modelBuilder.Entity<MallFreightTemplate>(entity =>
        {
            entity.ToTable("MallFreightTemplates");
            entity.Property(t => t.Name).HasMaxLength(100).IsRequired();
            entity.Property(t => t.BillingType).HasConversion<string>().HasMaxLength(20).IsRequired();
        });

        modelBuilder.Entity<MallFreightRegion>(entity =>
        {
            entity.ToTable("MallFreightRegions");
            entity.Property(r => r.RegionName).HasMaxLength(100).IsRequired();
            entity.Property(r => r.ProvinceNames).HasMaxLength(500).IsRequired();
            entity.Property(r => r.CityNames).HasMaxLength(1000);
            entity.Property(r => r.FirstFee).HasPrecision(18, 2);
            entity.Property(r => r.StepFee).HasPrecision(18, 2);
            entity.Property(r => r.FreeShippingAmount).HasPrecision(18, 2);
            entity.HasIndex(r => r.TemplateId);
            entity.HasOne<MallFreightTemplate>()
                .WithMany()
                .HasForeignKey(r => r.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MallOrderItem>(entity =>
        {
            entity.Property(item => item.PromotionName).HasMaxLength(100);
            entity.Property(item => item.OriginalUnitPrice).HasPrecision(18, 2);
        });

        modelBuilder.Entity<MallPromotion>(entity =>
        {
            entity.ToTable("MallPromotions");
            entity.Property(p => p.Name).HasMaxLength(100).IsRequired();
            entity.Property(p => p.Description).HasMaxLength(500);
            entity.Property(p => p.TriggerType).HasMaxLength(32).IsRequired();
            entity.Property(p => p.TriggerConfigJson).HasColumnType("nvarchar(max)");
            entity.Property(p => p.ActionType).HasMaxLength(32).IsRequired();
            entity.Property(p => p.ActionConfigJson).HasColumnType("nvarchar(max)");
            entity.Property(p => p.ScopeType).HasMaxLength(20).IsRequired();
            entity.HasIndex(p => p.IsActive);
        });

        modelBuilder.Entity<MallPromotionScope>(entity =>
        {
            entity.ToTable("MallPromotionScopes");
            entity.Property(s => s.ScopeTargetType).HasMaxLength(20).IsRequired();
            entity.HasIndex(s => s.PromotionId);
        });

        modelBuilder.Entity<MallPromotionUsage>(entity =>
        {
            entity.ToTable("MallPromotionUsages");
            entity.HasIndex(u => new { u.PromotionId, u.UserId }).IsUnique();
        });

        modelBuilder.Entity<MallActivity>(entity =>
        {
            entity.ToTable("MallActivities");
            entity.Property(a => a.Name).HasMaxLength(100).IsRequired();
            entity.Property(a => a.Subtitle).HasMaxLength(100);
            entity.Property(a => a.CoverImage).HasMaxLength(500);
            entity.Property(a => a.Description).HasMaxLength(500);
            entity.Property(a => a.DetailHtml).HasColumnType("nvarchar(max)");
            entity.HasIndex(a => a.IsActive);
        });

        modelBuilder.Entity<MallActivityPromotion>(entity =>
        {
            entity.ToTable("MallActivityPromotions");
            entity.HasIndex(link => link.PromotionId);
            entity.HasIndex(link => new { link.ActivityId, link.PromotionId })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
        });

        modelBuilder.Entity<MallCart>(entity =>
        {
            entity.ToTable("MallCarts");
            entity.HasIndex(c => c.UserId).IsUnique();
        });

        modelBuilder.Entity<MallCartItem>(entity =>
        {
            entity.ToTable("MallCartItems");
            entity.HasIndex(i => new { i.CartId, i.SkuId }).IsUnique();
        });

        modelBuilder.Entity<MallBanner>(entity =>
        {
            entity.ToTable("MallBanners");
            entity.Property(b => b.Position).HasMaxLength(50).IsRequired();
            entity.Property(b => b.ImageUrl).HasMaxLength(500).IsRequired();
            entity.Property(b => b.LinkValue).HasMaxLength(500).IsRequired();
            entity.Property(b => b.Title).HasMaxLength(100);
            entity.HasIndex(b => new { b.Position, b.SortOrder }).IsUnique();
        });

        modelBuilder.Entity<MallNotice>(entity =>
        {
            entity.ToTable("MallNotices");
            entity.Property(n => n.Title).HasMaxLength(100).IsRequired();
            entity.Property(n => n.Content).HasMaxLength(500).IsRequired();
            entity.HasIndex(n => new { n.IsTop, n.SortOrder });
        });

        modelBuilder.Entity<MallArticle>(entity =>
        {
            entity.ToTable("MallArticles");
            entity.Property(a => a.Title).HasMaxLength(200).IsRequired();
            entity.Property(a => a.Summary).HasMaxLength(500);
            entity.Property(a => a.Author).HasMaxLength(50);
            entity.Property(a => a.SeoTitle).HasMaxLength(100);
            entity.Property(a => a.SeoKeywords).HasMaxLength(200);
            entity.Property(a => a.SeoDescription).HasMaxLength(300);
            entity.HasIndex(a => new { a.ArticleType, a.IsActive, a.SortOrder });
        });

        modelBuilder.Entity<MallPage>(entity =>
        {
            entity.ToTable("MallPages");
            entity.Property(p => p.PageCode).HasMaxLength(50).IsRequired();
            entity.Property(p => p.PageName).HasMaxLength(100).IsRequired();
            entity.Property(p => p.Title).HasMaxLength(100);
            entity.Property(p => p.SeoTitle).HasMaxLength(100);
            entity.Property(p => p.SeoKeywords).HasMaxLength(200);
            entity.Property(p => p.SeoDescription).HasMaxLength(300);
            entity.HasIndex(p => p.PageCode).IsUnique();
        });

        modelBuilder.Entity<MallMember>(entity =>
        {
            entity.ToTable("MallMembers");
            entity.Property(m => m.Phone).HasMaxLength(20).IsRequired();
            entity.Property(m => m.PasswordHash).HasMaxLength(200).IsRequired();
            entity.Property(m => m.Nickname).HasMaxLength(50);
            entity.Property(m => m.Avatar).HasMaxLength(500);
            entity.Property(m => m.Birthday).HasMaxLength(20);
            entity.Property(m => m.Gender).HasMaxLength(10);
            entity.HasIndex(m => m.Phone).IsUnique();
            entity.HasIndex(m => m.LevelId);
        });

        modelBuilder.Entity<MallMemberLevel>(entity =>
        {
            entity.ToTable("MallMemberLevels");
            entity.Property(l => l.Name).HasMaxLength(50).IsRequired();
            entity.HasIndex(l => l.LevelCode).IsUnique();
            entity.HasIndex(l => l.IsActive);
        });

        modelBuilder.Entity<MallMemberPoint>(entity =>
        {
            entity.ToTable("MallMemberPoints");
            entity.Property(p => p.ChangeType).HasMaxLength(32).IsRequired();
            entity.Property(p => p.RefNo).HasMaxLength(50);
            entity.Property(p => p.Remark).HasMaxLength(200);
            entity.HasIndex(p => p.MemberId);
            entity.HasIndex(p => new { p.MemberId, p.ChangeType });
        });

        modelBuilder.Entity<MallCoupon>(entity =>
        {
            entity.ToTable("MallCoupons");
            entity.Property(c => c.Code).HasMaxLength(50).IsRequired();
            entity.Property(c => c.Name).HasMaxLength(100).IsRequired();
            entity.Property(c => c.CouponType).HasMaxLength(20).IsRequired();
            entity.Property(c => c.ScopeType).HasMaxLength(20);
            entity.Property(c => c.ScopeValue).HasMaxLength(500);
            entity.HasIndex(c => c.Code).IsUnique();
            entity.HasIndex(c => c.IsActive);
        });

        modelBuilder.Entity<MallMemberCoupon>(entity =>
        {
            entity.ToTable("MallMemberCoupons");
            entity.Property(mc => mc.CouponCode).HasMaxLength(50).IsRequired();
            entity.Property(mc => mc.Status).HasMaxLength(20).IsRequired();
            entity.Property(mc => mc.UsedOrderNo).HasMaxLength(50);
            entity.HasIndex(mc => new { mc.CouponId, mc.MemberId });
            entity.HasIndex(mc => new { mc.MemberId, mc.Status });
            entity.HasIndex(mc => mc.CouponCode).IsUnique();
            entity.HasIndex(mc => mc.ExpireAt);
        });
    }
}
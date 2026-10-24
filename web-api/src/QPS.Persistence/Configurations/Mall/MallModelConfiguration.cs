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
            entity.HasIndex(product => product.CategoryId);
            entity.HasOne<MallCategory>()
                .WithMany()
                .HasForeignKey(product => product.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MallSku>(entity =>
        {
            entity.ToTable("MallSkus");
            entity.Property(sku => sku.SkuCode).HasMaxLength(50).IsRequired();
            entity.Property(sku => sku.SpecName).HasMaxLength(100).IsRequired();
            entity.Property(sku => sku.Price).HasPrecision(18, 2);
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
            entity.HasIndex(order => new { order.StoreId, order.Status });
            entity.HasOne<MallStore>()
                .WithMany()
                .HasForeignKey(order => order.StoreId)
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

        modelBuilder.Entity<MallFreightRule>(entity =>
        {
            entity.ToTable("MallFreightRules");
            entity.Property(rule => rule.FirstWeightFee).HasPrecision(18, 2);
            entity.Property(rule => rule.AdditionalWeightFee).HasPrecision(18, 2);
            entity.Property(rule => rule.FreeShippingAmount).HasPrecision(18, 2);
            entity.HasIndex(rule => rule.StoreId).IsUnique();
            entity.HasOne<MallStore>()
                .WithMany()
                .HasForeignKey(rule => rule.StoreId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

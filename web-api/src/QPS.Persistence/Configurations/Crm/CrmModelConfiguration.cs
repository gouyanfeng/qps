using Microsoft.EntityFrameworkCore;
using QPS.Crm.Domain.Entities;

namespace QPS.Persistence.Configurations.Crm;

public static class CrmModelConfiguration
{
    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CrmHerbBase>(entity =>
        {
            entity.Property(herbBase => herbBase.BaseName).HasMaxLength(200);
            entity.Property(herbBase => herbBase.SubjectName).HasMaxLength(200);
            entity.Property(herbBase => herbBase.Scale).HasPrecision(18, 2);
            entity.Property(herbBase => herbBase.Lat).HasPrecision(10, 6);
            entity.Property(herbBase => herbBase.Lng).HasPrecision(10, 6);
            entity.HasOne(herbBase => herbBase.HerbBaseSubject)
                .WithMany(subject => subject.HerbBases)
                .HasForeignKey(herbBase => herbBase.HerbBaseSubjectId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CrmHerbBaseSubject>(entity =>
        {
            entity.Property(subject => subject.SubjectName).HasMaxLength(200);
            entity.Property(subject => subject.SubjectType).HasMaxLength(32);
            entity.Property(subject => subject.Status).HasMaxLength(32);
            entity.Property(subject => subject.Grade).HasMaxLength(32);
            entity.Property(subject => subject.Scale).HasPrecision(18, 2);
            entity.Property(subject => subject.PrimaryContactName).HasMaxLength(200);
            entity.Property(subject => subject.PrimaryContactPhone).HasMaxLength(100);
            entity.HasIndex(subject => subject.SubjectName);
            entity.HasIndex(subject => new { subject.SubjectType, subject.IsDeleted });
        });

        modelBuilder.Entity<CrmHerbBaseSupply>(entity =>
        {
            entity.Property(supply => supply.ProductName).HasMaxLength(200);
            entity.Property(supply => supply.QuantityUnit).HasMaxLength(32);
            entity.Property(supply => supply.Specification).HasMaxLength(200);
            entity.Property(supply => supply.QualityRequirement).HasMaxLength(500);
            entity.Property(supply => supply.HarvestSeason).HasMaxLength(100);
            entity.Property(supply => supply.PriceUnit).HasMaxLength(32);
            entity.Property(supply => supply.SupplyCycle).HasMaxLength(100);
            entity.Property(supply => supply.Status).HasMaxLength(32);
            entity.Property(supply => supply.AvailableQuantity).HasPrecision(18, 2);
            entity.Property(supply => supply.ExpectedPrice).HasPrecision(18, 2);
            entity.HasIndex(supply => new { supply.HerbBaseId, supply.Status });
            entity.HasIndex(supply => new { supply.HerbBaseSubjectId, supply.Status });
            entity.HasIndex(supply => new { supply.ProductName, supply.Status, supply.ValidUntil });
            entity.HasOne(supply => supply.HerbBase).WithMany(herbBase => herbBase.Supplies)
                .HasForeignKey(supply => supply.HerbBaseId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CrmFollowRecord>(entity =>
        {
            entity.Property(record => record.EntityType).HasMaxLength(64);
            entity.HasIndex(record => new { record.EntityType, record.EntityId, record.CreatedAt });
        });

        modelBuilder.Entity<CrmBusinessEntityAttribute>(entity =>
        {
            entity.HasIndex(attribute => new
            {
                attribute.EntityType,
                attribute.EntityId,
                attribute.AttributeCode
            });

            entity.HasIndex(attribute => new
            {
                attribute.EntityType,
                attribute.EntityId,
                attribute.AttributeCode,
                attribute.AttributeValue
            });

        });

        modelBuilder.Entity<CrmContact>(entity =>
        {
            entity.Property(contact => contact.EntityType).HasMaxLength(64);
            entity.Property(contact => contact.ContactName).HasMaxLength(200);
            entity.Property(contact => contact.Phone).HasMaxLength(100);
            entity.Property(contact => contact.PhoneType).HasMaxLength(50);
            entity.Property(contact => contact.Wechat).HasMaxLength(100);
            entity.Property(contact => contact.RoleName).HasMaxLength(100);
            entity.Property(contact => contact.Status).HasMaxLength(50);
            entity.HasIndex(contact => new { contact.EntityType, contact.EntityId });
            entity.HasIndex(contact => new { contact.EntityType, contact.EntityId, contact.Phone });
        });

        modelBuilder.Entity<CrmVendor>(entity =>
        {
            entity.Property(vendor => vendor.VendorName).HasMaxLength(200);
            entity.Property(vendor => vendor.NormalizedVendorName).HasMaxLength(200);
            entity.Property(vendor => vendor.PriorityLevel).HasMaxLength(20);
            entity.Property(vendor => vendor.LastFollowResult).HasMaxLength(32);
            entity.HasIndex(vendor => vendor.NormalizedVendorName).IsUnique();
            entity.HasIndex(vendor => vendor.PriorityLevel);
            entity.HasIndex(vendor => vendor.LatestPurchaseTime);
            entity.HasIndex(vendor => new { vendor.OwnerUserId, vendor.NextFollowAt });
        });

        modelBuilder.Entity<CrmVendorDemand>(entity =>
        {
            entity.ToTable("CrmVendorDemands");
            entity.Property(demand => demand.DemandNo).HasMaxLength(64);
            entity.Property(demand => demand.DemandName).HasMaxLength(500);
            entity.Property(demand => demand.Status).HasMaxLength(32);
            entity.Property(demand => demand.SourceType).HasMaxLength(32);
            entity.Property(demand => demand.SourceUrl).HasMaxLength(500);
            entity.HasIndex(demand => demand.DemandNo).IsUnique();
            entity.HasIndex(demand => new { demand.VendorId, demand.Status, demand.DemandAt });
            entity.HasIndex(demand => demand.ContactId);
            entity.HasOne(demand => demand.Vendor).WithMany(vendor => vendor.VendorDemands).HasForeignKey(demand => demand.VendorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(demand => demand.Contact).WithMany().HasForeignKey(demand => demand.ContactId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<CrmVendorDemandItem>(entity =>
        {
            entity.ToTable("CrmVendorDemandItems");
            entity.Property(item => item.ProductName).HasMaxLength(200);
            entity.Property(item => item.Quantity).HasPrecision(18, 2);
            entity.Property(item => item.TargetPrice).HasPrecision(18, 2);
            entity.HasIndex(item => new { item.VendorDemandId, item.SortOrder });
            entity.HasOne(item => item.VendorDemand).WithMany(demand => demand.Items).HasForeignKey(item => item.VendorDemandId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CrmMatchOpportunity>(entity =>
        {
            entity.ToTable("CrmMatchOpportunities");
            entity.Property(opportunity => opportunity.DemandProductName).HasMaxLength(200);
            entity.Property(opportunity => opportunity.DemandQuantity).HasPrecision(18, 2);
            entity.Property(opportunity => opportunity.QuantityUnit).HasMaxLength(32);
            entity.Property(opportunity => opportunity.Specification).HasMaxLength(200);
            entity.Property(opportunity => opportunity.QualityRequirement).HasMaxLength(500);
            entity.Property(opportunity => opportunity.DeliveryAddress).HasMaxLength(500);
            entity.Property(opportunity => opportunity.Status).HasMaxLength(32);
            entity.Property(opportunity => opportunity.Stage).HasMaxLength(32);
            entity.Property(opportunity => opportunity.NextAction).HasMaxLength(500);
            entity.Property(opportunity => opportunity.NextActionRemark).HasMaxLength(500);
            entity.Property(opportunity => opportunity.CloseResult).HasMaxLength(32);
            entity.Property(opportunity => opportunity.ClosedReason).HasMaxLength(500);
            entity.HasIndex(opportunity => opportunity.DemandItemId)
                .IsUnique()
                .HasFilter("[Status] IN (N'推进中', N'暂停') AND [IsDeleted] = 0");
            entity.HasIndex(opportunity => new { opportunity.OwnerUserId, opportunity.NextActionAt });
            entity.HasOne<CrmVendorDemandItem>()
                .WithMany()
                .HasForeignKey(opportunity => opportunity.DemandItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CrmMatchCandidate>(entity =>
        {
            entity.ToTable("CrmMatchCandidates");
            entity.Property(candidate => candidate.SubjectName).HasMaxLength(200);
            entity.Property(candidate => candidate.HerbBaseName).HasMaxLength(200);
            entity.Property(candidate => candidate.ProductName).HasMaxLength(200);
            entity.Property(candidate => candidate.AvailableQuantity).HasPrecision(18, 2);
            entity.Property(candidate => candidate.QuantityUnit).HasMaxLength(32);
            entity.Property(candidate => candidate.Specification).HasMaxLength(200);
            entity.Property(candidate => candidate.QualityRequirement).HasMaxLength(500);
            entity.Property(candidate => candidate.ExpectedPrice).HasPrecision(18, 2);
            entity.Property(candidate => candidate.PriceUnit).HasMaxLength(32);
            entity.Property(candidate => candidate.SupplyCycle).HasMaxLength(100);
            entity.Property(candidate => candidate.Status).HasMaxLength(32);
            entity.Property(candidate => candidate.EliminationReason).HasMaxLength(500);
            entity.HasIndex(candidate => new { candidate.MatchOpportunityId, candidate.HerbBaseSupplyId }).IsUnique();
            entity.HasOne<CrmMatchOpportunity>()
                .WithMany()
                .HasForeignKey(candidate => candidate.MatchOpportunityId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<CrmHerbBaseSupply>()
                .WithMany()
                .HasForeignKey(candidate => candidate.HerbBaseSupplyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CrmBusinessActivity>(entity =>
        {
            entity.ToTable("CrmBusinessActivities");
            entity.Property(activity => activity.EntityType).HasMaxLength(64);
            entity.Property(activity => activity.ActionType).HasMaxLength(32);
            entity.Property(activity => activity.Content).HasMaxLength(1000);
            entity.HasIndex(activity => new { activity.EntityType, activity.EntityId, activity.CreatedAt });
        });

        modelBuilder.Entity<CrmTransferRecord>(entity =>
        {
            entity.Property(record => record.ActionType).HasMaxLength(16).HasDefaultValue(CrmTransferActionType.Entry);
            entity.Property(record => record.EntityType).HasMaxLength(64);
            entity.HasIndex(record => new { record.EntityType, record.EntityId, record.CreatedAt });
        });
    }
}

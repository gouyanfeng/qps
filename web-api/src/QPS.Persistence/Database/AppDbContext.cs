using Microsoft.EntityFrameworkCore;
using QPS.Crm.Contracts;
using QPS.Mall.Contracts;
using QPS.Mall.Domain.Entities;
using QPS.Persistence.Configurations.Crm;
using QPS.Persistence.Configurations.Mall;
using QPS.Persistence.Configurations.System;
using QPS.Shared.Common;
using QPS.Shared.Contracts;
using QPS.System.Contracts;
using QPS.System.Domain.Entities;
using QPS.Crm.Domain.Entities;
using System.Linq.Expressions;
using System.Text.Json;

namespace QPS.Persistence.Database;

public class AppDbContext : DbContext, ISystemDbContext, ICrmDbContext, IMallDbContext, IUnitOfWork
{
    private readonly ICurrentUserService _currentUserService;

    public DbSet<SystemUser> SystemUsers { get; set; }
    public DbSet<SystemRole> SystemRoles { get; set; }
    public DbSet<SystemPermission> SystemPermissions { get; set; }
    public DbSet<SystemUserRole> SystemUserRoles { get; set; }
    public DbSet<SystemRolePermission> SystemRolePermissions { get; set; }
    public DbSet<SystemDataDictionary> SystemDataDictionaries { get; set; }
    public DbSet<SystemChinaRegion> SystemChinaRegions { get; set; }
    public DbSet<SystemErrorLog> SystemErrorLogs { get; set; }
    public DbSet<SystemOperationLog> SystemOperationLogs { get; set; }

    public DbSet<CrmHerbBaseSubject> CrmHerbBaseSubjects { get; set; }
    public DbSet<CrmHerbBase> CrmHerbBases { get; set; }
    public DbSet<CrmHerbBaseSupply> CrmHerbBaseSupplies { get; set; }
    public DbSet<CrmContact> CrmContacts { get; set; }
    public DbSet<CrmFollowRecord> CrmFollowRecords { get; set; }
    public DbSet<CrmBusinessEntityAttribute> CrmBusinessEntityAttributes { get; set; }
    public DbSet<CrmTransferRecord> CrmTransferRecords { get; set; }
    public DbSet<CrmVendor> CrmVendors { get; set; }
    public DbSet<CrmVendorDemand> CrmVendorDemands { get; set; }
    public DbSet<CrmVendorDemandItem> CrmVendorDemandItems { get; set; }
    public DbSet<CrmMatchOpportunity> CrmMatchOpportunities { get; set; }
    public DbSet<CrmMatchCandidate> CrmMatchCandidates { get; set; }
    public DbSet<CrmBusinessActivity> CrmBusinessActivities { get; set; }

    public DbSet<MallStore> MallStores { get; set; }
    public DbSet<MallAddress> MallAddresses { get; set; }
    public DbSet<MallCategory> MallCategories { get; set; }
    public DbSet<MallTag> MallTags { get; set; }
    public DbSet<MallProduct> MallProducts { get; set; }
    public DbSet<MallProductTag> MallProductTags { get; set; }
    public DbSet<MallProductReview> MallProductReviews { get; set; }
    public DbSet<MallSku> MallSkus { get; set; }
    public DbSet<MallOrder> MallOrders { get; set; }
    public DbSet<MallOrderItem> MallOrderItems { get; set; }
    public DbSet<MallPayment> MallPayments { get; set; }
    public DbSet<MallRefund> MallRefunds { get; set; }
    public DbSet<MallReturn> MallReturns { get; set; }
    public DbSet<MallShipment> MallShipments { get; set; }
    public DbSet<MallFreightTemplate> MallFreightTemplates { get; set; }
    public DbSet<MallFreightRegion> MallFreightRegions { get; set; }
    public DbSet<MallStockLog> MallStockLogs { get; set; }
    public DbSet<MallPromotion> MallPromotions { get; set; }
    public DbSet<MallPromotionScope> MallPromotionScopes { get; set; }
    public DbSet<MallPromotionUsage> MallPromotionUsages { get; set; }

    public DbSet<MallActivity> MallActivities { get; set; }

    public DbSet<MallActivityPromotion> MallActivityPromotions { get; set; }
    public DbSet<MallCart> MallCarts { get; set; }
    public DbSet<MallCartItem> MallCartItems { get; set; }
    public DbSet<MallBanner> MallBanners { get; set; }
    public DbSet<MallNotice> MallNotices { get; set; }
    public DbSet<MallArticle> MallArticles { get; set; }
    public DbSet<MallPage> MallPages { get; set; }
    public DbSet<MallMember> MallMembers { get; set; }
    public DbSet<MallMemberLevel> MallMemberLevels { get; set; }
    public DbSet<MallMemberPoint> MallMemberPoints { get; set; }
    public DbSet<MallCoupon> MallCoupons { get; set; }
    public DbSet<MallMemberCoupon> MallMemberCoupons { get; set; }

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        ICurrentUserService currentUserService) : base(options)
    {
        _currentUserService = currentUserService;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        SystemModelConfiguration.Apply(modelBuilder);
        CrmModelConfiguration.Apply(modelBuilder);
        MallModelConfiguration.Apply(modelBuilder);

        ApplySoftDeleteQueryFilters(modelBuilder);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (Database.CurrentTransaction == null)
        {
            await Database.BeginTransactionAsync(cancellationToken);
        }
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (Database.CurrentTransaction != null)
        {
            await Database.CurrentTransaction.CommitAsync(cancellationToken);
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (Database.CurrentTransaction != null)
        {
            await Database.CurrentTransaction.RollbackAsync(cancellationToken);
        }
    }

    public override int SaveChanges()
    {
        AddOperationLogs();
        SetAuditFields();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        AddOperationLogs();
        SetAuditFields();

        return await base.SaveChangesAsync(cancellationToken);
    }

    private void SetAuditFields()
    {
        var currentUser = _currentUserService.UserId ?? "System";
        var now = DateTime.Now;

        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is BaseEntity && (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var entity = (BaseEntity)entry.Entity;

            if (entry.State == EntityState.Added)
            {
                entity.CreatedAt = now;
                entity.CreatedBy = currentUser;
            }

            entity.UpdatedAt = now;
            entity.UpdatedBy = currentUser;
        }
    }

    private void AddOperationLogs()
    {
        var currentUser = _currentUserService.Username
            ?? _currentUserService.UserId
            ?? "System";

        var logs = ChangeTracker.Entries<BaseEntity>()
            .Where(entry => entry.Entity is not SystemOperationLog)
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(entry => CreateOperationLog(entry, currentUser))
            .Where(log => log is not null)
            .Cast<SystemOperationLog>()
            .ToList();

        if (logs.Count == 0)
        {
            return;
        }

        SystemOperationLogs.AddRange(logs);
    }

    private static SystemOperationLog? CreateOperationLog(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<BaseEntity> entry, string currentUser)
    {
        var actionType = GetActionType(entry);
        var changeJson = BuildChangeJson(entry);

        if (actionType == "Update" && changeJson == "{}")
        {
            return null;
        }

        return new SystemOperationLog(
            actionType: actionType,
            entityType: entry.Entity.GetType().Name,
            entityId: entry.Entity.Id.ToString(),
            operatorName: currentUser,
            requestPath: string.Empty,
            ipAddress: string.Empty,
            changeJson: changeJson);
    }

    private static string GetActionType(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        if (entry.State == EntityState.Added)
        {
            return "Create";
        }

        if (entry.State == EntityState.Deleted || IsSoftDelete(entry))
        {
            return "Delete";
        }

        return "Update";
    }

    private static bool IsSoftDelete(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        return entry.State == EntityState.Modified
            && entry.Properties.Any(property =>
                property.Metadata.Name == nameof(BaseEntity.IsDeleted)
                && property.IsModified
                && property.CurrentValue is true);
    }

    private static string BuildChangeJson(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var changes = new Dictionary<string, object?>();

        foreach (var property in entry.Properties)
        {
            if (property.Metadata.IsShadowProperty())
            {
                continue;
            }

            if (entry.State == EntityState.Added)
            {
                changes[property.Metadata.Name] = new
                {
                    old = (object?)null,
                    @new = property.CurrentValue
                };
                continue;
            }

            if (entry.State == EntityState.Deleted)
            {
                changes[property.Metadata.Name] = new
                {
                    old = property.OriginalValue,
                    @new = (object?)null
                };
                continue;
            }

            if (!property.IsModified || Equals(property.OriginalValue, property.CurrentValue))
            {
                continue;
            }

            changes[property.Metadata.Name] = new
            {
                old = property.OriginalValue,
                @new = property.CurrentValue
            };
        }

        return JsonSerializer.Serialize(changes, new JsonSerializerOptions
        {
            WriteIndented = false
        });
    }

    private static void ApplySoftDeleteQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(entityType => typeof(BaseEntity).IsAssignableFrom(entityType.ClrType)))
        {
            var parameter = Expression.Parameter(entityType.ClrType, "entity");
            var isDeleted = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
            var filter = Expression.Lambda(Expression.Equal(isDeleted, Expression.Constant(false)), parameter);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
        }
    }
}
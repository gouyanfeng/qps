using Microsoft.EntityFrameworkCore;
using QPS.System.Domain.Entities;

namespace QPS.Persistence.Configurations.System;

public static class SystemModelConfiguration
{
    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SystemChinaRegion>(entity =>
        {
            entity.HasIndex(region => region.Code);
            entity.HasIndex(region => new { region.Level, region.ParentCode });
            entity.Property(region => region.Code).HasMaxLength(20);
            entity.Property(region => region.Name).HasMaxLength(100);
            entity.Property(region => region.FullName).HasMaxLength(200);
            entity.Property(region => region.ParentCode).HasMaxLength(20);
            entity.Property(region => region.ProvinceCode).HasMaxLength(20);
            entity.Property(region => region.CityCode).HasMaxLength(20);
        });

        modelBuilder.Entity<SystemPermission>(entity =>
        {
            entity.Property(permission => permission.PermissionType).HasMaxLength(20);
        });

        modelBuilder.Entity<SystemOperationLog>(entity =>
        {
            entity.Property(log => log.ActionType).HasMaxLength(64);
            entity.Property(log => log.EntityType).HasMaxLength(128);
            entity.Property(log => log.EntityId).HasMaxLength(64);
            entity.Property(log => log.OperatorName).HasMaxLength(100);
            entity.Property(log => log.RequestPath).HasMaxLength(500);
            entity.Property(log => log.IpAddress).HasMaxLength(64);
            entity.HasIndex(log => log.CreatedAt);
            entity.HasIndex(log => new { log.EntityType, log.EntityId });
            entity.HasIndex(log => log.ActionType);
        });

        modelBuilder.Entity<SystemMessage>(entity =>
        {
            entity.Property(message => message.Title).HasMaxLength(200);
            entity.HasIndex(message => new { message.MessageType, message.IsPublished });
            entity.HasIndex(message => message.CreatedAt);
        });

        modelBuilder.Entity<SystemMessageRecipient>(entity =>
        {
            // 同一用户对同一消息只允许一条生效状态行；软删行不参与唯一约束（符合"唯一索引带 IsDeleted"约定）
            entity.HasIndex(recipient => new { recipient.MessageId, recipient.UserId, recipient.IsDeleted })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
            entity.HasIndex(recipient => new { recipient.UserId, recipient.IsRead });
            entity.HasIndex(recipient => recipient.UserId);
        });
    }
}
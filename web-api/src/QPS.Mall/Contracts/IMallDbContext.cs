using Microsoft.EntityFrameworkCore;
using QPS.Mall.Domain.Entities;

namespace QPS.Mall.Contracts;

public interface IMallDbContext
{
    DbSet<MallStore> MallStores { get; }

    DbSet<MallCategory> MallCategories { get; }

    DbSet<MallProduct> MallProducts { get; }

    DbSet<MallSku> MallSkus { get; }

    DbSet<MallOrder> MallOrders { get; }

    DbSet<MallOrderItem> MallOrderItems { get; }

    DbSet<MallPayment> MallPayments { get; }

    DbSet<MallRefund> MallRefunds { get; }

    DbSet<MallShipment> MallShipments { get; }

    DbSet<MallFreightRule> MallFreightRules { get; }

    DbSet<MallStockLog> MallStockLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

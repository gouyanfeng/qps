using Microsoft.EntityFrameworkCore;
using QPS.Crm.Domain.Entities;
using QPS.System.Domain.Entities;

namespace QPS.Crm.Contracts;

public interface ICrmDbContext
{
    DbSet<SystemUser> SystemUsers { get; }
    DbSet<SystemDataDictionary> SystemDataDictionaries { get; }
    DbSet<CrmHerbBaseSubject> CrmHerbBaseSubjects { get; }
    DbSet<CrmHerbBase> CrmHerbBases { get; }
    DbSet<CrmHerbBaseSupply> CrmHerbBaseSupplies { get; }
    DbSet<CrmContact> CrmContacts { get; }
    DbSet<CrmFollowRecord> CrmFollowRecords { get; }
    DbSet<CrmBusinessEntityAttribute> CrmBusinessEntityAttributes { get; }
    DbSet<CrmTransferRecord> CrmTransferRecords { get; }
    DbSet<CrmVendor> CrmVendors { get; }
    DbSet<CrmVendorDemand> CrmVendorDemands { get; }
    DbSet<CrmVendorDemandItem> CrmVendorDemandItems { get; }
    DbSet<CrmMatchOpportunity> CrmMatchOpportunities { get; }
    DbSet<CrmMatchCandidate> CrmMatchCandidates { get; }
    DbSet<CrmBusinessActivity> CrmBusinessActivities { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

using Microsoft.EntityFrameworkCore;
using QPS.Application.Interfaces;
using QPS.Domain.Entities.Crm;

namespace QPS.Application.Features.Crm.CrmHerbBaseSubjects;

public static class CrmHerbBaseSubjectScoreInputBuilder
{
    public static async Task<CrmHerbBaseSubjectScoreInput?> BuildAsync(IDbContext dbContext, Guid subjectId, CancellationToken cancellationToken)
    {
        var subject = await dbContext.CrmHerbBaseSubjects
            .FirstOrDefaultAsync(item => item.Id == subjectId, cancellationToken);
        if (subject == null)
        {
            return null;
        }

        var bases = await dbContext.CrmHerbBases
            .Where(item => item.HerbBaseSubjectId == subjectId && !item.IsDeleted)
            .Select(item => new
            {
                item.Id,
                item.Scale,
                item.Province,
                item.City,
                item.Area,
                item.Address,
                item.SourcePlatform,
                item.Remark
            })
            .ToListAsync(cancellationToken);

        var baseIds = bases.Select(item => item.Id).ToList();
        var supplyProductCount = baseIds.Count == 0
            ? 0
            : await dbContext.CrmHerbBaseSupplies
                .Where(item =>
                    !item.IsDeleted &&
                    baseIds.Contains(item.HerbBaseId) &&
                    !string.IsNullOrWhiteSpace(item.ProductName))
                .Select(item => item.ProductName)
                .Distinct()
                .CountAsync(cancellationToken);

        var contacts = await dbContext.CrmContacts
            .Where(item =>
                !item.IsDeleted &&
                item.EntityType == CrmCodes.HerbBaseSubjectEntityType &&
                item.EntityId == subjectId)
            .Select(item => new
            {
                item.Phone,
                item.Status
            })
            .ToListAsync(cancellationToken);
        var validContacts = contacts.Where(item => item.Status != "无效").ToList();

        var input = new CrmHerbBaseSubjectScoreInput
        {
            Status = subject.Status,
            Scale = subject.Scale ?? bases.Sum(item => item.Scale ?? 0),
            BaseCount = bases.Count,
            SupplyProductCount = supplyProductCount,
            HasPrimaryContactName = !string.IsNullOrWhiteSpace(subject.PrimaryContactName),
            HasPrimaryContactPhone = !string.IsNullOrWhiteSpace(subject.PrimaryContactPhone),
            HasValidContact = validContacts.Count > 0,
            HasValidContactPhone = validContacts.Any(item => !string.IsNullOrWhiteSpace(item.Phone)),
            LastFollowAt = subject.LastFollowAt,
            LastFollowResult = subject.LastFollowResult ?? string.Empty,
            HasRegion = bases.Any(item =>
                !string.IsNullOrWhiteSpace(item.Province) ||
                !string.IsNullOrWhiteSpace(item.City) ||
                !string.IsNullOrWhiteSpace(item.Area)),
            HasAddress = bases.Any(item => !string.IsNullOrWhiteSpace(item.Address)),
            SourcePlatforms = bases
                .Select(item => item.SourcePlatform)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct()
                .ToList(),
            HasRemark = !string.IsNullOrWhiteSpace(subject.Remark) ||
                bases.Any(item => !string.IsNullOrWhiteSpace(item.Remark))
        };

        return input;
    }
}

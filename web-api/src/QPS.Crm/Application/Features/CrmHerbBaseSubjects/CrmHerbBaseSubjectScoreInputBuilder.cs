using QPS.Crm.Contracts;
using Microsoft.EntityFrameworkCore;
using QPS.Shared.Contracts;
using QPS.Crm.Domain.Entities;

namespace QPS.Crm.Application.Features.CrmHerbBaseSubjects;

public static class CrmHerbBaseSubjectScoreInputBuilder
{
    public static async Task<CrmHerbBaseSubjectScoreInput?> BuildAsync(ICrmDbContext dbContext, Guid subjectId, CancellationToken cancellationToken)
    {
        var subject = await dbContext.CrmHerbBaseSubjects
            .FirstOrDefaultAsync(item => item.Id == subjectId, cancellationToken);
        if (subject == null)
        {
            return null;
        }

        var bases = await dbContext.CrmHerbBases
            .Where(item => item.HerbBaseSubjectId == subjectId)
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
        // 品类数按属性表口径统计（基地级 CRM_HERB_BASE_PRODUCT），与列表品类列保持一致
        var supplyProductCount = baseIds.Count == 0
            ? 0
            : await dbContext.CrmBusinessEntityAttributes
                .Where(item =>
                    item.EntityType == CrmCodes.HerbBaseEntityType &&
                    item.AttributeCode == CrmCodes.AttributeCodes.HerbBaseProduct &&
                    baseIds.Contains(item.EntityId) &&
                    !string.IsNullOrWhiteSpace(item.AttributeValue))
                .Select(item => item.AttributeValue)
                .Distinct()
                .CountAsync(cancellationToken);

        var contacts = await dbContext.CrmContacts
            .Where(item =>
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

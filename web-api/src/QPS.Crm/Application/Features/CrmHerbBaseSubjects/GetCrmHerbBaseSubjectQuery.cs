using QPS.Crm.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Crm.Contracts.DTOs;
using QPS.Crm.Contracts.DTOs.CrmHerbBaseSubjects;
using QPS.Crm.Contracts.DTOs.CrmContacts;
using QPS.Crm.Contracts.DTOs.CrmFollowRecords;
using QPS.Crm.Contracts.DTOs.CrmHerbBases;
using QPS.Crm.Application.Features.CrmTransfers;
using QPS.Shared.Contracts;
using QPS.Crm.Application.Features.CrmHerbBaseSupplies;
using QPS.Crm.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Crm.Application.Features.CrmHerbBaseSubjects;

public class GetCrmHerbBaseSubjectQuery : IRequest<CrmHerbBaseSubjectDetailDto>
{
    public Guid Id { get; set; }
}

public class GetCrmHerbBaseSubjectHandler : IRequestHandler<GetCrmHerbBaseSubjectQuery, CrmHerbBaseSubjectDetailDto>
{
    private readonly ICrmDbContext _dbContext;

    public GetCrmHerbBaseSubjectHandler(ICrmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CrmHerbBaseSubjectDetailDto> Handle(
        GetCrmHerbBaseSubjectQuery request,
        CancellationToken cancellationToken)
    {
        var subject = await GetSubjectAsync(request.Id, cancellationToken);
        await FillOwnerNameAsync(subject, cancellationToken);
        subject.HerbBases = await GetHerbBasesAsync(subject.Id, cancellationToken);
        await FillSuppliesAsync(subject.HerbBases, cancellationToken);
        await FillBaseProductNamesAsync(subject.HerbBases, cancellationToken);
        FillBaseSummary(subject);
        subject.Contacts = await GetContactsAsync(subject.Id, cancellationToken);
        subject.FollowRecords = await GetFollowRecordsAsync(subject.Id, cancellationToken);
        subject.Deals = await GetDealsAsync(subject.Id, cancellationToken);
        subject.TransferRecords = await CrmTransferRecordQuery.GetAsync(
            _dbContext,
            CrmCodes.HerbBaseSubjectEntityType,
            subject.Id,
            cancellationToken);
        return subject;
    }

    private async Task FillOwnerNameAsync(CrmHerbBaseSubjectDetailDto subject, CancellationToken cancellationToken)
    {
        if (!subject.OwnerUserId.HasValue)
        {
            return;
        }

        var users = await CrmUserNames.GetAsync(_dbContext, [subject.OwnerUserId.Value], cancellationToken);
        if (users.TryGetValue(subject.OwnerUserId.Value, out var owner))
        {
            subject.OwnerUserName = owner.RealName != string.Empty ? owner.RealName : owner.Username;
        }
    }

    private async Task<CrmHerbBaseSubjectDetailDto> GetSubjectAsync(Guid subjectId, CancellationToken cancellationToken)
    {
        var subject = await (
            from item in _dbContext.CrmHerbBaseSubjects
            where item.Id == subjectId
            select new CrmHerbBaseSubjectDetailDto
            {
                Id = item.Id,
                SubjectName = item.SubjectName,
                SubjectType = item.SubjectType,
                OwnerUserId = item.OwnerUserId,
                Status = item.Status,
                Grade = item.Grade,
                Score = item.Score,
                PrimaryContactName = item.PrimaryContactName,
                PrimaryContactPhone = item.PrimaryContactPhone,
                LastFollowAt = item.LastFollowAt,
                LastFollowResult = item.LastFollowResult,
                NextFollowAt = item.NextFollowAt,
                TotalScale = item.Scale ?? 0,
                Remark = item.Remark,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt
            }).FirstOrDefaultAsync(cancellationToken);

        return subject ?? throw new BusinessException(404, "药材基地主体不存在");
    }

    private async Task<List<CrmHerbBaseDto>> GetHerbBasesAsync(Guid subjectId, CancellationToken cancellationToken)
    {
        return await _dbContext.CrmHerbBases
            .Where(herbBase => herbBase.HerbBaseSubjectId == subjectId)
            .OrderByDescending(herbBase => herbBase.UpdatedAt)
            .Select(herbBase => new CrmHerbBaseDto
            {
                Id = herbBase.Id,
                HerbBaseSubjectId = herbBase.HerbBaseSubjectId,
                BaseName = herbBase.BaseName,
                SubjectName = herbBase.SubjectName,
                Grade = herbBase.Grade,
                Score = herbBase.Score,
                Scale = herbBase.Scale,
                Province = herbBase.Province,
                City = herbBase.City,
                Area = herbBase.Area,
                Address = herbBase.Address,
                Lat = herbBase.Lat,
                Lng = herbBase.Lng,
                SourcePlatform = herbBase.SourcePlatform,
                SourceId = herbBase.SourceId,
                Remark = herbBase.Remark,
                CreatedAt = herbBase.CreatedAt,
                UpdatedAt = herbBase.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }

    private async Task FillSuppliesAsync(List<CrmHerbBaseDto> herbBases, CancellationToken cancellationToken)
    {
        var herbBaseIds = herbBases.Select(herbBase => herbBase.Id).ToList();
        if (herbBaseIds.Count == 0) return;
        var supplies = await _dbContext.CrmHerbBaseSupplies.Where(item => herbBaseIds.Contains(item.HerbBaseId))
            .OrderByDescending(item => item.UpdatedAt).ToListAsync(cancellationToken);
        var grouped = supplies.GroupBy(item => item.HerbBaseId).ToDictionary(group => group.Key, group => group.Select(CrmHerbBaseSupplyMapper.ToDto).ToList());
        foreach (var herbBase in herbBases) herbBase.Supplies = grouped.GetValueOrDefault(herbBase.Id, []);
    }

    /// <summary>
    /// 填充基地品类，唯一来源是属性表（基地级 CRM_HERB_BASE_PRODUCT）。
    /// </summary>
    private async Task FillBaseProductNamesAsync(List<CrmHerbBaseDto> herbBases, CancellationToken cancellationToken)
    {
        var herbBaseIds = herbBases.Select(herbBase => herbBase.Id).ToList();
        if (herbBaseIds.Count == 0) return;
        var attributes = await _dbContext.CrmBusinessEntityAttributes
            .Where(attribute =>
                attribute.EntityType == CrmCodes.HerbBaseEntityType &&
                attribute.AttributeCode == CrmCodes.AttributeCodes.HerbBaseProduct &&
                herbBaseIds.Contains(attribute.EntityId))
            .Select(attribute => new { attribute.EntityId, attribute.AttributeValue, attribute.SortOrder })
            .ToListAsync(cancellationToken);
        var grouped = attributes
            .GroupBy(attribute => attribute.EntityId)
            .ToDictionary(
                group => group.Key,
                group => group.OrderBy(attribute => attribute.SortOrder)
                    .Select(attribute => attribute.AttributeValue)
                    .ToList());
        foreach (var herbBase in herbBases)
        {
            herbBase.ProductNames = grouped.GetValueOrDefault(herbBase.Id, []);
        }
    }

    private static void FillBaseSummary(CrmHerbBaseSubjectDetailDto subject)
    {
        subject.BaseCount = subject.HerbBases.Count;
        // 品类以属性表为准；属性缺失时回退到供应表，兼容只录了供应的老数据
        subject.ProductName = subject.HerbBases
            .SelectMany(herbBase => herbBase.ProductNames.Count > 0
                ? herbBase.ProductNames
                : herbBase.Supplies.Select(supply => supply.ProductName))
            .Distinct()
            .ToList();
        subject.Regions = subject.HerbBases
            .Select(herbBase => string.Join(' ', new[] { herbBase.Province, herbBase.City, herbBase.Area }.Where(value => !string.IsNullOrWhiteSpace(value))))
            .Where(region => !string.IsNullOrWhiteSpace(region))
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// 该主体作为供货方的历史成交：中选/发起供应 item -> 撮合机会 -> 采购方（实时取原明细）。
    /// </summary>
    private async Task<List<CrmSubjectDealDto>> GetDealsAsync(Guid subjectId, CancellationToken cancellationToken)
    {
        return await (
            from item in _dbContext.CrmMatchOpportunityItems
            join supply in _dbContext.CrmHerbBaseSupplies on item.EntityId equals supply.Id
            join opportunity in _dbContext.CrmMatchOpportunities on item.OpportunityId equals opportunity.Id
            join herbBase in _dbContext.CrmHerbBases on supply.HerbBaseId equals herbBase.Id
            where item.EntityType == CrmMatchOpportunityItem.EntityTypeSupply
                  && supply.HerbBaseSubjectId == subjectId
                  && opportunity.Status == CrmMatchOpportunity.Closed
                  && opportunity.CloseResult == CrmMatchOpportunity.Matched
                  && (item.Status == CrmMatchOpportunityItem.Selected || item.Role == CrmMatchOpportunityItem.Initiator)
                  && !item.IsDeleted
                  && !opportunity.IsDeleted
            orderby opportunity.UpdatedAt descending
            select new CrmSubjectDealDto
            {
                Id = opportunity.Id,
                VendorName = (
                    from demandItemRow in _dbContext.CrmMatchOpportunityItems
                    where demandItemRow.OpportunityId == opportunity.Id &&
                        demandItemRow.EntityType == CrmMatchOpportunityItem.EntityTypeDemand
                    from di in _dbContext.CrmVendorDemandItems.Where(d => d.Id == demandItemRow.EntityId).Take(1)
                    from dm in _dbContext.CrmVendorDemands.Where(d => d.Id == di.VendorDemandId).Take(1)
                    from v in _dbContext.CrmVendors.Where(v => v.Id == dm.VendorId).Take(1)
                    select v.VendorName).FirstOrDefault() ?? string.Empty,
                ProductName = supply.ProductName,
                BaseName = herbBase.BaseName,
                CloseResult = opportunity.CloseResult,
                ClosedReason = opportunity.ClosedReason,
                ClosedAt = opportunity.UpdatedAt
            }).ToListAsync(cancellationToken);
    }

    private async Task<List<CrmContactDto>> GetContactsAsync(Guid subjectId, CancellationToken cancellationToken)
    {
        return await _dbContext.CrmContacts
            .Where(contact => contact.EntityType == CrmCodes.HerbBaseSubjectEntityType && contact.EntityId == subjectId)
            .OrderByDescending(contact => contact.IsPrimary)
            .ThenBy(contact => contact.CreatedAt)
            .Select(contact => new CrmContactDto
            {
                Id = contact.Id,
                EntityType = contact.EntityType,
                EntityId = contact.EntityId,
                ContactName = contact.ContactName,
                Phone = contact.Phone,
                PhoneType = contact.PhoneType,
                Wechat = contact.Wechat,
                RoleName = contact.RoleName,
                IsPrimary = contact.IsPrimary,
                Status = contact.Status,
                Remark = contact.Remark,
                CreatedAt = contact.CreatedAt,
                UpdatedAt = contact.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<List<CrmFollowRecordDto>> GetFollowRecordsAsync(Guid subjectId, CancellationToken cancellationToken)
    {
        return await _dbContext.CrmFollowRecords
            .Include(record => record.Contact)
            .Where(record => record.EntityType == CrmCodes.HerbBaseSubjectEntityType && record.EntityId == subjectId)
            .OrderByDescending(record => record.CreatedAt)
            .Select(record => new CrmFollowRecordDto
            {
                Id = record.Id,
                EntityType = record.EntityType,
                EntityId = record.EntityId,
                ContactId = record.ContactId,
                ContactName = record.Contact == null ? null : record.Contact.ContactName,
                FollowType = record.FollowType,
                FollowResult = record.FollowResult,
                IntentLevel = record.IntentLevel,
                Content = record.Content,
                NextFollowAt = record.NextFollowAt,
                OperatorUserId = record.OperatorUserId,
                CreatedAt = record.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

}

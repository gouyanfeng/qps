using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QPS.Crm.Contracts;
using QPS.Crm.Domain.Entities;
using QPS.Persistence.Database;
using QPS.Shared.Common;

namespace QPS.Persistence.Seeds;

/// <summary>
/// CRM 种子数据：与商城种子完全同一策略 —— 数据放在 CrmSeed 目录下的 JSON 文件，
/// 以嵌入资源随程序集发布，不依赖源码目录，开发与生产环境行为一致。
/// 仅在 CRM 为空（首次运行）时写入，已有数据直接跳过，不覆盖运营维护的真实数据。
/// </summary>
public static class CrmMockDataSeeder
{
    private const string SeedResourcePrefix = "QPS.Persistence.Seeds.CrmSeed.";
    private const string SeedCreatedBy = "seed-test-data";
    private const string SeedSourcePlatform = "种子导入";
    private const string SeedAttributeRemark = "种子数据初始化";

    // 与 InitialCreate 中的 System 基线用户保持一致：admin 为系统管理员、user 为普通用户
    private static readonly Guid AdminUserId = Guid.Parse("7BC372F3-C80F-40FD-9019-F9C8D67343FF");
    private static readonly Guid NormalUserId = Guid.Parse("6F913E3C-0693-47C8-B858-99235A79DCE5");

    private static readonly DateTime Today = DateTime.Today;

    public static async Task SeedAsync(AppDbContext db, ILogger logger, CancellationToken ct = default)
    {
        // 首次运行判定：已有基地主体或厂商说明 CRM 已初始化过，直接跳过，避免覆盖运营维护的数据
        var initialized = await db.CrmHerbBaseSubjects.IgnoreQueryFilters().AnyAsync(ct)
            || await db.CrmVendors.IgnoreQueryFilters().AnyAsync(ct);
        if (initialized)
        {
            logger.LogInformation("CRM 已有数据，跳过种子数据初始化。");
            return;
        }

        var subjectsJson = ReadSeedJson("subjects.json");
        var basesJson = ReadSeedJson("bases.json");
        var suppliesJson = ReadSeedJson("supplies.json");
        var vendorsJson = ReadSeedJson("vendors.json");
        var demandsJson = ReadSeedJson("vendor-demands.json");
        var contactsJson = ReadSeedJson("contacts.json");
        var followRecordsJson = ReadSeedJson("follow-records.json");

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var subjectsByKey = new Dictionary<string, CrmHerbBaseSubject>(StringComparer.Ordinal);
        var subjects = ParseSubjects(db, subjectsJson, subjectsByKey);

        var basesByKey = new Dictionary<string, CrmHerbBase>(StringComparer.Ordinal);
        var baseProductsByBaseId = new Dictionary<Guid, string>();
        var bases = ParseBases(db, basesJson, subjectsByKey, basesByKey, baseProductsByBaseId);

        var supplies = ParseSupplies(db, suppliesJson, basesByKey);

        var vendorsByKey = new Dictionary<string, CrmVendor>(StringComparer.Ordinal);
        var vendorProductsByVendorId = new Dictionary<Guid, List<string>>();
        var vendors = ParseVendors(db, vendorsJson, vendorsByKey, vendorProductsByVendorId);

        var demands = ParseVendorDemands(db, demandsJson, vendorsByKey);

        var contactsByKey = new Dictionary<string, CrmContact>(StringComparer.Ordinal);
        var contacts = ParseContacts(db, contactsJson, subjectsByKey, vendorsByKey, contactsByKey);

        var followRecords = ParseFollowRecords(db, followRecordsJson, subjectsByKey, vendorsByKey, contactsByKey);

        var transferRecords = BuildTransferRecords(subjects, vendors);
        db.CrmTransferRecords.AddRange(transferRecords);

        var attributes = BuildAttributes(bases, baseProductsByBaseId, vendors, vendorProductsByVendorId);
        db.CrmBusinessEntityAttributes.AddRange(attributes);

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        logger.LogInformation(
            "✅ 已写入 CRM 种子数据：{SubjectCount} 个基地主体、{BaseCount} 个基地、{SupplyCount} 条供应、{VendorCount} 家厂商、{DemandCount} 个采购列表、{ContactCount} 位联系人、{FollowCount} 条沟通记录、{TransferCount} 条流转记录、{AttributeCount} 条品类属性",
            subjects.Count,
            bases.Count,
            supplies.Count,
            vendors.Count,
            demands.Count,
            contacts.Count,
            followRecords.Count,
            transferRecords.Count,
            attributes.Count);
    }

    private static string ReadSeedJson(string fileName)
    {
        var assembly = typeof(CrmMockDataSeeder).Assembly;
        var resourceName = $"{SeedResourcePrefix}{fileName}";
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException($"CRM 种子数据资源缺失: {resourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// 基地主体：按「一县一品」的真实中药材产区构造，覆盖高/中/低三档等级与不同跟进状态。
    /// </summary>
    private static List<CrmHerbBaseSubject> ParseSubjects(
        AppDbContext db,
        string json,
        IDictionary<string, CrmHerbBaseSubject> subjectsByKey)
    {
        using var document = JsonDocument.Parse(json);
        var subjects = new List<CrmHerbBaseSubject>();

        foreach (var item in document.RootElement.GetProperty("subjects").EnumerateArray())
        {
            var subjectName = item.GetProperty("subjectName").GetString()!;
            var subject = CrmHerbBaseSubject.Create(
                subjectName,
                subjectName,
                item.GetProperty("subjectType").GetString()!,
                ResolveUserId(item, "owner"),
                item.GetProperty("status").GetString()!,
                item.GetProperty("grade").GetString()!,
                item.GetProperty("score").GetInt32(),
                item.GetProperty("remark").GetString() ?? string.Empty,
                GetOptionalDecimal(item, "scale"));

            if (item.TryGetProperty("primaryContactName", out var contactName) &&
                item.TryGetProperty("primaryContactPhone", out var contactPhone))
            {
                subject.UpdatePrimaryContact(contactName.GetString() ?? string.Empty, contactPhone.GetString() ?? string.Empty);
            }

            if (item.TryGetProperty("lastFollowDaysAgo", out var lastFollowDays))
            {
                subject.UpdateFollowSummary(
                    Today.AddDays(-lastFollowDays.GetInt32()),
                    item.GetProperty("lastFollowResult").GetString()!,
                    GetNextFollowAt(item));
            }

            Stamp(subject);
            db.CrmHerbBaseSubjects.Add(subject);
            subjects.Add(subject);
            subjectsByKey[item.GetProperty("key").GetString()!] = subject;
        }

        return subjects;
    }

    /// <summary>
    /// 药材基地：挂在主体下，保留产区、规模、经纬度与负责人，便于地图与列表展示。
    /// </summary>
    private static List<CrmHerbBase> ParseBases(
        AppDbContext db,
        string json,
        IReadOnlyDictionary<string, CrmHerbBaseSubject> subjectsByKey,
        IDictionary<string, CrmHerbBase> basesByKey,
        IDictionary<Guid, string> baseProductsByBaseId)
    {
        using var document = JsonDocument.Parse(json);
        var bases = new List<CrmHerbBase>();

        foreach (var item in document.RootElement.GetProperty("bases").EnumerateArray())
        {
            var subjectKey = item.GetProperty("subjectKey").GetString()!;
            var subject = subjectsByKey[subjectKey];
            var remark = item.GetProperty("remark").GetString() ?? string.Empty;

            var herbBase = CrmHerbBase.Create(
                item.GetProperty("baseName").GetString()!,
                item.GetProperty("grade").GetString()!,
                item.GetProperty("score").GetInt32(),
                item.GetProperty("province").GetString()!,
                item.GetProperty("city").GetString()!,
                item.GetProperty("area").GetString()!,
                item.GetProperty("address").GetString()!,
                GetOptionalDecimal(item, "lat"),
                GetOptionalDecimal(item, "lng"),
                SeedSourcePlatform,
                null,
                subject.OwnerUserId,
                remark,
                subject.SubjectName ?? string.Empty,
                GetOptionalDecimal(item, "scale"));
            herbBase.SetHerbBaseSubject(subject.Id);
            herbBase.UpdatePrimaryContact(
                subject.PrimaryContactName ?? string.Empty,
                subject.PrimaryContactPhone ?? string.Empty);
            herbBase.UpdateStatus(subject.Status, remark);

            Stamp(herbBase);
            db.CrmHerbBases.Add(herbBase);
            bases.Add(herbBase);
            basesByKey[item.GetProperty("key").GetString()!] = herbBase;
            baseProductsByBaseId[herbBase.Id] = item.GetProperty("productName").GetString()!;
        }

        return bases;
    }

    /// <summary>
    /// 供应信息：大部分为「有效」供撮合直接使用，少量「待确认」「暂停」用于演示状态流转。
    /// </summary>
    private static List<CrmHerbBaseSupply> ParseSupplies(
        AppDbContext db,
        string json,
        IReadOnlyDictionary<string, CrmHerbBase> basesByKey)
    {
        using var document = JsonDocument.Parse(json);
        var supplies = new List<CrmHerbBaseSupply>();

        foreach (var item in document.RootElement.GetProperty("supplies").EnumerateArray())
        {
            var herbBase = basesByKey[item.GetProperty("baseKey").GetString()!];
            var supply = CrmHerbBaseSupply.Create(
                herbBase.Id,
                herbBase.HerbBaseSubjectId,
                item.GetProperty("productName").GetString()!,
                GetOptionalDecimal(item, "availableQuantity"),
                item.GetProperty("quantityUnit").GetString()!,
                item.GetProperty("specification").GetString()!,
                item.GetProperty("qualityRequirement").GetString()!,
                item.GetProperty("harvestSeason").GetString()!,
                GetOptionalDecimal(item, "expectedPrice"),
                item.GetProperty("priceUnit").GetString()!,
                item.GetProperty("supplyCycle").GetString()!,
                Today.AddDays(-item.GetProperty("confirmedDaysAgo").GetInt32()),
                Today.AddDays(item.GetProperty("validUntilDaysAhead").GetInt32()),
                item.GetProperty("remark").GetString() ?? string.Empty);

            var status = item.GetProperty("status").GetString()!;
            if (status != CrmHerbBaseSupply.Pending)
            {
                supply.ChangeStatus(status);
            }

            Stamp(supply);
            db.CrmHerbBaseSupplies.Add(supply);
            supplies.Add(supply);
        }

        return supplies;
    }

    /// <summary>
    /// 厂商：覆盖高/中/低优先级与不同跟进状态，品类属性随厂商一并写入。
    /// </summary>
    private static List<CrmVendor> ParseVendors(
        AppDbContext db,
        string json,
        IDictionary<string, CrmVendor> vendorsByKey,
        IDictionary<Guid, List<string>> vendorProductsByVendorId)
    {
        using var document = JsonDocument.Parse(json);
        var vendors = new List<CrmVendor>();

        foreach (var item in document.RootElement.GetProperty("vendors").EnumerateArray())
        {
            var vendorName = item.GetProperty("vendorName").GetString()!;
            var vendor = CrmVendor.Create(
                vendorName,
                NormalizeVendorName(vendorName),
                item.GetProperty("priorityLevel").GetString()!,
                item.GetProperty("remark").GetString() ?? string.Empty,
                ResolveUserId(item, "owner"));

            if (item.TryGetProperty("lastFollowDaysAgo", out var lastFollowDays))
            {
                vendor.UpdateFollowSummary(
                    Today.AddDays(-lastFollowDays.GetInt32()),
                    item.GetProperty("lastFollowResult").GetString()!,
                    GetNextFollowAt(item));
            }

            Stamp(vendor);
            db.CrmVendors.Add(vendor);
            vendors.Add(vendor);

            var key = item.GetProperty("key").GetString()!;
            vendorsByKey[key] = vendor;
            vendorProductsByVendorId[vendor.Id] = item.TryGetProperty("products", out var products)
                ? products.EnumerateArray().Select(product => product.GetString()!).ToList()
                : new List<string>();
        }

        return vendors;
    }

    private static string NormalizeVendorName(string vendorName) =>
        string.Concat(vendorName.Where(c => !char.IsWhiteSpace(c))).ToUpperInvariant();

    /// <summary>
    /// 采购列表：每个厂商 1-2 个采购需求，明细品类均取自 CRM 品类字典。
    /// </summary>
    private static List<CrmVendorDemand> ParseVendorDemands(
        AppDbContext db,
        string json,
        IReadOnlyDictionary<string, CrmVendor> vendorsByKey)
    {
        using var document = JsonDocument.Parse(json);
        var demands = new List<CrmVendorDemand>();

        foreach (var item in document.RootElement.GetProperty("demands").EnumerateArray())
        {
            var vendor = vendorsByKey[item.GetProperty("vendorKey").GetString()!];
            var demandAt = Today.AddDays(-item.GetProperty("demandDaysAgo").GetInt32());
            var expectedDeliveryAt = Today.AddDays(item.GetProperty("expectedDeliveryDaysAhead").GetInt32());

            var demandItems = item.GetProperty("items")
                .EnumerateArray()
                .Select(demandItem => new CrmVendorDemandItem(
                    demandItem.GetProperty("productName").GetString()!,
                    GetOptionalDecimal(demandItem, "quantity"),
                    demandItem.GetProperty("quantityUnit").GetString()!,
                    demandItem.GetProperty("specification").GetString()!,
                    demandItem.GetProperty("qualityRequirement").GetString()!,
                    GetOptionalDecimal(demandItem, "targetPrice"),
                    demandItem.GetProperty("priceUnit").GetString()!,
                    expectedDeliveryAt,
                    demandItem.GetProperty("remark").GetString() ?? string.Empty,
                    demandItem.GetProperty("sortOrder").GetInt32()))
                .ToList();

            foreach (var demandItem in demandItems)
            {
                Stamp(demandItem);
            }

            var demand = CrmVendorDemand.Create(
                vendor.Id,
                item.GetProperty("demandNo").GetString()!,
                item.GetProperty("demandName").GetString()!,
                demandAt,
                "人工录入",
                null,
                expectedDeliveryAt,
                item.GetProperty("receivingAddress").GetString()!,
                string.Empty,
                item.GetProperty("remark").GetString() ?? string.Empty,
                demandItems);
            Stamp(demand);
            demand.CreatedAt = demandAt;
            demand.UpdatedAt = demandAt;

            db.CrmVendorDemands.Add(demand);
            demands.Add(demand);
            vendor.UpdateLatestPurchaseDemandSummary(demandAt, demand.DemandName);
        }

        return demands;
    }

    /// <summary>
    /// 联系人：主体负责人与厂商采购对接人各一份，含主联系人标记与验证状态。
    /// </summary>
    private static List<CrmContact> ParseContacts(
        AppDbContext db,
        string json,
        IReadOnlyDictionary<string, CrmHerbBaseSubject> subjectsByKey,
        IReadOnlyDictionary<string, CrmVendor> vendorsByKey,
        IDictionary<string, CrmContact> contactsByKey)
    {
        using var document = JsonDocument.Parse(json);
        var contacts = new List<CrmContact>();

        foreach (var item in document.RootElement.GetProperty("contacts").EnumerateArray())
        {
            var entityType = item.GetProperty("entityType").GetString()!;
            var entityKey = item.GetProperty("entityKey").GetString()!;
            var entityId = ResolveEntityId(entityType, entityKey, subjectsByKey, vendorsByKey);

            var contact = CrmContact.Create(
                ResolveEntityType(entityType),
                entityId,
                item.GetProperty("contactName").GetString()!,
                item.GetProperty("phone").GetString()!,
                item.GetProperty("phoneType").GetString()!,
                item.GetProperty("wechat").GetString() ?? string.Empty,
                item.GetProperty("roleName").GetString() ?? string.Empty,
                item.GetProperty("isPrimary").GetBoolean(),
                item.GetProperty("remark").GetString() ?? string.Empty);
            contact.MarkStatus(
                item.GetProperty("status").GetString()!,
                item.GetProperty("remark").GetString() ?? string.Empty);

            Stamp(contact);
            db.CrmContacts.Add(contact);
            contacts.Add(contact);
            contactsByKey[item.GetProperty("key").GetString()!] = contact;
        }

        return contacts;
    }

    /// <summary>
    /// 沟通记录：主体与厂商各若干条，含下次跟进时间，便于「我的待办」列表展示。
    /// </summary>
    private static List<CrmFollowRecord> ParseFollowRecords(
        AppDbContext db,
        string json,
        IReadOnlyDictionary<string, CrmHerbBaseSubject> subjectsByKey,
        IReadOnlyDictionary<string, CrmVendor> vendorsByKey,
        IReadOnlyDictionary<string, CrmContact> contactsByKey)
    {
        using var document = JsonDocument.Parse(json);
        var records = new List<CrmFollowRecord>();

        foreach (var item in document.RootElement.GetProperty("followRecords").EnumerateArray())
        {
            var entityType = item.GetProperty("entityType").GetString()!;
            var entityKey = item.GetProperty("entityKey").GetString()!;
            var entityId = ResolveEntityId(entityType, entityKey, subjectsByKey, vendorsByKey);
            var contactId = contactsByKey[item.GetProperty("contactKey").GetString()!].Id;

            var record = CrmFollowRecord.Create(
                ResolveEntityType(entityType),
                entityId,
                contactId,
                item.GetProperty("followType").GetString()!,
                item.GetProperty("followResult").GetString()!,
                item.GetProperty("intentLevel").GetString()!,
                item.GetProperty("content").GetString()!,
                GetNextFollowAt(item),
                ResolveUserId(item, "operator"));

            Stamp(record);
            db.CrmFollowRecords.Add(record);
            records.Add(record);
        }

        return records;
    }

    /// <summary>
    /// 流转记录：主体与厂商各写一条「录入」流水，便于流转记录页有初始数据。
    /// </summary>
    private static List<CrmTransferRecord> BuildTransferRecords(
        IReadOnlyList<CrmHerbBaseSubject> subjects,
        IReadOnlyList<CrmVendor> vendors)
    {
        var records = new List<CrmTransferRecord>();
        foreach (var subject in subjects)
        {
            records.Add(CrmTransferRecord.CreateEntry(
                CrmCodes.HerbBaseSubjectEntityType, subject.Id, subject.OwnerUserId, AdminUserId, SeedAttributeRemark));
        }

        foreach (var vendor in vendors)
        {
            records.Add(CrmTransferRecord.CreateEntry(
                CrmCodes.VendorEntityType, vendor.Id, vendor.OwnerUserId, AdminUserId, SeedAttributeRemark));
        }

        foreach (var record in records)
        {
            Stamp(record);
        }

        return records;
    }

    /// <summary>
    /// 品类属性：基地品类挂在基地上、厂商品类挂在厂商上，列表/详情统一读这里。
    /// </summary>
    private static List<CrmBusinessEntityAttribute> BuildAttributes(
        IReadOnlyList<CrmHerbBase> bases,
        IReadOnlyDictionary<Guid, string> baseProductsByBaseId,
        IReadOnlyList<CrmVendor> vendors,
        IReadOnlyDictionary<Guid, List<string>> vendorProductsByVendorId)
    {
        var attributes = new List<CrmBusinessEntityAttribute>();

        foreach (var herbBase in bases)
        {
            attributes.Add(new CrmBusinessEntityAttribute(
                CrmCodes.HerbBaseEntityType, herbBase.Id, CrmCodes.AttributeCodes.HerbBaseProduct,
                baseProductsByBaseId[herbBase.Id], 0, SeedAttributeRemark));
        }

        foreach (var vendor in vendors)
        {
            var products = vendorProductsByVendorId[vendor.Id];
            for (var index = 0; index < products.Count; index++)
            {
                attributes.Add(new CrmBusinessEntityAttribute(
                    CrmCodes.VendorEntityType, vendor.Id, CrmCodes.AttributeCodes.VendorProduct,
                    products[index], index, SeedAttributeRemark));
            }
        }

        foreach (var attribute in attributes)
        {
            Stamp(attribute);
        }

        return attributes;
    }

    private static Guid ResolveEntityId(
        string entityType,
        string entityKey,
        IReadOnlyDictionary<string, CrmHerbBaseSubject> subjectsByKey,
        IReadOnlyDictionary<string, CrmVendor> vendorsByKey) =>
        entityType == "vendor"
            ? vendorsByKey[entityKey].Id
            : subjectsByKey[entityKey].Id;

    private static string ResolveEntityType(string entityType) =>
        entityType == "vendor"
            ? CrmCodes.VendorEntityType
            : CrmCodes.HerbBaseSubjectEntityType;

    private static Guid? ResolveUserId(JsonElement item, string propertyName)
    {
        if (!item.TryGetProperty(propertyName, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return value.GetString() switch
        {
            "admin" => AdminUserId,
            "user" => NormalUserId,
            _ => null
        };
    }

    private static DateTime? GetNextFollowAt(JsonElement item) =>
        item.TryGetProperty("nextFollowDaysAhead", out var days)
            ? Today.AddDays(days.GetInt32())
            : null;

    private static decimal? GetOptionalDecimal(JsonElement item, string propertyName)
    {
        if (!item.TryGetProperty(propertyName, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return value.GetDecimal();
    }

    private static T Stamp<T>(T entity) where T : BaseEntity
    {
        entity.CreatedBy = SeedCreatedBy;
        entity.UpdatedBy = SeedCreatedBy;
        return entity;
    }
}
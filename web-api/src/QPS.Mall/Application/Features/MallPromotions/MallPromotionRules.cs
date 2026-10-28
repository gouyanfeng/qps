using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallPromotions;

internal static class MallPromotionRules
{
    public const int NameMax = 50;
    public const int DescriptionMax = 200;
    public const int TriggerTypeMax = 30;
    public const int ActionTypeMax = 30;
    public const int ScopeTypeMax = 30;

    private static readonly HashSet<string> ValidTriggerTypes = new()
    {
        "FullAmount", "BuyProduct", "FirstOrder", "OrderCount", "FlashSale"
    };

    private static readonly HashSet<string> ValidActionTypes = new()
    {
        "Gift", "DiscountAmount", "DiscountRate", "FreightFree", "SecondHalfPrice", "FlashSalePrice"
    };

    private static readonly HashSet<string> ValidScopeTypes = new()
    {
        "Global", "Product", "Category", "Sku"
    };

    public static void ValidateCreate(MallPromotionCreateRequest req)
    {
        Validate(req.Name, req.Description, req.TriggerType, req.TriggerConfigJson, req.ActionType,
            req.ActionConfigJson, req.ScopeType, req.Scopes, req.Priority, req.MaxUsagePerUser,
            req.MaxUsagePerOrder, req.StartDate, req.EndDate);
    }

    public static void ValidateUpdate(MallPromotionUpdateRequest req)
    {
        Validate(req.Name, req.Description, req.TriggerType, req.TriggerConfigJson, req.ActionType,
            req.ActionConfigJson, req.ScopeType, req.Scopes, req.Priority, req.MaxUsagePerUser,
            req.MaxUsagePerOrder, req.StartDate, req.EndDate);
    }

    private static void Validate(
        string name,
        string? description,
        string triggerType,
        string triggerConfigJson,
        string actionType,
        string actionConfigJson,
        string scopeType,
        IReadOnlyCollection<MallPromotionScopeRequest> scopes,
        int priority,
        int maxUsagePerUser,
        int maxUsagePerOrder,
        DateTime? startDate,
        DateTime? endDate)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new BusinessException(400, "促销名称不能为空");
        if (name.Length > NameMax) throw new BusinessException(400, $"促销名称不能超过 {NameMax} 字");

        if (!string.IsNullOrEmpty(description) && description.Length > DescriptionMax)
            throw new BusinessException(400, $"描述不能超过 {DescriptionMax} 字");

        if (string.IsNullOrWhiteSpace(triggerType)) throw new BusinessException(400, "触发方式不能为空");
        if (!ValidTriggerTypes.Contains(triggerType)) throw new BusinessException(400, $"无效的触发方式：{triggerType}");

        if (string.IsNullOrWhiteSpace(actionType)) throw new BusinessException(400, "优惠方式不能为空");
        if (!ValidActionTypes.Contains(actionType)) throw new BusinessException(400, $"无效的优惠方式：{actionType}");

        if (string.IsNullOrWhiteSpace(scopeType)) throw new BusinessException(400, "适用范围不能为空");
        if (!ValidScopeTypes.Contains(scopeType)) throw new BusinessException(400, $"无效的适用范围：{scopeType}");

        if (priority < 0 || priority > 999) throw new BusinessException(400, "优先级需在 0-999 之间");
        if (maxUsagePerUser < 0) throw new BusinessException(400, "每人可用次数不能为负");
        if (maxUsagePerOrder < 0) throw new BusinessException(400, "每单可用次数不能为负");

        if (startDate.HasValue && endDate.HasValue && startDate > endDate)
            throw new BusinessException(400, "开始时间不能晚于结束时间");

        ValidateConfiguration(triggerType, triggerConfigJson, actionType, actionConfigJson);
        ValidateScopes(scopeType, scopes);
    }

    private static void ValidateConfiguration(
        string triggerType,
        string triggerConfigJson,
        string actionType,
        string actionConfigJson)
    {
        try
        {
            using var trigger = JsonDocument.Parse(triggerConfigJson);
            using var action = JsonDocument.Parse(actionConfigJson);
            var triggerConfig = trigger.RootElement;
            var actionConfig = action.RootElement;

            if (triggerType == "FullAmount" &&
                (!triggerConfig.TryGetProperty("minAmount", out var minAmount) || minAmount.GetDecimal() <= 0))
                throw new BusinessException(400, "满额促销门槛必须大于 0");

            if (triggerType == "BuyProduct")
            {
                if (!triggerConfig.TryGetProperty("productIds", out var productIds) ||
                    productIds.ValueKind != JsonValueKind.Array ||
                    productIds.GetArrayLength() == 0 ||
                    productIds.EnumerateArray().Any(item =>
                        !Guid.TryParse(item.GetString(), out var productId) || productId == Guid.Empty))
                    throw new BusinessException(400, "买赠促销必须指定购买商品");
                if (triggerConfig.TryGetProperty("minQuantity", out var quantity) && quantity.GetInt32() < 1)
                    throw new BusinessException(400, "购买数量必须大于 0");
            }

            if (actionType == "DiscountAmount" &&
                (!actionConfig.TryGetProperty("amount", out var amount) || amount.GetDecimal() <= 0))
                throw new BusinessException(400, "减免金额必须大于 0");

            if (actionType == "DiscountRate" &&
                (!actionConfig.TryGetProperty("rate", out var rate) || rate.GetDecimal() <= 0 || rate.GetDecimal() > 1))
                throw new BusinessException(400, "折扣比例必须在 0 到 1 之间");

            if (actionType == "Gift")
            {
                if (!actionConfig.TryGetProperty("skuId", out var skuId) ||
                    !Guid.TryParse(skuId.GetString(), out _))
                    throw new BusinessException(400, "买赠促销必须指定有效的赠送规格");
                if (!actionConfig.TryGetProperty("quantity", out var giftQuantity) || giftQuantity.GetInt32() < 1)
                    throw new BusinessException(400, "赠送数量必须大于 0");
            }
        }
        catch (JsonException)
        {
            throw new BusinessException(400, "促销规则配置不是有效 JSON");
        }
        catch (InvalidOperationException)
        {
            throw new BusinessException(400, "促销规则配置格式无效");
        }
        catch (FormatException)
        {
            throw new BusinessException(400, "促销规则数值格式无效");
        }
    }

    private static void ValidateScopes(string scopeType, IReadOnlyCollection<MallPromotionScopeRequest> scopes)
    {
        if (scopeType == "Global") return;
        if (scopes.Count == 0)
            throw new BusinessException(400, "指定适用范围时必须选择适用对象");
        if (scopes.Any(scope => scope.ScopeTargetId == Guid.Empty || scope.ScopeTargetType != scopeType))
            throw new BusinessException(400, "促销适用对象与适用范围不匹配");
        if (scopes.Select(scope => scope.ScopeTargetId).Distinct().Count() != scopes.Count)
            throw new BusinessException(400, "促销适用对象不能重复");
    }

    public static async Task ValidateScopeTargetsAsync(
        IMallDbContext dbContext,
        string scopeType,
        IReadOnlyCollection<MallPromotionScopeRequest> scopes,
        CancellationToken cancellationToken)
    {
        if (scopeType == "Global") return;

        var targetIds = scopes.Select(scope => scope.ScopeTargetId).ToArray();
        var targetCount = scopeType switch
        {
            "Category" => await dbContext.MallCategories
                .CountAsync(category => targetIds.Contains(category.Id) && category.IsActive, cancellationToken),
            "Product" => await dbContext.MallProducts
                .CountAsync(product => targetIds.Contains(product.Id) && product.IsActive, cancellationToken),
            "Sku" => await (
                from sku in dbContext.MallSkus
                join product in dbContext.MallProducts on sku.ProductId equals product.Id
                where targetIds.Contains(sku.Id) && sku.IsActive && product.IsActive
                select sku.Id)
                .CountAsync(cancellationToken),
            _ => 0
        };
        if (targetCount != targetIds.Length)
            throw new BusinessException(400, "部分促销适用对象不存在或已停用");
    }
}
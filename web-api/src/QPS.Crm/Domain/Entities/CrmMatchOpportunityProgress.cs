namespace QPS.Crm.Domain.Entities;

public static class CrmMatchOpportunityStage
{
    public const string MatchingVerification = "匹配核实";
    public const string BusinessNegotiation = "商务沟通";
    public const string IntentConfirmation = "意向确认";

    public static IReadOnlyList<string> All { get; } =
    [
        MatchingVerification,
        BusinessNegotiation,
        IntentConfirmation
    ];

    public static bool IsValid(string value) => All.Contains(value);

    public static bool CanTransit(string currentStage, string targetStage)
    {
        var currentIndex = Array.IndexOf(All.ToArray(), currentStage);
        var targetIndex = Array.IndexOf(All.ToArray(), targetStage);
        return currentIndex >= 0 && targetIndex >= 0 && Math.Abs(currentIndex - targetIndex) <= 1;
    }
}

public static class CrmMatchOpportunityNextAction
{
    public const string VerifyQuantity = "核实数量";
    public const string VerifySpecificationAndQuality = "核实规格与质量";
    public const string VerifyDeliveryDate = "核实交期";
    public const string ArrangeSampleOrInspection = "安排样品或看货";
    public const string DiscussPrice = "沟通价格";
    public const string DiscussPaymentAndTransport = "沟通付款与运输";
    public const string AwaitResponse = "等待对方反馈";
    public const string ConfirmCooperationIntent = "确认合作意向";
    public const string ConfirmSelectedSupply = "确认中选供应";
    public const string AddCandidate = "补充候选";
    public const string Other = "其他";

    public static IReadOnlyList<string> All { get; } =
    [
        VerifyQuantity,
        VerifySpecificationAndQuality,
        VerifyDeliveryDate,
        ArrangeSampleOrInspection,
        DiscussPrice,
        DiscussPaymentAndTransport,
        AwaitResponse,
        ConfirmCooperationIntent,
        ConfirmSelectedSupply,
        AddCandidate,
        Other
    ];

    public static IReadOnlyList<string> GetAvailableActions(string stage) => stage switch
    {
        CrmMatchOpportunityStage.MatchingVerification =>
        [
            VerifyQuantity,
            VerifySpecificationAndQuality,
            VerifyDeliveryDate,
            ArrangeSampleOrInspection,
            AddCandidate,
            Other
        ],
        CrmMatchOpportunityStage.BusinessNegotiation =>
        [
            DiscussPrice,
            DiscussPaymentAndTransport,
            AwaitResponse,
            AddCandidate,
            Other
        ],
        CrmMatchOpportunityStage.IntentConfirmation =>
        [
            ConfirmCooperationIntent,
            ConfirmSelectedSupply,
            AddCandidate,
            Other
        ],
        _ => []
    };

    public static void Validate(string action, string? remark)
    {
        if (!All.Contains(action))
        {
            throw new InvalidOperationException("下一步动作无效");
        }

        if (action == Other && string.IsNullOrWhiteSpace(remark))
        {
            throw new InvalidOperationException("选择其他待办时必须填写说明");
        }
    }

    public static void Validate(string stage, string action, string? remark)
    {
        Validate(action, remark);
        if (!GetAvailableActions(stage).Contains(action))
        {
            throw new InvalidOperationException("下一步动作不适用于当前撮合阶段");
        }
    }
}

namespace QPS.Mall.Domain.Services;

public interface ITriggerStrategy
{
    string TriggerType { get; }

    bool IsMatch(OrderContext ctx, System.Text.Json.JsonElement config);
}
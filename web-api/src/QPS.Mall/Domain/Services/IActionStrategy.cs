namespace QPS.Mall.Domain.Services;

public interface IActionStrategy
{
    string ActionType { get; }

    void Execute(ApplyContext ctx, System.Text.Json.JsonElement config, MatchedPromotion matchedPromotion);
}
using DotnetClaude.Core.Models;

namespace DotnetClaude.Core.Services;

public static class CostCalculator
{
    public static decimal EstimateCostUsd(
        ChatModelInfo model,
        int inputTokens,
        int outputTokens,
        int cacheCreationInputTokens,
        int cacheReadInputTokens)
    {
        var inputCost = inputTokens / 1_000_000m * model.InputPricePerMillionTokens;
        var outputCost = outputTokens / 1_000_000m * model.OutputPricePerMillionTokens;
        var cacheWriteCost = cacheCreationInputTokens / 1_000_000m * model.CacheWritePricePerMillionTokens;
        var cacheReadCost = cacheReadInputTokens / 1_000_000m * model.CacheReadPricePerMillionTokens;

        return inputCost + outputCost + cacheWriteCost + cacheReadCost;
    }
}

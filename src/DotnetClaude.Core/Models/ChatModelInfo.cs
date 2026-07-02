using AnthropicModel = Anthropic.Models.Messages.Model;

namespace DotnetClaude.Core.Models;

/// <summary>
/// Describes a Claude model the user can pick in the UI, including the pricing
/// needed to estimate query cost from token usage.
/// </summary>
public sealed record ChatModelInfo(
    string WireId,
    AnthropicModel ApiModel,
    string DisplayName,
    string Description,
    int ContextWindowTokens,
    decimal InputPricePerMillionTokens,
    decimal OutputPricePerMillionTokens)
{
    /// <summary>Cache writes (5m TTL) are billed at ~1.25x the input price.</summary>
    public decimal CacheWritePricePerMillionTokens => InputPricePerMillionTokens * 1.25m;

    /// <summary>Cache reads are billed at ~0.1x the input price.</summary>
    public decimal CacheReadPricePerMillionTokens => InputPricePerMillionTokens * 0.1m;
}

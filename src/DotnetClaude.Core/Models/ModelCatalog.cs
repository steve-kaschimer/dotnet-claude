using AnthropicModel = Anthropic.Models.Messages.Model;

namespace DotnetClaude.Core.Models;

/// <summary>
/// The set of Claude models exposed in the UI's model switcher, with the pricing
/// used to estimate cost per query. Update this list as Anthropic ships new models.
/// </summary>
public static class ModelCatalog
{
    public static readonly IReadOnlyList<ChatModelInfo> Models =
    [
        new ChatModelInfo(
            WireId: "claude-opus-4-8",
            ApiModel: AnthropicModel.ClaudeOpus4_8,
            DisplayName: "Claude Opus 4.8",
            Description: "Most capable Opus-tier model. Best for hard reasoning and long-horizon agentic work.",
            ContextWindowTokens: 1_000_000,
            InputPricePerMillionTokens: 5.00m,
            OutputPricePerMillionTokens: 25.00m),

        new ChatModelInfo(
            WireId: "claude-sonnet-5",
            ApiModel: AnthropicModel.ClaudeSonnet5,
            DisplayName: "Claude Sonnet 5",
            Description: "Best balance of speed and intelligence. Near-Opus quality on coding and agentic work.",
            ContextWindowTokens: 1_000_000,
            InputPricePerMillionTokens: 3.00m,
            OutputPricePerMillionTokens: 15.00m),

        new ChatModelInfo(
            WireId: "claude-haiku-4-5",
            ApiModel: AnthropicModel.ClaudeHaiku4_5,
            DisplayName: "Claude Haiku 4.5",
            Description: "Fastest and most cost-effective. Best for simple, latency-sensitive tasks.",
            ContextWindowTokens: 200_000,
            InputPricePerMillionTokens: 1.00m,
            OutputPricePerMillionTokens: 5.00m),

        new ChatModelInfo(
            WireId: "claude-fable-5",
            ApiModel: AnthropicModel.ClaudeFable5,
            DisplayName: "Claude Fable 5",
            Description: "Anthropic's most capable widely released model. For the hardest reasoning and long-horizon agentic work.",
            ContextWindowTokens: 1_000_000,
            InputPricePerMillionTokens: 10.00m,
            OutputPricePerMillionTokens: 50.00m),
    ];

    public static ChatModelInfo Default => Models[0];

    public static ChatModelInfo GetByWireId(string wireId) =>
        Models.FirstOrDefault(m => m.WireId == wireId) ?? Default;
}

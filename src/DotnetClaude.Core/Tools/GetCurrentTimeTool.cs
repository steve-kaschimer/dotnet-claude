using System.Text.Json;
using Anthropic.Models.Messages;

namespace DotnetClaude.Core.Tools;

/// <summary>Demo tool: returns the current date/time, optionally in an IANA time zone.</summary>
public sealed class GetCurrentTimeTool : IDemoTool
{
    public string Name => "get_current_time";

    public Tool Definition => new()
    {
        Name = Name,
        Description = "Get the current date and time. Call this when the user asks what time or date it is, " +
                      "or needs a timestamp for something in the conversation.",
        InputSchema = new()
        {
            Properties = new Dictionary<string, JsonElement>
            {
                ["timezone"] = JsonSerializer.SerializeToElement(new
                {
                    type = "string",
                    description = "Optional IANA time zone id, e.g. 'America/New_York'. Defaults to UTC.",
                }),
            },
        },
    };

    public Task<string> ExecuteAsync(IReadOnlyDictionary<string, JsonElement> input, CancellationToken cancellationToken)
    {
        var utcNow = DateTimeOffset.UtcNow;

        if (input.TryGetValue("timezone", out var tzElement) && tzElement.ValueKind == JsonValueKind.String)
        {
            var tzId = tzElement.GetString();
            if (!string.IsNullOrWhiteSpace(tzId))
            {
                try
                {
                    var tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);
                    var local = TimeZoneInfo.ConvertTime(utcNow, tz);
                    return Task.FromResult($"{local:yyyy-MM-dd HH:mm:ss zzz} ({tzId})");
                }
                catch (TimeZoneNotFoundException)
                {
                    return Task.FromResult($"Unknown time zone '{tzId}'. UTC time is {utcNow:yyyy-MM-dd HH:mm:ss} UTC.");
                }
            }
        }

        return Task.FromResult($"{utcNow:yyyy-MM-dd HH:mm:ss} UTC");
    }
}

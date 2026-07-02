using Anthropic;
using DotnetClaude.Core.Data;
using DotnetClaude.Core.Services;
using DotnetClaude.Core.Tools;
using DotnetClaude.Tui;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var services = new ServiceCollection();

// No console logging provider: Terminal.Gui owns the terminal, and interleaved
// Console.Write calls from a logger would corrupt the screen.
services.AddLogging();

services.Configure<ClaudeOptions>(configuration.GetSection(ClaudeOptions.SectionName));

var dbPath = Path.Combine(AppContext.BaseDirectory, "dotnet-claude.db");
services.AddDbContextFactory<AppDbContext>(options => options.UseSqlite($"Data Source={dbPath}"));

services.AddSingleton(_ =>
{
    var apiKey = configuration["Anthropic:ApiKey"];
    return string.IsNullOrWhiteSpace(apiKey)
        ? new AnthropicClient() // falls back to the ANTHROPIC_API_KEY environment variable
        : new AnthropicClient { ApiKey = apiKey };
});

services.AddSingleton<IDemoTool, GetCurrentTimeTool>();
services.AddSingleton<IDemoTool, CalculatorTool>();
services.AddSingleton<ToolRegistry>();

services.AddScoped<IClaudeChatService, ClaudeChatService>();
services.AddScoped<ConversationService>();

await using var provider = services.BuildServiceProvider();

using (var scope = provider.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    await using var db = await dbFactory.CreateDbContextAsync();
    // WAL mode relies on shared-memory-mapped files that some sandboxed/containerized
    // filesystems don't support; the default rollback journal is more portable.
    await db.Database.OpenConnectionAsync();
    await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode='DELETE';");
    await db.Database.EnsureCreatedAsync();
}

using var appScope = provider.CreateScope();
var chatService = appScope.ServiceProvider.GetRequiredService<IClaudeChatService>();
var conversationService = appScope.ServiceProvider.GetRequiredService<ConversationService>();
var hasApiKey = !string.IsNullOrWhiteSpace(configuration["Anthropic:ApiKey"])
    || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY"));

var app = new ChatApp(chatService, conversationService, hasApiKey);
await app.RunAsync();

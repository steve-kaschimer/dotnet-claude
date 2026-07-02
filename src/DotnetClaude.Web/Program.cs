using Anthropic;
using DotnetClaude.Core.Data;
using DotnetClaude.Core.Services;
using DotnetClaude.Core.Tools;
using DotnetClaude.Web.Components;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.Configure<ClaudeOptions>(builder.Configuration.GetSection(ClaudeOptions.SectionName));

var dbPath = Path.Combine(AppContext.BaseDirectory, "dotnet-claude.db");
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddSingleton(_ =>
{
    var apiKey = builder.Configuration["Anthropic:ApiKey"];
    return string.IsNullOrWhiteSpace(apiKey)
        ? new AnthropicClient() // falls back to the ANTHROPIC_API_KEY environment variable
        : new AnthropicClient { ApiKey = apiKey };
});

builder.Services.AddSingleton<IDemoTool, GetCurrentTimeTool>();
builder.Services.AddSingleton<IDemoTool, CalculatorTool>();
builder.Services.AddSingleton<ToolRegistry>();

builder.Services.AddScoped<IClaudeChatService, ClaudeChatService>();
builder.Services.AddScoped<ConversationService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    await using var db = await dbFactory.CreateDbContextAsync();
    // WAL mode relies on shared-memory-mapped files that some sandboxed/containerized
    // filesystems don't support; the default rollback journal is more portable.
    await db.Database.OpenConnectionAsync();
    await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode='DELETE';");
    await db.Database.EnsureCreatedAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

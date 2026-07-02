# Dotnet Claude

A .NET 8 / Blazor Server application for chatting with the Claude API: switch
models mid-conversation, watch a bounded tool-use ("skills"/agent) loop run
live, and inspect per-query performance (tokens, latency, cost, context
window usage, tool calls) on a dashboard.

## Using Superpowers

This repo enables the [Superpowers](https://github.com/obra/superpowers)
plugin marketplace (see `.claude/settings.json`). Prefer its skills for
nontrivial work in this repo: brainstorm/design before implementing,
write a bite-sized plan, and favor TDD (see the test project below) —
especially for changes to `ClaudeChatService` or the data model, where a
wrong assumption is expensive to unwind. If the plugin isn't installed in
your Claude Code environment yet, install the marketplace with
`/plugin marketplace add obra/superpowers-marketplace` and then
`/plugin install superpowers@superpowers-marketplace`.

## Solution layout

```
src/DotnetClaude.Core   Class library: Anthropic API integration, EF Core
                         data model, demo tools ("skills"), cost/telemetry.
src/DotnetClaude.Web     Blazor Server UI: Chat page, Performance dashboard.
tests/DotnetClaude.Core.Tests  xUnit tests (model catalog, cost math, tools,
                         and an integration test that exercises the real
                         ClaudeChatService request/response pipeline against
                         the live API with a deliberately invalid key, so it
                         doesn't need a real credential to run in CI).
```

Key files:

- `src/DotnetClaude.Core/Services/ClaudeChatService.cs` — the agentic loop:
  calls the Messages API, executes any `tool_use` blocks against
  `ToolRegistry`, round-trips results, and persists a `QueryMetric` (tokens,
  latency, cost, tool calls) per turn.
- `src/DotnetClaude.Core/Models/ModelCatalog.cs` — the models offered in the
  UI's model switcher, with pricing for cost estimation. Update this when
  Anthropic ships new models — see the `claude-api` skill for current model
  IDs and pricing before changing it.
- `src/DotnetClaude.Core/Tools/` — demo tools (`get_current_time`,
  `calculate`) the assistant can call. Add new `IDemoTool` implementations
  and register them in `Program.cs` to add more.
- `src/DotnetClaude.Web/Components/Pages/Chat.razor` — chat UI with live
  agent-step trace (tool calls, thinking, results) as they happen.
- `src/DotnetClaude.Web/Components/Pages/Metrics.razor` — performance
  dashboard across every query.

## Running locally

```sh
export ANTHROPIC_API_KEY=sk-ant-...   # or set Anthropic:ApiKey in appsettings
dotnet run --project src/DotnetClaude.Web
```

SQLite database (`dotnet-claude.db`) is created automatically next to the
built binaries on first run — no migration step needed.

## Testing

```sh
dotnet test
```

## Conventions

- Target framework: net8.0 everywhere.
- EF Core is pinned to 8.0.x (the `Microsoft.EntityFrameworkCore.*` latest
  packages on NuGet target net10.0 and won't restore against net8.0 — don't
  bump those without also bumping the target framework).
- Order query results by `Id`, not `CreatedAt` — SQLite's EF Core provider
  can't translate `ORDER BY` on `DateTimeOffset` columns.
- The Anthropic C# SDK (`Anthropic` NuGet package) is the only supported way
  to call the Claude API from this repo — see the `claude-api` skill for
  usage patterns (model IDs, tool use, streaming, pricing) before writing
  code against it; don't hand-roll HTTP calls.

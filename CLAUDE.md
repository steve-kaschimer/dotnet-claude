# Dotnet Claude

A .NET 8 app (Blazor Server web UI + a terminal UI) for chatting with the
Claude API: switch models mid-conversation, watch a bounded tool-use
("skills"/agent) loop run live, and inspect per-query performance (tokens,
latency, cost, context window usage, tool calls) on a dashboard. Both front
ends share the same `DotnetClaude.Core` chat pipeline and data model; each
keeps its own local SQLite database (see Solution layout below).

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
src/DotnetClaude.Tui     Terminal.Gui (v2) UI: same chat + model switching +
                         live agent trace, in the terminal.
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
- `src/DotnetClaude.Tui/ChatApp.cs` — the terminal UI: conversation list,
  model picker (F2), transcript, and the same live agent trace as the web
  Chat page, built on `Terminal.Gui` v2.

## Running locally

```sh
export ANTHROPIC_API_KEY=sk-ant-...   # or set Anthropic:ApiKey in appsettings
dotnet run --project src/DotnetClaude.Web   # browser UI
dotnet run --project src/DotnetClaude.Tui   # terminal UI
```

SQLite database (`dotnet-claude.db`) is created automatically next to the
built binaries on first run — no migration step needed. `DotnetClaude.Web`
and `DotnetClaude.Tui` each keep their own database (same convention, `next
to the binaries`), so conversations aren't shared between the two front ends.

### Terminal.Gui gotchas (DotnetClaude.Tui)

- `Ctrl+M` and `Enter` send the identical control byte (`0x0D`) in every
  terminal — don't bind `Ctrl+M` to anything; it will silently trigger
  whatever handles Enter instead. Use a different key (this app uses `F2`
  for the model picker).
- `Label`/`Button` text treats a single `_` as a hotkey marker and strips it
  from the render. Any label that might display a literal underscore (e.g.
  `ANTHROPIC_API_KEY`) needs `HotKeySpecifier` set to an unused rune to
  disable that parsing — see `_errorLabel` in `ChatApp.cs`.
- Don't add a console logging provider — `Terminal.Gui` owns the terminal via
  raw-mode rendering, and interleaved `Console.Write` calls corrupt the
  screen. `Program.cs` registers logging with no providers (a no-op sink).
- Terminal.Gui apps need a real pty; verify changes by driving them under
  `tmux` (`send-keys` / `capture-pane`), not by piping stdin/stdout.

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

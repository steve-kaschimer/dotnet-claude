# Dotnet Claude

A .NET 8 app for chatting with Anthropic's Claude API — from a browser
(Blazor Server) or a terminal (Terminal.Gui). Switch models mid-conversation,
watch the assistant's tool-use ("skills") loop run live, and inspect
per-query performance (tokens, latency, cost, context window usage, tool
calls) on a dashboard.

## Features

- **Ask questions** in a chat UI backed by the [Anthropic C# SDK](https://github.com/anthropics/anthropic-sdk-csharp) — in the browser or the terminal.
- **Switch models** per message — Claude Opus 4.8, Sonnet 5, Haiku 4.5, or Fable 5.
- **Tool-using agent** — the assistant can call demo tools (`get_current_time`,
  `calculate`) in a bounded agentic loop; every call is shown live and logged.
- **Query performance dashboard** (web UI) — input/output/cache tokens, context
  window utilization, latency, API call count, tool call count, stop reason,
  and estimated cost for every query, plus aggregate stats by model and tool.
- **Conversation history** persisted to a local SQLite database.

## Getting started

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```sh
export ANTHROPIC_API_KEY=sk-ant-...
dotnet run --project src/DotnetClaude.Web   # browser UI
```

Then open the URL printed in the console (defaults to `http://localhost:5029`
in Development) and go to **Chat**.

```sh
export ANTHROPIC_API_KEY=sk-ant-...
dotnet run --project src/DotnetClaude.Tui   # terminal UI
```

`F2` opens the model picker, `Ctrl+N` starts a new conversation, `Ctrl+Q`
quits. The two front ends each keep their own local SQLite database, so
conversations aren't shared between them.

If you'd rather not use an environment variable, set `Anthropic:ApiKey` in
`appsettings.Development.json` (web) / `appsettings.json` (TUI) or via
[.NET user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets)
— don't commit a real key to `appsettings.json`.

## Project structure

```
src/DotnetClaude.Core   Anthropic API integration, EF Core data model,
                         demo tools, cost/telemetry calculation.
src/DotnetClaude.Web     Blazor Server UI (Chat, Performance dashboard).
src/DotnetClaude.Tui     Terminal UI (Terminal.Gui v2) — same chat pipeline.
tests/DotnetClaude.Core.Tests  xUnit test suite.
```

See [`CLAUDE.md`](./CLAUDE.md) for a deeper architecture tour aimed at
Claude Code (or any contributor working with an AI pair programmer).

## Running tests

```sh
dotnet test
```

## Configuration

Both `DotnetClaude.Web` and `DotnetClaude.Tui` read the same `Anthropic`
settings section from their own `appsettings.json`:

| Setting                     | Default | Description                                   |
| ---------------------------- | ------- | ---------------------------------------------- |
| `Anthropic:ApiKey`            | (unset) | Falls back to the `ANTHROPIC_API_KEY` env var. |
| `Anthropic:MaxOutputTokens`   | `4096`  | Max output tokens per API call.                |
| `Anthropic:MaxToolIterations` | `6`     | Cap on tool-use round trips per turn.          |
| `Anthropic:ToolsEnabled`      | `true`  | Whether demo tools are offered to the model.   |

## License

No license specified.

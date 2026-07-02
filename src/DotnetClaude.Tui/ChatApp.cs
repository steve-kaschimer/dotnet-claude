using System.Collections.ObjectModel;
using DotnetClaude.Core.Data;
using DotnetClaude.Core.Models;
using DotnetClaude.Core.Services;
using Terminal.Gui;

namespace DotnetClaude.Tui;

/// <summary>
/// A terminal front end for the same chat pipeline the Blazor app uses: ask
/// questions, switch models, and watch the tool-use agentic loop run live.
/// </summary>
public sealed class ChatApp(IClaudeChatService chatService, ConversationService conversationService, bool hasApiKey)
{
    private List<Conversation> _conversations = [];
    private List<ChatMessageEntity> _messages = [];
    private List<QueryMetric> _metrics = [];
    private int? _currentConversationId;
    private ChatModelInfo _selectedModel = ModelCatalog.Default;
    private bool _isSending;
    private string? _errorMessage;
    private readonly List<string> _liveTrace = [];

    private ListView _conversationListView = null!;
    private Label _modelLabel = null!;
    private TextView _transcript = null!;
    private TextView _traceView = null!;
    private Label _errorLabel = null!;
    private TextField _input = null!;
    private Button _sendButton = null!;

    public async Task RunAsync()
    {
        await LoadConversationsAsync();

        Application.Init();
        try
        {
            var top = BuildUi();
            RenderConversationList();
            RenderTranscript();
            if (!hasApiKey)
            {
                _errorMessage = "No Claude API key detected. Set ANTHROPIC_API_KEY before sending a message.";
                RenderError();
            }
            Application.Run(top);
        }
        finally
        {
            Application.Shutdown();
        }
    }

    private Toplevel BuildUi()
    {
        var win = new Window
        {
            Title = "Dotnet Claude — TUI",
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(1),
        };

        var conversationsFrame = new FrameView
        {
            Title = "Conversations (Ctrl+N: new)",
            X = 0,
            Y = 0,
            Width = 32,
            Height = Dim.Fill(),
        };
        _conversationListView = new ListView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
        _conversationListView.OpenSelectedItem += (_, e) => _ = SelectConversationAsync(e.Item);
        conversationsFrame.Add(_conversationListView);
        win.Add(conversationsFrame);

        _modelLabel = new Label { X = 34, Y = 0, Width = Dim.Fill() };
        UpdateModelLabel();
        win.Add(_modelLabel);

        _transcript = new TextView
        {
            X = 34,
            Y = 2,
            Width = Dim.Fill(),
            Height = Dim.Fill(7),
            ReadOnly = true,
        };
        win.Add(_transcript);

        var traceFrame = new FrameView
        {
            Title = "Agent trace",
            X = 34,
            Y = Pos.Bottom(_transcript),
            Width = Dim.Fill(),
            Height = 4,
        };
        _traceView = new TextView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), ReadOnly = true };
        traceFrame.Add(_traceView);
        win.Add(traceFrame);

        _errorLabel = new Label
        {
            X = 34,
            Y = Pos.Bottom(traceFrame),
            Width = Dim.Fill(),
            Height = 1,
            // Disable hotkey-underscore parsing: error text often contains literal
            // underscores (e.g. "ANTHROPIC_API_KEY"), which Label otherwise strips.
            HotKeySpecifier = new System.Text.Rune('￿'),
            ColorScheme = new ColorScheme { Normal = new Terminal.Gui.Attribute(Color.BrightRed, Color.Black) },
        };
        win.Add(_errorLabel);

        _input = new TextField { X = 34, Y = Pos.Bottom(_errorLabel) + 1, Width = Dim.Fill(12) };
        _input.KeyDown += (_, key) =>
        {
            if (key.KeyCode == KeyCode.Enter)
            {
                _ = SendAsync();
                key.Handled = true;
            }
        };
        win.Add(_input);

        _sendButton = new Button { X = Pos.Right(_input) + 1, Y = Pos.Bottom(_errorLabel) + 1, Text = "Send" };
        _sendButton.Accepting += (_, _) => _ = SendAsync();
        win.Add(_sendButton);

        var statusBar = new StatusBar(
        [
            // Ctrl+M is indistinguishable from Enter at the terminal protocol level (both
            // send 0x0D), so it can't be used as a distinct shortcut -- F2 instead.
            new Shortcut(Key.N.WithCtrl, "New chat", () => _ = NewConversationAsync(), ""),
            new Shortcut(Key.F2, "Change model", ShowModelPicker, ""),
            new Shortcut(Key.Q.WithCtrl, "Quit", () => Application.RequestStop(), ""),
        ]);

        var top = new Toplevel();
        top.Add(win, statusBar);
        return top;
    }

    private void UpdateModelLabel()
    {
        _modelLabel.Text = $"Model: {_selectedModel.DisplayName}  ({_selectedModel.Description})  [F2 to change]";
    }

    private void ShowModelPicker()
    {
        var dialog = new Dialog { Title = "Select model", Width = 70, Height = 12 };

        var list = new ListView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(2) };
        var items = ModelCatalog.Models.Select(m => $"{m.DisplayName} — {m.Description}").ToList();
        list.SetSource(new ObservableCollection<string>(items));
        var currentIndex = ModelCatalog.Models.ToList().FindIndex(m => m.WireId == _selectedModel.WireId);
        if (currentIndex >= 0)
        {
            list.SelectedItem = currentIndex;
        }
        dialog.Add(list);

        var selectButton = new Button { Text = "Select", IsDefault = true };
        selectButton.Accepting += (_, _) =>
        {
            _selectedModel = ModelCatalog.Models[list.SelectedItem];
            UpdateModelLabel();
            Application.RequestStop(dialog);
        };
        var cancelButton = new Button { Text = "Cancel" };
        cancelButton.Accepting += (_, _) => Application.RequestStop(dialog);

        dialog.AddButton(selectButton);
        dialog.AddButton(cancelButton);

        Application.Run(dialog);
    }

    private async Task LoadConversationsAsync()
    {
        _conversations = await conversationService.GetConversationsAsync();
    }

    private void RenderConversationList()
    {
        var items = _conversations.Select(c => c.Title).ToList();
        if (items.Count == 0)
        {
            items = ["(no conversations yet)"];
        }
        _conversationListView.SetSource(new ObservableCollection<string>(items));

        if (_currentConversationId is int currentId)
        {
            var index = _conversations.FindIndex(c => c.Id == currentId);
            if (index >= 0)
            {
                _conversationListView.SelectedItem = index;
            }
        }
    }

    private async Task SelectConversationAsync(int index)
    {
        if (_isSending || index < 0 || index >= _conversations.Count)
        {
            return;
        }

        _currentConversationId = _conversations[index].Id;
        await ReloadCurrentConversationAsync();
        RenderTranscript();
    }

    private async Task NewConversationAsync()
    {
        if (_isSending)
        {
            return;
        }

        _currentConversationId = null;
        _messages = [];
        _metrics = [];
        _errorMessage = null;
        RenderTranscript();
        RenderError();
        _input.SetFocus();
        await Task.CompletedTask;
    }

    private async Task ReloadCurrentConversationAsync()
    {
        if (_currentConversationId is int id)
        {
            _messages = await conversationService.GetMessagesAsync(id);
            _metrics = await conversationService.GetMetricsForConversationAsync(id);
        }
    }

    private async Task SendAsync()
    {
        if (_isSending || string.IsNullOrWhiteSpace(_input.Text))
        {
            return;
        }

        var userText = _input.Text.Trim();
        _errorMessage = null;
        RenderError();

        if (_currentConversationId is null)
        {
            var conversation = await conversationService.CreateConversationAsync(BuildTitle(userText));
            _currentConversationId = conversation.Id;
            await LoadConversationsAsync();
            RenderConversationList();
        }

        _isSending = true;
        _input.Text = "";
        _input.Enabled = false;
        _sendButton.Enabled = false;
        _liveTrace.Clear();
        RenderTrace();

        try
        {
            await foreach (var step in chatService.SendMessageAsync(_currentConversationId.Value, userText, _selectedModel.WireId))
            {
                AppendTraceEntry(step);
                RenderTrace();
                RenderError();
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Something went wrong: {ex.Message}";
        }
        finally
        {
            _isSending = false;
            _input.Enabled = true;
            _sendButton.Enabled = true;
            _liveTrace.Clear();
            RenderTrace();
            await ReloadCurrentConversationAsync();
            RenderTranscript();
            RenderError();
            _input.SetFocus();
        }
    }

    private void AppendTraceEntry(AgentStep step)
    {
        if (step is AgentStep.Failed failed)
        {
            // Surfaced as a persistent error label (see SendAsync's finally block) rather
            // than a trace line, since the live trace is cleared once sending finishes.
            _errorMessage = failed.Message;
            return;
        }

        var text = step switch
        {
            AgentStep.ApiCallStarted s => $"Calling {_selectedModel.DisplayName}... (round {s.Iteration + 1})",
            AgentStep.ToolCallStarted s => $"[tool] {s.ToolName}({s.InputJson})",
            AgentStep.ToolCallCompleted s => $"[{(s.IsError ? "error" : "ok")}] {s.ToolName} -> {s.OutputText} ({s.LatencyMs} ms)",
            AgentStep.AssistantText s => $"[claude] {Truncate(s.Text, 300)}",
            AgentStep.Completed => "Done.",
            _ => step.ToString() ?? "",
        };
        _liveTrace.Add(text);
    }

    private void RenderTrace()
    {
        _traceView.Text = string.Join("\n", _liveTrace);
        _traceView.MoveEnd();
    }

    private void RenderError()
    {
        _errorLabel.Text = _errorMessage ?? "";
    }

    private void RenderTranscript()
    {
        var lines = new List<string>();

        if (_currentConversationId is null)
        {
            lines.Add("Ask a question below to start a new conversation.");
            lines.Add("Try: \"What time is it in Tokyo?\" or \"What's (18 + 4) * 3?\" to see tool calls in action.");
        }

        foreach (var message in _messages)
        {
            var role = message.Role == ChatRole.User ? "You" : ModelDisplayNameOrDefault(message.ModelWireId);
            lines.Add($"[{role}]");
            lines.Add(message.Content);

            if (message.Role == ChatRole.Assistant)
            {
                var metric = _metrics.FirstOrDefault(m => m.AssistantMessageId == message.Id);
                if (metric is not null)
                {
                    lines.Add(
                        $"  in={metric.InputTokens} out={metric.OutputTokens} cached={metric.CacheReadInputTokens} " +
                        $"latency={metric.TotalLatencyMs}ms tools={metric.ToolCallCount} " +
                        $"cost=${metric.EstimatedCostUsd:0.000000} context={metric.ContextUtilizationPercent}%");
                }
            }

            lines.Add("");
        }

        _transcript.Text = string.Join("\n", lines);
        _transcript.MoveEnd();
    }

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..maxLength] + "...";

    private static string BuildTitle(string firstMessage)
    {
        var trimmed = firstMessage.Trim();
        return trimmed.Length <= 60 ? trimmed : trimmed[..60] + "...";
    }

    private static string ModelDisplayNameOrDefault(string? wireId) =>
        wireId is null ? "Claude" : ModelCatalog.GetByWireId(wireId).DisplayName;
}

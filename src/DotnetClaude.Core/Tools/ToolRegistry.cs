namespace DotnetClaude.Core.Tools;

/// <summary>Holds the set of demo tools ("skills") available to the assistant.</summary>
public sealed class ToolRegistry
{
    private readonly Dictionary<string, IDemoTool> _tools;

    public ToolRegistry(IEnumerable<IDemoTool> tools)
    {
        _tools = tools.ToDictionary(t => t.Name);
    }

    public IReadOnlyCollection<IDemoTool> All => _tools.Values;

    public bool TryGet(string name, out IDemoTool tool) => _tools.TryGetValue(name, out tool!);
}

namespace StoreAssistantPro.Core.Workflows;

/// <summary>
/// Mutable bag of state carried through a single workflow execution.
/// Workflows read/write values so steps can communicate without coupling.
/// </summary>
public sealed class WorkflowContext
{
    private readonly Dictionary<string, object?> _data = [];

    public void Set<T>(string key, T value) => _data[key] = value;

    public T? Get<T>(string key) =>
        _data.TryGetValue(key, out var value) ? (T?)value : default;

    public bool Has(string key) => _data.ContainsKey(key);

    public void Clear() => _data.Clear();
}

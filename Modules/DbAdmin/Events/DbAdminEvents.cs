using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Models.DbAdmin;

namespace StoreAssistantPro.Modules.DbAdmin.Events;

/// <summary>Published when database maintenance is completed.</summary>
public sealed class DatabaseMaintenanceCompletedEvent(string operationType) : IEvent
{
    public string OperationType { get; } = operationType;
}

/// <summary>Published when pending migrations are applied.</summary>
public sealed class MigrationsAppliedEvent(int count) : IEvent
{
    public int Count { get; } = count;
}

/// <summary>Published when old data is purged.</summary>
public sealed class DataPurgedEvent(PurgeResult result) : IEvent
{
    public PurgeResult Result { get; } = result;
}

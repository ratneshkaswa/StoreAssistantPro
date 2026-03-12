namespace StoreAssistantPro.Models;

public enum UserType
{
    Admin = 0,
    // Deprecated role kept only for persisted DB enum-int compatibility.
    [Obsolete("Manager role is deprecated and no longer used by UI/workflows.")]
    Manager = 1,
    User = 2
}

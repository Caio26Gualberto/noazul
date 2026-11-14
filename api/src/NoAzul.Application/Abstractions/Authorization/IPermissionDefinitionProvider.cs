using NoAzul.Domain.Authorization;

namespace NoAzul.Application.Abstractions.Authorization
{
    public interface IPermissionDefinitionProvider
    {
        IReadOnlyList<PermissionGroupDefinition> GetGroups();
        IReadOnlyList<PermissionDefinition> GetPermissions();
        bool Exists(string name);
    }
}

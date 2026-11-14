using Microsoft.AspNetCore.Authorization;

namespace NoAzul.Api.Authorization
{
    public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;
}

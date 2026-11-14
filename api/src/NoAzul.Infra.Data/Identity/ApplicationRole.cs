using Microsoft.AspNetCore.Identity;

namespace NoAzul.Infra.Data.Identity
{
    public class ApplicationRole : IdentityRole<int>
    {
        public string? Description { get; set; }
        public bool IsSystem { get; set; }
        public bool IsDefault { get; set; }

        public ApplicationRole() { }

        public ApplicationRole(string roleName) : base(roleName) { }
    }
}

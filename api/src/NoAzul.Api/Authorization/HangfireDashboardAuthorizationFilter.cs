using Hangfire.Dashboard;

namespace NoAzul.Api.Authorization
{
    public sealed class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context) => true;
    }
}

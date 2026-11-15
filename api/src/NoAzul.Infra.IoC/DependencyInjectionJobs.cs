using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using NoAzul.Workers.Wrappers.Recurring.BankConnections;
using NoAzul.Workers.Wrappers.Recurring.Items;

namespace NoAzul.Infra.IoC
{
    public static class DependencyInjectionJobs
    {
        public static WebApplication RegisterJobs(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();

            scope.ServiceProvider.GetRequiredService<ItemRecurringJobsWrapper>().Register();
            scope.ServiceProvider.GetRequiredService<BankConnectionRecurringJobsWrapper>().Register();

            return app;
        }
    }
}

using Hangfire;
using NoAzul.Application.Common.Time;
using NoAzul.Workers.Triggers.Recurring.BankConnections;

namespace NoAzul.Workers.Wrappers.Recurring.BankConnections
{
    public class BankConnectionRecurringJobsWrapper
    {
        public void Register()
        {
            RecurringJob.AddOrUpdate<BankConnectionRecurringJobsTrigger>(
                recurringJobId: "bank-connections:sync",
                queue: "bank-connections",
                methodCall: trigger => trigger.SyncAllAsync(),
                cronExpression: Cron.Daily(hour: 8),
                options: new RecurringJobOptions { TimeZone = AppTime.Zone }
            );
        }
    }
}

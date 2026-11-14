using Hangfire;
using NoAzul.Application.Common.Time;
using NoAzul.Workers.Triggers.Recurring.Items;

namespace NoAzul.Workers.Wrappers.Recurring.Items
{
    public class ItemRecurringJobsWrapper
    {
        public void Register()
        {
            RecurringJob.AddOrUpdate<ItemRecurringJobsTrigger>(
                recurringJobId: "items:archive-expired",
                queue: "items",
                methodCall: trigger => trigger.ArchiveExpiredAsync(),
                cronExpression: Cron.Hourly(),
                options: new RecurringJobOptions { TimeZone = AppTime.Zone }
            );
        }
    }
}

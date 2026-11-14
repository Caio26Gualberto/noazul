using NoAzul.Application.BackgroundJobs.Dtos;

namespace NoAzul.Application.BackgroundJobs.Interfaces
{
    public interface IRecurringJobReader
    {
        IReadOnlyList<RecurringJobDto> GetRecurringJobs();
    }
}

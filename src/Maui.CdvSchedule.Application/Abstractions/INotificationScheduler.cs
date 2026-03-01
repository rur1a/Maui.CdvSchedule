using Maui.CdvSchedule.Domain.Models;

namespace Maui.CdvSchedule.Application.Abstractions;

public interface INotificationScheduler
{
    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task CancelAllAsync(CancellationToken cancellationToken = default);

    Task RescheduleAsync(
        IReadOnlyList<ScheduleLesson> lessons,
        AppSettings settings,
        CancellationToken cancellationToken = default);
}


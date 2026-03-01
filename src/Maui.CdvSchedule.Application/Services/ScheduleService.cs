using Maui.CdvSchedule.Application.Abstractions;
using Maui.CdvSchedule.Application.State;
using Maui.CdvSchedule.Domain.Models;

namespace Maui.CdvSchedule.Application.Services;

public sealed class ScheduleService
{
    public const int CalendarPastMonths = 2;
    public const int CalendarFutureMonths = 6;
    public const string CanceledStatus = "ODWOLANE";

    private readonly ICdvApiClient _apiClient;
    private readonly AppSessionState _sessionState;
    private readonly INotificationScheduler _notificationScheduler;

    public ScheduleService(
        ICdvApiClient apiClient,
        AppSessionState sessionState,
        INotificationScheduler notificationScheduler)
    {
        _apiClient = apiClient;
        _sessionState = sessionState;
        _notificationScheduler = notificationScheduler;
    }

    public DateTime GetFirstSupportedDate()
    {
        var now = DateTime.Now;
        return new DateTime(now.Year, now.Month, 1).AddMonths(-CalendarPastMonths);
    }

    public DateTime GetLastSupportedDate()
    {
        var now = DateTime.Now;
        return new DateTime(now.Year, now.Month, 1).AddMonths(CalendarFutureMonths + 1).AddDays(-1);
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (!_sessionState.IsLoggedIn || _sessionState.User is null)
        {
            _sessionState.SetSchedule(Array.Empty<ScheduleLesson>());
            await _notificationScheduler.CancelAllAsync(cancellationToken);
            return;
        }

        var firstDay = GetFirstSupportedDate();
        var lastDay = GetLastSupportedDate();
        var lessons = await _apiClient.GetScheduleAsync(
            _sessionState.User.UserType,
            _sessionState.User.UserId,
            _sessionState.TokenEncoded,
            firstDay,
            lastDay,
            cancellationToken);

        _sessionState.SetSchedule(lessons);

        if (_sessionState.Settings.NotificationsEnabled)
        {
            await _notificationScheduler.RescheduleAsync(lessons, _sessionState.Settings, cancellationToken);
        }
        else
        {
            await _notificationScheduler.CancelAllAsync(cancellationToken);
        }
    }

    public IReadOnlyList<ScheduleLesson> GetLessonsForMonth(DateTime month)
    {
        var start = new DateTime(month.Year, month.Month, 1);
        var end = start.AddMonths(1);

        return _sessionState.ScheduleLessons
            .Where(l => l.StartDate >= start && l.StartDate < end)
            .ToList();
    }
}


using Android.App;
using Android.Content;
using Android.OS;
using Microsoft.Maui.ApplicationModel;
using Maui.CdvSchedule.Application.Abstractions;
using Maui.CdvSchedule.Application.Services;
using Maui.CdvSchedule.Domain.Models;

namespace Maui.CdvSchedule.App.Infrastructure.Notifications;

public sealed class AndroidNotificationScheduler : INotificationScheduler
{
    private const int QueueSize = 32;
    private readonly Context _context;

    public AndroidNotificationScheduler()
    {
        _context = global::Android.App.Application.Context;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        CdvNotificationPlatform.EnsureChannel(_context);
        await RequestPostNotificationsPermissionIfNeededAsync();
    }

    public Task CancelAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var alarmManager = _context.GetSystemService(Context.AlarmService) as AlarmManager;
        var notificationManager = _context.GetSystemService(Context.NotificationService) as NotificationManager;

        for (var id = 0; id < QueueSize; id++)
        {
            using var pendingIntent = CreateBroadcastPendingIntent(id, string.Empty, string.Empty);
            alarmManager?.Cancel(pendingIntent);
            pendingIntent.Cancel();
            notificationManager?.Cancel(id);
        }

        notificationManager?.CancelAll();
        return Task.CompletedTask;
    }

    public async Task RescheduleAsync(
        IReadOnlyList<ScheduleLesson> lessons,
        AppSettings settings,
        CancellationToken cancellationToken = default)
    {
        await CancelAllAsync(cancellationToken);

        if (!settings.NotificationsEnabled || lessons.Count == 0)
        {
            return;
        }

        CdvNotificationPlatform.EnsureChannel(_context);
        var alarmManager = _context.GetSystemService(Context.AlarmService) as AlarmManager;
        if (alarmManager is null)
        {
            return;
        }

        var sorted = lessons.OrderBy(x => x.StartDate).ToList();
        var now = DateTime.Now;
        var slice = 0;

        while (slice < sorted.Count - 1)
        {
            if ((now - sorted[slice].StartDate).TotalSeconds < settings.NotificationOffsetSeconds + 60)
            {
                break;
            }

            slice++;
        }

        if (slice > 0 && slice < sorted.Count)
        {
            sorted = sorted.Skip(slice).ToList();
        }

        var limit = Math.Min(sorted.Count, QueueSize);
        for (var i = 0; i < limit; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var lesson = sorted[i];

            if (string.Equals(lesson.Status, ScheduleService.CanceledStatus, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var fireAt = ComputeFireTime(lesson, settings.NotificationOffsetSeconds);
            if (fireAt <= DateTime.Now)
            {
                continue;
            }

            var title = $"{lesson.SubjectName} [{lesson.Room}]";
            var body = $"{lesson.StartDate:H:mm}-{lesson.EndDate:H:mm}";
            ScheduleAlarm(alarmManager, i, title, body, fireAt);
        }
    }

    private static DateTime ComputeFireTime(ScheduleLesson lesson, int secondsOffset)
    {
        if (lesson.StartDate.Hour <= 9)
        {
            return new DateTime(
                lesson.StartDate.Year,
                lesson.StartDate.Month,
                lesson.StartDate.Day,
                21,
                0,
                0,
                lesson.StartDate.Kind).AddDays(-1);
        }

        return lesson.StartDate.AddSeconds(-secondsOffset);
    }

    private void ScheduleAlarm(AlarmManager alarmManager, int id, string title, string body, DateTime fireAtLocal)
    {
        using var pendingIntent = CreateBroadcastPendingIntent(id, title, body);
        var triggerAtMillis = new DateTimeOffset(fireAtLocal).ToUnixTimeMilliseconds();

        try
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                alarmManager.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, triggerAtMillis, pendingIntent);
            }
            else
            {
                alarmManager.SetExact(AlarmType.RtcWakeup, triggerAtMillis, pendingIntent);
            }
        }
        catch (Java.Lang.SecurityException)
        {
            // Android 12+ may block exact alarms when the app is not allowed; fall back to inexact scheduling.
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                alarmManager.SetAndAllowWhileIdle(AlarmType.RtcWakeup, triggerAtMillis, pendingIntent);
            }
            else
            {
                alarmManager.Set(AlarmType.RtcWakeup, triggerAtMillis, pendingIntent);
            }
        }
    }

    private PendingIntent CreateBroadcastPendingIntent(int id, string title, string body)
    {
        var intent = new Intent(_context, typeof(CdvNotificationReceiver));
        intent.PutExtra(CdvNotificationPlatform.ExtraNotificationId, id);
        intent.PutExtra(CdvNotificationPlatform.ExtraTitle, title);
        intent.PutExtra(CdvNotificationPlatform.ExtraBody, body);

        return PendingIntent.GetBroadcast(
            _context,
            id,
            intent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable)!;
    }

    private static async Task RequestPostNotificationsPermissionIfNeededAsync()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.Tiramisu)
        {
            return;
        }

        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
            if (status != PermissionStatus.Granted)
            {
                await Permissions.RequestAsync<Permissions.PostNotifications>();
            }
        }
        catch
        {
            // Permission request availability can vary on emulators/devices; keep scheduler non-fatal.
        }
    }
}

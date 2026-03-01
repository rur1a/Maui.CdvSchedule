using Maui.CdvSchedule.Application.Services;
using Maui.CdvSchedule.Application.State;
using Maui.CdvSchedule.Domain.Enums;
using Maui.CdvSchedule.Domain.Models;
using Maui.CdvSchedule.App.Tests.TestDoubles;

namespace Maui.CdvSchedule.App.Tests.Application.Services;

public sealed class AppSettingsServiceTests
{
    [Fact]
    public async Task UpdateThemeAsync_SavesSettingsAndUpdatesThemeService()
    {
        var session = new AppSessionState();
        var settingsStore = new FakeSettingsStore();
        var themeService = new FakeThemeService();
        var scheduler = new FakeNotificationScheduler();
        var sut = new AppSettingsService(
            session,
            settingsStore,
            themeService,
            scheduler);

        await sut.UpdateThemeAsync(AppThemeKind.Dark);

        Assert.Single(settingsStore.SavedSettings);
        Assert.Equal(AppThemeKind.Dark, settingsStore.SavedSettings[0].Theme);
        Assert.Equal(AppThemeKind.Dark, session.Settings.Theme);
        Assert.Equal(AppThemeKind.Dark, themeService.CurrentTheme);
    }

    [Fact]
    public async Task UpdateNotificationsEnabledAsync_WhenEnabled_Reschedules()
    {
        var session = new AppSessionState();
        session.UpdateSettings(AppSettings.Default with { NotificationsEnabled = false });
        session.SetSchedule(new[] { CreateLesson(new DateTime(2026, 7, 5, 8, 0, 0)) });

        var settingsStore = new FakeSettingsStore();
        var scheduler = new FakeNotificationScheduler();
        var sut = new AppSettingsService(
            session,
            settingsStore,
            new FakeThemeService(),
            scheduler);

        await sut.UpdateNotificationsEnabledAsync(true);

        Assert.True(session.Settings.NotificationsEnabled);
        Assert.Single(settingsStore.SavedSettings);
        Assert.Equal(1, scheduler.RescheduleCalls);
        Assert.Equal(0, scheduler.CancelAllCalls);
    }

    [Fact]
    public async Task UpdateNotificationsEnabledAsync_WhenDisabled_Cancels()
    {
        var session = new AppSessionState();
        session.UpdateSettings(AppSettings.Default with { NotificationsEnabled = true });
        var settingsStore = new FakeSettingsStore();
        var scheduler = new FakeNotificationScheduler();
        var sut = new AppSettingsService(
            session,
            settingsStore,
            new FakeThemeService(),
            scheduler);

        await sut.UpdateNotificationsEnabledAsync(false);

        Assert.False(session.Settings.NotificationsEnabled);
        Assert.Single(settingsStore.SavedSettings);
        Assert.Equal(0, scheduler.RescheduleCalls);
        Assert.Equal(1, scheduler.CancelAllCalls);
    }

    [Fact]
    public async Task UpdateNotificationOffsetAsync_WhenNotificationsEnabled_Reschedules()
    {
        var session = new AppSessionState();
        session.UpdateSettings(AppSettings.Default with { NotificationsEnabled = true, NotificationOffsetSeconds = 3600 });
        session.SetSchedule(new[] { CreateLesson(new DateTime(2026, 8, 5, 8, 0, 0)) });

        var settingsStore = new FakeSettingsStore();
        var scheduler = new FakeNotificationScheduler();
        var sut = new AppSettingsService(
            session,
            settingsStore,
            new FakeThemeService(),
            scheduler);

        await sut.UpdateNotificationOffsetAsync(1200);

        Assert.Equal(1200, session.Settings.NotificationOffsetSeconds);
        Assert.Single(settingsStore.SavedSettings);
        Assert.Equal(1, scheduler.RescheduleCalls);
    }

    [Fact]
    public async Task UpdateNotificationOffsetAsync_WhenNotificationsDisabled_DoesNotReschedule()
    {
        var session = new AppSessionState();
        session.UpdateSettings(AppSettings.Default with { NotificationsEnabled = false, NotificationOffsetSeconds = 3600 });
        session.SetSchedule(new[] { CreateLesson(new DateTime(2026, 8, 5, 8, 0, 0)) });

        var settingsStore = new FakeSettingsStore();
        var scheduler = new FakeNotificationScheduler();
        var sut = new AppSettingsService(
            session,
            settingsStore,
            new FakeThemeService(),
            scheduler);

        await sut.UpdateNotificationOffsetAsync(1200);

        Assert.Equal(1200, session.Settings.NotificationOffsetSeconds);
        Assert.Single(settingsStore.SavedSettings);
        Assert.Equal(0, scheduler.RescheduleCalls);
    }

    private static ScheduleLesson CreateLesson(DateTime start)
    {
        return new ScheduleLesson(
            Status: "OK",
            Subject: "Sub",
            SubjectName: "Subject",
            GroupNumber: "1",
            StartDate: start,
            EndDate: start.AddHours(1),
            Form: "W",
            Teacher: "Teacher",
            Room: "101",
            MeetLink: string.Empty);
    }
}

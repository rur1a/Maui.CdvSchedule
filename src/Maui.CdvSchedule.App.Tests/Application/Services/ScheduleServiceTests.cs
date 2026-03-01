using Maui.CdvSchedule.Application.Services;
using Maui.CdvSchedule.Application.State;
using Maui.CdvSchedule.Domain.Models;
using Maui.CdvSchedule.App.Tests.TestDoubles;

namespace Maui.CdvSchedule.App.Tests.Application.Services;

public sealed class ScheduleServiceTests
{
    [Fact]
    public async Task RefreshAsync_WhenLoggedOut_ClearsScheduleAndCancelsNotifications()
    {
        var api = new FakeCdvApiClient();
        var scheduler = new FakeNotificationScheduler();
        var session = new AppSessionState();
        session.SetSchedule(new[] { CreateLesson(new DateTime(2026, 5, 15, 10, 0, 0)) });

        var sut = new ScheduleService(api, session, scheduler);

        await sut.RefreshAsync();

        Assert.Equal(0, api.ScheduleCalls);
        Assert.Equal(1, scheduler.CancelAllCalls);
        Assert.Empty(session.ScheduleLessons);
    }

    [Fact]
    public async Task RefreshAsync_WhenLoggedInAndNotificationsEnabled_ReschedulesLessons()
    {
        var api = new FakeCdvApiClient
        {
            ScheduleResponse = new[] { CreateLesson(new DateTime(2026, 6, 4, 8, 0, 0)) }
        };
        var scheduler = new FakeNotificationScheduler();
        var session = new AppSessionState();
        session.SetAuthenticated(
            "user@example.com",
            "token-123",
            new UserProfile("55", "user@example.com", "student", "User", "A-1"),
            null);
        session.UpdateSettings(AppSettings.Default with { NotificationsEnabled = true });

        var sut = new ScheduleService(api, session, scheduler);

        await sut.RefreshAsync();

        Assert.Equal(1, api.ScheduleCalls);
        Assert.Single(api.GetScheduleArgs);
        Assert.Equal("student", api.GetScheduleArgs[0].UserType);
        Assert.Equal("55", api.GetScheduleArgs[0].UserId);
        Assert.Equal("token-123", api.GetScheduleArgs[0].TokenEncoded);
        Assert.Equal(1, scheduler.RescheduleCalls);
        Assert.Equal(0, scheduler.CancelAllCalls);
        Assert.Same(api.ScheduleResponse, session.ScheduleLessons);
    }

    [Fact]
    public async Task RefreshAsync_WhenLoggedInAndNotificationsDisabled_CancelsInsteadOfRescheduling()
    {
        var api = new FakeCdvApiClient
        {
            ScheduleResponse = new[] { CreateLesson(new DateTime(2026, 6, 4, 8, 0, 0)) }
        };
        var scheduler = new FakeNotificationScheduler();
        var session = new AppSessionState();
        session.SetAuthenticated(
            "user@example.com",
            "token-123",
            new UserProfile("55", "user@example.com", "student", "User", "A-1"),
            null);
        session.UpdateSettings(AppSettings.Default with { NotificationsEnabled = false });

        var sut = new ScheduleService(api, session, scheduler);

        await sut.RefreshAsync();

        Assert.Equal(1, api.ScheduleCalls);
        Assert.Equal(0, scheduler.RescheduleCalls);
        Assert.Equal(1, scheduler.CancelAllCalls);
    }

    [Fact]
    public void GetLessonsForMonth_ReturnsOnlyLessonsForSelectedMonth()
    {
        var session = new AppSessionState();
        session.SetSchedule(
            new[]
            {
                CreateLesson(new DateTime(2026, 5, 10, 10, 0, 0)),
                CreateLesson(new DateTime(2026, 5, 29, 12, 0, 0)),
                CreateLesson(new DateTime(2026, 6, 2, 9, 0, 0))
            });

        var sut = new ScheduleService(new FakeCdvApiClient(), session, new FakeNotificationScheduler());

        var mayLessons = sut.GetLessonsForMonth(new DateTime(2026, 5, 1));

        Assert.Equal(2, mayLessons.Count);
        Assert.All(mayLessons, lesson => Assert.Equal(5, lesson.StartDate.Month));
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

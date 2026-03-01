using Maui.CdvSchedule.Domain.Enums;

namespace Maui.CdvSchedule.Domain.Models;

public sealed record AppSettings(
    string Locale,
    AppThemeKind Theme,
    bool NotificationsEnabled,
    int NotificationOffsetSeconds)
{
    public static AppSettings Default { get; } = new("pl", AppThemeKind.System, true, 3600);
}

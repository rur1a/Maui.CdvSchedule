namespace Maui.CdvSchedule.Domain.Enums;

public enum AppThemeKind
{
    Light = 0,
    Dark = 1,
    Amoled = 2,
    System = 3,
}

public static class AppThemeKindExtensions
{
    public static AppThemeKind FromId(int id)
    {
        return Enum.IsDefined(typeof(AppThemeKind), id)
            ? (AppThemeKind)id
            : AppThemeKind.System;
    }
}

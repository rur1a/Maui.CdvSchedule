using Maui.CdvSchedule.Domain.Enums;

namespace Maui.CdvSchedule.App.Tests.Domain.Enums;

public sealed class AppThemeKindExtensionsTests
{
    [Fact]
    public void FromId_WhenUnknown_ReturnsSystem()
    {
        var result = AppThemeKindExtensions.FromId(999);

        Assert.Equal(AppThemeKind.System, result);
    }

    [Theory]
    [InlineData(0, AppThemeKind.Light)]
    [InlineData(1, AppThemeKind.Dark)]
    [InlineData(2, AppThemeKind.Amoled)]
    [InlineData(3, AppThemeKind.System)]
    public void FromId_WhenKnown_ReturnsMappedEnum(int id, AppThemeKind expected)
    {
        var result = AppThemeKindExtensions.FromId(id);

        Assert.Equal(expected, result);
    }
}

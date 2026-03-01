using Microsoft.Maui.ApplicationModel;
using Maui.CdvSchedule.App.Presentation.ViewModels;
#if ANDROID
using Android.Content.Res;
using AndroidX.AppCompat.Widget;
using Microsoft.Maui.Platform;
#endif

namespace Maui.CdvSchedule.App.Presentation.Pages;

public partial class SettingsPage : ContentPage
{
    private bool _suppressEvents = true;
    private SettingsViewModel ViewModel => (SettingsViewModel)BindingContext;

    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _suppressEvents = true;
        ViewModel.RefreshFromState();
        ViewModel.MarkLoaded();
        _suppressEvents = false;
        ApplyAndroidControlTintsDeferred();
    }

    private async void OnLocaleChanged(object? sender, EventArgs e)
    {
        if (_suppressEvents || !ViewModel.IsLoaded)
        {
            return;
        }

        var changed = await ViewModel.ApplyLocaleAsync();
        if (changed)
        {
            await DisplayAlert(ViewModel.RestartRequiredTitle, ViewModel.RestartRequiredQuestion, ViewModel.RestartButtonText);
        }
    }

    private async void OnThemeChanged(object? sender, EventArgs e)
    {
        if (_suppressEvents || !ViewModel.IsLoaded)
        {
            return;
        }

        await ViewModel.ApplyThemeAsync();
        ApplyAndroidControlTintsDeferred();
    }

    private async void OnNotificationsToggled(object? sender, ToggledEventArgs e)
    {
        if (_suppressEvents || !ViewModel.IsLoaded)
        {
            return;
        }

        await ViewModel.ApplyNotificationsEnabledAsync(e.Value);
    }

    private async void OnNotificationOffsetChanged(object? sender, EventArgs e)
    {
        if (_suppressEvents || !ViewModel.IsLoaded)
        {
            return;
        }

        await ViewModel.ApplyNotificationOffsetAsync();
    }

    private void ApplyAndroidControlTintsDeferred()
    {
#if ANDROID
        MainThread.BeginInvokeOnMainThread(ApplyAndroidControlTints);
#endif
    }

#if ANDROID
    private void ApplyAndroidControlTints()
    {
        var resources = global::Microsoft.Maui.Controls.Application.Current?.Resources;
        if (resources is null)
        {
            return;
        }

        var accent = ResolveColor(resources, "AccentColor", Color.FromArgb("#2196F3"));
        var surface = ResolveColor(resources, "SurfaceColor", Colors.White);
        var offTrack = ResolveColor(resources, "BorderColor", Color.FromArgb("#D0D0D0"));
        var offThumb = ResolveColor(resources, "SurfaceAltColor", surface);

        TintPicker(LocalePicker, accent);
        TintPicker(ThemePicker, accent);
        TintPicker(NotificationOffsetPicker, accent);
        TintSwitch(NotificationsSwitch, accent, offTrack, accent, offThumb);
    }

    private static void TintPicker(Picker picker, Color accent)
    {
        if (picker.Handler?.PlatformView is not global::Android.Views.View nativeView)
        {
            return;
        }

        nativeView.BackgroundTintList = ColorStateList.ValueOf(accent.ToPlatform());

        if (nativeView is AppCompatEditText appCompatEditText)
        {
            appCompatEditText.SupportBackgroundTintList = ColorStateList.ValueOf(accent.ToPlatform());
        }

        if (nativeView is global::Android.Widget.TextView textView && OperatingSystem.IsAndroidVersionAtLeast(23))
        {
            textView.CompoundDrawableTintList = ColorStateList.ValueOf(accent.ToPlatform());
        }
    }

    private static void TintSwitch(Switch switchControl, Color onTrack, Color offTrack, Color onThumb, Color offThumb)
    {
        if (switchControl.Handler?.PlatformView is not SwitchCompat nativeSwitch)
        {
            return;
        }

        var states = new[]
        {
            new[] { global::Android.Resource.Attribute.StateChecked },
            new[] { -global::Android.Resource.Attribute.StateChecked }
        };

        var trackColors = new[]
        {
            onTrack.ToPlatform().ToArgb(),
            offTrack.ToPlatform().ToArgb()
        };

        var thumbColors = new[]
        {
            onThumb.ToPlatform().ToArgb(),
            offThumb.ToPlatform().ToArgb()
        };

        nativeSwitch.TrackTintList = new ColorStateList(states, trackColors);
        nativeSwitch.ThumbTintList = new ColorStateList(states, thumbColors);
    }

    private static Color ResolveColor(ResourceDictionary resources, string key, Color fallback)
    {
        return resources.TryGetValue(key, out var value) && value is Color color
            ? color
            : fallback;
    }
#endif
}

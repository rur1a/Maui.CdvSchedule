using Microsoft.Maui.ApplicationModel;
using Maui.CdvSchedule.App.Presentation.ViewModels;
using Plugin.Maui.Calendar.Enums;
using PluginCalendar = Plugin.Maui.Calendar.Controls.Calendar;

namespace Maui.CdvSchedule.App.Presentation.Pages;

public partial class SchedulePage : ContentPage
{
    private PluginCalendar? _calendar;
    private bool _calendarRebuildPending;
    private readonly Command<object?> _dayTappedCommand;
    private ScheduleViewModel ViewModel => (ScheduleViewModel)BindingContext;

    public SchedulePage(ScheduleViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _dayTappedCommand = new Command<object?>(OnCalendarDayTapped);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        EnsureCalendar();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        ReleaseCalendar();
    }

    private void OnPrevMonthClicked(object? sender, EventArgs e)
    {
        ViewModel.GoToPreviousMonth();
    }

    private void OnTodayClicked(object? sender, EventArgs e)
    {
        ViewModel.GoToToday();
        RequestCalendarRebuild();
    }

    private void OnNextMonthClicked(object? sender, EventArgs e)
    {
        ViewModel.GoToNextMonth();
    }

    private async void OnHelpClicked(object? sender, EventArgs e)
    {
        await DisplayAlert(ViewModel.HelpDialogTitle, ViewModel.HelpDialogBody, ViewModel.OkText);
    }

    private async void OnRefreshing(object? sender, EventArgs e)
    {
        await ViewModel.RefreshAsync();
    }

    private async void OnLessonSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not ScheduleLessonItemViewModel item)
        {
            return;
        }

        LessonsCollection.SelectedItem = null;

        var title = string.IsNullOrWhiteSpace(item.SubjectName) ? item.SubjectCode : item.SubjectName;
        var message = ViewModel.BuildLessonDetails(item);

        if (item.HasMeetingLink)
        {
            var open = await DisplayAlert(title, message, ViewModel.JoinMeetingText, ViewModel.CloseText);
            if (open && Uri.TryCreate(item.Lesson.MeetLink, UriKind.Absolute, out var uri))
            {
                await Launcher.Default.OpenAsync(uri);
            }
            return;
        }

        await DisplayAlert(title, message, ViewModel.CloseText);
    }

    private void EnsureCalendar()
    {
        if (_calendar is not null)
        {
            return;
        }

        var calendar = new PluginCalendar
        {
            EventsScrollViewVisible = false,
            HeaderSectionVisible = false,
            FooterSectionVisible = false,
            SwipeUpToHideEnabled = false,
            ShowMonthPicker = false,
            ShowYearPicker = false,
            OtherMonthDayIsVisible = false,
            DayViewSize = 30,
            DayViewFontSize = 11,
            HeightRequest = 236,
            EventIndicatorType = EventIndicatorType.BottomDot
        };

        calendar.BindingContext = BindingContext;
        calendar.SetBinding(PluginCalendar.ShownDateProperty, nameof(ScheduleViewModel.CalendarShownDate), mode: BindingMode.TwoWay);
        calendar.SetBinding(PluginCalendar.EventsProperty, nameof(ScheduleViewModel.CalendarEvents));
        calendar.SetBinding(PluginCalendar.CultureProperty, nameof(ScheduleViewModel.CalendarCulture));
        calendar.SetBinding(PluginCalendar.MinimumDateProperty, nameof(ScheduleViewModel.MinDate));
        calendar.SetBinding(PluginCalendar.MaximumDateProperty, nameof(ScheduleViewModel.MaxDate));
        calendar.DayTappedCommand = _dayTappedCommand;

        // The plugin is crash-prone with live SelectedDate binding on Android. We push the current
        // selection only when the control is recreated and keep tap->VM sync manual.
        calendar.SelectedDate = ViewModel.CalendarSelectedDate;

        calendar.SetDynamicResource(PluginCalendar.MonthLabelColorProperty, "TextPrimaryColor");
        calendar.SetDynamicResource(PluginCalendar.YearLabelColorProperty, "TextPrimaryColor");
        calendar.SetDynamicResource(PluginCalendar.DaysTitleColorProperty, "TextMutedColor");
        calendar.SetDynamicResource(PluginCalendar.DaysTitleWeekendColorProperty, "TextMutedColor");
        calendar.SetDynamicResource(PluginCalendar.WeekendDayColorProperty, "TextPrimaryColor");
        calendar.SetDynamicResource(PluginCalendar.DeselectedDayTextColorProperty, "TextPrimaryColor");
        calendar.SetDynamicResource(PluginCalendar.OtherMonthDayColorProperty, "TextMutedColor");
        calendar.SetDynamicResource(PluginCalendar.DisabledDayColorProperty, "TextMutedColor");
        calendar.SetDynamicResource(PluginCalendar.SelectedDateColorProperty, "TextPrimaryColor");
        calendar.SetDynamicResource(PluginCalendar.SelectedDayBackgroundColorProperty, "AccentColor");
        calendar.SetDynamicResource(PluginCalendar.SelectedDayTextColorProperty, "AccentContrastColor");
        calendar.SetDynamicResource(PluginCalendar.SelectedTodayTextColorProperty, "AccentContrastColor");
        calendar.SetDynamicResource(PluginCalendar.TodayOutlineColorProperty, "AccentColor");
        calendar.SetDynamicResource(PluginCalendar.TodayFillColorProperty, "SurfaceAltColor");
        calendar.SetDynamicResource(PluginCalendar.TodayTextColorProperty, "TextPrimaryColor");
        calendar.SetDynamicResource(PluginCalendar.EventIndicatorColorProperty, "AccentColor");
        calendar.SetDynamicResource(PluginCalendar.EventIndicatorSelectedColorProperty, "AccentContrastColor");
        calendar.SetDynamicResource(PluginCalendar.EventIndicatorTextColorProperty, "TextPrimaryColor");
        calendar.SetDynamicResource(PluginCalendar.EventIndicatorSelectedTextColorProperty, "AccentContrastColor");

        _calendar = calendar;
        CalendarHost.Content = calendar;
    }

    private void ReleaseCalendar()
    {
        if (_calendar is null)
        {
            return;
        }

        CalendarHost.Content = null;
        _calendar = null;
    }

    private void OnCalendarDayTapped(object? parameter)
    {
        if (!TryGetTappedDate(parameter, out var date))
        {
            return;
        }

        ViewModel.SelectDate(date);
    }

    private void RequestCalendarRebuild()
    {
        if (_calendarRebuildPending || _calendar is null)
        {
            return;
        }

        _calendarRebuildPending = true;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            _calendarRebuildPending = false;

            if (!this.IsLoaded)
            {
                return;
            }

            ReleaseCalendar();
            EnsureCalendar();
        });
    }

    private static bool TryGetTappedDate(object? parameter, out DateTime date)
    {
        switch (parameter)
        {
            case DateTime value:
                date = value.Date;
                return true;

            case DateTimeOffset offset:
                date = offset.Date;
                return true;

        }

        var dateProperty = parameter?.GetType().GetProperty("Date");
        if (dateProperty?.GetValue(parameter) is DateTime reflectedDate)
        {
            date = reflectedDate.Date;
            return true;
        }

        if (dateProperty?.GetValue(parameter) is DateTimeOffset reflectedOffset)
        {
            date = reflectedOffset.Date;
            return true;
        }

        date = default;
        return false;
    }
}

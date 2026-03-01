using System.Globalization;
using Maui.CdvSchedule.Application.Abstractions;
using Maui.CdvSchedule.Application.Services;
using Maui.CdvSchedule.App.Presentation.Services;
using Maui.CdvSchedule.Application.State;
using Plugin.Maui.Calendar.Interfaces;
using Plugin.Maui.Calendar.Models;

namespace Maui.CdvSchedule.App.Presentation.ViewModels;

public sealed class ScheduleViewModel : BaseViewModel
{
    private readonly AppSessionState _sessionState;
    private readonly ScheduleService _scheduleService;
    private readonly ILocalizationService _localizationService;
    private readonly LessonMetadataService _lessonMetadataService;

    private DateTime _visibleMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private DateTime _selectedDate = DateTime.Today;
    private bool _isRefreshing;
    private Dictionary<DateTime, List<Maui.CdvSchedule.Domain.Models.ScheduleLesson>> _lessonsByDate = new();
    private EventCollection _calendarEvents = new();

    public ScheduleViewModel(
        AppSessionState sessionState,
        ScheduleService scheduleService,
        ILocalizationService localizationService,
        LessonMetadataService lessonMetadataService)
    {
        _sessionState = sessionState;
        _scheduleService = scheduleService;
        _localizationService = localizationService;
        _lessonMetadataService = lessonMetadataService;

        MinDate = _scheduleService.GetFirstSupportedDate();
        MaxDate = _scheduleService.GetLastSupportedDate();

        if (_selectedDate < MinDate)
        {
            _selectedDate = MinDate.Date;
            _visibleMonth = new DateTime(_selectedDate.Year, _selectedDate.Month, 1);
        }
        else if (_selectedDate > MaxDate)
        {
            _selectedDate = MaxDate.Date;
            _visibleMonth = new DateTime(_selectedDate.Year, _selectedDate.Month, 1);
        }

        _sessionState.Changed += (_, _) => RefreshFromState();
        _localizationService.LocaleChanged += (_, _) =>
        {
            RefreshLocalizedText();
            BuildSelectedDayLessons();
        };

        ReindexLessons();
        BuildSelectedDayLessons();
    }

    public ObservableCollection<ScheduleLessonItemViewModel> SelectedDayLessons { get; } = new();

    public DateTime MinDate { get; }
    public DateTime MaxDate { get; }
    public bool IsLoggedIn => _sessionState.IsLoggedIn;
    public EventCollection CalendarEvents
    {
        get => _calendarEvents;
        private set => SetProperty(ref _calendarEvents, value);
    }
    public CultureInfo CalendarCulture => _localizationService.CurrentCulture;

    public DateTime CalendarShownDate
    {
        get => _visibleMonth;
        set
        {
            var normalized = ClampMonth(new DateTime(value.Year, value.Month, 1));

            if (_visibleMonth != normalized)
            {
                _visibleMonth = normalized;
                OnPropertyChanged();

                OnPropertyChanged(nameof(MonthTitle));
                BuildSelectedDayLessons();
            }
        }
    }

    public DateTime CalendarSelectedDate
    {
        get => _selectedDate;
        set => SelectDate(value);
    }

    public bool IsRefreshing
    {
        get => _isRefreshing;
        set => SetProperty(ref _isRefreshing, value);
    }

    public string PageTitle => _localizationService.Translate("Main.Schedule");
    public string TodayText => _localizationService.Translate("Schedule.Today");
    public string PrevMonthText => _localizationService.Translate("Schedule.PrevMonth");
    public string NextMonthText => _localizationService.Translate("Schedule.NextMonth");
    public string HelpText => _localizationService.Translate("Static.HelpDialog");
    public string MonthTitle => _visibleMonth.ToString("Y", _localizationService.CurrentCulture);
    public string JoinMeetingText => _localizationService.Translate("Schedule.joinMeeting");
    public string OkText => _localizationService.Translate("Common.Ok");
    public string CloseText => _localizationService.Translate("Profile.Cancel");
    public string HelpDialogTitle => _localizationService.Translate("Static.HelpDialog");
    public string HelpDialogBody =>
        $"{_lessonMetadataService.BuildLegendSummary()}{Environment.NewLine}{Environment.NewLine}" +
        $"{_localizationService.Translate("Static.HelpText1")}{Environment.NewLine}" +
        $"{_localizationService.Translate("Static.HelpText2")}{Environment.NewLine}" +
        $"{_localizationService.Translate("Static.HelpText3")}";

    public string EmptyStateText =>
        !_sessionState.IsLoggedIn
            ? _localizationService.Translate("noSchedule.page")
            : _localizationService.Translate("Schedule.EmptyDay");

    public string SelectedDayTitle =>
        _selectedDate.ToString("D", _localizationService.CurrentCulture);

    public string SelectedDaySubtitle =>
        _sessionState.IsLoggedIn
            ? string.Format(
                _localizationService.CurrentCulture,
                _localizationService.Translate("Schedule.SelectedDayEvents"),
                SelectedDayLessons.Count)
            : string.Empty;

    public void RefreshFromState()
    {
        OnPropertiesChanged(nameof(IsLoggedIn), nameof(EmptyStateText));

        if (_selectedDate < MinDate)
        {
            _selectedDate = MinDate.Date;
        }
        else if (_selectedDate > MaxDate)
        {
            _selectedDate = MaxDate.Date;
        }

        _visibleMonth = ClampMonth(new DateTime(_selectedDate.Year, _selectedDate.Month, 1));
        OnPropertiesChanged(nameof(CalendarShownDate), nameof(CalendarSelectedDate), nameof(MonthTitle));

        ReindexLessons();
        BuildSelectedDayLessons();
    }

    public async Task RefreshAsync()
    {
        if (IsRefreshing)
        {
            return;
        }

        IsRefreshing = true;
        try
        {
            await _scheduleService.RefreshAsync();
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    public void GoToToday()
    {
        SelectDate(DateTime.Today, forceMonthSync: true);
    }

    public void GoToPreviousMonth()
    {
        var previous = new DateTime(_visibleMonth.Year, _visibleMonth.Month, 1).AddMonths(-1);
        CalendarShownDate = previous;
    }

    public void GoToNextMonth()
    {
        var next = new DateTime(_visibleMonth.Year, _visibleMonth.Month, 1).AddMonths(1);
        CalendarShownDate = next;
    }

    public void SelectDate(DateTime date, bool forceMonthSync = false)
    {
        var clamped = ClampDate(date);

        var targetMonth = ClampMonth(new DateTime(clamped.Year, clamped.Month, 1));
        var monthChanged = _visibleMonth != targetMonth;
        var dateChanged = clamped != _selectedDate.Date;

        if (!dateChanged && !monthChanged && !forceMonthSync)
        {
            return;
        }

        _selectedDate = clamped;
        _visibleMonth = targetMonth;

        if (monthChanged || forceMonthSync)
        {
            OnPropertiesChanged(nameof(CalendarShownDate), nameof(MonthTitle));
        }

        if (dateChanged || forceMonthSync)
        {
            OnPropertyChanged(nameof(CalendarSelectedDate));
        }

        BuildSelectedDayLessons();
    }

    public string BuildLessonDetails(ScheduleLessonItemViewModel item)
    {
        var lesson = item.Lesson;
        var culture = _localizationService.CurrentCulture;
        var dateText = lesson.StartDate.ToString("MMMM d", culture);

        return
            $"{_localizationService.Translate("Schedule.date")}: {dateText} ({lesson.StartDate:H:mm}-{lesson.EndDate:H:mm}){Environment.NewLine}" +
            $"{_localizationService.Translate("Schedule.room")}: {lesson.Room}{Environment.NewLine}" +
            $"{_localizationService.Translate("Schedule.group")}: {lesson.GroupNumber}{Environment.NewLine}" +
            $"{_localizationService.Translate("Schedule.teacher")}: {lesson.Teacher}";
    }

    private void BuildSelectedDayLessons()
    {
        SelectedDayLessons.Clear();

        if (_sessionState.IsLoggedIn)
        {
            _lessonsByDate.TryGetValue(_selectedDate.Date, out var dayLessons);
            var lessons = (dayLessons ?? [])
                .OrderBy(x => x.StartDate)
                .Select(x => new ScheduleLessonItemViewModel(x, _lessonMetadataService, _localizationService));

            foreach (var lesson in lessons)
            {
                SelectedDayLessons.Add(lesson);
            }
        }

        OnPropertiesChanged(nameof(SelectedDayTitle), nameof(SelectedDaySubtitle), nameof(EmptyStateText));
    }

    private void ReindexLessons()
    {
        _lessonsByDate = _sessionState.ScheduleLessons
            .GroupBy(x => x.StartDate.Date)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.StartDate).ToList());

        var deselectedEventTextColor = ResolveThemeColor("TextPrimaryColor", Colors.Black);
        var selectedEventTextColor = ResolveThemeColor("AccentContrastColor", Colors.White);

        var events = new EventCollection(_lessonsByDate.Count);
        foreach (var (date, lessons) in _lessonsByDate)
        {
            var indicatorColor = lessons.Count > 0
                ? _lessonMetadataService.GetColor(lessons[0].Form)
                : Colors.Transparent;

            var dayEvents = new PersonalizableDayEvents<Maui.CdvSchedule.Domain.Models.ScheduleLesson>
            {
                EventIndicatorColor = indicatorColor,
                EventIndicatorSelectedColor = indicatorColor,
                EventIndicatorTextColor = deselectedEventTextColor,
                EventIndicatorSelectedTextColor = selectedEventTextColor
            };

            foreach (var lesson in lessons)
            {
                dayEvents.Add(lesson);
            }

            events.Add(date, dayEvents);
        }

        CalendarEvents = events;
    }

    private static Color ResolveThemeColor(string key, Color fallback)
    {
        var resources = Microsoft.Maui.Controls.Application.Current?.Resources;
        return resources is not null && resources.TryGetValue(key, out var value) && value is Color color
            ? color
            : fallback;
    }

    private DateTime ClampMonth(DateTime monthDate)
    {
        var first = new DateTime(MinDate.Year, MinDate.Month, 1);
        var last = new DateTime(MaxDate.Year, MaxDate.Month, 1);

        if (monthDate < first)
        {
            return first;
        }

        if (monthDate > last)
        {
            return last;
        }

        return monthDate;
    }

    private DateTime ClampDate(DateTime date)
    {
        var clamped = date.Date;
        if (clamped < MinDate.Date)
        {
            return MinDate.Date;
        }

        if (clamped > MaxDate.Date)
        {
            return MaxDate.Date;
        }

        return clamped;
    }

    private void RefreshLocalizedText()
    {
        OnPropertiesChanged(
            nameof(PageTitle),
            nameof(TodayText),
            nameof(PrevMonthText),
            nameof(NextMonthText),
            nameof(HelpText),
            nameof(JoinMeetingText),
            nameof(OkText),
            nameof(CloseText),
            nameof(HelpDialogTitle),
            nameof(HelpDialogBody),
            nameof(EmptyStateText),
            nameof(SelectedDayTitle),
            nameof(SelectedDaySubtitle),
            nameof(MonthTitle),
            nameof(CalendarCulture));
    }

    private sealed class PersonalizableDayEvents<T> : List<T>, IPersonalizableDayEvent
    {
        public Color? EventIndicatorColor { get; set; }
        public Color? EventIndicatorSelectedColor { get; set; }
        public Color? EventIndicatorTextColor { get; set; }
        public Color? EventIndicatorSelectedTextColor { get; set; }
    }
}



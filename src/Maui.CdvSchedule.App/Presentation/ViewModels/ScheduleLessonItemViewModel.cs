using Maui.CdvSchedule.Application.Abstractions;
using Maui.CdvSchedule.Application.Services;
using Maui.CdvSchedule.App.Presentation.Services;
using Maui.CdvSchedule.Domain.Models;

namespace Maui.CdvSchedule.App.Presentation.ViewModels;

public sealed class ScheduleLessonItemViewModel
{
    private readonly ILocalizationService _localizationService;

    public ScheduleLessonItemViewModel(
        ScheduleLesson lesson,
        LessonMetadataService lessonMetadataService,
        ILocalizationService localizationService)
    {
        Lesson = lesson;
        _localizationService = localizationService;
        StripeColor = lessonMetadataService.GetColor(lesson.Form);
    }

    public ScheduleLesson Lesson { get; }
    public Color StripeColor { get; }

    public bool IsCancelled => string.Equals(Lesson.Status, ScheduleService.CanceledStatus, StringComparison.OrdinalIgnoreCase);
    public bool HasMeetingLink => !string.IsNullOrWhiteSpace(Lesson.MeetLink);
    public string SubjectName => Lesson.SubjectName;
    public string SubjectCode => Lesson.Subject;
    public string TimeRange => $"{Lesson.StartDate:H:mm} - {Lesson.EndDate:H:mm}";
    public string RoomOrStatus => IsCancelled ? _localizationService.Translate("Event.CANCELLED") : Lesson.Room;
    public Color RoomTextColor => IsCancelled ? Colors.Red : (Microsoft.Maui.Controls.Application.Current?.Resources["TextPrimaryColor"] as Color ?? Colors.Black);
}



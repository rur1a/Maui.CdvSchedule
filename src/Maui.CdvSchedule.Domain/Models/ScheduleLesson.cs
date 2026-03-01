namespace Maui.CdvSchedule.Domain.Models;

public sealed record ScheduleLesson(
    string Status,
    string Subject,
    string SubjectName,
    string GroupNumber,
    DateTime StartDate,
    DateTime EndDate,
    string Form,
    string Teacher,
    string Room,
    string MeetLink);


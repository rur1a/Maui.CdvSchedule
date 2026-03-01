using Maui.CdvSchedule.Application.Abstractions;

namespace Maui.CdvSchedule.App.Presentation.Services;

public sealed class LessonMetadataService
{
    private sealed record LessonMeta(string TranslationKey, Color Color);

    private readonly ILocalizationService _localizationService;

    private readonly IReadOnlyDictionary<string, LessonMeta> _metadata = new Dictionary<string, LessonMeta>(StringComparer.OrdinalIgnoreCase)
    {
        ["W"] = new("Static.Wyklad", Colors.Green),
        ["WR"] = new("Static.Warsztaty", Color.FromArgb("#4DB6AC")),
        ["L"] = new("Static.Labaratoria", Colors.Blue),
        ["TODO_E_LEARNING"] = new("Static.Elearning", Colors.Gray),
        ["C"] = new("Static.Cwiczenia", Colors.Orange),
        ["LK"] = new("Static.Lektorat", Colors.Purple),
        ["TODO_PROJEKT"] = new("Static.Projekt", Colors.HotPink),
        ["TODO_PRAKTYKI"] = new("Static.Praktyki", Color.FromArgb("#1A237E")),
        ["TODO_SEMINARIUM"] = new("Static.Seminarium", Colors.Indigo),
        ["TODO_KONWERSATORIUM"] = new("Static.Konwersatorium", Colors.DarkOrange),
        ["TODO_SPOTKANIE"] = new("Static.Spotkanie", Colors.MediumPurple),
        ["MEETING"] = new("Static.Spotkanie", Colors.MediumPurple),
        ["EGSAM"] = new("Static.Zaliczenie", Colors.Purple),
        ["TODO_REZERWACJA"] = new("Static.Rezerwacja", Color.FromArgb("#6A1B9A")),
        ["TODO_DYZUR"] = new("Static.Dyzur", Color.FromArgb("#4A148C")),
    };

    public LessonMetadataService(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
    }

    public Color GetColor(string lessonForm)
    {
        return _metadata.TryGetValue(lessonForm, out var meta) ? meta.Color : Colors.Gray;
    }

    public string GetDisplayName(string lessonForm)
    {
        return _metadata.TryGetValue(lessonForm, out var meta)
            ? _localizationService.Translate(meta.TranslationKey)
            : lessonForm;
    }

    public string BuildLegendSummary()
    {
        var ordered = new[]
        {
            "W", "WR", "L", "C", "LK", "TODO_E_LEARNING",
            "TODO_PROJEKT", "TODO_PRAKTYKI", "TODO_SEMINARIUM",
            "TODO_KONWERSATORIUM", "TODO_SPOTKANIE", "EGSAM",
            "TODO_REZERWACJA", "TODO_DYZUR"
        };

        return string.Join(Environment.NewLine, ordered.Select(code => $"- {GetDisplayName(code)} ({code})"));
    }
}



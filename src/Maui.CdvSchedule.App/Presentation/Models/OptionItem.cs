namespace Maui.CdvSchedule.App.Presentation.Models;

public sealed class OptionItem<T>
{
    public required string Label { get; init; }
    public required T Value { get; init; }

    public override string ToString() => Label;
}


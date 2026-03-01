using System.Globalization;

namespace Maui.CdvSchedule.Application.Abstractions;

public interface ILocalizationService
{
    event EventHandler? LocaleChanged;

    string CurrentLocale { get; }

    CultureInfo CurrentCulture { get; }

    Task InitializeAsync(string locale, CancellationToken cancellationToken = default);

    string Translate(string key);
}


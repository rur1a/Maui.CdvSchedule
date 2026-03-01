using System.Globalization;
using System.Text;
using Maui.CdvSchedule.Application.Abstractions;

namespace Maui.CdvSchedule.App.Infrastructure.Localization;

public sealed class CsvLocalizationService : ILocalizationService
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private Dictionary<string, string[]> _rows = new(StringComparer.Ordinal);
    private bool _loaded;
    private int _localeColumn = 2; // default PL

    public event EventHandler? LocaleChanged;

    public string CurrentLocale { get; private set; } = "pl";

    public CultureInfo CurrentCulture { get; private set; } = new("pl-PL");

    public async Task InitializeAsync(string locale, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (!_loaded)
            {
                await LoadRowsAsync(cancellationToken);
                _loaded = true;
            }

            ApplyLocale(locale);
        }
        finally
        {
            _lock.Release();
        }

        LocaleChanged?.Invoke(this, EventArgs.Empty);
    }

    public string Translate(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return "No key provided";
        }

        if (_rows.TryGetValue(key, out var values))
        {
            if (_localeColumn >= 0 && _localeColumn < values.Length && !string.IsNullOrWhiteSpace(values[_localeColumn]))
            {
                return values[_localeColumn];
            }

            if (values.Length > 1 && !string.IsNullOrWhiteSpace(values[1]))
            {
                return values[1];
            }
        }

        return $"Translation missing for {key}";
    }

    private async Task LoadRowsAsync(CancellationToken cancellationToken)
    {
        using var stream = await FileSystem.Current.OpenAppPackageFileAsync("locale.csv");
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        string? line;
        var lineNumber = 0;

        // Android asset streams can throw when using async line reads on packaged assets.
        while ((line = reader.ReadLine()) is not null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lineNumber++;
            if (lineNumber == 1 || string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var columns = ParseCsvLine(line);
            if (columns.Count == 0)
            {
                continue;
            }

            var key = columns[0];
            if (!string.IsNullOrWhiteSpace(key))
            {
                _rows[key] = columns.ToArray();
            }
        }
    }

    private void ApplyLocale(string locale)
    {
        CurrentLocale = locale switch
        {
            "en" => "en",
            "pl" => "pl",
            "ru" => "ru",
            "tr" => "tr",
            _ => "pl",
        };

        (_localeColumn, var cultureName) = CurrentLocale switch
        {
            "en" => (1, "en-US"),
            "pl" => (2, "pl-PL"),
            "ru" => (3, "ru-RU"),
            "tr" => (4, "tr-TR"),
            _ => (2, "pl-PL"),
        };

        CurrentCulture = new CultureInfo(cultureName);
        CultureInfo.CurrentCulture = CurrentCulture;
        CultureInfo.CurrentUICulture = CurrentCulture;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(c);
        }

        result.Add(current.ToString());
        return result;
    }
}

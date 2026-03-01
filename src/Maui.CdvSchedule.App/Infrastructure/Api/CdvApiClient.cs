using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Maui.CdvSchedule.Application.Abstractions;
using Maui.CdvSchedule.Domain.Exceptions;
using Maui.CdvSchedule.Domain.Models;

namespace Maui.CdvSchedule.App.Infrastructure.Api;

public sealed class CdvApiClient : ICdvApiClient
{
    private readonly HttpClient _httpClient;

    public CdvApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<LoginResponseModel> LoginAsync(string login, string password, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "login")
        {
            Content = JsonContent.Create(new
            {
                login,
                password,
            }),
        };

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw BuildApiException("Failed to log in.", response, content);
        }

        using var document = JsonDocument.Parse(content);
        var root = document.RootElement;

        return new LoginResponseModel(
            root.GetProperty("result").GetBoolean(),
            root.GetProperty("token").GetString() ?? string.Empty,
            root.TryGetProperty("message", out var messageEl) ? messageEl.GetString() : null,
            root.TryGetProperty("photo", out var photoEl) ? (photoEl.GetString() ?? string.Empty) : string.Empty);
    }

    public async Task<IReadOnlyList<ScheduleLesson>> GetScheduleAsync(
        string userType,
        string userId,
        string tokenEncoded,
        DateTime fromInclusive,
        DateTime toInclusive,
        CancellationToken cancellationToken = default)
    {
        var from = fromInclusive.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var to = toInclusive.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var url = $"schedule/{userType}/{userId}/2/{from}/{to}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenEncoded);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw BuildApiException("Failed to get schedule.", response, content);
        }

        using var document = JsonDocument.Parse(content);
        var result = new List<ScheduleLesson>();

        foreach (var lesson in document.RootElement.EnumerateArray())
        {
            result.Add(new ScheduleLesson(
                Status: GetString(lesson, "Status"),
                Subject: GetString(lesson, "Subject"),
                SubjectName: GetString(lesson, "ThemaGroup"),
                GroupNumber: GetString(lesson, "GroupNr"),
                StartDate: ParseServerDate(GetString(lesson, "Start")),
                EndDate: ParseServerDate(GetString(lesson, "End")),
                Form: GetString(lesson, "Form"),
                Teacher: GetString(lesson, "Teacher"),
                Room: GetString(lesson, "Room"),
                MeetLink: GetString(lesson, "HangoutLink")));
        }

        result.Sort((a, b) => a.StartDate.CompareTo(b.StartDate));
        return result;
    }

    private static CdvApiException BuildApiException(string fallbackMessage, HttpResponseMessage response, string responseContent)
    {
        var message = fallbackMessage;

        if (!string.IsNullOrWhiteSpace(responseContent))
        {
            try
            {
                using var document = JsonDocument.Parse(responseContent);
                if (document.RootElement.TryGetProperty("message", out var messageEl))
                {
                    var serverMessage = messageEl.GetString();
                    if (!string.IsNullOrWhiteSpace(serverMessage))
                    {
                        message = serverMessage;
                    }
                }
            }
            catch
            {
                // Response body is not JSON; preserve fallback message.
            }
        }

        return new CdvApiException(message, response.StatusCode);
    }

    private static string GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return string.Empty;
        }

        return property.ValueKind switch
        {
            JsonValueKind.Null => string.Empty,
            JsonValueKind.String => property.GetString() ?? string.Empty,
            JsonValueKind.Number => property.ToString(),
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            _ => property.ToString(),
        };
    }

    private static DateTime ParseServerDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DateTime.MinValue;
        }

        var parsed = DateTime.Parse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind | DateTimeStyles.AllowWhiteSpaces);

        return parsed.Kind == DateTimeKind.Unspecified ? parsed : parsed.ToLocalTime();
    }
}

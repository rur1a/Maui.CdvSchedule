using System.Text;
using System.Text.Json;
using Maui.CdvSchedule.Application.Abstractions;
using Maui.CdvSchedule.Domain.Models;

namespace Maui.CdvSchedule.App.Infrastructure.Security;

public sealed class JwtTokenDecoder : ITokenDecoder
{
    public UserToken Decode(string tokenEncoded)
    {
        if (string.IsNullOrWhiteSpace(tokenEncoded))
        {
            throw new InvalidOperationException("JWT token is empty.");
        }

        var parts = tokenEncoded.Split('.');
        if (parts.Length < 2)
        {
            throw new InvalidOperationException("JWT token format is invalid.");
        }

        var payloadBytes = Convert.FromBase64String(PadBase64(parts[1].Replace('-', '+').Replace('_', '/')));
        using var document = JsonDocument.Parse(payloadBytes);

        if (!document.RootElement.TryGetProperty("data", out var data))
        {
            throw new InvalidOperationException("JWT token payload does not contain 'data'.");
        }

        return new UserToken(
            UserId: data.GetProperty("verbisId").GetInt32(),
            UserEmail: data.GetProperty("login").GetString() ?? string.Empty,
            UserType: data.GetProperty("userType").GetString() ?? string.Empty,
            UserName: data.GetProperty("displayName").GetString() ?? string.Empty,
            UserAlbumNumber: data.GetProperty("album").GetString() ?? string.Empty);
    }

    private static string PadBase64(string input)
    {
        var padding = 4 - (input.Length % 4);
        if (padding is > 0 and < 4)
        {
            input = input + new string('=', padding);
        }

        return input;
    }
}


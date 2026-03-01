namespace Maui.CdvSchedule.App.Infrastructure.Api;

public sealed class CdvApiOptions
{
    public const string SectionName = "CdvApi";

    public string BaseUrl { get; set; } = "https://api.cdv.pl/mobilnecdv-api/";

    public int TimeoutSeconds { get; set; } = 30;
}

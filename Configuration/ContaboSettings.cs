namespace ContaboSnapshotService.Configuration;

public class ContaboSettings
{
    public const string SectionName = "Contabo";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string ApiUser { get; set; } = string.Empty;
    public string ApiPassword { get; set; } = string.Empty;
    public string AuthUrl { get; set; } = "https://auth.contabo.com/auth/realms/contabo/protocol/openid-connect/token";
    public string ApiBaseUrl { get; set; } = "https://api.contabo.com";
}

public class SnapshotScheduleSettings
{
    public const string SectionName = "SnapshotSchedule";

    public string DayOfWeek { get; set; } = "Sunday";
    public string TimeOfDay { get; set; } = "02:00";
}

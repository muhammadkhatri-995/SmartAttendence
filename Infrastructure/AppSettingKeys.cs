namespace SmartAttendance.Web.Infrastructure;

public static class AppSettingKeys
{
    public const string InstitutionName = "MUET";
    public const string TimeZoneId = "Asia/Karachi";
    public const string CorrectionWindowHours = "24";

    public static readonly IReadOnlyDictionary<string, string> Defaults =
        new Dictionary<string, string>
        {
            [InstitutionName] =
                "Mehran University of Engineering & Technology, Jamshoro",

            [TimeZoneId] = "Asia/Karachi",

            [CorrectionWindowHours] = "24"
        };
}
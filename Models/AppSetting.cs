namespace SmartAttendance.Web.Models;

/// <summary>Non-secret, admin-editable settings. Never store credentials here.</summary>
public class AppSetting
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
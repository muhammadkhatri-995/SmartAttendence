namespace SmartAttendance.Web.Services.Interfaces;

public interface IAppSettingsService
{
    /// <summary>Returns the stored value, or the built-in default if the key is missing.</summary>
    Task<string> GetAsync(string key, CancellationToken ct = default);
}
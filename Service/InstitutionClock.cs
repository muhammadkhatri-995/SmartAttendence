using SmartAttendance.Web.Infrastructure;
using SmartAttendance.Web.Services.Interfaces;

namespace SmartAttendance.Web.Services;

public class InstitutionClock : IInstitutionClock
{
    private readonly IAppSettingsService _settings;
    private readonly ILogger<InstitutionClock> _logger;

    public InstitutionClock(IAppSettingsService settings, ILogger<InstitutionClock> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task<DateOnly> TodayAsync(CancellationToken ct = default)
    {
        var tzId = await _settings.GetAsync(AppSettingKeys.TimeZoneId, ct);

        TimeZoneInfo zone;
        try
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById(tzId);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            _logger.LogWarning("Time zone '{TimeZone}' was not found; falling back to UTC.", tzId);
            zone = TimeZoneInfo.Utc;
        }

        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
        return DateOnly.FromDateTime(local);
    }
}
using Microsoft.EntityFrameworkCore;
using SmartAttendance.Web.Data;
using SmartAttendance.Web.Infrastructure;
using SmartAttendance.Web.Services.Interfaces;

namespace SmartAttendance.Web.Services;

public class AppSettingsService : IAppSettingsService
{
    private readonly AppDbContext _db;

    public AppSettingsService(AppDbContext db) => _db = db;

    public async Task<string> GetAsync(string key, CancellationToken ct = default)
    {
        var value = await _db.AppSettings.AsNoTracking()
            .Where(s => s.Key == key)
            .Select(s => s.Value)
            .FirstOrDefaultAsync(ct);

        if (value is not null) return value;

        return AppSettingKeys.Defaults.TryGetValue(key, out var fallback) ? fallback : string.Empty;
    }
}
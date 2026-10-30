using Microsoft.EntityFrameworkCore;
using SmartAttendance.Web.Data;
using SmartAttendance.Web.Models;

namespace SmartAttendance.Web.Infrastructure;

public static class DbContextExtensions
{
    /// <summary>The app is for one class: the newest active one.</summary>
    public static Task<ClassRoom?> CurrentClassAsync(this AppDbContext db, CancellationToken ct) =>
        db.ClassRooms.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.AcademicYear)
            .ThenBy(c => c.ClassName)
            .ThenBy(c => c.Section)
            .FirstOrDefaultAsync(ct);
}
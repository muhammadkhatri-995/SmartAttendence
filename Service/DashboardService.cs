using Microsoft.EntityFrameworkCore;
using SmartAttendance.Models;
using SmartAttendance.Web.Data;
using SmartAttendance.Web.Infrastructure;
using SmartAttendance.Web.Models;
using SmartAttendance.Web.Services.Interfaces;
using SmartAttendance.Web.ViewModels;

namespace SmartAttendance.Web.Services;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;
    private readonly IInstitutionClock _clock;
    private readonly IAppSettingsService _settings;

    public DashboardService(AppDbContext db, IInstitutionClock clock, IAppSettingsService settings)
    {
        _db = db;
        _clock = clock;
        _settings = settings;
    }

    public async Task<AdminDashboardViewModel> GetAdminAsync(CancellationToken ct = default)
    {
        var today = await _clock.TodayAsync(ct);

        // A DbContext is not thread-safe, so these run one after another (each is a cheap COUNT).
        return new AdminDashboardViewModel
        {
            InstitutionName = await _settings.GetAsync(AppSettingKeys.InstitutionName, ct),
            Today = today,
            ActiveTeachers = await _db.Teachers.CountAsync(t => t.IsActive, ct),
            ActiveSubjects = await _db.Subjects.CountAsync(s => s.IsActive, ct),
            ActiveClasses = await _db.ClassRooms.CountAsync(c => c.IsActive, ct),
            ActiveStudents = await _db.Students.CountAsync(s => s.IsActive, ct),
            ActiveAssignments = await _db.TeacherAssignments.CountAsync(a => a.IsActive, ct),
            SubmittedToday = await _db.AttendanceSessions
                .CountAsync(s => s.AttendanceDate == today && s.Status == AttendanceStatus.Submitted, ct),
            DraftsToday = await _db.AttendanceSessions
                .CountAsync(s => s.AttendanceDate == today && s.Status == AttendanceStatus.Draft, ct),
            FailedNotifications = await _db.NotificationLogs
                .CountAsync(n => n.Status == NotificationStatus.Failed, ct)
        };
    }

    public async Task<TeacherDashboardViewModel> GetTeacherAsync(string userId, CancellationToken ct = default)
    {
        var today = await _clock.TodayAsync(ct);

        // Assignments the user owns as a teacher...
        var own = _db.TeacherAssignments
            .Where(a => a.Teacher.ApplicationUserId == userId && a.Teacher.IsActive);

        // ...and assignments an admin delegated to them as an operator.
        var delegated = _db.OperatorAssignments
            .Where(o => o.UserId == userId && o.IsActive)
            .Select(o => o.TeacherAssignment);

        var items = new List<AssignedClassItem>();
        items.AddRange(await ProjectAsync(own, false, today, ct));
        items.AddRange(await ProjectAsync(delegated, true, today, ct));

        return new TeacherDashboardViewModel
        {
            Today = today,
            HasTeacherProfile = await _db.Teachers.AnyAsync(t => t.ApplicationUserId == userId, ct),
            Assignments = items
                .GroupBy(i => i.TeacherAssignmentId)
                .Select(g => g.First())
                .ToList()
        };
    }

    private static async Task<List<AssignedClassItem>> ProjectAsync(
        IQueryable<TeacherAssignment> query, bool isDelegated, DateOnly today, CancellationToken ct)
    {
        var rows = await query
            .Where(a => a.IsActive && a.Subject.IsActive && a.ClassRoom.IsActive)
            .OrderBy(a => a.ClassRoom.ClassName)
            .ThenBy(a => a.ClassRoom.Section)
            .ThenBy(a => a.Subject.SubjectName)
            .Select(a => new
            {
                a.TeacherAssignmentId,
                a.ClassRoom.ClassName,
                a.ClassRoom.Section,
                a.Subject.SubjectCode,
                a.Subject.SubjectName,
                a.AcademicYear,
                TodayStatus = a.AttendanceSessions
                    .Where(s => s.AttendanceDate == today)
                    .Select(s => (AttendanceStatus?)s.Status)
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        return rows.Select(r => new AssignedClassItem
        {
            TeacherAssignmentId = r.TeacherAssignmentId,
            ClassName = r.ClassName,
            Section = r.Section,
            SubjectCode = r.SubjectCode,
            SubjectName = r.SubjectName,
            AcademicYear = r.AcademicYear,
            TodayStatus = r.TodayStatus,
            IsDelegated = isDelegated
        }).ToList();
    }
}
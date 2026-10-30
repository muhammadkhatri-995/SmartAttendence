using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartAttendance.Models;
using SmartAttendance.Web.Data;
using SmartAttendance.Web.Infrastructure;
using SmartAttendance.Web.Models;
using SmartAttendance.Web.Services.Interfaces;
using SmartAttendance.Web.ViewModels;

namespace SmartAttendance.Web.Controllers;

[Authorize]
public class AttendanceController : BaseController
{
    private const int PageSize = 15;

    private readonly AppDbContext _db;
    private readonly IInstitutionClock _clock;
    private readonly ILogger<AttendanceController> _logger;

    public AttendanceController(AppDbContext db, IInstitutionClock clock, ILogger<AttendanceController> logger)
    {
        _db = db;
        _clock = clock;
        _logger = logger;
    }

    // ---------- Today's lectures ----------
    [HttpGet]
    public async Task<IActionResult> Index(DateOnly? date, CancellationToken ct)
    {
        var today = await _clock.TodayAsync(ct);
        var d = date ?? today;
        if (d > today) d = today;

        var cls = await _db.CurrentClassAsync(ct);
        if (cls is null)
        {
            SetWarning("Create your class first.");
            return RedirectToAction("Create", "Classes");
        }

        var dow = d.DayOfWeek;
        var slots = await _db.TimetableSlots.AsNoTracking()
            .Where(s => s.DayOfWeek == dow && s.IsActive &&
                        s.TeacherAssignment.ClassRoomId == cls.ClassRoomId && s.TeacherAssignment.IsActive)
            .OrderBy(s => s.StartTime)
            .Select(s => new SlotCard
            {
                AssignmentId = s.TeacherAssignmentId,
                Start = s.StartTime,
                Subject = s.TeacherAssignment.Subject.SubjectName,
                Teacher = s.TeacherAssignment.Teacher.FullName,
                SessionId = s.TeacherAssignment.AttendanceSessions
                    .Where(x => x.AttendanceDate == d)
                    .Select(x => (int?)x.AttendanceSessionId)
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        return View(new AttendanceIndexViewModel { Date = d, Today = today, Class = cls, Slots = slots });
    }

    // ---------- Mark ----------
    [HttpGet]
    public async Task<IActionResult> Mark(int assignmentId, DateOnly date, CancellationToken ct)
    {
        var today = await _clock.TodayAsync(ct);
        if (date > today)
        {
            SetError("You cannot take attendance for a future date.");
            return RedirectToAction(nameof(Index));
        }

        var a = await _db.TeacherAssignments.AsNoTracking()
            .Where(x => x.TeacherAssignmentId == assignmentId && x.IsActive)
            .Select(x => new
            {
                x.ClassRoomId,
                x.ClassRoom.ClassName,
                x.ClassRoom.Section,
                Subject = x.Subject.SubjectName,
                Teacher = x.Teacher.FullName
            })
            .FirstOrDefaultAsync(ct);
        if (a is null) return NotFound();

        var absentIds = (await _db.AttendanceRecords.AsNoTracking()
                .Where(r => r.AttendanceSession.TeacherAssignmentId == assignmentId &&
                            r.AttendanceSession.AttendanceDate == date && !r.IsPresent)
                .Select(r => r.StudentId).ToListAsync(ct)).ToHashSet();

        var hasSession = await _db.AttendanceSessions
            .AnyAsync(s => s.TeacherAssignmentId == assignmentId && s.AttendanceDate == date, ct);

        var students = await _db.Students.AsNoTracking()
            .Where(s => s.ClassRoomId == a.ClassRoomId && s.IsActive)
            .OrderBy(s => s.RollNumber.Length).ThenBy(s => s.RollNumber)
            .Select(s => new MarkStudent { Id = s.StudentId, Roll = s.RollNumber, Name = s.FullName })
            .ToListAsync(ct);

        foreach (var s in students) s.IsAbsent = absentIds.Contains(s.Id);

        return View(new MarkViewModel
        {
            AssignmentId = assignmentId,
            Date = date,
            ClassLabel = $"{a.ClassName} - {a.Section}",
            Subject = a.Subject,
            Teacher = a.Teacher,
            IsEdit = hasSession,
            Students = students
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Mark(int assignmentId, DateOnly date, List<int>? absentIds, CancellationToken ct)
    {
        var today = await _clock.TodayAsync(ct);
        if (date > today)
        {
            SetError("You cannot take attendance for a future date.");
            return RedirectToAction(nameof(Index));
        }

        var assignment = await _db.TeacherAssignments.AsNoTracking()
            .FirstOrDefaultAsync(x => x.TeacherAssignmentId == assignmentId && x.IsActive, ct);
        if (assignment is null) return NotFound();

        // Only students of this class count; ids from the browser are never trusted.
        var students = await _db.Students
            .Where(s => s.ClassRoomId == assignment.ClassRoomId && s.IsActive)
            .ToListAsync(ct);

        if (students.Count == 0)
        {
            SetError("This class has no active students.");
            return RedirectToAction(nameof(Index), new { date = date.ToString("yyyy-MM-dd") });
        }

        var absent = (absentIds ?? new List<int>()).ToHashSet();

        var session = await _db.AttendanceSessions.Include(s => s.Records)
            .FirstOrDefaultAsync(s => s.TeacherAssignmentId == assignmentId && s.AttendanceDate == date, ct);

        var now = DateTime.UtcNow;
        if (session is null)
        {
            session = new AttendanceSession
            {
                TeacherAssignmentId = assignmentId,
                AttendanceDate = date,
                CreatedByUserId = User.GetUserId()
            };
            _db.AttendanceSessions.Add(session);
        }

        session.Status = AttendanceStatus.Submitted;
        session.SubmittedAt = now;

        var existing = session.Records.ToDictionary(r => r.StudentId);
        foreach (var st in students)
        {
            var present = !absent.Contains(st.StudentId);
            if (existing.TryGetValue(st.StudentId, out var rec))
            {
                rec.IsPresent = present;
                rec.MarkedAt = now;
            }
            else
            {
                session.Records.Add(new AttendanceRecord { StudentId = st.StudentId, IsPresent = present, MarkedAt = now });
            }
        }

        try
        {
            // One SaveChanges is a single database transaction: all records or none.
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Saving attendance failed for assignment {AssignmentId}", assignmentId);
            SetError("Could not save attendance. Please try again.");
            return RedirectToAction(nameof(Index), new { date = date.ToString("yyyy-MM-dd") });
        }

        SetSuccess("Attendance saved.");
        return RedirectToAction(nameof(Result), new { id = session.AttendanceSessionId });
    }

    // ---------- Result + WhatsApp ----------
    [HttpGet]
    public async Task<IActionResult> Result(int id, CancellationToken ct)
    {
        var s = await _db.AttendanceSessions.AsNoTracking()
            .Where(x => x.AttendanceSessionId == id)
            .Select(x => new
            {
                x.AttendanceDate,
                x.TeacherAssignmentId,
                x.TeacherAssignment.ClassRoom.ClassName,
                x.TeacherAssignment.ClassRoom.Section,
                Subject = x.TeacherAssignment.Subject.SubjectName,
                Teacher = x.TeacherAssignment.Teacher.FullName,
                Phone = x.TeacherAssignment.Teacher.PhoneNumber
            })
            .FirstOrDefaultAsync(ct);
        if (s is null) return NotFound();

        var records = await _db.AttendanceRecords.AsNoTracking()
            .Where(r => r.AttendanceSessionId == id)
            .Select(r => new { r.Student.RollNumber, r.IsPresent })
            .ToListAsync(ct);

        var classLabel = $"{s.ClassName} - {s.Section}";
        var absentRolls = records.Where(r => !r.IsPresent).Select(r => r.RollNumber).ToList();
        var present = records.Count(r => r.IsPresent);
        var message = AttendanceMessage.Build(classLabel, s.Subject, s.AttendanceDate, records.Count, present, absentRolls);

        return View(new AttendanceResultViewModel
        {
            AssignmentId = s.TeacherAssignmentId,
            Date = s.AttendanceDate,
            ClassLabel = classLabel,
            Subject = s.Subject,
            Teacher = s.Teacher,
            Total = records.Count,
            Present = present,
            AbsentRolls = absentRolls.OrderBy(r => r.Length).ThenBy(r => r, StringComparer.Ordinal).ToList(),
            Message = message,
            WhatsAppUrl = AttendanceMessage.WhatsAppUrl(s.Phone, message)
        });
    }

    // ---------- History ----------
    [HttpGet]
    public async Task<IActionResult> History(int page = 1, CancellationToken ct = default)
    {
        var query = _db.AttendanceSessions.AsNoTracking()
            .Where(s => s.Status == AttendanceStatus.Submitted)
            .OrderByDescending(s => s.AttendanceDate).ThenByDescending(s => s.SubmittedAt)
            .Select(s => new HistoryRow
            {
                Id = s.AttendanceSessionId,
                Date = s.AttendanceDate,
                Subject = s.TeacherAssignment.Subject.SubjectName,
                Teacher = s.TeacherAssignment.Teacher.FullName,
                Present = s.Records.Count(r => r.IsPresent),
                Absent = s.Records.Count(r => !r.IsPresent)
            });

        return View(await PagedResult<HistoryRow>.CreateAsync(query, page, PageSize, ct));
    }
}
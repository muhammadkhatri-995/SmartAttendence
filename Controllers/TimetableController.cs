using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartAttendance.Web.Data;
using SmartAttendance.Web.Infrastructure;
using SmartAttendance.Web.Models;
using SmartAttendance.Web.ViewModels;

namespace SmartAttendance.Web.Controllers;

[Authorize]
public class TimetableController : BaseController
{
    private readonly AppDbContext _db;

    public TimetableController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var cls = await _db.CurrentClassAsync(ct);
        if (cls is null)
        {
            SetWarning("Create your class first.");
            return RedirectToAction("Create", "Classes");
        }

        var rows = await _db.TimetableSlots.AsNoTracking()
            .Where(s => s.TeacherAssignment.ClassRoomId == cls.ClassRoomId)
            .OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime)
            .Select(s => new TimetableRow
            {
                Id = s.TimetableSlotId,
                Day = s.DayOfWeek,
                Start = s.StartTime,
                SubjectCode = s.TeacherAssignment.Subject.SubjectCode,
                SubjectName = s.TeacherAssignment.Subject.SubjectName,
                TeacherName = s.TeacherAssignment.Teacher.FullName
            })
            .ToListAsync(ct);

        var subjects = await _db.Subjects.AsNoTracking().Where(s => s.IsActive)
            .OrderBy(s => s.SubjectName)
            .Select(s => new SelectListItem(s.SubjectName + " (" + s.SubjectCode + ")", s.SubjectId.ToString()))
            .ToListAsync(ct);

        var teachers = await _db.Teachers.AsNoTracking().Where(t => t.IsActive)
            .OrderBy(t => t.FullName)
            .Select(t => new SelectListItem(t.FullName, t.TeacherId.ToString()))
            .ToListAsync(ct);

        return View(new TimetableIndexViewModel
        {
            Class = cls,
            Rows = rows,
            Subjects = subjects,
            Teachers = teachers
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(DayOfWeek day, TimeOnly startTime, int subjectId, int teacherId, CancellationToken ct)
    {
        var cls = await _db.CurrentClassAsync(ct);
        if (cls is null) return RedirectToAction("Create", "Classes");

        if (!await _db.Subjects.AnyAsync(s => s.SubjectId == subjectId && s.IsActive, ct) ||
            !await _db.Teachers.AnyAsync(t => t.TeacherId == teacherId && t.IsActive, ct))
        {
            SetError("Choose a valid subject and teacher.");
            return RedirectToAction(nameof(Index));
        }

        if (await _db.TimetableSlots.AnyAsync(s =>
                s.DayOfWeek == day && s.StartTime == startTime &&
                s.TeacherAssignment.ClassRoomId == cls.ClassRoomId, ct))
        {
            SetError("A lecture already exists at that day and time.");
            return RedirectToAction(nameof(Index));
        }

        // The timetable slot points to a teacher assignment (class + subject + teacher).
        // Find it or create it automatically, so you never manage assignments by hand.
        var assignment = await _db.TeacherAssignments.FirstOrDefaultAsync(a =>
            a.TeacherId == teacherId && a.SubjectId == subjectId &&
            a.ClassRoomId == cls.ClassRoomId && a.AcademicYear == cls.AcademicYear, ct);

        if (assignment is null)
        {
            assignment = new TeacherAssignment
            {
                TeacherId = teacherId,
                SubjectId = subjectId,
                ClassRoomId = cls.ClassRoomId,
                AcademicYear = cls.AcademicYear
            };
            _db.TeacherAssignments.Add(assignment);
        }
        else if (!assignment.IsActive)
        {
            assignment.IsActive = true;
        }

        _db.TimetableSlots.Add(new TimetableSlot
        {
            TeacherAssignment = assignment,
            DayOfWeek = day,
            StartTime = startTime
        });

        await _db.SaveChangesAsync(ct);
        SetSuccess("Lecture added to the timetable.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var slot = await _db.TimetableSlots.FirstOrDefaultAsync(s => s.TimetableSlotId == id, ct);
        if (slot is null) return NotFound();

        _db.TimetableSlots.Remove(slot);
        await _db.SaveChangesAsync(ct);
        SetSuccess("Lecture removed. Past attendance is not affected.");
        return RedirectToAction(nameof(Index));
    }
}
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartAttendance.Web.Data;
using SmartAttendance.Web.Infrastructure;
using SmartAttendance.Web.Models;
using SmartAttendance.Web.ViewModels;

namespace SmartAttendance.Web.Controllers;

[Authorize]
public class StudentsController : BaseController
{
    // "roll name", "roll,name" or "roll<TAB>name" (what you get when pasting from Excel)
    private static readonly Regex LineRx = new(@"^\s*([^\s,;\t]+)[\s,;\t]+(.+?)\s*$", RegexOptions.Compiled);

    private readonly AppDbContext _db;
    private readonly ILogger<StudentsController> _logger;

    public StudentsController(AppDbContext db, ILogger<StudentsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var cls = await _db.CurrentClassAsync(ct);
        if (cls is null)
        {
            SetWarning("Create your class first.");
            return RedirectToAction("Create", "Classes");
        }

        var students = await _db.Students.AsNoTracking()
            .Where(s => s.ClassRoomId == cls.ClassRoomId)
            .OrderBy(s => s.RollNumber.Length).ThenBy(s => s.RollNumber)
            .ToListAsync(ct);

        return View(new StudentsIndexViewModel { Class = cls, Students = students });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(string? bulkText, CancellationToken ct)
    {
        var cls = await _db.CurrentClassAsync(ct);
        if (cls is null) return RedirectToAction("Create", "Classes");

        if (string.IsNullOrWhiteSpace(bulkText))
        {
            SetError("Write at least one line: roll number and name.");
            return RedirectToAction(nameof(Index));
        }

        var existing = (await _db.Students
                .Where(s => s.ClassRoomId == cls.ClassRoomId)
                .Select(s => s.RollNumber).ToListAsync(ct))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        int added = 0, duplicates = 0;
        var bad = new List<string>();

        foreach (var raw in bulkText.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0) continue;

            var m = LineRx.Match(line);
            if (!m.Success || m.Groups[1].Value.Length > 30 || m.Groups[2].Value.Length > 150)
            {
                bad.Add(line);
                continue;
            }

            var roll = m.Groups[1].Value;
            if (!existing.Add(roll)) { duplicates++; continue; }

            _db.Students.Add(new Student
            {
                ClassRoomId = cls.ClassRoomId,
                RollNumber = roll,
                FullName = m.Groups[2].Value
            });
            added++;
        }

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Student add failed");
            SetError("Could not save the students. Please try again.");
            return RedirectToAction(nameof(Index));
        }

        var msg = $"{added} student(s) added.";
        if (duplicates > 0) msg += $" {duplicates} skipped (roll number already exists).";
        if (added > 0) SetSuccess(msg); else SetWarning(msg);
        if (bad.Count > 0) SetError($"{bad.Count} line(s) not understood: {string.Join(" | ", bad.Take(5))}");

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var s = await _db.Students.AsNoTracking().FirstOrDefaultAsync(x => x.StudentId == id, ct);
        return s is null
            ? NotFound()
            : View(new StudentEditViewModel { Id = s.StudentId, RollNumber = s.RollNumber, FullName = s.FullName });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, StudentEditViewModel model, CancellationToken ct)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View(model);

        var s = await _db.Students.FirstOrDefaultAsync(x => x.StudentId == id, ct);
        if (s is null) return NotFound();

        var roll = model.RollNumber.Trim();
        if (await _db.Students.AnyAsync(x => x.ClassRoomId == s.ClassRoomId && x.RollNumber == roll && x.StudentId != id, ct))
        {
            ModelState.AddModelError(nameof(model.RollNumber), "This roll number already exists in the class.");
            return View(model);
        }

        s.RollNumber = roll;
        s.FullName = model.FullName.Trim();
        await _db.SaveChangesAsync(ct);

        SetSuccess("Student updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id, CancellationToken ct)
    {
        var s = await _db.Students.FirstOrDefaultAsync(x => x.StudentId == id, ct);
        if (s is null) return NotFound();

        s.IsActive = !s.IsActive;
        await _db.SaveChangesAsync(ct);

        SetSuccess(s.IsActive ? "Student activated." : "Student deactivated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var s = await _db.Students.FirstOrDefaultAsync(x => x.StudentId == id, ct);
        if (s is null) return NotFound();

        if (await _db.AttendanceRecords.AnyAsync(r => r.StudentId == id, ct))
        {
            SetError("This student has attendance history and cannot be deleted. Deactivate instead.");
            return RedirectToAction(nameof(Index));
        }

        _db.Students.Remove(s);
        await _db.SaveChangesAsync(ct);
        SetSuccess("Student deleted.");
        return RedirectToAction(nameof(Index));
    }
}
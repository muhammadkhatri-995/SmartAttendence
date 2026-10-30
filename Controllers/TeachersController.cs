using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartAttendance.Web.Data;
using SmartAttendance.Web.Infrastructure;
using SmartAttendance.Web.Models;
using SmartAttendance.Web.ViewModels;

namespace SmartAttendance.Web.Controllers;

[Authorize]
public class TeachersController : BaseController
{
    private const string BadPhone = "Invalid WhatsApp number. Example: 03001234567 or +923001234567.";
    private readonly AppDbContext _db;

    public TeachersController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct) =>
        View(await _db.Teachers.AsNoTracking().OrderBy(t => t.FullName).ToListAsync(ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(TeacherFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            SetError(ModelState.ErrorText());
            return RedirectToAction(nameof(Index));
        }

        var phone = PhoneHelper.Normalize(model.PhoneNumber);
        if (phone is null)
        {
            SetError(BadPhone);
            return RedirectToAction(nameof(Index));
        }

        _db.Teachers.Add(new Teacher { FullName = model.FullName.Trim(), PhoneNumber = phone });

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            SetError("Could not save the teacher: " + ex.GetBaseException().Message);
            return RedirectToAction(nameof(Index));
        }

        SetSuccess("Teacher added.");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var t = await _db.Teachers.AsNoTracking().FirstOrDefaultAsync(x => x.TeacherId == id, ct);
        return t is null
            ? NotFound()
            : View(new TeacherFormViewModel { Id = t.TeacherId, FullName = t.FullName, PhoneNumber = t.PhoneNumber });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TeacherFormViewModel model, CancellationToken ct)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View(model);

        var phone = PhoneHelper.Normalize(model.PhoneNumber);
        if (phone is null)
        {
            ModelState.AddModelError(nameof(model.PhoneNumber), BadPhone);
            return View(model);
        }

        var t = await _db.Teachers.FirstOrDefaultAsync(x => x.TeacherId == id, ct);
        if (t is null) return NotFound();

        t.FullName = model.FullName.Trim();
        t.PhoneNumber = phone;
        await _db.SaveChangesAsync(ct);

        SetSuccess("Teacher updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id, CancellationToken ct)
    {
        var t = await _db.Teachers.FirstOrDefaultAsync(x => x.TeacherId == id, ct);
        if (t is null) return NotFound();

        t.IsActive = !t.IsActive;
        await _db.SaveChangesAsync(ct);
        SetSuccess(t.IsActive ? "Teacher activated." : "Teacher deactivated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var t = await _db.Teachers.FirstOrDefaultAsync(x => x.TeacherId == id, ct);
        if (t is null) return NotFound();

        if (await _db.TeacherAssignments.AnyAsync(a => a.TeacherId == id, ct))
        {
            SetError("This teacher is used in the timetable or attendance history. Deactivate instead.");
            return RedirectToAction(nameof(Index));
        }

        _db.Teachers.Remove(t);
        await _db.SaveChangesAsync(ct);
        SetSuccess("Teacher deleted.");
        return RedirectToAction(nameof(Index));
    }
}
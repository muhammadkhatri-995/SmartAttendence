using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartAttendance.Web.Data;
using SmartAttendance.Web.Infrastructure;
using SmartAttendance.Web.Models;
using SmartAttendance.Web.ViewModels;

namespace SmartAttendance.Web.Controllers;

[Authorize]
public class ClassesController : BaseController
{
    private readonly AppDbContext _db;
    private readonly ILogger<ClassesController> _logger;

    public ClassesController(AppDbContext db, ILogger<ClassesController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        var existing = await _db.CurrentClassAsync(ct);
        if (existing is not null) return RedirectToAction("Index", "Students");

        return View(new ClassesCreateInput());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClassesCreateInput model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);

        var existing = await _db.CurrentClassAsync(ct);
        if (existing is not null) return RedirectToAction("Index", "Students");

        _db.ClassRooms.Add(new ClassRoom
        {
            ClassName = model.ClassName.Trim(),
            Section = model.Section.Trim(),
            AcademicYear = model.AcademicYear.Trim(),
            Semester = model.Semester
        });

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Class create failed");
            ModelState.AddModelError(string.Empty, "Could not save the class: " + ex.GetBaseException().Message);
            return View(model);
        }

        SetSuccess($"Class '{model.ClassName.Trim()}' was created.");
        return RedirectToAction("Index", "Students");
    }
}
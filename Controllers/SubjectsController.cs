using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartAttendance.Web.Authorization;
using SmartAttendance.Web.Infrastructure;
using SmartAttendance.Web.Services.Interfaces;
using SmartAttendance.Web.ViewModels;

namespace SmartAttendance.Web.Controllers;

[Authorize(Policy = PolicyNames.AdminOnly)]
public class SubjectsController : BaseController
{
    private const int PageSize = 10;
    private readonly ISubjectService _subjects;

    public SubjectsController(ISubjectService subjects) => _subjects = subjects;

    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? status, int page = 1, CancellationToken ct = default)
    {
        var normalized = QueryHelpers.NormalizeStatus(status);
        var vm = new SubjectIndexViewModel
        {
            Search = search?.Trim(),
            Status = normalized,
            Result = await _subjects.ListAsync(search, normalized, page, PageSize, ct)
        };
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var vm = await _subjects.GetDetailsAsync(id, ct);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpGet]
    public IActionResult Create() => View(new SubjectFormViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SubjectFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _subjects.CreateAsync(model, ct);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        SetSuccess($"Subject '{model.SubjectName.Trim()}' was created.");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var vm = await _subjects.GetFormAsync(id, ct);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SubjectFormViewModel model, CancellationToken ct)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View(model);

        var result = await _subjects.UpdateAsync(model, ct);
        if (result.IsNotFound) return NotFound();
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        SetSuccess("Subject was updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Activate(int id, CancellationToken ct) => ToggleAsync(id, true, ct);

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Deactivate(int id, CancellationToken ct) => ToggleAsync(id, false, ct);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _subjects.DeleteAsync(id, ct);
        if (result.IsNotFound) return NotFound();

        if (result.Succeeded) SetSuccess("Subject was deleted.");
        else SetError(result.Error!);

        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> ToggleAsync(int id, bool active, CancellationToken ct)
    {
        var result = await _subjects.SetActiveAsync(id, active, ct);
        if (result.IsNotFound) return NotFound();

        SetSuccess(active ? "Subject was activated." : "Subject was deactivated.");
        return RedirectToAction(nameof(Index));
    }
}
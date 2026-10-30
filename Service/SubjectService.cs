using Microsoft.EntityFrameworkCore;
using SmartAttendance.Web.Data;
using SmartAttendance.Web.Infrastructure;
using SmartAttendance.Web.Models;
using SmartAttendance.Web.Services.Interfaces;
using SmartAttendance.Web.ViewModels;

namespace SmartAttendance.Web.Services;

public class SubjectService : ISubjectService
{
    private readonly AppDbContext _db;
    private readonly ILogger<SubjectService> _logger;

    public SubjectService(AppDbContext db, ILogger<SubjectService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<PagedResult<SubjectListItem>> ListAsync(
        string? search, string status, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Subjects.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = QueryHelpers.ContainsPattern(search);
            query = query.Where(s => EF.Functions.ILike(s.SubjectCode, pattern) ||
                                     EF.Functions.ILike(s.SubjectName, pattern));
        }

        query = QueryHelpers.NormalizeStatus(status) switch
        {
            "active" => query.Where(s => s.IsActive),
            "inactive" => query.Where(s => !s.IsActive),
            _ => query
        };

        var projected = query
            .OrderBy(s => s.SubjectCode)
            .Select(s => new SubjectListItem
            {
                Id = s.SubjectId,
                Code = s.SubjectCode,
                Name = s.SubjectName,
                IsActive = s.IsActive,
                AssignmentCount = s.TeacherAssignments.Count()
            });

        return await PagedResult<SubjectListItem>.CreateAsync(projected, page, pageSize, ct);
    }

    public Task<SubjectDetailsViewModel?> GetDetailsAsync(int id, CancellationToken ct = default) =>
        _db.Subjects.AsNoTracking()
            .Where(s => s.SubjectId == id)
            .Select(s => new SubjectDetailsViewModel
            {
                Id = s.SubjectId,
                Code = s.SubjectCode,
                Name = s.SubjectName,
                Description = s.Description,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt,
                AssignmentCount = s.TeacherAssignments.Count()
            })
            .FirstOrDefaultAsync(ct);

    public Task<SubjectFormViewModel?> GetFormAsync(int id, CancellationToken ct = default) =>
        _db.Subjects.AsNoTracking()
            .Where(s => s.SubjectId == id)
            .Select(s => new SubjectFormViewModel
            {
                Id = s.SubjectId,
                SubjectCode = s.SubjectCode,
                SubjectName = s.SubjectName,
                Description = s.Description
            })
            .FirstOrDefaultAsync(ct);

    public async Task<ServiceResult> CreateAsync(SubjectFormViewModel model, CancellationToken ct = default)
    {
        var code = model.SubjectCode.Trim().ToUpperInvariant();

        if (await _db.Subjects.AnyAsync(s => s.SubjectCode == code, ct))
            return ServiceResult.Fail($"A subject with code '{code}' already exists.");

        _db.Subjects.Add(new Subject
        {
            SubjectCode = code,
            SubjectName = model.SubjectName.Trim(),
            Description = Clean(model.Description)
        });

        return await SaveAsync(code, ct);
    }

    public async Task<ServiceResult> UpdateAsync(SubjectFormViewModel model, CancellationToken ct = default)
    {
        var subject = await _db.Subjects.FirstOrDefaultAsync(s => s.SubjectId == model.Id, ct);
        if (subject is null) return ServiceResult.NotFound();

        var code = model.SubjectCode.Trim().ToUpperInvariant();

        if (await _db.Subjects.AnyAsync(s => s.SubjectCode == code && s.SubjectId != model.Id, ct))
            return ServiceResult.Fail($"A subject with code '{code}' already exists.");

        subject.SubjectCode = code;
        subject.SubjectName = model.SubjectName.Trim();
        subject.Description = Clean(model.Description);

        return await SaveAsync(code, ct);
    }

    public async Task<ServiceResult> SetActiveAsync(int id, bool isActive, CancellationToken ct = default)
    {
        var subject = await _db.Subjects.FirstOrDefaultAsync(s => s.SubjectId == id, ct);
        if (subject is null) return ServiceResult.NotFound();

        subject.IsActive = isActive;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken ct = default)
    {
        var subject = await _db.Subjects.FirstOrDefaultAsync(s => s.SubjectId == id, ct);
        if (subject is null) return ServiceResult.NotFound();

        if (await _db.TeacherAssignments.AnyAsync(a => a.SubjectId == id, ct))
            return ServiceResult.Fail(
                "This subject is used by teacher assignments and cannot be deleted. Deactivate it instead.");

        _db.Subjects.Remove(subject);

        try
        {
            await _db.SaveChangesAsync(ct);
            return ServiceResult.Ok();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Could not delete subject {SubjectId}", id);
            return ServiceResult.Fail("The subject could not be deleted because it is still referenced. Deactivate it instead.");
        }
    }

    private async Task<ServiceResult> SaveAsync(string code, CancellationToken ct)
    {
        try
        {
            await _db.SaveChangesAsync(ct);
            return ServiceResult.Ok();
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            // Two admins saving the same code at once: the database constraint catches it.
            return ServiceResult.Fail($"A subject with code '{code}' already exists.");
        }
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
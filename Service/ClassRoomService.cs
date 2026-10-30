using Microsoft.EntityFrameworkCore;
using SmartAttendance.Web.Data;
using SmartAttendance.Web.Infrastructure;
using SmartAttendance.Web.Models;
using SmartAttendance.Web.Services.Interfaces;
using SmartAttendance.Web.ViewModels;

namespace SmartAttendance.Web.Services;

public class ClassRoomService : IClassRoomService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ClassRoomService> _logger;

    public ClassRoomService(AppDbContext db, ILogger<ClassRoomService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<PagedResult<ClassListItem>> ListAsync(
        string? search, string status, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.ClassRooms.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = QueryHelpers.ContainsPattern(search);
            query = query.Where(c => EF.Functions.ILike(c.ClassName, pattern) ||
                                     EF.Functions.ILike(c.Section, pattern) ||
                                     EF.Functions.ILike(c.AcademicYear, pattern));
        }

        query = QueryHelpers.NormalizeStatus(status) switch
        {
            "active" => query.Where(c => c.IsActive),
            "inactive" => query.Where(c => !c.IsActive),
            _ => query
        };

        var projected = query
            .OrderByDescending(c => c.AcademicYear)
            .ThenBy(c => c.ClassName)
            .ThenBy(c => c.Section)
            .Select(c => new ClassListItem
            {
                Id = c.ClassRoomId,
                ClassName = c.ClassName,
                Section = c.Section,
                Semester = c.Semester,
                AcademicYear = c.AcademicYear,
                IsActive = c.IsActive,
                StudentCount = c.Students.Count(),
                AssignmentCount = c.TeacherAssignments.Count()
            });

        return await PagedResult<ClassListItem>.CreateAsync(projected, page, pageSize, ct);
    }

    public Task<ClassDetailsViewModel?> GetDetailsAsync(int id, CancellationToken ct = default) =>
        _db.ClassRooms.AsNoTracking()
            .Where(c => c.ClassRoomId == id)
            .Select(c => new ClassDetailsViewModel
            {
                Id = c.ClassRoomId,
                ClassName = c.ClassName,
                Section = c.Section,
                Semester = c.Semester,
                AcademicYear = c.AcademicYear,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                ActiveStudentCount = c.Students.Count(s => s.IsActive),
                TotalStudentCount = c.Students.Count(),
                AssignmentCount = c.TeacherAssignments.Count()
            })
            .FirstOrDefaultAsync(ct);

    public Task<ClassFormViewModel?> GetFormAsync(int id, CancellationToken ct = default) =>
        _db.ClassRooms.AsNoTracking()
            .Where(c => c.ClassRoomId == id)
            .Select(c => new ClassFormViewModel
            {
                Id = c.ClassRoomId,
                ClassName = c.ClassName,
                Section = c.Section,
                Semester = c.Semester,
                AcademicYear = c.AcademicYear
            })
            .FirstOrDefaultAsync(ct);

    public async Task<ServiceResult> CreateAsync(ClassFormViewModel model, CancellationToken ct = default)
    {
        var name = model.ClassName.Trim();
        var section = model.Section.Trim().ToUpperInvariant();
        var year = model.AcademicYear.Trim();

        if (await ExistsAsync(name, section, year, excludeId: null, ct))
            return ServiceResult.Fail($"The class {name} - {section} already exists for {year}.");

        _db.ClassRooms.Add(new ClassRoom
        {
            ClassName = name,
            Section = section,
            Semester = model.Semester,
            AcademicYear = year
        });

        return await SaveAsync(name, section, year, ct);
    }

    public async Task<ServiceResult> UpdateAsync(ClassFormViewModel model, CancellationToken ct = default)
    {
        var entity = await _db.ClassRooms.FirstOrDefaultAsync(c => c.ClassRoomId == model.Id, ct);
        if (entity is null) return ServiceResult.NotFound();

        var name = model.ClassName.Trim();
        var section = model.Section.Trim().ToUpperInvariant();
        var year = model.AcademicYear.Trim();

        if (year != entity.AcademicYear &&
            await _db.TeacherAssignments.AnyAsync(a => a.ClassRoomId == entity.ClassRoomId, ct))
        {
            return ServiceResult.Fail(
                "The academic year cannot be changed because this class already has teacher assignments.");
        }

        if (await ExistsAsync(name, section, year, excludeId: entity.ClassRoomId, ct))
            return ServiceResult.Fail($"The class {name} - {section} already exists for {year}.");

        entity.ClassName = name;
        entity.Section = section;
        entity.Semester = model.Semester;
        entity.AcademicYear = year;

        return await SaveAsync(name, section, year, ct);
    }

    public async Task<ServiceResult> SetActiveAsync(int id, bool isActive, CancellationToken ct = default)
    {
        var entity = await _db.ClassRooms.FirstOrDefaultAsync(c => c.ClassRoomId == id, ct);
        if (entity is null) return ServiceResult.NotFound();

        entity.IsActive = isActive;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.ClassRooms.FirstOrDefaultAsync(c => c.ClassRoomId == id, ct);
        if (entity is null) return ServiceResult.NotFound();

        var students = await _db.Students.CountAsync(s => s.ClassRoomId == id, ct);
        if (students > 0)
            return ServiceResult.Fail(
                $"This class has {students} student record(s) and cannot be deleted. Deactivate it instead.");

        if (await _db.TeacherAssignments.AnyAsync(a => a.ClassRoomId == id, ct))
            return ServiceResult.Fail(
                "This class is used by teacher assignments and cannot be deleted. Deactivate it instead.");

        _db.ClassRooms.Remove(entity);

        try
        {
            await _db.SaveChangesAsync(ct);
            return ServiceResult.Ok();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Could not delete class {ClassRoomId}", id);
            return ServiceResult.Fail("The class could not be deleted because it is still referenced. Deactivate it instead.");
        }
    }

    // Case-insensitive duplicate check (the database index itself is case-sensitive).
    private Task<bool> ExistsAsync(string name, string section, string year, int? excludeId, CancellationToken ct)
    {
        var lowerName = name.ToLower();
        return _db.ClassRooms.AnyAsync(c =>
            c.ClassName.ToLower() == lowerName &&
            c.Section == section &&
            c.AcademicYear == year &&
            (excludeId == null || c.ClassRoomId != excludeId), ct);
    }

    private async Task<ServiceResult> SaveAsync(string name, string section, string year, CancellationToken ct)
    {
        try
        {
            await _db.SaveChangesAsync(ct);
            return ServiceResult.Ok();
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            return ServiceResult.Fail($"The class {name} - {section} already exists for {year}.");
        }
    }
}
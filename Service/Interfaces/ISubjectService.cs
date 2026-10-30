using SmartAttendance.Web.Infrastructure;
using SmartAttendance.Web.ViewModels;

namespace SmartAttendance.Web.Services.Interfaces;

public interface ISubjectService
{
    Task<PagedResult<SubjectListItem>> ListAsync(string? search, string status, int page, int pageSize, CancellationToken ct = default);
    Task<SubjectDetailsViewModel?> GetDetailsAsync(int id, CancellationToken ct = default);
    Task<SubjectFormViewModel?> GetFormAsync(int id, CancellationToken ct = default);
    Task<ServiceResult> CreateAsync(SubjectFormViewModel model, CancellationToken ct = default);
    Task<ServiceResult> UpdateAsync(SubjectFormViewModel model, CancellationToken ct = default);
    Task<ServiceResult> SetActiveAsync(int id, bool isActive, CancellationToken ct = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken ct = default);
}
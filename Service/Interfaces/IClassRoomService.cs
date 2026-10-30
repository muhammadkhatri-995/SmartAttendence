using SmartAttendance.Web.Infrastructure;
using SmartAttendance.Web.ViewModels;

namespace SmartAttendance.Web.Services.Interfaces;

public interface IClassRoomService
{
    Task<PagedResult<ClassListItem>> ListAsync(string? search, string status, int page, int pageSize, CancellationToken ct = default);
    Task<ClassDetailsViewModel?> GetDetailsAsync(int id, CancellationToken ct = default);
    Task<ClassFormViewModel?> GetFormAsync(int id, CancellationToken ct = default);
    Task<ServiceResult> CreateAsync(ClassFormViewModel model, CancellationToken ct = default);
    Task<ServiceResult> UpdateAsync(ClassFormViewModel model, CancellationToken ct = default);
    Task<ServiceResult> SetActiveAsync(int id, bool isActive, CancellationToken ct = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken ct = default);
}
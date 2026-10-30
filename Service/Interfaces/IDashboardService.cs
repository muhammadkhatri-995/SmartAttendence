using SmartAttendance.Web.ViewModels;

namespace SmartAttendance.Web.Services.Interfaces;

public interface IDashboardService
{
    Task<AdminDashboardViewModel> GetAdminAsync(CancellationToken ct = default);
    Task<TeacherDashboardViewModel> GetTeacherAsync(string userId, CancellationToken ct = default);
}
using System.Security.Claims;
using SmartAttendance.Web.Models;

namespace SmartAttendance.Web.Infrastructure;

public static class UserPrincipalExtensions
{
    public const string FullNameClaim = "FullName";

    public static string GetUserId(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("The signed-in user has no user id claim.");

    public static string GetFullName(this ClaimsPrincipal user) =>
        user.FindFirstValue(FullNameClaim) ?? user.Identity?.Name ?? "User";

    public static string GetRoleLabel(this ClaimsPrincipal user) =>
        user.IsInRole(Roles.Admin) ? "Administrator"
        : user.IsInRole(Roles.Teacher) ? "Teacher"
        : user.IsInRole(Roles.AttendanceOperator) ? "Attendance Operator"
        : "User";
}
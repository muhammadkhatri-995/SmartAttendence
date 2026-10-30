using SmartAttendance.Models;

namespace SmartAttendance.Web.Models;

/// <summary>Authorizes an AttendanceOperator user to work on one teacher assignment.</summary>
public class OperatorAssignment
{
    public int OperatorAssignmentId { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int TeacherAssignmentId { get; set; }
    public TeacherAssignment TeacherAssignment { get; set; } = null!;

    public bool CanSendReports { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
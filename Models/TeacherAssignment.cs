using SmartAttendance.Models;

namespace SmartAttendance.Web.Models;

public class TeacherAssignment
{
    public int TeacherAssignmentId { get; set; }

    public int TeacherId { get; set; }
    public Teacher Teacher { get; set; } = null!;

    public int SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;

    public int ClassRoomId { get; set; }
    public ClassRoom ClassRoom { get; set; } = null!;

    public string AcademicYear { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<AttendanceSession> AttendanceSessions { get; set; } = new List<AttendanceSession>();
    public ICollection<OperatorAssignment> OperatorAssignments { get; set; } = new List<OperatorAssignment>();
}
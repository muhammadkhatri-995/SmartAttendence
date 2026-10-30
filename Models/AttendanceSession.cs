using SmartAttendance.Models;

namespace SmartAttendance.Web.Models;

public class AttendanceSession : IHasUpdatedAt
{
    public int AttendanceSessionId { get; set; }

    public int TeacherAssignmentId { get; set; }
    public TeacherAssignment TeacherAssignment { get; set; } = null!;

    /// <summary>Institution-local calendar date (no time, no time zone).</summary>
    public DateOnly AttendanceDate { get; set; }

    public AttendanceStatus Status { get; set; } = AttendanceStatus.Draft;

    public string CreatedByUserId { get; set; } = string.Empty;
    public ApplicationUser CreatedByUser { get; set; } = null!;

    public DateTime? SubmittedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Optimistic concurrency token (PostgreSQL xmin).</summary>
    public uint Version { get; set; }

    public ICollection<AttendanceRecord> Records { get; set; } = new List<AttendanceRecord>();
    public ICollection<AttendanceCorrection> Corrections { get; set; } = new List<AttendanceCorrection>();
    public ICollection<NotificationLog> Notifications { get; set; } = new List<NotificationLog>();
}
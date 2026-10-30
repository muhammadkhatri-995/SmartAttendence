namespace SmartAttendance.Web.Models;

public class AttendanceRecord : IHasUpdatedAt
{
    public int AttendanceRecordId { get; set; }

    public int AttendanceSessionId { get; set; }
    public AttendanceSession AttendanceSession { get; set; } = null!;

    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public bool IsPresent { get; set; }
    public DateTime MarkedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
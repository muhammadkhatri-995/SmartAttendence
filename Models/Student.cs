namespace SmartAttendance.Web.Models;

public class Student : IHasUpdatedAt
{
    public int StudentId { get; set; }
    public string RollNumber { get; set; } = string.Empty;   // always a string
    public string FullName { get; set; } = string.Empty;

    public int ClassRoomId { get; set; }
    public ClassRoom ClassRoom { get; set; } = null!;

    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
}
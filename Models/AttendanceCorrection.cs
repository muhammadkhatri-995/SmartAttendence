using SmartAttendance.Models;

namespace SmartAttendance.Web.Models;

/// <summary>Audit row written whenever a submitted attendance record is changed.</summary>
public class AttendanceCorrection
{
    public int AttendanceCorrectionId { get; set; }

    public int AttendanceSessionId { get; set; }
    public AttendanceSession AttendanceSession { get; set; } = null!;

    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public bool OldIsPresent { get; set; }
    public bool NewIsPresent { get; set; }
    public string Reason { get; set; } = string.Empty;

    public string CorrectedByUserId { get; set; } = string.Empty;
    public ApplicationUser CorrectedByUser { get; set; } = null!;

    public DateTime CorrectedAt { get; set; } = DateTime.UtcNow;
}
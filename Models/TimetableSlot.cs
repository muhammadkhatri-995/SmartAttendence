namespace SmartAttendance.Web.Models;

/// <summary>One weekly lecture: a day and start time for a teacher assignment (class + subject + teacher).</summary>
public class TimetableSlot
{
    public int TimetableSlotId { get; set; }

    public int TeacherAssignmentId { get; set; }
    public TeacherAssignment TeacherAssignment { get; set; } = null!;

    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public bool IsActive { get; set; } = true;
}
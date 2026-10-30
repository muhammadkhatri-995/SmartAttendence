namespace SmartAttendance.Web.Models;

public class ClassRoom : IHasUpdatedAt
{
    public int ClassRoomId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;       // required; use "A" if no sections
    public int Semester { get; set; }
    public string AcademicYear { get; set; } = string.Empty;  // e.g. "2025-2026"
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Student> Students { get; set; } = new List<Student>();
    public ICollection<TeacherAssignment> TeacherAssignments { get; set; } = new List<TeacherAssignment>();
}
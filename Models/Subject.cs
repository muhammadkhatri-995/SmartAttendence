using SmartAttendance.Web.Models;

namespace SmartAttendance.Web.Models
{
    public class Subject : IHasUpdatedAt
    {
        public int SubjectId { get; set; }
        public string SubjectCode { get; set; } = string.Empty;  // stored upper-case
        public string SubjectName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<TeacherAssignment> TeacherAssignments { get; set; } = new List<TeacherAssignment>();

    }
}

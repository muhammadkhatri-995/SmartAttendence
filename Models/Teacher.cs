using Microsoft.EntityFrameworkCore;
using SmartAttendance.Models;

namespace SmartAttendance.Web.Models
{
    [Index(nameof(Email), IsUnique = true)]
    [Index(nameof(PhoneNumber), IsUnique = true)]
    public class Teacher : IHasUpdatedAt
    {

        public int TeacherId { get; set; }

        public string? ApplicationUserId { get; set; }
        public ApplicationUser? ApplicationUser { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string? Email { get; set; } = string.Empty; 
        // stored lower-case
        public string PhoneNumber { get; set; } = string.Empty;  // E.164, e.g. +923001234567
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<TeacherAssignment> TeacherAssignments { get; set; } = new List<TeacherAssignment>();
    }
}

